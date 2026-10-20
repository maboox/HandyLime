using Windows.Storage;
using Windows.Storage.Streams;

namespace HandyLime.Core;

public static class PreviewEngine
{
    public static async Task Frame(Engines engines,string input,double seconds,string output,CancellationToken token)
    {
        if(!double.IsFinite(seconds)||seconds<0)throw new ArgumentException("Invalid preview position.");
        await ProcessRunner.Run(engines.Require("FFmpeg"),["-hide_banner","-v","error","-nostdin","-n","-ss",Values.Invariant(seconds),"-protocol_whitelist","file,pipe","-i",input,"-map","0:v:0","-frames:v","1","-vf","scale=w='min(960,iw)':h=-1","-update","1",output],token);
        if(!File.Exists(output)||new FileInfo(output).Length==0){
            var info=await new MediaEngine(engines).Probe(input,token);
            if(seconds>=Math.Max(0,info.Duration-0.5)&&seconds<=info.Duration+0.05){if(File.Exists(output))File.Delete(output);await LastFrame(engines,input,output,token,true);}
        }
        if(!File.Exists(output)||new FileInfo(output).Length==0)throw new IOException("This position has no decodable video frame.");
    }
    public static async Task LastFrame(Engines engines,string input,string output,CancellationToken token,bool preview)
    {
        var filter=preview?"scale=w='min(960,iw)':h=-1,reverse":"reverse";
        await ProcessRunner.Run(engines.Require("FFmpeg"),["-hide_banner","-v","error","-nostdin","-n","-sseof","-0.15","-protocol_whitelist","file,pipe","-i",input,"-map","0:v:0","-vf",filter,"-frames:v","1","-update","1",output],token);
    }
    public static async Task Waveform(Engines engines,string input,string output,CancellationToken token) =>
        await ProcessRunner.Run(engines.Require("FFmpeg"),["-hide_banner","-v","error","-nostdin","-n","-protocol_whitelist","file,pipe","-i",input,"-filter_complex","[0:a:0]aformat=channel_layouts=mono,showwavespic=s=1200x180:colors=0xB9E44D[v]","-map","[v]","-frames:v","1","-update","1",output],token);
    public static async Task<byte[]> PdfPage(string input,int index,CancellationToken token)
    {
        var pdf=await Windows.Data.Pdf.PdfDocument.LoadFromFileAsync(await StorageFile.GetFileFromPathAsync(Path.GetFullPath(input)));
        if(index<0||index>=pdf.PageCount)throw new ArgumentOutOfRangeException(nameof(index));
        token.ThrowIfCancellationRequested();using var page=pdf.GetPage((uint)index);
        using var memory=new InMemoryRandomAccessStream();
        await page.RenderToStreamAsync(memory,new Windows.Data.Pdf.PdfPageRenderOptions{DestinationWidth=400,DestinationHeight=(uint)Math.Max(1,400*page.Size.Height/page.Size.Width)});
        token.ThrowIfCancellationRequested();memory.Seek(0);using var reader=new DataReader(memory.GetInputStreamAt(0));
        await reader.LoadAsync((uint)memory.Size);var bytes=new byte[(int)memory.Size];reader.ReadBytes(bytes);return bytes;
    }
}
