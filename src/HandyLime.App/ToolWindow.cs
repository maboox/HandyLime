using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using HandyLime.Core;
namespace HandyLime.App;
public sealed partial class ToolWindow : Window
{
    readonly List<string> inputs;
    readonly Operation operation;
    readonly Engines engines;
    readonly Dictionary<string,string> values;
    readonly Dictionary<string,TextBox> boxes=[];
    readonly Dictionary<string,Slider> sliders=[];
    readonly Dictionary<string,(double Min,double Max)> numericBounds=[];
    readonly StackPanel controls=new();
    readonly Grid previewArea=new();
    readonly EditorCanvas canvas=new();
    readonly TimelineControl timeline=new();
    readonly TextBlock status=new(){Foreground=Theme.Brush("Muted"),FontSize=12};
    readonly TextBlock summary=new(){Foreground=Theme.Brush("Muted"),FontSize=12,Margin=new Thickness(0,8,0,12)};
    readonly Button save=new(){Content="Save new copy",IsDefault=true};
    readonly MediaElement player=new(){LoadedBehavior=MediaState.Manual,UnloadedBehavior=MediaState.Manual,Stretch=Stretch.Uniform,Visibility=Visibility.Hidden,Margin=new Thickness(16)};
    readonly Button play=new(){Content=Icons.Label("play","Play"),Height=36,Width=130,MinWidth=92,FontSize=12,Padding=new Thickness(10,0,10,0),Margin=new Thickness(0,0,6,6)};
    readonly DispatcherTimer playback=new(){Interval=TimeSpan.FromMilliseconds(60)};
    readonly DispatcherTimer debounce=new(){Interval=TimeSpan.FromMilliseconds(32)};
    ImagePreviewSession? imageSession;
    long previewVersion;
    readonly CancellationTokenSource lifetime=new();
    readonly SemaphoreSlim previewSerial=new(1,1);
    CancellationTokenSource? refresh;
    readonly string temp=Path.Combine(Path.GetTempPath(),"HandyLime-editor-"+Guid.NewGuid().ToString("N"));
    BitmapSource? original;
    double originalWidth,originalHeight;
    MediaEngine.Info? info;
    bool ready,updating,compare,playing,mediaOpened,proxyAttempted,rendering;
    Task initializationTask=Task.CompletedTask;
    Task pendingPreview=Task.CompletedTask,filmstripTask=Task.CompletedTask,proxyTask=Task.CompletedTask,outputPreviewTask=Task.CompletedTask;
    ListBox? pdfList;
    sealed record PageItem(int Index,int Rotation,BitmapSource Picture);
    readonly FileKind kind;
    Dictionary<string,string>? acceptedOptions;
    public string[] Inputs=>inputs.ToArray();
    public Dictionary<string,string> Options {
        get{
            if(acceptedOptions!=null)return new(acceptedOptions);
            var result=new Dictionary<string,string>(values);
            if(canvas.RegionEnabled){var x=Math.Clamp((int)Math.Round(canvas.Region.X),0,(int)canvas.DocumentWidth-1);var y=Math.Clamp((int)Math.Round(canvas.Region.Y),0,(int)canvas.DocumentHeight-1);result["x"]=x.ToString();result["y"]=y.ToString();result["width"]=Math.Clamp((int)Math.Round(canvas.Region.Width),1,(int)canvas.DocumentWidth-x).ToString();result["height"]=Math.Clamp((int)Math.Round(canvas.Region.Height),1,(int)canvas.DocumentHeight-y).ToString();}
            if(canvas.Annotating)result["marks"]=JsonSerializer.Serialize(canvas.Marks);
            if(info!=null){result["start"]=Values.Invariant(operation.Id=="frame"?timeline.Position:timeline.Start);result["end"]=Values.Invariant(timeline.End);if(operation.Id=="split-video")result["cuts"]=string.Join(',',timeline.Cuts.Select(Values.Invariant));}
            if(pdfList!=null&&operation.Id=="pdf-pages"){var pages=pdfList.Items.Cast<ListBoxItem>().Select(i=>(PageItem)i.Tag).ToArray();result["pages"]=string.Join(',',pages.Select(p=>p.Index+1));result["rotations"]=string.Join(',',pages.Select(p=>p.Rotation));result["rotation"]="0";}
            return result;
        }
    }
    public ToolWindow(string[] paths,Operation operation,Engines engines)
    {
        inputs=paths.ToList();this.operation=operation;this.engines=engines;kind=operation.Id is "zip" or "tar" or "hash" or "extract"?FileKind.Other:Catalog.Kind(paths[0]);values=operation.Fields.ToDictionary(f=>f.Key,f=>f.Default);
        Title=operation.Title+" — HandyLime";Width=1080;Height=790;MinWidth=830;MinHeight=600;WindowStartupLocation=WindowStartupLocation.CenterOwner;Background=Theme.Brush("Canvas");
        var root=new DockPanel{Margin=new Thickness(24)};Content=root;
        var footer=new DockPanel{Margin=new Thickness(0,18,0,0)};DockPanel.SetDock(footer,Dock.Bottom);root.Children.Add(footer);
        var buttons=new StackPanel{Orientation=Orientation.Horizontal};DockPanel.SetDock(buttons,Dock.Right);footer.Children.Add(buttons);
        buttons.Children.Add(new Button{Content="Cancel",IsCancel=true,Margin=new Thickness(0,0,8,0)});save.Style=(Style)FindResource("Primary");save.IsEnabled=false;save.Click+=(_,_)=>Save();buttons.Children.Add(save);footer.Children.Add(status);
        var header=new StackPanel{Margin=new Thickness(0,0,0,18)};DockPanel.SetDock(header,Dock.Top);root.Children.Add(header);
        header.Children.Add(new TextBlock{Text=operation.Title,FontSize=26,FontWeight=FontWeights.SemiBold});header.Children.Add(new TextBlock{Text=Path.GetFileName(paths[0])+(paths.Length>1?$"  ·  {paths.Length} files":""),Foreground=Theme.Brush("Muted"),Margin=new Thickness(0,6,0,0)});
        var body=new Grid();body.ColumnDefinitions.Add(new());body.ColumnDefinitions.Add(new(){Width=new GridLength(300)});root.Children.Add(body);body.Children.Add(previewArea);
        previewArea.RowDefinitions.Add(new());previewArea.RowDefinitions.Add(new(){Height=GridLength.Auto});
        var stage=new Grid{Background=Theme.Brush("Stage")};stage.Children.Add(player);stage.Children.Add(canvas);previewArea.Children.Add(new Border{CornerRadius=new CornerRadius(16),ClipToBounds=true,Child=stage});
        var scroll=new ScrollViewer{Content=controls,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Margin=new Thickness(22,0,0,0)};Grid.SetColumn(scroll,1);body.Children.Add(scroll);
        controls.Children.Add(new TextBlock{Text=operation.Description,Foreground=Theme.Brush("Muted"),FontSize=12,Margin=new Thickness(0,0,0,14)});controls.Children.Add(summary);
        if(paths.Length>1)controls.Children.Add(new TextBlock{Text=operation.IsBatch?"Reorder the selected files below.":"Preview shows the first file. These settings apply to each selected file.",FontSize=11,Foreground=Theme.Brush("Muted"),Margin=new Thickness(0,0,0,10)});
        debounce.Tick+=(_,_)=>{debounce.Stop();pendingPreview=RefreshPreview();};canvas.Changed+=()=>{UpdateRegionFields();UpdateSummary();};
        timeline.Seek+=()=>{StopPlayback();if(timeline.IsMouseCaptured&&kind==FileKind.Video&&operation.Id!="adjust-video"&&timeline.Frames.Count>0){refresh?.Cancel();debounce.Stop();canvas.Picture=timeline.Frames[Math.Min(timeline.Frames.Count-1,(int)(timeline.Position/Math.Max(0.01,timeline.Duration)*timeline.Frames.Count))];canvas.ShowPicture=true;canvas.InvalidateVisual();}else SchedulePreview();};timeline.Changed+=()=>{UpdateTimeFields();UpdateSummary();};
        player.MediaOpened+=async(_,_)=>{mediaOpened=true;if(resumeSequence||playRequested){var opened=player.Source;resumeSequence=playRequested=false;await Dispatcher.Yield(DispatcherPriority.Background);if(ready&&!lifetime.IsCancellationRequested&&mediaOpened&&Equals(opened,player.Source)&&!playing)TogglePlayback();}};
        player.MediaFailed+=(_,e)=>{StopPlayback();mediaOpened=false;if(!proxyAttempted&&ready)proxyTask=BuildPlaybackProxy();else if(ready){playbackUnavailable=true;playRequested=resumeSequence=sequencePlaying=false;Error("Playback unavailable: "+e.ErrorException.Message+" Frame scrubbing and export remain available.");}};player.MediaEnded+=(_,_)=>PlaybackEnded();
        playback.Tick+=(_,_)=>{if(!playing)return;timeline.Position=player.Position.TotalSeconds;timeline.InvalidateVisual();if(timeline.Position>=(timeline.SelectRange?timeline.End:timeline.Duration)-0.03){if(sequencePlaying&&operation.Id=="join-video")PlaybackEnded();else StopPlayback();}};
        Closed+=async(_,_)=>{ready=false;lifetime.Cancel();refresh?.Cancel();debounce.Stop();playback.Stop();player.Close();try{await Task.WhenAll(initializationTask,pendingPreview,filmstripTask,proxyTask,outputPreviewTask);await previewSerial.WaitAsync();imageSession?.Dispose();adjustmentFrame=null;adjustmentPixels=null;frameCache.Clear();timeline.Frames.Clear();canvas.Picture=original=null;clips.Clear();clipList?.Items.Clear();pdfList?.Items.Clear();Content=null;previewSerial.Release();Directory.Delete(temp,true);}catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or OperationCanceledException){};};
        Loaded+=async(_,_)=>{initializationTask=Initialize();await initializationTask;};
    }
    async Task Initialize()
    {
        Directory.CreateDirectory(temp);status.Text="Loading preview…";
        try{
            if(operation.Id is "strip-image" or "strip-media" or "strip-audio"){await ConfigureMetadata();ready=true;save.IsEnabled=true;return;}
            if(kind==FileKind.Image){
                if(operation.Id=="collage"&&inputs.Count is <2 or >100)throw new ArgumentException("Select 2–100 different images in the workspace before opening Collage.");
                var loaded=await Task.Run(()=>{imageSession=new ImagePreviewSession(operation.Id=="collage"?inputs.ToArray():[inputs[0]],lifetime.Token);return PreviewBitmap.FromPixels(imageSession.Original());},lifetime.Token);
                originalWidth=canvas.DocumentWidth=imageSession!.OriginalWidth;originalHeight=canvas.DocumentHeight=imageSession.OriginalHeight;canvas.Picture=original=loaded;ConfigureImage();
            }else if(operation.Id=="join-video"){await ConfigureSequence();}else if(kind is FileKind.Video or FileKind.Audio){
                info=await new MediaEngine(engines).Probe(inputs[0],lifetime.Token);if(info.Duration<=0)throw new InvalidDataException("The media has no usable duration.");
                canvas.DocumentWidth=info.Width>0?info.Width:1200;canvas.DocumentHeight=info.Height>0?info.Height:180;
                timeline.Duration=timeline.End=info.Duration;timeline.SelectRange=operation.Id is "trim-video" or "trim-audio" or "bleep" or "redact-video" or "convert:gif";timeline.Splitting=operation.Id=="split-video";ConfigureMedia();
            }else if(kind==FileKind.Pdf)await ConfigurePdf();else ConfigureFiles();
            if(lifetime.IsCancellationRequested)return;ready=true;save.IsEnabled=true;canvas.InvalidateVisual();UpdateSummary();
            if(kind is FileKind.Video or FileKind.Audio){await RefreshPreview();if(lifetime.IsCancellationRequested)return;player.Source=new Uri(PreviewInput);player.Pause();filmstripTask=LoadFilmstrip();}
            if(kind==FileKind.Image&&!canvas.RegionEnabled&&!canvas.Annotating)SchedulePreview();status.Text="Ready · saves a new copy beside the original";
        }catch(OperationCanceledException){}catch(Exception ex){Error("Preview could not load: "+ex.Message);save.IsEnabled=false;}
    }
    void Section(string text)=>controls.Children.Add(new TextBlock{Text=text,FontWeight=FontWeights.SemiBold,FontSize=14,Margin=new Thickness(0,16,0,10)});
    static void ActionButton(Panel panel,string label,Action action){var icon=label switch{"Previous frame"=>"step-back","Next frame"=>"step-forward","Split here"=>"scissors","Remove marker" or "Delete selected" or "Remove page"=>"x","Move earlier"=>"arrow-left","Move up"=>"arrow-up","Move later"=>"arrow-right","Move down"=>"arrow-down","Undo" or "Reset"=>"undo-2","Rotate 90°"=>"rotate-cw","Duplicate"=>"copy",_=>""};var button=new Button{Content=icon.Length>0?Icons.Label(icon,label):label,Height=36,MinWidth=78,Padding=new Thickness(10,0,10,0),FontSize=12,Margin=new Thickness(0,0,6,6)};if(label is "Previous frame" or "Next frame")button.Width=130;button.Click+=(_,_)=>action();panel.Children.Add(button);}
    void TextField(string key,string label,string value,Action<string>? changed=null)
    {
        values[key]=value;var box=new TextBox{Text=value,Margin=new Thickness(0,5,0,12)};boxes[key]=box;System.Windows.Automation.AutomationProperties.SetName(box,label);controls.Children.Add(new Label{Content=label,Target=box,Padding=new Thickness(0),FontSize=12});controls.Children.Add(box);
        box.TextChanged+=(_,_)=>{if(updating)return;values[key]=box.Text;changed?.Invoke(box.Text);SchedulePreview();};
    }
    void NumericField(string key,string label,double value,double min,double max)
    {
        numericBounds[key]=(min,max);
        TextField(key,label,Values.Invariant(value));boxes[key].TextChanged+=(_,_)=>{
            if(updating||!double.TryParse(boxes[key].Text,NumberStyles.Float,CultureInfo.InvariantCulture,out var n)||!double.IsFinite(n)||n<min||n>max)return;
            if(canvas.RegionEnabled&&key is "width" or "height" or "x" or "y"){
                double V(string name,double fallback)=>double.TryParse(values.GetValueOrDefault(name),NumberStyles.Float,CultureInfo.InvariantCulture,out var v)&&double.IsFinite(v)?v:fallback;
                var x=Math.Clamp(V("x",0),0,canvas.DocumentWidth-1);var y=Math.Clamp(V("y",0),0,canvas.DocumentHeight-1);canvas.Region=new(x,y,Math.Clamp(V("width",canvas.Region.Width),1,canvas.DocumentWidth-x),Math.Clamp(V("height",canvas.Region.Height),1,canvas.DocumentHeight-y));UpdateSummary();
            }
            if(info!=null&&key=="start"){if(operation.Id=="frame")timeline.Position=Math.Min(n,info.Duration-0.03);else{timeline.Start=Math.Min(n,timeline.End-0.05);timeline.Position=timeline.Start;}timeline.InvalidateVisual();SchedulePreview();}
            if(info!=null&&key=="end"){timeline.End=Math.Max(n,timeline.Start+0.05);timeline.InvalidateVisual();}
        };
    }
    void SliderField(string key,string label,double min,double max,double value,double step,Action<double>? changed=null)
    {
        values[key]=Values.Invariant(value);var header=new DockPanel();var reset=new Button{Content=Icons.Create("undo-2",14),ToolTip="Reset "+label,Padding=new Thickness(5,1,5,1),FontSize=12};DockPanel.SetDock(reset,Dock.Right);header.Children.Add(reset);header.Children.Add(new TextBlock{Text=label,FontSize=12,VerticalAlignment=VerticalAlignment.Center});controls.Children.Add(header);
        var row=new Grid{Margin=new Thickness(0,5,0,15)};row.ColumnDefinitions.Add(new());row.ColumnDefinitions.Add(new(){Width=new GridLength(65)});controls.Children.Add(row);
        var slider=new Slider{Minimum=min,Maximum=max,Value=value,TickFrequency=step,IsSnapToTickEnabled=true,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,8,0)};sliders[key]=slider;row.Children.Add(slider);var box=new TextBox{Text=Values.Invariant(value),Padding=new Thickness(5,3,5,3)};boxes[key]=box;Grid.SetColumn(box,1);row.Children.Add(box);
        System.Windows.Automation.AutomationProperties.SetName(slider,label);System.Windows.Automation.AutomationProperties.SetName(box,label+" value");
        slider.ValueChanged+=(_,_)=>{if(updating)return;Set(key,slider.Value);changed?.Invoke(slider.Value);SchedulePreview();};box.TextChanged+=(_,_)=>{if(updating)return;values[key]=box.Text;if(double.TryParse(box.Text,NumberStyles.Float,CultureInfo.InvariantCulture,out var n)&&double.IsFinite(n)&&n>=min&&n<=max){updating=true;slider.Value=n;updating=false;changed?.Invoke(n);SchedulePreview();}};
        slider.PreviewMouseLeftButtonUp+=(_,_)=>SchedulePreview();
        reset.Click+=(_,_)=>{Set(key,value);changed?.Invoke(value);SchedulePreview();};
    }
    void ColorControl(string key,string label,string value,Action<string>? changed=null)
    {
        TextField(key,label,value,text=>{if(System.Text.RegularExpressions.Regex.IsMatch(text,"^#[0-9a-fA-F]{6}$"))changed?.Invoke(text);});var colors=new WrapPanel();controls.Children.Add(colors);foreach(var color in new[]{"#FFFFFF","#111111","#F05A2A","#EA3434","#2D91F2","#45A477","#FFD65B"}){var button=new Button{Width=27,Height=27,Background=(Brush)new BrushConverter().ConvertFromString(color)!,ToolTip=color,Margin=new Thickness(0,0,5,10),Padding=new Thickness(0)};button.Click+=(_,_)=>boxes[key].Text=color;colors.Children.Add(button);}changed?.Invoke(value);
    }
    void Set(string key,double value){updating=true;values[key]=Values.Invariant(value);if(boxes.TryGetValue(key,out var box))box.Text=values[key];if(sliders.TryGetValue(key,out var slider))slider.Value=value;updating=false;UpdateSummary();}
    void UpdateRegionFields(){if(!canvas.RegionEnabled)return;var x=Math.Clamp(Math.Round(canvas.Region.X),0,canvas.DocumentWidth-1);var y=Math.Clamp(Math.Round(canvas.Region.Y),0,canvas.DocumentHeight-1);Set("x",x);Set("y",y);Set("width",Math.Clamp(Math.Round(canvas.Region.Width),1,canvas.DocumentWidth-x));Set("height",Math.Clamp(Math.Round(canvas.Region.Height),1,canvas.DocumentHeight-y));}
    void UpdateTimeFields(){if(info==null)return;if(boxes.ContainsKey("start"))Set("start",operation.Id=="frame"?timeline.Position:timeline.Start);if(boxes.ContainsKey("end"))Set("end",timeline.End);}
    void UpdateSummary()
    {
        if(operation.Id=="join-video"&&clips.Count>0){summary.Text=$"{inputs.Count} clips · total {TimelineControl.Stamp(clips.Values.Sum(c=>c.Info.Duration))}\nPreviewing clip {inputs.IndexOf(PreviewInput)+1}: {Path.GetFileName(PreviewInput)}";return;}
        if(operation.Id=="collage"){
            if(int.TryParse(values.GetValueOrDefault("columns"),out var cols)&&int.TryParse(values.GetValueOrDefault("cell"),out var cell)&&int.TryParse(values.GetValueOrDefault("gap"),out var gap)&&cols>0){cols=Math.Min(inputs.Count,cols);var rows=(inputs.Count+cols-1)/cols;summary.Text=$"{inputs.Count} images · output {cols*(cell+gap)+gap} × {rows*(cell+gap)+gap} px";}return;
        }
        summary.Text=info!=null?$"{TimelineControl.Stamp(info.Duration)}"+(info.Width>0?$" · {info.Width} × {info.Height}":" · audio")+(timeline.Splitting?$"\n{timeline.Cuts.Count+1} output clips":"")+(timeline.SelectRange?$"\nSelected: {TimelineControl.Stamp(timeline.End-timeline.Start)}":""):canvas.Picture!=null?$"Original {originalWidth:0} × {originalHeight:0} px"+(canvas.RegionEnabled?$"\nSelection: {canvas.Region.Width:0} × {canvas.Region.Height:0}":""):"";
    }
    void Error(string message){status.Text=message;status.Foreground=Theme.Brush("Danger");}
    void Save()
    {
        try {
            foreach(var pair in sliders)Values.Number(values,pair.Key,pair.Value.Value,pair.Value.Minimum,pair.Value.Maximum);
            foreach(var pair in numericBounds)Values.Number(values,pair.Key,0,pair.Value.Min,pair.Value.Max);
            foreach(var key in new[]{"width","height","x","y","columns","cell","gap","fps","quality","channels"})if(values.ContainsKey(key)&&double.TryParse(values[key],NumberStyles.Float,CultureInfo.InvariantCulture,out var integer)&&integer!=Math.Truncate(integer))throw new ArgumentException(key+" must be a whole number.");
            if(values.TryGetValue("color",out var color)&&!System.Text.RegularExpressions.Regex.IsMatch(color,"^#[0-9a-fA-F]{6}$"))throw new ArgumentException("Use a six-digit color such as #B9E44D.");
            if(info!=null&&timeline.SelectRange){
                var start=Values.Number(values,"start",0,0,info.Duration);
                var end=Values.Number(values,"end",info.Duration,0,info.Duration);
                if(end-start<0.05)throw new ArgumentException("End must be at least 0.05 seconds after start.");
                timeline.Start=start;timeline.End=end;
            }
            if(info!=null&&operation.Id=="frame"&&Values.Number(values,"start",0,0,info.Duration)>=info.Duration)
                throw new ArgumentException("Frame time must be before the end of the video.");
            if(canvas.RegionEnabled){
                var x=Values.Number(values,"x",0,0,canvas.DocumentWidth-1);var y=Values.Number(values,"y",0,0,canvas.DocumentHeight-1);
                var width=Values.Number(values,"width",canvas.Region.Width,1,canvas.DocumentWidth);var height=Values.Number(values,"height",canvas.Region.Height,1,canvas.DocumentHeight);
                if(x+width>canvas.DocumentWidth||y+height>canvas.DocumentHeight)throw new ArgumentException("The selected rectangle extends beyond the image. Reduce its size or position.");
                canvas.Region=new Rect(x,y,width,height);
            }
            var options=Options;
            if(canvas.Annotating)Editing.ReadMarks(options["marks"],canvas.DocumentWidth,canvas.DocumentHeight);
            if(info!=null&&timeline.Splitting)Editing.SplitPoints(options["cuts"],info.Duration);
            if(pdfList!=null&&operation.Id=="pdf-pages"&&pdfList.Items.Count==0)throw new ArgumentException("Keep at least one page.");
            acceptedOptions=new(options);StopPlayback();DialogResult=true;
        }catch(Exception ex){Error(ex.Message);}
    }
}

