using System.Security.Cryptography;

namespace HandyLime.Core;

public sealed class JobExecutor(Engines engines)
{
    public async Task<JobResult> Execute(JobRequest request, CancellationToken token, IProgress<JobProgress>? progress = null)
    {
        if (request.Inputs.Length == 0) throw new ArgumentException("Add at least one file.");
        var inputs = request.Inputs.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        foreach (var path in inputs) {
            if (!File.Exists(path) && !Directory.Exists(path)) throw new FileNotFoundException("Input no longer exists.",path);
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new IOException("Links and junctions are not supported as inputs.");
        }
        var allowed = Catalog.ForSelection(inputs,!request.Operation.Id.StartsWith("convert:"));
        if (!allowed.Any(o => o.Id == request.Operation.Id)) throw new ArgumentException("This operation is not available for the selected files.");
        var outputs = new List<string>();
        var batches = request.Operation.IsBatch ? new[] { inputs } : inputs.Select(p => new[] {p}).ToArray();
        foreach (var batch in batches) {
            token.ThrowIfCancellationRequested(); progress?.Report(new($"Preparing {Path.GetFileName(batch[0])}"));
            var id = request.Operation.Id; var kind = Catalog.Kind(batch[0]);
            if (id is "zip" or "tar") {
                var target = Path.GetFullPath(request.OutputDirectory ?? Path.GetDirectoryName(batch[0])!);
                if (batch.Where(Directory.Exists).Any(d => target.Equals(d,StringComparison.OrdinalIgnoreCase) || target.StartsWith(d.TrimEnd('\\')+"\\",StringComparison.OrdinalIgnoreCase)))
                    throw new ArgumentException("Archive output cannot be inside a selected folder. Choose its parent or another output folder.");
            }
            using var tx = new OutputTransaction(batch[0],request.OutputDirectory);
            string ext = id.StartsWith("convert:") ? id[8..] : id switch {
                "compress-image" => "webp", "pdf-merge" or "pdf-pages" => "pdf", "zip" => "zip", "tar" => "tar", "hash" => "sha256",
                "frame" => "png", "strip-media" => "mkv", "strip-audio" => "flac",
                "trim-audio" or "normalize" or "channels" or "bleep" => "wav",
                _ => kind == FileKind.Video || id == "visualizer" ? "mp4" : "png"
            };
            var folderOutput = id is "extract" or "pdf-split" or "split-video" || kind is FileKind.Pdf or FileKind.Slides && ext is "png" or "jpg" || kind == FileKind.Document && ext == "html" && request.Operation.Engine == "LibreOffice";
            var output = folderOutput ? Path.Combine(tx.Work,"result") : tx.FilePath(ext);
            if (id is "zip" or "tar") ArchiveEngine.Pack(batch,output,id == "tar",token);
            else if (id == "extract") await ArchiveEngine.Extract(batch[0],output,token);
            else if (id == "hash") {
                await using var stream = File.OpenRead(batch[0]); var hash = await SHA256.HashDataAsync(stream,token);
                await File.WriteAllTextAsync(output,$"{Convert.ToHexString(hash).ToLowerInvariant()}  {Path.GetFileName(batch[0])}{Environment.NewLine}",token);
            }
            else if (kind == FileKind.Image) ImageEngine.Process(batch,id,request.Options,output,token);
            else if (kind is FileKind.Audio or FileKind.Video) await new MediaEngine(engines).Process(batch,id,request.Options,output,token,progress);
            else if (id.StartsWith("pdf-")) PdfEngine.Modify(batch,id,request.Options,output,token);
            else if (kind == FileKind.Pdf) {
                if (ext is "png" or "jpg") await PdfEngine.Render(batch[0],output,ext,token);
                else await File.WriteAllTextAsync(output,PdfEngine.ExtractText(batch[0]),token);
            }
            else if (kind == FileKind.Subtitle) await File.WriteAllTextAsync(output,SubtitleEngine.Convert(await File.ReadAllTextAsync(batch[0],token),ext),token);
            else if (kind == FileKind.Slides && ext is "png" or "jpg") {
                var pdf = tx.FilePath("pdf"); await DocumentEngine.Convert(batch[0],"pdf",pdf,"LibreOffice",engines,token);
                await PdfEngine.Render(pdf,output,ext,token);
            }
            else await DocumentEngine.Convert(batch[0],ext,output,request.Operation.Engine,engines,token);
            token.ThrowIfCancellationRequested();
            progress?.Report(new("Saving the new copy…",0.99));
            outputs.Add(tx.Commit(output,id.StartsWith("convert:") ? ext : id));
        }
        progress?.Report(new("Saved successfully",1));
        return new(outputs.ToArray(),$"Saved {outputs.Count} output{(outputs.Count == 1 ? "" : "s")}. Originals preserved.");
    }
}
