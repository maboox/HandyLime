using System.Formats.Tar;
using System.IO.Compression;

namespace HandyLime.Core;

public static class ArchiveEngine
{
    public const long MaxExtractedBytes = 4L * 1024 * 1024 * 1024;
    public const int MaxEntries = 10000;
    public static string SafePath(string root, string name)
    {
        name = name.Replace('\\','/');
        if (name.StartsWith('/') || name.Contains(':') || name.Split('/').Any(p => p is ".." || p.EndsWith('.') || p.EndsWith(' ')))
            throw new InvalidDataException("Unsafe archive entry: " + name);
        var reserved = new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };
        if (name.Split('/').Any(p => reserved.Contains(p.Split('.')[0], StringComparer.OrdinalIgnoreCase))) throw new InvalidDataException("Reserved device name in archive.");
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var target = Path.GetFullPath(Path.Combine(fullRoot,name));
        if (!target.StartsWith(fullRoot,StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Archive entry escapes destination.");
        return target;
    }
    public static async Task Extract(string input, string output, CancellationToken token)
    {
        Directory.CreateDirectory(output); long total = 0; int count = 0;
        async Task Write(string name, Stream source, long length)
        {
            if (++count > MaxEntries || length > MaxExtractedBytes - total) throw new InvalidDataException("Archive exceeds extraction limits (10,000 entries / 4 GiB).");
            var path = SafePath(output,name); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await using var dest = new FileStream(path,FileMode.CreateNew,FileAccess.Write);
            var buffer = new byte[81920]; int read;
            while ((read = await source.ReadAsync(buffer,token)) > 0) {
                total += read; if (total > MaxExtractedBytes) throw new InvalidDataException("Archive exceeds 4 GiB extraction limit.");
                await dest.WriteAsync(buffer.AsMemory(0,read),token);
            }
        }
        if (Catalog.Extension(input) == "zip") {
            using var zip = ZipFile.OpenRead(input);
            if (zip.Entries.Count > MaxEntries) throw new InvalidDataException("Too many archive entries.");
            foreach (var entry in zip.Entries) {
                token.ThrowIfCancellationRequested(); var path = SafePath(output,entry.FullName);
                if (((entry.ExternalAttributes >> 16) & 0xF000) == 0xA000) throw new InvalidDataException("Archive symlinks are not supported.");
                if (entry.FullName.EndsWith('/')) { Directory.CreateDirectory(path); continue; }
                await using var source = entry.Open(); await Write(entry.FullName,source,entry.Length);
            }
        } else {
            await using var file = File.OpenRead(input);
            var gzip = Catalog.Extension(input) is "gz" or "tgz";
            using Stream source = gzip ? new GZipStream(file,CompressionMode.Decompress,true) : file;
            var tar = !gzip || input.EndsWith(".tar.gz",StringComparison.OrdinalIgnoreCase) || input.EndsWith(".tgz",StringComparison.OrdinalIgnoreCase);
            if (!tar) { await Write(Path.GetFileNameWithoutExtension(input),source,0); return; }
            using var reader = new TarReader(source,leaveOpen:true); TarEntry? entry;
            while ((entry = reader.GetNextEntry()) != null) {
                token.ThrowIfCancellationRequested();
                if (entry.EntryType == TarEntryType.Directory) { if (++count > MaxEntries) throw new InvalidDataException("Too many entries."); Directory.CreateDirectory(SafePath(output,entry.Name)); }
                else if (entry.EntryType is TarEntryType.RegularFile or TarEntryType.V7RegularFile) { if (entry.DataStream != null) await Write(entry.Name,entry.DataStream,entry.Length); }
                else throw new InvalidDataException("Archive links and special entries are not supported.");
            }
        }
    }
    public static void Pack(string[] inputs, string output, bool tar, CancellationToken token)
    {
        var entries = new List<(string path,string name)>();
        void Add(string path,string name)
        {
            token.ThrowIfCancellationRequested();
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new IOException("Links and junctions cannot be archived.");
            if (entries.Count >= MaxEntries) throw new IOException("Archive creation supports at most 10,000 entries.");
            entries.Add((path,name));
            if (Directory.Exists(path)) foreach (var child in Directory.EnumerateFileSystemEntries(path)) Add(child,name+"/"+Path.GetFileName(child));
        }
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var input in inputs) {
            var name = Path.GetFileName(input.TrimEnd('\\','/'));
            if (!names.Add(name)) throw new ArgumentException("Selected items have duplicate names. Rename them or archive separately.");
            // The work folder is outside selected folders in the executor.
            Add(input,name);
        }
        if (tar) {
            using var file = File.Create(output); using var writer = new TarWriter(file,leaveOpen:true);
            foreach (var entry in entries) { token.ThrowIfCancellationRequested(); writer.WriteEntry(entry.path,entry.name); }
        } else {
            using var zip = ZipFile.Open(output,ZipArchiveMode.Create);
            foreach (var entry in entries) {
                token.ThrowIfCancellationRequested();
                if (Directory.Exists(entry.path)) zip.CreateEntry(entry.name+"/");
                else zip.CreateEntryFromFile(entry.path,entry.name,CompressionLevel.Optimal);
            }
        }
    }
}
