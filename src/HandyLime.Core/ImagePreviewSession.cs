using ImageMagick;
namespace HandyLime.Core;

public sealed record PreviewPixels(int Width,int Height,byte[] Bgra);

// A session owns bounded, decoded images. Call Render serially; no disk writes occur.
public sealed class ImagePreviewSession : IDisposable
{
    readonly Dictionary<string,MagickImage> images=new(StringComparer.OrdinalIgnoreCase);
    public int OriginalWidth{get;}
    public int OriginalHeight{get;}
    public ImagePreviewSession(string[] paths,CancellationToken token)
    {
        try{
            foreach(var path in paths.Distinct(StringComparer.OrdinalIgnoreCase)){
                token.ThrowIfCancellationRequested();var image=ImageEngine.Read(path);
                if(images.Count==0){OriginalWidth=(int)image.Width;OriginalHeight=(int)image.Height;}
                var limit=paths.Length>1?480:1100;
                if(image.Width>limit||image.Height>limit)image.Thumbnail((uint)limit,(uint)limit);
                images.Add(path,image);
            }
        }catch{Dispose();throw;}
    }
    static PreviewPixels Pixels(MagickImage image){image.Alpha(AlphaAction.On);using var pixels=image.GetPixels();return new((int)image.Width,(int)image.Height,pixels.ToByteArray(PixelMapping.BGRA)!);}
    public PreviewPixels Original()=>Pixels(images.Values.First());
    public PreviewPixels Render(string[] order,string id,Dictionary<string,string> options,CancellationToken token,bool interactive=false)
    {
        token.ThrowIfCancellationRequested();
        if(id=="collage"){
            if(order.Length<2)throw new ArgumentException("Add at least two different images to create a collage.");
            var cols=Math.Min(order.Length,Values.Integer(options,"columns",2,1,10));var cell=Values.Integer(options,"cell",640,64,2048);var gap=Values.Integer(options,"gap",16,0,200);var rows=(order.Length+cols-1)/cols;
            var fullWidth=cols*(cell+gap)+gap;var fullHeight=rows*(cell+gap)+gap;
            if((long)fullWidth*fullHeight>100_000_000)throw new ArgumentException("Collage exceeds 100 megapixels. Reduce cell size.");
            var scale=Math.Min(1,1100d/Math.Max(fullWidth,fullHeight));
            using var canvas=new MagickImage(ImageEngine.ParseColor(options),(uint)Math.Max(1,Math.Round(fullWidth*scale)),(uint)Math.Max(1,Math.Round(fullHeight*scale)));
            var size=(uint)Math.Max(1,Math.Round(cell*scale));
            for(int i=0;i<order.Length;i++){
                token.ThrowIfCancellationRequested();using var tile=(MagickImage)images[order[i]].Clone();tile.Resize(size,size);
                var x=(int)Math.Round((gap+(i%cols)*(cell+gap))*scale)+((int)size-(int)tile.Width)/2;
                var y=(int)Math.Round((gap+(i/cols)*(cell+gap))*scale)+((int)size-(int)tile.Height)/2;
                canvas.Composite(tile,x,y,CompositeOperator.Over);
            }
            return Pixels(canvas);
        }
        using var image=(MagickImage)images[order[0]].Clone();var adjusted=new Dictionary<string,string>(options);
        if(interactive&&id is "adjust" or "rotate"&& (image.Width>720||image.Height>720))image.Thumbnail(720,720);
        if(id is "resize" or "background"){
            var width=Values.Integer(options,"width",OriginalWidth,1,16384);var height=Values.Integer(options,"height",OriginalHeight,1,16384);var scale=Math.Min(1,1100d/Math.Max(width,height));
            adjusted["width"]=Math.Max(1,(int)Math.Round(width*scale)).ToString();adjusted["height"]=Math.Max(1,(int)Math.Round(height*scale)).ToString();
        }
        ImageEngine.Apply(image,id,adjusted,token);
        if(image.Width>1400||image.Height>1400)image.Thumbnail(1400,1400);
        token.ThrowIfCancellationRequested();return Pixels(image);
    }
    public void Dispose(){foreach(var image in images.Values)image.Dispose();images.Clear();}
}
