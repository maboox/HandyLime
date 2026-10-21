using HandyLime.Core;
using ImageMagick;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using UglyToad.PdfPig.Writer;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;

ImageEngine.EnsureInitialized();
var root=Path.Combine(Path.GetTempPath(),"HandyLime-tests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
var results=new List<(string name,string? error)>();var skipped=new List<string>();
var settings=new AppSettings();
if(Environment.GetEnvironmentVariable("HANDYLIME_FFMPEG") is {Length:>0} ff) settings.EnginePaths["FFmpeg"]=ff;
if(Environment.GetEnvironmentVariable("HANDYLIME_LIBREOFFICE") is {Length:>0} lo) settings.EnginePaths["LibreOffice"]=lo;
if(Environment.GetEnvironmentVariable("HANDYLIME_PANDOC") is {Length:>0} pd) settings.EnginePaths["Pandoc"]=pd;
var engines=new Engines(settings);var executor=new JobExecutor(engines);
void Assert(bool condition,string message="Assertion failed") {if(!condition)throw new Exception(message);}
async Task Test(string name,Func<Task> action) {var only=args.FirstOrDefault(a=>a.StartsWith("--only="))?[7..];if(only!=null&&!name.Contains(only,StringComparison.OrdinalIgnoreCase)&&!name.StartsWith("Generate "))return;try {await action();results.Add((name,null));Console.WriteLine("PASS "+name);}catch(Exception ex){results.Add((name,ex.ToString()));Console.WriteLine("FAIL "+name+": "+ex.Message);}}
void Throws(Action action){try{action();}catch(ArgumentException){return;}catch(InvalidDataException){return;}catch(OperationCanceledException){return;}throw new Exception("Expected validation error");}
async Task ThrowsAsync(Func<Task> action){try{await action();}catch(Exception ex) when(ex is ArgumentException or InvalidDataException or IOException or OperationCanceledException){return;}throw new Exception("Expected failure");}
async Task<string> Run(string input,string id,Dictionary<string,string>? options=null,string[]? all=null){
    var op=(id.StartsWith("convert:")?Catalog.Conversions(input):Catalog.Tools(input)).Single(o=>o.Id==id);
    var values=op.Fields.ToDictionary(f=>f.Key,f=>f.Default);if(options!=null)foreach(var pair in options)values[pair.Key]=pair.Value;
    return (await executor.Execute(new(all??[input],op,values,root),CancellationToken.None)).Outputs.Single();
}
var imagePath=Path.Combine(root,"photo فارسی & [test].png");
using(var image=new MagickImage(MagickColors.Coral,320,240)){image.Write(imagePath);}
var original=SHA256.HashData(File.ReadAllBytes(imagePath));
await Test("Visual split markers: sorting and invalid boundaries",()=>{
    Assert(Editing.SplitPoints("1.5,0.4",2).SequenceEqual(new[]{0d,0.4,1.5,2d}));
    foreach(var invalid in new[]{"0","2","1,1","1,1.02","NaN",""})Throws(()=>Editing.SplitPoints(invalid,2));return Task.CompletedTask;
});
await Test("Missing engine is identified before starting work",()=>{Assert(engines.Missing(new("missing","Missing","Test","NotAnEngine")).SequenceEqual(new[]{"NotAnEngine"}));return Task.CompletedTask;});
await Test("Video adjustment catalog shares every photo slider",()=>{
    var photo=Catalog.Tools("photo.png").Single(o=>o.Id=="adjust");var video=Catalog.Tools("clip.mp4").Single(o=>o.Id=="adjust-video");
    Assert(photo.Fields.SequenceEqual(video.Fields)&&video.Fields.Length==14&&video.Engine=="FFmpeg");
    Assert(!Catalog.Tools("track.mp3").Any(o=>o.Id=="adjust-video"));return Task.CompletedTask;
});
await Test("Windows startup quotes paths and rejects unsafe commands",()=>{
    var exe=Path.Combine(root,"app with spaces & فارسی.exe");
    Assert(StartupRegistration.CommandFor(exe)=="\""+exe+"\" --tray");
    foreach(var invalid in new[]{"relative.exe",exe+"\" --other",exe+"\n",Path.Combine(root,"app.dll")})Throws(()=>StartupRegistration.CommandFor(invalid));
    return Task.CompletedTask;
});
await Test("Windows startup registration isolated roundtrip",()=>{
    var fixtureKey="Software\\HandyLime.Tests-"+Guid.NewGuid().ToString("N");var exe=Path.Combine(root,"startup fixture.exe");File.WriteAllBytes(exe,[]);
    try{
        using var key=Microsoft.Win32.Registry.CurrentUser.CreateSubKey(fixtureKey,true);
        key.SetValue("Unrelated entry","preserve me");Assert(key.GetValue(StartupRegistration.ValueName)==null);
        StartupRegistration.Write(key,exe,true);Assert((string?)key.GetValue(StartupRegistration.ValueName)==StartupRegistration.CommandFor(exe));
        Assert(key.GetValueKind(StartupRegistration.ValueName)==Microsoft.Win32.RegistryValueKind.String);
        StartupRegistration.Write(key,exe,false);StartupRegistration.Write(key,exe,false);
        Assert(key.GetValue(StartupRegistration.ValueName)==null&&(string?)key.GetValue("Unrelated entry")=="preserve me");
    }finally{Microsoft.Win32.Registry.CurrentUser.DeleteSubKey(fixtureKey,false);}
    return Task.CompletedTask;
});
await Test("Video frame adjustments are neutral, bounded and cancellable",()=>{
    var data=new byte[1600*900*4];for(int i=0;i<data.Length;i+=4){data[i]=(byte)(i%251);data[i+1]=90;data[i+2]=160;data[i+3]=255;}
    var small=new PreviewPixels(20,10,data[..800]);var neutral=VideoAdjustments.Preview(small,new(),CancellationToken.None);Assert(neutral.Bgra.SequenceEqual(small.Bgra));
    var adjusted=VideoAdjustments.Preview(small,new(){["exposure"]="-1"},CancellationToken.None);Assert(adjusted.Bgra[2]<neutral.Bgra[2]);
    var quick=VideoAdjustments.Preview(new(1600,900,data),new(),CancellationToken.None,true);Assert(quick.Width<=720&&quick.Height<=720);
    using var cancelled=new CancellationTokenSource();cancelled.Cancel();Throws(()=>VideoAdjustments.Preview(small,new(),cancelled.Token));
    var cube=Path.Combine(root,"cancelled.cube");Throws(()=>VideoAdjustments.WriteCube(cube,new(),cancelled.Token));Assert(!File.Exists(cube));
    Throws(()=>VideoAdjustments.Preview(small,new(){["gamma"]="NaN"},CancellationToken.None));return Task.CompletedTask;
});
await Test("Subtitle roundtrip and Unicode",()=>{var srt="1\n00:00:01,250 --> 00:00:03,500\nسلام <b>world</b>\n\n2\n00:01:00,000 --> 00:01:01,000\nSecond cue\n";var vtt=SubtitleEngine.Convert(srt,"vtt");Assert(vtt.StartsWith("WEBVTT"));var cues=SubtitleEngine.Parse(SubtitleEngine.Convert(vtt,"srt"));Assert(cues.Count==2&&cues[0].Start.TotalMilliseconds==1250);Assert(SubtitleEngine.Convert(srt,"txt").Contains("سلام world"));return Task.CompletedTask;});
await Test("Reject malformed subtitle timing",()=>{Throws(()=>SubtitleEngine.Parse("1\n00:00:05,000 --> 00:00:03,000\nInvalid"));return Task.CompletedTask;});
await Test("PDF page ranges, descending and duplicates",()=>{Assert(PdfEngine.ParsePages("3,1-2,3-2",3).SequenceEqual(new[]{2,0,1,2,1}));Throws(()=>PdfEngine.ParsePages("0",3));Throws(()=>PdfEngine.ParsePages("1-4",3));return Task.CompletedTask;});
await Test("Reject NaN, infinity and fractional integers",()=>{Throws(()=>Values.Number(new(){["x"]="NaN"},"x",0,0,1));Throws(()=>Values.Number(new(){["x"]="Infinity"},"x",0,0,1));Throws(()=>Values.Integer(new(){["x"]="1.5"},"x",1,1,5));return Task.CompletedTask;});
await Test("Archive path validation",()=>{foreach(var entry in new[]{"../evil","/absolute","C:/evil","a/../../evil","file:stream","CON.txt","a/NUL","x./file","x /file"})Throws(()=>ArchiveEngine.SafePath(root,entry));Assert(ArchiveEngine.SafePath(root,"nested/file.txt").StartsWith(root));return Task.CompletedTask;});
foreach(var format in new[]{"jpg","webp","avif","tiff","bmp","gif","ico","pdf"})await Test("Image PNG → "+format,async()=>{var output=await Run(imagePath,"convert:"+format);Assert(new FileInfo(output).Length>0);if(format!="pdf"){using var image=ImageEngine.Read(output);Assert(image.Width>0&&image.Height>0);}else {using var pdf=PdfSharp.Pdf.IO.PdfReader.Open(output,PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);Assert(pdf.PageCount==1);}});
await Test("PNG resize preserves aspect",async()=>{var output=await Run(imagePath,"resize",new(){["width"]="100",["height"]="100"});using var image=ImageEngine.Read(output);Assert(image.Width==100&&image.Height==75);});
await Test("Crop exact dimensions and out of bounds",async()=>{var output=await Run(imagePath,"crop-image",new(){["x"]="10",["y"]="20",["width"]="40",["height"]="60"});using var image=ImageEngine.Read(output);Assert(image.Width==40&&image.Height==60);await ThrowsAsync(()=>Run(imagePath,"crop-image",new(){["width"]="500"}));});
foreach(var id in new[]{"rotate","flip","adjust","annotate","background","strip-image","compress-image"})await Test("Image tool "+id,async()=>{Assert(File.Exists(await Run(imagePath,id)));});
await Test("Solid redaction burns pixels",async()=>{var output=await Run(imagePath,"redact-image",new(){["x"]="5",["y"]="5",["width"]="25",["height"]="25"});using var image=ImageEngine.Read(output);using var pixels=image.GetPixels();var color=pixels.GetPixel(10,10).ToColor();Assert(color!.R==0&&color.G==0&&color.B==0);});
await Test("Visual annotation document burns shapes and Unicode text",async()=>{
    var marks=new[]{new Mark("line",10,30,100,30,"#000000",8),new Mark("arrow",20,80,120,80),new Mark("rectangle",140,20,180,60),new Mark("ellipse",200,20,250,70),new Mark("text",10,140,10,140,Text:"سلام test",FontSize:20),new Mark("pen",20,200,80,210,Points:[20,200,50,190,80,210])};
    var output=await Run(imagePath,"annotate",new(){["marks"]=System.Text.Json.JsonSerializer.Serialize(marks)});using var image=ImageEngine.Read(output);using var pixels=image.GetPixels();Assert(pixels.GetPixel(50,30).ToColor()!.R==0);Assert(image.Width==320&&image.Height==240);
});
await Test("Reject invalid annotation geometry and colors",()=>{foreach(var mark in new[]{new Mark("line",-1,0,4,4),new Mark("bad",0,0,4,4),new Mark("pen",0,0,4,4,Points:[0,0]),new Mark("arrow",0,0,4,4,"not-a-color")})Throws(()=>Editing.ReadMarks(System.Text.Json.JsonSerializer.Serialize(new[]{mark}),320,240));return Task.CompletedTask;});
await Test("Photo neutral sliders preserve decoded pixels",async()=>{var output=await Run(imagePath,"adjust");using var a=ImageEngine.Read(imagePath);using var b=ImageEngine.Read(output);using var pa=a.GetPixels();using var pb=b.GetPixels();Assert(pa.ToByteArray(PixelMapping.RGBA)!.SequenceEqual(pb.ToByteArray(PixelMapping.RGBA)!));});
await Test("Photo light and color sliders alter pixels",async()=>{var output=await Run(imagePath,"adjust",new(){["exposure"]="-1",["contrast"]="15",["highlights"]="-20",["shadows"]="10",["whites"]="10",["blacks"]="-10",["gamma"]="1.2",["temperature"]="15",["tint"]="10",["vibrance"]="20",["hue"]="15"});using var image=ImageEngine.Read(output);using var pixels=image.GetPixels();Assert(pixels.GetPixel(10,10).ToColor()!.R<250);});
await Test("Collage output geometry",async()=>{var other=Path.Combine(root,"second.png");File.Copy(imagePath,other);var output=await Run(imagePath,"collage",new(){["columns"]="2",["cell"]="100",["gap"]="10"},[imagePath,other]);using var image=ImageEngine.Read(output);Assert(image.Width==230&&image.Height==120);});
await Test("Collage source order and cached preview agree",async()=>{
    var red=Path.Combine(root,"red.png");var blue=Path.Combine(root,"blue.png");using(var a=new MagickImage(MagickColors.Red,800,600))a.Write(red);using(var b=new MagickImage(MagickColors.Blue,400,400))b.Write(blue);
    var options=new Dictionary<string,string>{{"columns","2"},{"cell","200"},{"gap","10"},{"color","#FFFFFF"}};
    var output=await Run(red,"collage",options,[blue,red]);using(var image=ImageEngine.Read(output)){using var pixels=image.GetPixels();Assert(pixels.GetPixel(110,110).ToColor()!.B>200&&pixels.GetPixel(320,110).ToColor()!.R>200);}
    using var session=new ImagePreviewSession([red,blue],CancellationToken.None);var preview=session.Render([blue,red],"collage",options,CancellationToken.None);Assert(preview.Width==430&&preview.Height==220);Assert(preview.Bgra[(110*430+110)*4]>200&&preview.Bgra[(110*430+320)*4+2]>200);
    await ThrowsAsync(()=>Run(red,"collage",options));
});
await Test("Cached photo preview is bounded and writes no intermediate files",()=>{
    var large=Path.Combine(root,"large photo.png");using(var source=new MagickImage(MagickColors.Coral,6000,4000))source.Write(large);
    using var session=new ImagePreviewSession([large],CancellationToken.None);var files=Directory.GetFiles(root).Length;var times=new List<double>();
    for(int i=0;i<8;i++){var timer=System.Diagnostics.Stopwatch.StartNew();var preview=session.Render([large],"adjust",new(){["exposure"]=(i*0.1).ToString(System.Globalization.CultureInfo.InvariantCulture)},CancellationToken.None);times.Add(timer.Elapsed.TotalMilliseconds);Assert(preview.Width<=1100&&preview.Height<=1100);}
    Assert(Directory.GetFiles(root).Length==files);Assert(session.OriginalWidth==6000&&session.OriginalHeight==4000);Console.WriteLine($"  24 MP source → cached preview median {times.Order().ElementAt(times.Count/2):0} ms");
    var rotate=session.Render([large],"rotate",new(){["degrees"]="90"},CancellationToken.None);Assert(rotate.Height>rotate.Width);
    var quick=session.Render([large],"adjust",new(),CancellationToken.None,true);Assert(quick.Width<=720&&quick.Height<=720,"Interactive preview must use the smaller render budget");
    using var cancelled=new CancellationTokenSource();cancelled.Cancel();Throws(()=>session.Render([large],"adjust",new(),cancelled.Token));
    return Task.CompletedTask;
});
await Test("Image metadata is inspectable and removed in clean copy",async()=>{
    var tagged=Path.Combine(root,"tagged.png");using(var image=new MagickImage(MagickColors.Lime,80,80)){image.SetAttribute("comment","HandyLime synthetic tag");image.Write(tagged);}
    var before=await MetadataEngine.Read(tagged,engines,CancellationToken.None);Assert(before.Any(row=>row.Value.Contains("HandyLime synthetic tag")));
    var output=await Run(tagged,"strip-image");var after=await MetadataEngine.Read(output,engines,CancellationToken.None);Assert(!after.Any(row=>row.Value.Contains("HandyLime synthetic tag")));
    var oriented=Path.Combine(root,"oriented.jpg");using(var image=new MagickImage(MagickColors.Coral,80,40)){var profile=new ExifProfile();profile.SetValue(ExifTag.Orientation,(ushort)6);image.SetProfile(profile);image.Orientation=ImageMagick.Orientation.RightTop;image.Write(oriented);}
    var raw=await MetadataEngine.Read(oriented,engines,CancellationToken.None);Assert(raw.Any(row=>row.Property=="Dimensions"&&row.Value=="80 × 40"),"Metadata inspection must report original stored dimensions");using(var image=ImageEngine.Read(oriented))Assert(image.Width==40&&image.Height==80,"Editing still uses oriented dimensions");
});
await Test("Safe SVG input and external resource rejection",async()=>{var svg=Path.Combine(root,"vector.svg");await File.WriteAllTextAsync(svg,"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"20\" height=\"20\"><rect width=\"20\" height=\"20\" fill=\"red\"/></svg>");Assert(File.Exists(await Run(svg,"convert:png")));await File.WriteAllTextAsync(svg,"<svg xmlns=\"http://www.w3.org/2000/svg\"><image href=\"https://example.com/a.png\"/></svg>");await ThrowsAsync(()=>Run(svg,"convert:png"));});
await Test("No overwrite and original hash preserved",async()=>{var a=await Run(imagePath,"convert:jpg");var b=await Run(imagePath,"convert:jpg");Assert(a!=b);Assert(SHA256.HashData(File.ReadAllBytes(imagePath)).SequenceEqual(original));});
await Test("A renamed drawing script cannot enter the PNG decoder",async()=>{var spoof=Path.Combine(root,"drawing script.png");await File.WriteAllTextAsync(spoof,"push graphic-context\nviewbox 0 0 20 20\nfill red\nrectangle 0,0 20,20\npop graphic-context");try{using var image=ImageEngine.Read(spoof);}catch(MagickException){return;}throw new Exception("A drawing script disguised as a PNG must be rejected.");});
await Test("Cancelled job leaves no partial output",async()=>{using var cancel=new CancellationTokenSource();cancel.Cancel();await ThrowsAsync(()=>executor.Execute(new([imagePath],Catalog.Conversions(imagePath).First(),new(),root),cancel.Token));Assert(!Directory.EnumerateDirectories(root,".handylime-*").Any());});
var textPath=Path.Combine(root,"hello فارسی.txt");await File.WriteAllTextAsync(textPath,"Hello world — سلام");
foreach(var format in new[]{"zip","tar"})await Test("Archive "+format+" roundtrip",async()=>{var archive=await Run(textPath,format);var extracted=await Run(archive,"extract");Assert(await File.ReadAllTextAsync(Path.Combine(extracted,Path.GetFileName(textPath)))==await File.ReadAllTextAsync(textPath));});
await Test("ZIP traversal is rejected with cleanup",async()=>{var evil=Path.Combine(root,"malicious.zip");using(var z=ZipFile.Open(evil,ZipArchiveMode.Create)){using var w=new StreamWriter(z.CreateEntry("../outside.txt").Open());w.Write("bad");}await ThrowsAsync(()=>Run(evil,"extract"));Assert(!File.Exists(Path.Combine(root,"outside.txt")));Assert(!Directory.EnumerateDirectories(root,".handylime-*").Any());});
await Test("Folder archive cannot contain its output",async()=>{var folder=Path.Combine(root,"folder");Directory.CreateDirectory(folder);await File.WriteAllTextAsync(Path.Combine(folder,"a.txt"),"test");var op=Catalog.Tools(folder).Single(o=>o.Id=="zip");await ThrowsAsync(()=>executor.Execute(new([folder],op,new(),folder),CancellationToken.None));});
await Test("Checksum matches source",async()=>{var hash=await Run(textPath,"hash");Assert((await File.ReadAllTextAsync(hash)).StartsWith(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(textPath))).ToLowerInvariant()));});
var pdfPath=Path.Combine(root,"document.pdf");
var builder=new PdfDocumentBuilder();var font=builder.AddStandard14Font(Standard14Font.Helvetica);
for(var i=0;i<3;i++){var page=builder.AddPage(UglyToad.PdfPig.Content.PageSize.A4);page.AddText("HandyLime page "+(i+1),18,new PdfPoint(40,700),font);}File.WriteAllBytes(pdfPath,builder.Build());
await Test("PDF text extraction",async()=>{var text=await Run(pdfPath,"convert:txt");Assert((await File.ReadAllTextAsync(text)).Contains("HandyLime page 3"));});
await Test("PDF rearrange and rotation",async()=>{var output=await Run(pdfPath,"pdf-pages",new(){["pages"]="3,1,1",["rotation"]="90"});using var pdf=PdfSharp.Pdf.IO.PdfReader.Open(output,PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);Assert(pdf.PageCount==3&&pdf.Pages[0].Rotate==90);});
await Test("Visual PDF editor preserves independent page rotations",async()=>{var output=await Run(pdfPath,"pdf-pages",new(){["pages"]="3,1,1",["rotations"]="90,180,270"});using var pdf=PdfSharp.Pdf.IO.PdfReader.Open(output,PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);Assert(pdf.Pages.Cast<PdfSharp.Pdf.PdfPage>().Select(p=>p.Rotate).SequenceEqual(new[]{90,180,270}));});
await Test("PDF thumbnail renderer produces a decoded page",async()=>{var bytes=await PreviewEngine.PdfPage(pdfPath,1,CancellationToken.None);using var image=new MagickImage(bytes);Assert(image.Width==400&&image.Height>400);});
await Test("PDF split",async()=>{var output=await Run(pdfPath,"pdf-split");Assert(Directory.GetFiles(output,"*.pdf").Length==3);});
await Test("PDF merge",async()=>{var second=Path.Combine(root,"second.pdf");File.Copy(pdfPath,second);var output=await Run(pdfPath,"pdf-merge",null,[pdfPath,second]);using var pdf=PdfSharp.Pdf.IO.PdfReader.Open(output,PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);Assert(pdf.PageCount==6);});
foreach(var ext in new[]{"png","jpg"})await Test("PDF render all pages to "+ext,async()=>{var output=await Run(pdfPath,"convert:"+ext);Assert(Directory.GetFiles(output,"*."+ext).Length==3);using var image=ImageEngine.Read(Directory.GetFiles(output)[0]);Assert(image.Width>2400);});
await Test("Mixed selection only exposes common tools",()=>{Assert(Catalog.ForSelection([textPath,imagePath],false).Count==0);Assert(Catalog.ForSelection([textPath,imagePath],true).Any(o=>o.Id=="zip"));return Task.CompletedTask;});
if(engines.Find("FFmpeg")!=null && engines.Find("FFprobe")!=null) {
    var video=Path.Combine(root,"video test.mp4");var audio=Path.Combine(root,"audio.wav");var silent=Path.Combine(root,"silent.mp4");
    await Test("Generate media fixtures",async()=>{
        await ProcessRunner.Run(engines.Require("FFmpeg"),["-v","error","-f","lavfi","-i","testsrc2=s=320x240:r=15:d=2","-f","lavfi","-i","sine=frequency=440:duration=2","-c:v","libx264","-pix_fmt","yuv420p","-c:a","aac","-shortest",video],CancellationToken.None);
        await ProcessRunner.Run(engines.Require("FFmpeg"),["-v","error","-f","lavfi","-i","sine=frequency=440:duration=2",audio],CancellationToken.None);
        await ProcessRunner.Run(engines.Require("FFmpeg"),["-v","error","-i",video,"-an","-c:v","copy",silent],CancellationToken.None);
    });
    async Task<PreviewPixels> VideoPixels(string path){var frame=Path.Combine(root,Guid.NewGuid().ToString("N")+".png");await PreviewEngine.Frame(engines,path,0.6,frame,CancellationToken.None);using var image=ImageEngine.Read(frame);using var pixels=image.GetPixels();return new((int)image.Width,(int)image.Height,pixels.ToByteArray(PixelMapping.BGRA)!);}
    double PixelDifference(PreviewPixels a,PreviewPixels b){Assert(a.Width==b.Width&&a.Height==b.Height);long error=0;for(var i=0;i<a.Bgra.Length;i++)if(i%4!=3)error+=Math.Abs(a.Bgra[i]-b.Bgra[i]);return error/(a.Width*a.Height*3d);}
    await Test("Video adjustments preserve geometry duration audio and neutral color",async()=>{
        var output=await Run(video,"adjust-video");var info=await new MediaEngine(engines).Probe(output,CancellationToken.None);
        Assert(info.Width==320&&info.Height==240&&info.HasAudio&&Math.Abs(info.Duration-2)<0.15);
        var difference=PixelDifference(await VideoPixels(video),await VideoPixels(output));Assert(difference<6,$"Neutral adjustment changed colors too much: {difference:0.00}");
    });
    await Test("Video edited color matches the photo frame preview",async()=>{
        var options=new Dictionary<string,string>{["exposure"]="-0.7",["brightness"]="90",["contrast"]="12",["highlights"]="-20",["shadows"]="15",["whites"]="5",["blacks"]="-5",["gamma"]="1.1",["temperature"]="12",["tint"]="8",["saturation"]="115",["vibrance"]="20",["hue"]="15"};
        var source=await VideoPixels(video);var expected=VideoAdjustments.Preview(source,options,CancellationToken.None);var output=await Run(video,"adjust-video",options);
        var actual=await VideoPixels(output);var difference=PixelDifference(expected,actual);Console.WriteLine($"DIAGNOSTIC Edited video vs frame preview: {difference:0.00}/255 mean channel difference");
        Assert(difference<8,$"Rendered colors differ from the preview: {difference:0.00}");Assert(PixelDifference(source,actual)>10,"Video edit must visibly change the saved pixels");
    });
    await Test("Video adjustments handle silent clips sharpening and unusual paths",async()=>{
        var folder=Path.Combine(root,"ویدیو [x]; , ' and spaces");Directory.CreateDirectory(folder);var input=Path.Combine(folder,"silent & clip.mp4");File.Copy(silent,input);
        var op=Catalog.Tools(input).Single(o=>o.Id=="adjust-video");var reports=new RecordingProgress<JobProgress>();
        var output=(await executor.Execute(new([input],op,new(){["sharpen"]="2",["saturation"]="0"},folder),CancellationToken.None,reports)).Outputs.Single();
        var info=await new MediaEngine(engines).Probe(output,CancellationToken.None);Assert(!info.HasAudio&&info.Width==320&&Math.Abs(info.Duration-2)<0.15);
        var frame=await VideoPixels(output);Assert(Enumerable.Range(0,frame.Width*frame.Height).Average(i=>Math.Abs(frame.Bgra[i*4]-frame.Bgra[i*4+2]))<3,"Zero saturation should produce neutral gray");
        Assert(reports.Values.Last().Fraction==1&&reports.Values.Any(p=>p.Fraction is >=0 and <1));Assert(!Directory.GetDirectories(folder,".handylime-*").Any());
    });
    await Test("Video adjustments reject invalid values and clean cancelled work",async()=>{
        foreach(var invalid in new[]{new Dictionary<string,string>{["exposure"]="NaN"},new(){["gamma"]="0"},new(){["sharpen"]="8"}})await ThrowsAsync(()=>Run(video,"adjust-video",invalid));
        using var cancelled=new CancellationTokenSource();cancelled.Cancel();var op=Catalog.Tools(video).Single(o=>o.Id=="adjust-video");
        await ThrowsAsync(()=>executor.Execute(new([video],op,new(),root),cancelled.Token));Assert(!Directory.GetDirectories(root,".handylime-*").Any());
    });
    foreach(var ext in new[]{"mov","mkv","webm","avi","wmv","gif","mp3","wav"})await Test("Video to "+ext,async()=>{var path=await Run(video,"convert:"+ext);Assert(new FileInfo(path).Length>0);});
    await Test("GIF selected interval, dimensions and visible progress",async()=>{
        var reports=new RecordingProgress<JobProgress>();var op=Catalog.Conversions(video).Single(o=>o.Id=="convert:gif");
        var output=(await executor.Execute(new([video],op,new(){["start"]="0.2",["end"]="1.2",["width"]="160",["fps"]="10"},root),CancellationToken.None,reports)).Outputs.Single();
        Assert(System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(output),0,3)=="GIF");
        var info=await new MediaEngine(engines).Probe(output,CancellationToken.None);Assert(info.Width==160&&Math.Abs(info.Duration-1)<0.2);
        using var frames=new MagickImageCollection(output);Assert(frames.Count>=2,"GIF has no animation frames");
        frames.Coalesce();using var first=frames[0].GetPixels();using var last=frames[^1].GetPixels();Assert(!first.ToByteArray(PixelMapping.RGBA)!.SequenceEqual(last.ToByteArray(PixelMapping.RGBA)!),"GIF frames must contain changing image content");
        Assert(reports.Values.Any(p=>p.Fraction is >=0 and <1)&&reports.Values.Last().Fraction==1,"Expected conversion and completion progress");
    });
    await Test("Media metadata is shown and tags are removed",async()=>{var tagged=Path.Combine(root,"tagged video.mp4");await ProcessRunner.Run(engines.Require("FFmpeg"),["-v","error","-i",video,"-c","copy","-metadata","title=HandyLime test title",tagged],CancellationToken.None);var before=await MetadataEngine.Read(tagged,engines,CancellationToken.None);Assert(before.Any(row=>row.Value=="HandyLime test title"));Assert(before.Any(row=>row.Property.Contains("codec_name")));var output=await Run(tagged,"strip-media");var after=await MetadataEngine.Read(output,engines,CancellationToken.None);Assert(!after.Any(row=>row.Value=="HandyLime test title"));});
    await Test("Video split at visual markers yields expected clip durations",async()=>{var output=await Run(video,"split-video",new(){["cuts"]="0.4,1.3"});var clips=Directory.GetFiles(output,"*.mp4").Order().ToArray();Assert(clips.Length==3);var expected=new[]{0.4,0.9,0.7};for(int i=0;i<clips.Length;i++)Assert(Math.Abs((await new MediaEngine(engines).Probe(clips[i],CancellationToken.None)).Duration-expected[i])<0.15);});
    await Test("Video crop preserves odd selection dimensions before codec padding",async()=>{var output=await Run(video,"crop-video",new(){["x"]="9",["y"]="13",["width"]="101",["height"]="79"});var info=await new MediaEngine(engines).Probe(output,CancellationToken.None);Assert(info.Width==102&&info.Height==80,"Odd crop dimensions must round up with padding, never round the selected content down");});
    await Test("Video scrub preview and audio waveform decode",async()=>{var frame=Path.Combine(root,"scrub.png");await PreviewEngine.Frame(engines,video,0.7,frame,CancellationToken.None);using(var image=ImageEngine.Read(frame))Assert(image.Width==320&&image.Height==240);var last=Path.Combine(root,"last preview.png");await PreviewEngine.Frame(engines,video,1.99,last,CancellationToken.None);using(var image=ImageEngine.Read(last))Assert(image.Width==320&&image.Height==240);var savedLast=await Run(video,"frame",new(){["start"]="1.99"});using(var image=ImageEngine.Read(savedLast))Assert(image.Width==320);var wave=Path.Combine(root,"wave.png");await PreviewEngine.Waveform(engines,audio,wave,CancellationToken.None);using(var image=ImageEngine.Read(wave))Assert(image.Width==1200&&image.Height==180);});
    foreach(var ext in new[]{"mp3","m4a","flac","ogg","opus","aiff","wma"})await Test("Audio to "+ext,async()=>{var path=await Run(audio,"convert:"+ext);Assert((await new MediaEngine(engines).Probe(path,CancellationToken.None)).HasAudio);});
    foreach(var id in new[]{"compress-video","trim-video","crop-video","speed","frame","split-video","redact-video","strip-media"})await Test("Video tool "+id,async()=>{var path=await Run(video,id,new(){["start"]="0.2",["end"]="1.2",["width"]="100",["height"]="100",["seconds"]="1"});Assert(File.Exists(path)||Directory.Exists(path));});
    foreach(var id in new[]{"trim-audio","normalize","channels","bleep","visualizer","strip-audio"})await Test("Audio tool "+id,async()=>{Assert(File.Exists(await Run(audio,id,new(){["start"]="0.2",["end"]="1.2"})));});
    await Test("Speed silent video",async()=>{Assert(File.Exists(await Run(silent,"speed")));});
    await Test("Join mixed silent and audio clips",async()=>{var output=await Run(video,"join-video",null,[video,silent]);var info=await new MediaEngine(engines).Probe(output,CancellationToken.None);Assert(info.Duration>3.5&&info.HasAudio&&info.Width==1280);});
    await Test("Join output follows the selected clip order",async()=>{
        var red=Path.Combine(root,"red clip.mp4");var blue=Path.Combine(root,"blue clip.mp4");
        foreach(var item in new[]{(red,"red"),(blue,"blue")})await ProcessRunner.Run(engines.Require("FFmpeg"),["-v","error","-f","lavfi","-i",$"color=c={item.Item2}:s=160x90:r=30:d=0.4","-c:v","libx264","-pix_fmt","yuv420p",item.Item1],CancellationToken.None);
        var output=await Run(red,"join-video",null,[blue,red]);var frame=Path.Combine(root,"joined first.png");await PreviewEngine.Frame(engines,output,0.1,frame,CancellationToken.None);using var image=ImageEngine.Read(frame);using var pixels=image.GetPixels();var color=pixels.GetPixel((int)image.Width/2,(int)image.Height/2).ToColor()!;Assert(color.B>200&&color.R<50,"First output clip must be the first selected blue clip");
    });
    await Test("FFmpeg cancellation kills process",async()=>{using var cancel=new CancellationTokenSource(300);await ThrowsAsync(()=>ProcessRunner.Run(engines.Require("FFmpeg"),["-re","-f","lavfi","-i","sine=duration=60","-f","null","-"],cancel.Token));});
} else {skipped.Add("FFmpeg integration suite: engine not installed");}
if(engines.Find("LibreOffice")!=null) {
    await Test("LibreOffice TXT to DOCX and PDF",async()=>{var docx=await Run(textPath,"convert:docx");Assert(File.Exists(docx));var pdf=await Run(docx,"convert:pdf");Assert(PdfEngine.ExtractText(pdf).Contains("Hello"));});
    await Test("LibreOffice CSV to XLSX and TSV",async()=>{var csv=Path.Combine(root,"table.csv");await File.WriteAllTextAsync(csv,"Name,Value\nExample,42\n");var xlsx=await Run(csv,"convert:xlsx");var tsv=await Run(xlsx,"convert:tsv");Assert((await File.ReadAllTextAsync(tsv)).Contains("\t"));var direct=await Run(csv,"convert:tsv");Assert((await File.ReadAllTextAsync(direct)).Contains("\t"));var back=await Run(direct,"convert:csv");Assert((await File.ReadAllTextAsync(back)).Contains(","));});
    await Test("LibreOffice HTML keeps resource folder",async()=>{var html=await Run(textPath,"convert:html");Assert(Directory.Exists(html)&&Directory.GetFiles(html,"*.html").Length==1);});
}else skipped.Add("LibreOffice integration suite: engine not installed");
if(engines.Find("Pandoc")!=null) {
    await Test("Pandoc Markdown to DOCX, EPUB and HTML",async()=>{var md=Path.Combine(root,"notes.md");await File.WriteAllTextAsync(md,"# Hello\n\nA local document.");foreach(var ext in new[]{"docx","epub","html"})Assert(File.Exists(await Run(md,"convert:"+ext)));});
}else skipped.Add("Pandoc integration suite: engine not installed");
if(args.Contains("--require-engines")&&skipped.Count>0)results.Add(("Required engines installed",string.Join("; ",skipped)));
var reportDir=Path.GetFullPath(args.FirstOrDefault(a=>a.StartsWith("--report="))?[9..]??"TestResults");Directory.CreateDirectory(reportDir);
new XDocument(new XElement("testsuite",new XAttribute("name","HandyLime"),new XAttribute("tests",results.Count),new XAttribute("failures",results.Count(r=>r.error!=null)),results.Select(r=>new XElement("testcase",new XAttribute("name",r.name),r.error==null?null:new XElement("failure",r.error))))).Save(Path.Combine(reportDir,"results.xml"));
await File.WriteAllTextAsync(Path.Combine(reportDir,"summary.txt"),$"{results.Count(r=>r.error==null)} passed; {results.Count(r=>r.error!=null)} failed.\n"+string.Join('\n',skipped));
Console.WriteLine($"\n{results.Count(r=>r.error==null)} passed; {results.Count(r=>r.error!=null)} failed.");foreach(var skip in skipped)Console.WriteLine("SKIP "+skip);
if(args.Contains("--keep-fixtures"))Console.WriteLine("Fixtures: "+root);else Directory.Delete(root,true);
return results.Any(r=>r.error!=null)?1:0;

sealed class RecordingProgress<T> : IProgress<T>
{
    public List<T> Values {get;}=[];
    public void Report(T value){lock(Values)Values.Add(value);}
}

