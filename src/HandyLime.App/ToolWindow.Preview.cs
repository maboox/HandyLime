using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HandyLime.Core;
namespace HandyLime.App;
public sealed partial class ToolWindow
{
    bool imagePreviewBusy;
    readonly Dictionary<(string Source,long Frame),BitmapSource> frameCache=[];
    void SchedulePreview(){if(!ready||rendering||operation.Id.StartsWith("strip-"))return;previewVersion++;if(kind==FileKind.Image){if(imagePreviewBusy)return;}else refresh?.Cancel();if(!debounce.IsEnabled)debounce.Start();}
    async Task RefreshPreview()
    {
        if(!ready||lifetime.IsCancellationRequested)return;imagePreviewBusy=kind==FileKind.Image;refresh?.Cancel();var request=CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);refresh=request;var token=request.Token;var version=previewVersion;var options=Options;var source=inputs.ToArray();var mediaInput=PreviewInput;var position=timeline.Position;
        try{
            await previewSerial.WaitAsync(token);
            try{
                token.ThrowIfCancellationRequested();
                if(kind==FileKind.Image){
                    if(compare||canvas.RegionEnabled||canvas.Annotating){canvas.Picture=original;canvas.DocumentWidth=originalWidth;canvas.DocumentHeight=originalHeight;canvas.InvalidateVisual();return;}
                    var watch=System.Diagnostics.Stopwatch.StartNew();var interactive=System.Windows.Input.Mouse.LeftButton==System.Windows.Input.MouseButtonState.Pressed;
                    var result=await Task.Run(()=>PreviewBitmap.FromPixels(imageSession!.Render(source,operation.Id,options,token,interactive)),token);
                    token.ThrowIfCancellationRequested();canvas.Picture=result;canvas.DocumentWidth=result.PixelWidth;canvas.DocumentHeight=result.PixelHeight;canvas.InvalidateVisual();status.Text=$"Preview · {watch.ElapsedMilliseconds} ms · save uses original resolution";status.SetResourceReference(TextBlock.ForegroundProperty,"Muted");
                }else if(kind is FileKind.Video or FileKind.Audio){
                    var key=(mediaInput,(long)Math.Round(position*1000));if(kind==FileKind.Video&&frameCache.TryGetValue(key,out var cached)){canvas.Picture=cached;canvas.ShowPicture=true;canvas.InvalidateVisual();return;}
                    var path=Path.Combine(temp,Guid.NewGuid().ToString("N")+".png");
                    if(kind==FileKind.Audio){if(original!=null){canvas.Picture=original;return;}await PreviewEngine.Waveform(engines,source[0],path,token);}else await PreviewEngine.Frame(engines,mediaInput,Math.Min(position,Math.Max(0,timeline.Duration-0.04)),path,token);
                    token.ThrowIfCancellationRequested();canvas.Picture=PreviewBitmap.FromFile(path);if(kind==FileKind.Audio){original=canvas.Picture;timeline.Frames.Clear();timeline.Frames.Add(original);timeline.InvalidateVisual();}else{if(frameCache.Count>=32)frameCache.Remove(frameCache.Keys.First());frameCache[key]=canvas.Picture;}canvas.ShowPicture=true;canvas.InvalidateVisual();File.Delete(path);
                }
            }finally{previewSerial.Release();}
        }catch(OperationCanceledException){}catch(Exception ex){if(!token.IsCancellationRequested)Error("Preview: "+ex.Message);}finally{request.Dispose();if(ReferenceEquals(refresh,request))refresh=null;if(kind==FileKind.Image){imagePreviewBusy=false;if(ready&&!lifetime.IsCancellationRequested&&previewVersion!=version&&!debounce.IsEnabled)debounce.Start();}}
    }
    CancellationTokenSource? filmstripCancellation;
    async Task LoadFilmstrip()
    {
        if(kind!=FileKind.Video)return;filmstripCancellation?.Cancel();using var request=CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);filmstripCancellation=request;var source=PreviewInput;var duration=timeline.Duration;timeline.Frames.Clear();
        try{for(int i=0;i<7;i++){request.Token.ThrowIfCancellationRequested();var path=Path.Combine(temp,Guid.NewGuid().ToString("N")+".png");await PreviewEngine.Frame(engines,source,duration*i/7,path,request.Token);request.Token.ThrowIfCancellationRequested();if(source!=PreviewInput)return;timeline.Frames.Add(PreviewBitmap.FromFile(path));timeline.InvalidateVisual();File.Delete(path);}}catch(OperationCanceledException){}catch(Exception){/* Frame scrubbing is independent of the filmstrip. */}finally{if(ReferenceEquals(filmstripCancellation,request))filmstripCancellation=null;}
    }
    void Seek(double delta){StopPlayback();timeline.Position=Math.Clamp(timeline.Position+delta,0,Math.Max(0,timeline.Duration-0.04));timeline.InvalidateVisual();UpdateTimeFields();SchedulePreview();}
    void TogglePlayback()
    {
        if(playbackUnavailable){Error("Playback is unavailable for this clip. Frame scrubbing and saving still work.");return;}
        if(playing){StopPlayback();return;}if(!mediaOpened){playRequested=true;status.Text="Preparing playback…";if(!proxyAttempted)proxyTask=BuildPlaybackProxy();return;}
        player.Position=TimeSpan.FromSeconds(Math.Clamp(timeline.Position,timeline.SelectRange?timeline.Start:0,timeline.SelectRange?timeline.End:timeline.Duration));
        if(operation.Id=="speed")player.SpeedRatio=Values.Number(values,"speed",2,0.25,4);
        player.Visibility=Visibility.Visible;canvas.ShowPicture=kind==FileKind.Audio;canvas.InvalidateVisual();player.Play();playing=true;play.Content=Icons.Label("pause","Pause");playback.Start();
    }
    void StopPlayback(){if(!playing)return;player.Pause();playing=false;playback.Stop();play.Content=Icons.Label("play","Play");canvas.ShowPicture=true;canvas.InvalidateVisual();SchedulePreview();}
    CancellationTokenSource? proxyCancellation;
    async Task BuildPlaybackProxy()
    {
        proxyCancellation?.Cancel();using var request=CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);proxyCancellation=request;var source=PreviewInput;
        proxyAttempted=true;mediaOpened=false;status.Text="Preparing a compatible playback preview…";
        try{var path=Path.Combine(temp,"playback-"+Guid.NewGuid().ToString("N")+".mp4");var args=new List<string>{"-hide_banner","-v","error","-nostdin","-n","-protocol_whitelist","file,pipe","-i",source};
            if(kind==FileKind.Video)args.AddRange(["-map","0:v:0","-map","0:a:0?","-vf","scale=w='min(960,iw)':h=-2,pad=ceil(iw/2)*2:ceil(ih/2)*2","-c:v","libx264","-preset","ultrafast","-crf","26"]);
            args.AddRange(["-c:a","aac",path]);await ProcessRunner.Run(engines.Require("FFmpeg"),args,request.Token);request.Token.ThrowIfCancellationRequested();if(source!=PreviewInput)return;player.Source=new Uri(path);player.Pause();status.Text="Playback preview ready";
        }catch(OperationCanceledException){}catch(Exception ex){if(!request.IsCancellationRequested)Error("Playback unavailable; frame scrubbing still works. "+ex.Message);}finally{if(ReferenceEquals(proxyCancellation,request))proxyCancellation=null;}
    }
    async Task PreviewResult()
    {
        StopPlayback();rendering=true;save.IsEnabled=false;refresh?.Cancel();
        try{
            status.Text="Rendering a short output preview…";var start=timeline.SelectRange?timeline.Start:timeline.Position;var duration=Math.Min(5,(timeline.SelectRange?timeline.End:timeline.Duration)-start);if(duration<0.05)throw new ArgumentException("Move the playhead away from the end.");
            var sample=Path.Combine(temp,Guid.NewGuid().ToString("N")+(kind==FileKind.Audio?".wav":".mp4"));var args=new List<string>{"-hide_banner","-v","error","-nostdin","-n","-ss",Values.Invariant(start),"-protocol_whitelist","file,pipe","-i",PreviewInput,"-t",Values.Invariant(duration)};
            if(kind==FileKind.Video)args.AddRange(["-map","0:v:0","-map","0:a:0?","-vf","pad=ceil(iw/2)*2:ceil(ih/2)*2","-c:v","libx264","-preset","ultrafast","-crf","18","-c:a","aac"]);
            args.Add(sample);await ProcessRunner.Run(engines.Require("FFmpeg"),args,lifetime.Token);var options=Options;options["start"]="0";options["end"]=Values.Invariant(duration);
            var ext=operation.Id=="convert:gif"?"gif":kind==FileKind.Audio?"wav":"mp4";var output=Path.Combine(temp,Guid.NewGuid().ToString("N")+"."+ext);
            await new MediaEngine(engines).Process([sample],operation.Id,options,output,lifetime.Token,new Progress<JobProgress>(p=>{if(!lifetime.IsCancellationRequested)status.Text=p.Message;}));
            if(ext=="gif"){var movie=Path.ChangeExtension(output,".mp4");await new MediaEngine(engines).Process([output],"convert:mp4",new(),movie,lifetime.Token,null);output=movie;}
            lifetime.Token.ThrowIfCancellationRequested();
            var media=new MediaElement{Source=new Uri(output),LoadedBehavior=MediaState.Manual,UnloadedBehavior=MediaState.Manual,Stretch=Stretch.Uniform};media.MediaEnded+=(_,_)=>{media.Position=TimeSpan.Zero;media.Play();};
            var dialog=new Window{Owner=this,Title="Rendered output preview · up to 5 seconds",Width=760,Height=540,Content=media,Background=Brushes.Black,WindowStartupLocation=WindowStartupLocation.CenterOwner};dialog.Loaded+=(_,_)=>media.Play();dialog.Closed+=(_,_)=>media.Close();dialog.ShowDialog();status.Text="Output preview ready. Save creates the full selected result.";
        }catch(OperationCanceledException){}catch(Exception ex){Error("Output preview: "+ex.Message);}finally{rendering=false;if(ready)save.IsEnabled=true;}
    }
}
