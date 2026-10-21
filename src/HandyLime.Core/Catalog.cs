namespace HandyLime.Core;

public enum FileKind { Image, Video, Audio, Pdf, Document, Slides, Sheet, Subtitle, Archive, Folder, Other }
public record Field(string Key, string Label, string Default, string Hint);
public record Operation(string Id, string Title, string Description, string Engine = "Built in", params Field[] Fields)
{
    public override string ToString() => Title;
    public bool IsBatch => Id is "pdf-merge" or "collage" or "zip" or "tar" or "join-video";
}
public sealed record JobRequest(string[] Inputs, Operation Operation, Dictionary<string, string> Options, string? OutputDirectory = null);
public sealed record JobResult(string[] Outputs, string Message);

public static class Catalog
{
    static readonly string[] Images = ["jpg", "jpeg", "png", "webp", "heic", "heif", "tif", "tiff", "avif", "bmp", "gif", "ico", "svg"];
    static readonly string[] Audio = ["mp3", "m4a", "wav", "flac", "ogg", "opus", "aiff", "aif", "wma", "aac"];
    static readonly string[] Video = ["mp4", "mov", "mkv", "webm", "avi", "wmv", "m4v"];
    public static string Extension(string path) => Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
    public static FileKind Kind(string path)
    {
        if (Directory.Exists(path)) return FileKind.Folder;
        var ext = Extension(path);
        if (Images.Contains(ext)) return FileKind.Image;
        if (Audio.Contains(ext)) return FileKind.Audio;
        if (Video.Contains(ext)) return FileKind.Video;
        return ext switch {
            "pdf" => FileKind.Pdf,
            "docx" or "doc" or "odt" or "rtf" or "md" or "txt" or "epub" or "html" => FileKind.Document,
            "ppt" or "pptx" or "odp" => FileKind.Slides,
            "xlsx" or "xls" or "ods" or "csv" or "tsv" => FileKind.Sheet,
            "srt" or "vtt" => FileKind.Subtitle,
            "zip" or "tar" or "gz" or "tgz" => FileKind.Archive,
            _ => FileKind.Other
        };
    }
    static Field F(string key, string label, string value, string hint = "") => new(key, label, value, hint);
    public static readonly Field Quality = F("quality", "Image quality", "85", "1–100; JPEG, WebP, HEIC and AVIF");
    static readonly Field Width = F("width", "Width (pixels)", "1280", "1–16384");
    static readonly Field Height = F("height", "Height (pixels)", "720", "1–16384");
    static readonly Field X = F("x", "Left (pixels)", "0");
    static readonly Field Y = F("y", "Top (pixels)", "0");
    static readonly Field Start = F("start", "Start (seconds)", "0", "Decimals accepted");
    static readonly Field End = F("end", "End (seconds)", "10", "Must be after start and within the file");
    static readonly Field Color = F("color", "Background color", "#FFFFFF", "Hex color, for example #F2EDE4");
    public static List<Operation> Conversions(string path)
    {
        var kind = Kind(path); var ext = Extension(path);
        string[] outputs = kind switch {
            FileKind.Image => ["png", "jpg", "webp", "avif", "tiff", "bmp", "gif", "ico", "pdf"],
            FileKind.Audio => ["mp3", "m4a", "wav", "flac", "ogg", "opus", "aiff", "wma"],
            FileKind.Video => ["mp4", "mov", "mkv", "webm", "avi", "wmv", "gif", "mp3", "wav"],
            FileKind.Pdf => ["png", "jpg", "txt", "md"],
            FileKind.Document => ext is "md" or "epub" or "html" ? ["docx", "html", "md", "txt", "epub"] : ["pdf", "docx", "odt", "rtf", "txt", "html"],
            FileKind.Slides => ["pdf", "pptx", "odp", "png", "jpg"],
            FileKind.Sheet => ["xlsx", "ods", "csv", "tsv", "pdf"],
            FileKind.Subtitle => ["srt", "vtt", "txt"],
            _ => []
        };
        return outputs.Where(o => o != ext).Select(o => new Operation("convert:" + o, o.ToUpperInvariant(),
            kind == FileKind.Pdf && o is "txt" or "md" ? "Extract selectable text; no OCR or layout reconstruction." : "Save a converted copy. Original stays untouched.",
            kind is FileKind.Audio or FileKind.Video ? "FFmpeg" : kind is FileKind.Document && ext is "md" or "epub" or "html" ? "Pandoc" : kind is FileKind.Document or FileKind.Sheet or FileKind.Slides ? "LibreOffice" : "Built in",
            kind == FileKind.Image && o != "pdf" ? [Quality] : [])).ToList();
    }
    public static List<Operation> Tools(string path)
    {
        var list = new List<Operation>();
        switch (Kind(path))
        {
            case FileKind.Image:
                list.AddRange([
                    new("compress-image", "Compress image", "Write an optimized WebP copy. Size reduction depends on the source.", "Built in", Quality),
                    new("resize", "Resize image", "Fit inside a bounding box, preserving aspect ratio.", "Built in", Width, Height),
                    new("crop-image", "Crop image", "Keep a precise rectangle; coordinates use the oriented image.", "Built in", X, Y, Width, Height),
                    new("rotate", "Rotate image", "Rotate clockwise and save as PNG.", "Built in", F("degrees", "Degrees clockwise", "90", "−360 to 360")),
                    new("flip", "Flip horizontally", "Mirror the image left to right."),
                    new("adjust", "Adjust photo", "Adjust light, color and detail with a live preview.", "Built in", ColorAdjustments.Fields),
                    new("annotate", "Annotate photo", "Draw arrows, lines, shapes, text or pen strokes. Drag annotations to move them.", "Built in", F("text", "Text", "Your label"), X, Y, F("fontSize", "Font size", "36", "8–400"), F("color", "Text color", "#FFFFFF")),
                    new("background", "Add background", "Center the image on a solid canvas; fits without stretching.", "Built in", Width, Height, Color),
                    new("redact-image", "Cover image area", "Permanently cover one rectangular area in a new flattened PNG.", "Built in", X, Y, Width, Height),
                    new("collage", "Create collage", "Arrange all selected images into a grid in listed order.", "Built in", F("columns", "Columns", "2", "1–10"), F("cell", "Cell size (pixels)", "640", "64–2048"), F("gap", "Spacing (pixels)", "16", "0–200"), Color),
                    new("strip-image", "Image metadata", "Inspect image properties and save a clean PNG copy without removable profiles and tags.")]);
                break;
            case FileKind.Video:
                list.AddRange([
                    new("adjust-video", "Adjust video", "Adjust light, color and detail across the video. Preview a frame or play a short edited sample; save a new MP4 with audio.", "FFmpeg", ColorAdjustments.Fields),
                    new("compress-video", "Compress video", "Encode an MP4 copy. Higher CRF means a smaller, less detailed image.", "FFmpeg", F("crf", "CRF (18–40)", "28")),
                    new("trim-video", "Trim video", "Re-encode a precise time interval as MP4.", "FFmpeg", Start, End),
                    new("crop-video", "Crop video", "Crop a rectangle, preserving audio when present.", "FFmpeg", X, Y, Width, Height),
                    new("speed", "Change video speed", "Change video and audio speed; preserve audio pitch.", "FFmpeg", F("speed", "Speed multiplier", "2", "0.25–4")),
                    new("frame", "Save video frame", "Export one frame as a full-size PNG.", "FFmpeg", Start),
                    new("split-video", "Split video", "Add and move split markers on the timeline, or divide into equal clips.", "FFmpeg", F("seconds", "Seconds per clip", "30", "1–86400")),
                    new("join-video", "Join videos", "Normalize clips to 1280×720 / 30 fps with stereo audio, then join in list order.", "FFmpeg"),
                    new("redact-video", "Cover video area", "Burn a solid rectangle into every frame during the chosen interval.", "FFmpeg", X, Y, Width, Height, Start, End),
                    new("strip-media", "Media metadata", "Inspect media properties and tags; save a clean stream-copy MKV.", "FFmpeg")]);
                break;
            case FileKind.Audio:
                list.AddRange([
                    new("trim-audio", "Trim audio", "Keep a precise interval as WAV.", "FFmpeg", Start, End),
                    new("normalize", "Normalize loudness", "Single-pass EBU R128 normalization to −16 LUFS / −1.5 dBTP.", "FFmpeg"),
                    new("channels", "Convert channels", "Save mono or stereo WAV.", "FFmpeg", F("channels", "Channels", "1", "1 = mono, 2 = stereo")),
                    new("bleep", "Bleep audio", "Replace one interval with a 1 kHz tone in a new WAV.", "FFmpeg", Start, End),
                    new("visualizer", "Audio visualizer", "Create a 1280×720 MP4 with an animated waveform.", "FFmpeg"),
                    new("strip-audio", "Audio metadata", "Inspect audio properties and tags; save a clean FLAC copy.", "FFmpeg")]);
                break;
            case FileKind.Pdf:
                list.AddRange([
                    new("pdf-merge", "Merge PDFs", "Combine selected PDFs in list order. Bookmarks/forms may not survive."),
                    new("pdf-pages", "Arrange PDF pages", "Select, reorder or duplicate pages; omitted pages are removed.", "Built in", F("pages", "Page order", "1", "Example: 3,1,2-4,2 (one-based)"), F("rotation", "Clockwise rotation", "0", "0, 90, 180 or 270")),
                    new("pdf-split", "Split PDF", "Save every page as a separate PDF in a new folder.")]);
                break;
            case FileKind.Archive:
                list.Add(new("extract", "Extract archive", "Extract ZIP, TAR, TAR.GZ or GZIP into a new folder. Links and unsafe paths are rejected."));
                break;
        }
        list.Add(new("zip", "Create ZIP", "Package selected files and folders. Symlinks are rejected."));
        list.Add(new("tar", "Create TAR", "Package selected files and folders without compression."));
        if (File.Exists(path)) list.Add(new("hash", "SHA-256 checksum", "Write a checksum file for integrity verification."));
        return list;
    }
    public static List<Operation> ForSelection(string[] paths, bool tools)
    {
        if (paths.Length == 0) return [];
        var first = tools ? Tools(paths[0]) : Conversions(paths[0]);
        return first.Where(o => paths.All(p => (tools ? Tools(p) : Conversions(p)).Any(x => x.Id == o.Id && x.Engine == o.Engine))).ToList();
    }
}
