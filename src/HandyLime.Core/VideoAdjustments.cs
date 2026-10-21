using System.Globalization;
using System.Text;
using ImageMagick;

namespace HandyLime.Core;

public static class VideoAdjustments
{
    public const int CubeSize = 65;
    public static PreviewPixels Preview(PreviewPixels frame, Dictionary<string, string> options,
        CancellationToken token, bool interactive = false)
    {
        ColorAdjustments.Validate(options);
        token.ThrowIfCancellationRequested();
        if (frame.Width < 1 || frame.Height < 1 || frame.Bgra.LongLength != (long)frame.Width * frame.Height * 4)
            throw new ArgumentException("Invalid video preview pixels.");
        using var image = new MagickImage();
        image.ReadPixels(frame.Bgra, new PixelReadSettings((uint)frame.Width, (uint)frame.Height, StorageType.Char, PixelMapping.BGRA));
        var limit = interactive ? 720u : 1100u;
        if (image.Width > limit || image.Height > limit) image.Thumbnail(limit, limit);
        ImageEngine.Apply(image, "adjust", options, token);
        token.ThrowIfCancellationRequested();
        using var pixels = image.GetPixels();
        return new((int)image.Width, (int)image.Height, pixels.ToByteArray(PixelMapping.BGRA)!);
    }
    // Sample the photo editor's complete transform. Red is the fastest-changing
    // .cube axis; spatial sharpening is applied separately after the video LUT.
    public static void WriteCube(string path, Dictionary<string, string> options, CancellationToken token)
    {
        ColorAdjustments.Validate(options);
        token.ThrowIfCancellationRequested();
        var size = CubeSize;
        var data = new byte[size * size * size * 4];
        for (var b = 0; b < size; b++)
        for (var g = 0; g < size; g++)
        for (var r = 0; r < size; r++) {
            var i = ((b * size + g) * size + r) * 4;
            data[i] = (byte)Math.Round(r * 255d / (size - 1));
            data[i + 1] = (byte)Math.Round(g * 255d / (size - 1));
            data[i + 2] = (byte)Math.Round(b * 255d / (size - 1));
            data[i + 3] = 255;
        }
        using var image = new MagickImage();
        image.ReadPixels(data, new PixelReadSettings((uint)size, (uint)(size * size), StorageType.Char, PixelMapping.RGBA));
        var colorOnly = new Dictionary<string, string>(options) { ["sharpen"] = "0" };
        ImageEngine.Apply(image, "adjust", colorOnly, token);
        using (var pixels = image.GetPixels()) data = pixels.ToByteArray(PixelMapping.RGB)!;
        using var writer = new StreamWriter(path, false, new UTF8Encoding(false), 65536);
        writer.WriteLine("TITLE \"HandyLime color adjustment\"");
        writer.WriteLine($"LUT_3D_SIZE {size}");
        writer.WriteLine("DOMAIN_MIN 0 0 0");
        writer.WriteLine("DOMAIN_MAX 1 1 1");
        for (var i = 0; i < data.Length; i += 3) {
            if (i % (size * 3) == 0) token.ThrowIfCancellationRequested();
            writer.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"{data[i] / 255d:0.######} {data[i + 1] / 255d:0.######} {data[i + 2] / 255d:0.######}"));
        }
    }
}
