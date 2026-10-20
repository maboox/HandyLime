using ImageMagick;
using ImageMagick.Drawing;
using ImageMagick.Configuration;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Xml;

namespace HandyLime.Core;

public static class ImageEngine
{
    static readonly Lazy<bool> Initialized = new(() => {
        var configuration = ConfigurationFiles.Default;
        configuration.Policy.Data = """
            <policymap>
              <policy domain="resource" name="memory" value="512MiB"/>
              <policy domain="resource" name="map" value="1GiB"/>
              <policy domain="resource" name="disk" value="2GiB"/>
              <policy domain="resource" name="width" value="32KP"/>
              <policy domain="resource" name="height" value="32KP"/>
              <policy domain="resource" name="list-length" value="200"/>
              <policy domain="resource" name="time" value="120"/>
              <policy domain="delegate" rights="none" pattern="*"/>
              <policy domain="filter" rights="none" pattern="*"/>
              <policy domain="path" rights="none" pattern="@*"/>
              <policy domain="coder" rights="none" pattern="*"/>
              <policy domain="coder" rights="read|write" pattern="{PNG,JPEG,JPG,WEBP,HEIC,AVIF,TIFF,BMP,GIF,ICO,SVG,MSVG,XC}"/>
              <policy domain="coder" rights="read" pattern="MVG"/>
            </policymap>
            """;
        MagickNET.Initialize(configuration);
        return true;
    });
    public static void EnsureInitialized()=>_ = Initialized.Value;
    public static MagickImage Read(string path,bool autoOrient=true)
    {
        _ = Initialized.Value;
        if (new FileInfo(path).Length > 512L * 1024 * 1024) throw new IOException("Image input exceeds the 512 MB safety limit.");
        if (Catalog.Extension(path) == "svg") ValidateSvg(path);
        using var stream = File.OpenRead(path);
        // Select the decoder explicitly: only validated SVG can enter the internal
        // SVG/MVG drawing route. Renaming an arbitrary drawing script to PNG is rejected.
        var settings = new MagickReadSettings { FrameIndex = 0, FrameCount = 1, Format=Format(Catalog.Extension(path)) };
        var image = new MagickImage(stream, settings);
        if(autoOrient)image.AutoOrient();
        return image;
    }
    static void ValidateSvg(string path)
    {
        using var reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 10_000_000 });
        while (reader.Read()) {
            if (reader.NodeType == XmlNodeType.Element && reader.LocalName is "script" or "foreignObject" or "style")
                throw new InvalidDataException("SVG scripts, embedded HTML and stylesheets are not supported.");
            if (!reader.HasAttributes) continue;
            while (reader.MoveToNextAttribute()) {
                if ((reader.LocalName == "href" && !reader.Value.StartsWith('#')) || reader.Value.Contains("url(", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("SVG external resources and URL references are not supported.");
            }
            reader.MoveToElement();
        }
    }
    public static void Process(string[] inputs, string id, Dictionary<string,string> options, string output, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (id == "collage") { Collage(inputs, options, output, token); return; }
        using var image = Read(inputs[0]);
        Apply(image,id,options,token);
        Write(image,options,output,token);
    }
    public static void Apply(MagickImage image,string id,Dictionary<string,string> options,CancellationToken token)
    {
        int Int(string key, int fallback, int min = 0, int max = 16384) => Values.Integer(options, key, fallback, min, max);
        double Num(string key, double fallback, double min, double max) => Values.Number(options, key, fallback, min, max);
        switch (id)
        {
            case "resize": image.Resize(new MagickGeometry((uint)Int("width",1280,1), (uint)Int("height",720,1))); break;
            case "crop-image":
                var x = Int("x",0); var y = Int("y",0); var w = Int("width",1280,1); var h = Int("height",720,1);
                if (x + w > image.Width || y + h > image.Height) throw new ArgumentException($"Crop is outside the {image.Width} × {image.Height} image.");
                image.Crop(new MagickGeometry(x,y,(uint)w,(uint)h)); image.ResetPage(); break;
            case "rotate": image.BackgroundColor = MagickColors.Transparent; image.Rotate(Num("degrees",90,-360,360)); break;
            case "flip": image.Flop(); break;
            case "adjust":
                Adjust(image,options,token); break;
            case "annotate":
                if (options.TryGetValue("marks",out var json)) { Annotate(image,Editing.ReadMarks(json,image.Width,image.Height)); break; }
                var text = Values.Text(options,"text"); if (text.Length is 0 or > 2000) throw new ArgumentException("Text must contain 1–2000 characters.");
                new Drawables().Font("Segoe UI").FontPointSize(Int("fontSize",36,8,400)).FillColor(ParseColor(options)).StrokeColor(MagickColors.None)
                    .Text(Int("x",0), Int("y",0) + Int("fontSize",36,8,400), text).Draw(image); break;
            case "background":
                var cw = (uint)Int("width",1280,1); var ch = (uint)Int("height",720,1);
                image.Resize(new MagickGeometry(cw,ch)); image.BackgroundColor = ParseColor(options);
                image.Extent(cw,ch,Gravity.Center); image.Alpha(AlphaAction.Remove); break;
            case "redact-image":
                var rx = Int("x",0); var ry = Int("y",0); var rw = Int("width",1280,1); var rh = Int("height",720,1);
                if (rx + rw > image.Width || ry + rh > image.Height) throw new ArgumentException("Cover rectangle is outside the image.");
                new Drawables().FillColor(MagickColors.Black).StrokeColor(MagickColors.Black).Rectangle(rx,ry,rx+rw-1,ry+rh-1).Draw(image);
                image.Strip(); break;
            case "strip-image": image.Strip(); break;
            case "compress-image": image.Strip(); break;
        }
        token.ThrowIfCancellationRequested();
    }
    static void Write(MagickImage image,Dictionary<string,string> options,string output,CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var ext=Path.GetExtension(output).TrimStart('.');
        if (ext == "pdf") { ToPdf(image, output); return; }
        if (ext == "jpg") { image.BackgroundColor = MagickColors.White; image.Alpha(AlphaAction.Remove); }
        if (ext == "ico") image.Resize(256,256);
        image.Quality = (uint)Values.Integer(options,"quality",85,1,100);
        image.Format = Format(ext);
        using var stream = File.Create(output);
        image.Write(stream);
    }
    static void Adjust(MagickImage image,Dictionary<string,string> options,CancellationToken token)
    {
        double N(string key,double fallback=0,double min=-100,double max=100)=>Values.Number(options,key,fallback,min,max);
        var exposure=Math.Pow(2,N("exposure",0,-3,3)); var brightness=N("brightness",100,0,200)/100;
        var contrast=Math.Pow(2,N("contrast")/100); var gamma=N("gamma",1,0.2,3);
        var highlights=N("highlights")/100;var shadows=N("shadows")/100;var whites=N("whites")/100;var blacks=N("blacks")/100;
        var temperature=N("temperature")/500;var tint=N("tint")/500;var vibrance=N("vibrance")/100;
        image.ColorSpace=ColorSpace.sRGB;
        byte[] pixels;
        using(var collection=image.GetPixels()) pixels=collection.ToByteArray(PixelMapping.RGBA)!;
        Parallel.For(0,(int)image.Height,new ParallelOptions{CancellationToken=token,MaxDegreeOfParallelism=Math.Min(8,Environment.ProcessorCount)},row=>{
          var end=(row+1)*(int)image.Width*4;
          for(int i=row*(int)image.Width*4;i<end;i+=4){
            double r=pixels[i]/255d,g=pixels[i+1]/255d,b=pixels[i+2]/255d;
            var l=0.2126*r+0.7152*g+0.0722*b;
            var shadowWeight=(1-l)*(1-l); var highlightWeight=l*l;
            var tonal=shadows*shadowWeight*0.35+highlights*highlightWeight*0.35+whites*highlightWeight*highlightWeight*0.3+blacks*shadowWeight*shadowWeight*0.3;
            var saturation=Math.Max(r,Math.Max(g,b))-Math.Min(r,Math.Min(g,b));
            double Transform(double value,double warmth) {
                value=(l+(value-l)*(1+vibrance*(1-saturation)))*exposure*brightness;
                value=(value-0.5)*contrast+0.5+tonal+warmth;
                value=Math.Clamp(value,0,1);return gamma==1?value:Math.Pow(value,1/gamma);
            }
            pixels[i]=(byte)Math.Round(255*Transform(r,temperature+tint/2));
            pixels[i+1]=(byte)Math.Round(255*Transform(g,-tint));
            pixels[i+2]=(byte)Math.Round(255*Transform(b,-temperature+tint/2));
          }
        });
        image.ReadPixels(pixels,new PixelReadSettings(image.Width,image.Height,StorageType.Char,PixelMapping.RGBA));
        image.Modulate(new Percentage(100),new Percentage(N("saturation",100,0,200)),new Percentage(100+N("hue",0,-180,180)/1.8));
        var sharpen=N("sharpen",0,0,5);if(sharpen>0)image.Sharpen(0,sharpen);
    }
    static void Annotate(MagickImage image,IEnumerable<Mark> marks)
    {
        foreach(var mark in marks) {
            var color=new MagickColor(mark.Color);
            var d=new Drawables().FillColor(MagickColors.None).StrokeColor(color).StrokeWidth(mark.Width);
            var left=Math.Min(mark.X,mark.X2);var top=Math.Min(mark.Y,mark.Y2);var w=Math.Abs(mark.X2-mark.X);var h=Math.Abs(mark.Y2-mark.Y);
            switch(mark.Kind) {
                case "line": d.Line(mark.X,mark.Y,mark.X2,mark.Y2);break;
                case "arrow":
                    d.Line(mark.X,mark.Y,mark.X2,mark.Y2);
                    var a=Math.Atan2(mark.Y2-mark.Y,mark.X2-mark.X);var size=Math.Max(12,mark.Width*4);
                    d.Line(mark.X2,mark.Y2,mark.X2-size*Math.Cos(a-0.45),mark.Y2-size*Math.Sin(a-0.45));
                    d.Line(mark.X2,mark.Y2,mark.X2-size*Math.Cos(a+0.45),mark.Y2-size*Math.Sin(a+0.45));break;
                case "rectangle":d.Rectangle(left,top,left+w,top+h);break;
                case "ellipse":d.Ellipse(left+w/2,top+h/2,w/2,h/2,0,360);break;
                case "text":d.Font("Segoe UI").FontPointSize(mark.FontSize).StrokeColor(MagickColors.None).FillColor(color).Text(mark.X,mark.Y+mark.FontSize,mark.Text);break;
                case "pen":
                    for(var i=2;i<mark.Points!.Length;i+=2)d.Line(mark.Points[i-2],mark.Points[i-1],mark.Points[i],mark.Points[i+1]);break;
            }
            d.Draw(image);
        }
    }
    public static MagickFormat Format(string ext) => ext switch {
        "jpg" or "jpeg" => MagickFormat.Jpeg, "png" => MagickFormat.Png, "webp" => MagickFormat.WebP,
        "avif" => MagickFormat.Avif, "heic" or "heif" => MagickFormat.Heic, "tiff" or "tif" => MagickFormat.Tiff, "svg"=>MagickFormat.Msvg,
        "bmp" => MagickFormat.Bmp, "gif" => MagickFormat.Gif, "ico" => MagickFormat.Ico,
        _ => throw new NotSupportedException("Unsupported image output: " + ext)
    };
    internal static MagickColor ParseColor(Dictionary<string,string> o)
    {
        var value = Values.Text(o,"color","#FFFFFF");
        if (!System.Text.RegularExpressions.Regex.IsMatch(value,"^#[0-9a-fA-F]{6}$")) throw new ArgumentException("Use a six-digit hex color, such as #FFFFFF.");
        return new MagickColor(value);
    }
    static void ToPdf(MagickImage image, string path)
    {
        using var bytes = new MemoryStream(); image.Format = MagickFormat.Png; image.Write(bytes); bytes.Position = 0;
        using var pdf = new PdfDocument(); var page = pdf.AddPage();
        page.Width = XUnit.FromPoint(image.Width * 0.75); page.Height = XUnit.FromPoint(image.Height * 0.75);
        using (var drawing = XGraphics.FromPdfPage(page)) using (var bitmap = XImage.FromStream(bytes)) drawing.DrawImage(bitmap,0,0,page.Width.Point,page.Height.Point);
        pdf.Save(path);
    }
    static void Collage(string[] inputs, Dictionary<string,string> options, string output, CancellationToken token)
    {
        _ = Initialized.Value;
        if (inputs.Length is <2 or >100) throw new ArgumentException("Select 2–100 different images for a collage.");
        var cols = Math.Min(inputs.Length,Values.Integer(options,"columns",2,1,10)); var cell = Values.Integer(options,"cell",640,64,2048); var gap = Values.Integer(options,"gap",16,0,200);
        var rows = (inputs.Length + cols - 1) / cols;
        var width = cols*(cell+gap)+gap; var height = rows*(cell+gap)+gap;
        if ((long)width*height > 100_000_000) throw new ArgumentException("Collage exceeds 100 megapixels. Reduce cell size.");
        using var canvas = new MagickImage(ParseColor(options),(uint)width,(uint)height);
        for (var i=0;i<inputs.Length;i++) {
            token.ThrowIfCancellationRequested(); using var image = Read(inputs[i]); image.Resize((uint)cell,(uint)cell);
            canvas.Composite(image,gap+(i%cols)*(cell+gap)+(cell-(int)image.Width)/2,gap+(i/cols)*(cell+gap)+(cell-(int)image.Height)/2,CompositeOperator.Over);
        }
        canvas.Format = MagickFormat.Png; using var stream = File.Create(output); canvas.Write(stream);
    }
}
