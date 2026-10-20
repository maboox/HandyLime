using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace HandyLime.Core;

public sealed class AppSettings
{
    public string? OutputDirectory { get; set; }
    public bool DragWheelEnabled { get; set; } = true;
    public bool MinimizeToTray { get; set; } = true;
    public string Theme { get; set; } = "System";
    public Dictionary<string, string> EnginePaths { get; set; } = new();
    public static string SettingsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HandyLime", "settings.json");
    public static AppSettings Load()
    {
        try { return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath)) ?? new(); }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException) { return new(); }
    }
    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        var tmp = SettingsPath + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(tmp, SettingsPath, true);
    }
}

public sealed class Engines(AppSettings settings)
{
    public string? Find(string engine)
    {
        if (settings.EnginePaths.TryGetValue(engine, out var configured) && File.Exists(configured)) return Path.GetFullPath(configured);
        var exe = engine switch { "FFmpeg" => "ffmpeg.exe", "FFprobe" => "ffprobe.exe", "LibreOffice" => "soffice.exe", "Pandoc" => "pandoc.exe", _ => "" };
        if (exe.Length == 0) return null;
        if (engine == "FFprobe" && Find("FFmpeg") is { } ff) {
            var probe = Path.Combine(Path.GetDirectoryName(ff)!, exe); if (File.Exists(probe)) return probe;
        }
        string[] candidates = [Path.Combine(AppContext.BaseDirectory, "tools", engine.ToLowerInvariant(), exe),
            Path.Combine(AppContext.BaseDirectory, "tools", engine.ToLowerInvariant(), "bin", exe),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WinGet", "Links", exe),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LibreOffice", "program", exe),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Pandoc", exe),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Pandoc", exe)];
        var paths = string.Join(Path.PathSeparator, Environment.GetEnvironmentVariable("PATH"), Environment.GetEnvironmentVariable("PATH",EnvironmentVariableTarget.User), Environment.GetEnvironmentVariable("PATH",EnvironmentVariableTarget.Machine));
        var found = candidates.Concat(paths.Split(Path.PathSeparator).Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => Path.Combine(p.Trim('"'), exe))).FirstOrDefault(File.Exists);
        if (found != null) return found;
        // Winget portable packages are not always visible in an already-running process's PATH.
        if (engine is "FFmpeg" or "FFprobe") {
            var packages = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WinGet", "Packages");
            try { if (Directory.Exists(packages)) return Directory.EnumerateDirectories(packages,"Gyan.FFmpeg*").SelectMany(p=>Directory.EnumerateFiles(p,exe,SearchOption.AllDirectories)).FirstOrDefault(); }
            catch (IOException) {} catch (UnauthorizedAccessException) {}
        }
        return null;
    }
    public string[] Missing(Operation operation) => (operation.Engine switch { "Built in" => Array.Empty<string>(), "FFmpeg" => ["FFmpeg","FFprobe"], _ => [operation.Engine] }).Where(e=>Find(e)==null).ToArray();
    public string Require(string engine) => Find(engine) ?? throw new InvalidOperationException($"{engine} is not installed. Open Settings → Conversion engines and choose its executable. See docs/ENGINES.md.");
}

public static class ProcessRunner
{
    public static async Task<string> Run(string executable, IEnumerable<string> arguments, CancellationToken token, IProgress<string>? progress = null, string? workingDirectory = null)
    {
        var start = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true };
        if (workingDirectory != null) start.WorkingDirectory = workingDirectory;
        foreach (var arg in arguments) start.ArgumentList.Add(arg);
        using var process = new Process { StartInfo = start };
        token.ThrowIfCancellationRequested();
        process.Start();
        var error = new StringBuilder(); var output = new StringBuilder();
        async Task Drain(StreamReader reader, StringBuilder buffer, bool report)
        {
            while (await reader.ReadLineAsync() is { } line) {
                if (buffer.Length > 32_000) buffer.Remove(0, 16_000);
                buffer.AppendLine(line);
                if (report && line.StartsWith("out_time=")) progress?.Report("Processed " + line[9..]);
            }
        }
        var drain = Task.WhenAll(Drain(process.StandardOutput, output, true), Drain(process.StandardError, error, false));
        using var registration = token.Register(() => { try { if (!process.HasExited) process.Kill(true); } catch (InvalidOperationException) {} catch (System.ComponentModel.Win32Exception) {} });
        await process.WaitForExitAsync(CancellationToken.None);
        await drain;
        token.ThrowIfCancellationRequested();
        if (process.ExitCode != 0) {
            var details=error.Length>0?error.ToString():output.ToString();
            throw new InvalidOperationException($"{Path.GetFileName(executable)} failed ({process.ExitCode}). {details[Math.Max(0,details.Length-2500)..]}");
        }
        return output.ToString();
    }
}

public sealed class OutputTransaction : IDisposable
{
    public string Work { get; }
    readonly string destination;
    readonly string stem;
    public OutputTransaction(string input, string? directory)
    {
        destination = Path.GetFullPath(directory ?? Path.GetDirectoryName(Path.GetFullPath(input))!);
        Directory.CreateDirectory(destination);
        stem = Path.GetFileNameWithoutExtension(input.TrimEnd(Path.DirectorySeparatorChar));
        Work = Path.Combine(destination, ".handylime-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Work);
    }
    public string FilePath(string extension) => Path.Combine(Work, "result." + extension);
    public string Commit(string source, string label)
    {
        if (File.Exists(source) && new FileInfo(source).Length == 0) throw new IOException("The converter produced an empty file.");
        if (!File.Exists(source) && !Directory.Exists(source)) throw new IOException("The converter did not produce an output.");
        var isDirectory = Directory.Exists(source);
        var ext = isDirectory ? "" : Path.GetExtension(source);
        for (var i = 0; i < 10000; i++) {
            var target = Path.Combine(destination, $"{stem}-{label}{(i == 0 ? "" : $" ({i})")}{ext}");
            try {
                if (isDirectory) Directory.Move(source, target); else File.Move(source, target, false);
                return target;
            } catch (IOException) when (File.Exists(target) || Directory.Exists(target)) { }
        }
        throw new IOException("Could not allocate a unique output name.");
    }
    public void Dispose() { try { Directory.Delete(Work, true); } catch (IOException) {} catch (UnauthorizedAccessException) {} }
}

public static class Values
{
    public static string Text(Dictionary<string, string> values, string key, string fallback = "") => values.GetValueOrDefault(key, fallback).Trim();
    public static double Number(Dictionary<string, string> values, string key, double fallback, double min, double max)
    {
        var text = Text(values, key, fallback.ToString(CultureInfo.InvariantCulture));
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || !double.IsFinite(value) || value < min || value > max)
            throw new ArgumentException($"{key} must be between {min} and {max} (use a dot for decimals).");
        return value;
    }
    public static int Integer(Dictionary<string, string> values, string key, int fallback, int min, int max)
    {
        var value = Number(values, key, fallback, min, max);
        if (value != Math.Truncate(value)) throw new ArgumentException($"{key} must be a whole number.");
        return (int)value;
    }
    public static string Invariant(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);
}
