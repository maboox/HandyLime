using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using Windows.Storage;
using Windows.Storage.Streams;

namespace HandyLime.Core;

public static class PdfEngine
{
    public static int[] ParsePages(string text, int count)
    {
        var pages = new List<int>();
        foreach (var part in text.Split(',',StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)) {
            var ends = part.Split('-');
            if (ends.Length > 2 || !int.TryParse(ends[0],out var start) || start < 1 || start > count) throw new ArgumentException("Invalid PDF page range.");
            var end = start;
            if (ends.Length == 2 && (!int.TryParse(ends[1],out end) || end < 1 || end > count)) throw new ArgumentException("Invalid PDF page range.");
            for (var p = start; ; p += start <= end ? 1 : -1) {
                if (pages.Count >= 10000) throw new ArgumentException("Too many output pages.");
                pages.Add(p-1); if (p == end) break;
            }
        }
        if (pages.Count == 0) throw new ArgumentException("Select at least one PDF page.");
        return pages.ToArray();
    }
    public static void Modify(string[] inputs, string id, Dictionary<string,string> options, string output, CancellationToken token)
    {
        using var result = new PdfDocument();
        if (id == "pdf-merge") {
            foreach (var input in inputs) {
                using var pdf = PdfReader.Open(input,PdfDocumentOpenMode.Import);
                foreach (var page in pdf.Pages) { token.ThrowIfCancellationRequested(); result.AddPage(page); }
            }
        } else {
            using var pdf = PdfReader.Open(inputs[0],PdfDocumentOpenMode.Import);
            if (id == "pdf-split") {
                Directory.CreateDirectory(output);
                for (int p=0;p<pdf.PageCount;p++) {
                    token.ThrowIfCancellationRequested(); using var one = new PdfDocument(); one.AddPage(pdf.Pages[p]); one.Save(Path.Combine(output,$"page-{p+1:0000}.pdf"));
                }
                return;
            }
            var rotation = Values.Integer(options,"rotation",0,0,270);
            if (rotation % 90 != 0) throw new ArgumentException("Rotation must be 0, 90, 180 or 270.");
            var ordered=ParsePages(Values.Text(options,"pages","1"),pdf.PageCount);
            var rotations=options.TryGetValue("rotations",out var text)?text.Split(',').Select(value=>Values.Integer(new(){["rotation"]=value},"rotation",0,0,270)).ToArray():Enumerable.Repeat(rotation,ordered.Length).ToArray();
            if(rotations.Length!=ordered.Length || rotations.Any(r=>r%90!=0))throw new ArgumentException("Each output page needs a rotation of 0, 90, 180 or 270 degrees.");
            for(var i=0;i<ordered.Length;i++) {
                token.ThrowIfCancellationRequested(); var page = result.AddPage(pdf.Pages[ordered[i]]); page.Rotate = (page.Rotate + rotations[i]) % 360;
            }
        }
        if (result.PageCount == 0) throw new InvalidDataException("PDF has no pages.");
        result.Save(output);
    }
    public static async Task Render(string input, string outputDirectory, string format, CancellationToken token)
    {
        Directory.CreateDirectory(outputDirectory);
        var file = await StorageFile.GetFileFromPathAsync(Path.GetFullPath(input));
        var pdf = await Windows.Data.Pdf.PdfDocument.LoadFromFileAsync(file);
        if (pdf.PageCount > 1000) throw new InvalidDataException("Rendering is limited to 1,000 PDF pages per job.");
        for (uint i=0;i<pdf.PageCount;i++) {
            token.ThrowIfCancellationRequested();
            using var page = pdf.GetPage(i);
            var width = (uint)Math.Ceiling(page.Size.Width * 300/96); var height = (uint)Math.Ceiling(page.Size.Height * 300/96);
            if ((long)width*height > 100_000_000) throw new InvalidDataException("PDF page exceeds 100 megapixels at 300 DPI.");
            using var memory = new InMemoryRandomAccessStream();
            await page.RenderToStreamAsync(memory,new Windows.Data.Pdf.PdfPageRenderOptions { DestinationWidth = width, DestinationHeight = height });
            memory.Seek(0);
            using var reader = new DataReader(memory.GetInputStreamAt(0));
            await reader.LoadAsync((uint)memory.Size); var bytes = new byte[(int)memory.Size]; reader.ReadBytes(bytes);
            var target = Path.Combine(outputDirectory,$"page-{i+1:0000}.{format}");
            if (format == "png") await File.WriteAllBytesAsync(target,bytes,token);
            else { using var image = new ImageMagick.MagickImage(bytes); image.BackgroundColor = ImageMagick.MagickColors.White; image.Alpha(ImageMagick.AlphaAction.Remove); image.Write(target,ImageMagick.MagickFormat.Jpeg); }
        }
    }
    public static string ExtractText(string input)
    {
        using var pdf = UglyToad.PdfPig.PdfDocument.Open(input);
        var text = string.Join("\n\n",pdf.GetPages().Select(p => ContentOrderTextExtractor.GetText(p)));
        if (string.IsNullOrWhiteSpace(text)) throw new InvalidDataException("No selectable text found. Scanned PDFs require OCR, which is not included.");
        return text;
    }
}
