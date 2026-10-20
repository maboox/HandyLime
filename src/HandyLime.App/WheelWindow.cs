using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using HandyLime.Core;

namespace HandyLime.App;

public sealed class WheelWindow : Window
{
    readonly App app;
    readonly Canvas canvas=new(){Background=Brushes.Transparent};
    readonly bool clickable;
    string[] paths;
    int mode,page;
    readonly List<(System.Windows.Shapes.Path Shape,StackPanel Label,Operation Operation)> sectors=[];
    readonly TextBlock centerLabel=new(){FontSize=12,FontWeight=FontWeights.SemiBold,TextAlignment=TextAlignment.Center,Width=98,IsHitTestVisible=false};
    readonly TextBlock hint=new(){FontSize=12,TextAlignment=TextAlignment.Center,Width=310,IsHitTestVisible=false};
    Operation? hovered;
    bool finishing;
    BitmapSource? thumbnail;
    readonly CancellationTokenSource lifetime=new();
    public int Mode=>mode;
    static Brush Ink=>Theme.Brush("Ink");
    static Brush Accent=>Theme.Brush("Accent");
    public WheelWindow(App app,int mode,string[]? paths=null)
    {
        Style=null;this.app=app;this.mode=mode;this.paths=paths??[];clickable=paths!=null;
        Width=360;Height=420;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;AllowsTransparency=true;Background=Brushes.Transparent;
        ShowInTaskbar=false;Topmost=true;ShowActivated=clickable;AllowDrop=true;Content=canvas;Title="HandyLime wheel";
        DragEnter+=Entered;DragOver+=Over;Drop+=Dropped;
        MouseMove+=(_,e)=>Highlight(e.GetPosition(canvas));
        MouseLeftButtonUp+=async(_,e)=>{if(clickable){if(hovered is {} op)await Apply(op);else if((e.GetPosition(canvas)-new Point(180,180)).Length<50)Close();}};
        PreviewKeyDown+=(_,e)=>{
            var key=e.Key==Key.System?e.SystemKey:e.Key;
            if(key==Key.Escape){Close();e.Handled=true;}
            else if(key==Key.Tab){NextPage();e.Handled=true;}
            else if(!e.IsRepeat && clickable){
                int next=key switch{Key.LeftShift or Key.RightShift=>0,Key.LeftAlt or Key.RightAlt=>1,Key.LeftCtrl or Key.RightCtrl=>2,_=>-1};
                if(next>=0){ToggleMode(next);e.Handled=true;}
            }
        };
        Closed+=(_,_)=>lifetime.Cancel();Render();Loaded+=async(_,_)=>await LoadThumbnail();
    }
    public void ToggleMode(int value){if(mode==value)Close();else{mode=value;page=0;Render();}}
    public void NextPage(){page++;Render();}
    public void OpenAtPointer()
    {
        Native.GetCursorPos(out var p);Left=0;Top=0;Show();
        var transform=PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice??Matrix.Identity;
        var pointer=transform.Transform(new Point(p.X,p.Y));
        var screen=System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(p.X,p.Y)).WorkingArea;
        var min=transform.Transform(new Point(screen.Left,screen.Top));var max=transform.Transform(new Point(screen.Right,screen.Bottom));
        Left=Math.Clamp(pointer.X-180,min.X,Math.Max(min.X,max.X-Width));Top=Math.Clamp(pointer.Y-180,min.Y,Math.Max(min.Y,max.Y-Height));
        Native.SetWindowPos(new WindowInteropHelper(this).Handle,new IntPtr(-1),0,0,0,0,0x0001|0x0002|0x0010);
        if(clickable)Activate();
    }
    async Task LoadThumbnail()
    {
        var path=paths.FirstOrDefault();if(path==null||Catalog.Kind(path)!=FileKind.Image||thumbnail!=null)return;
        try{
            var bitmap=await Task.Run(()=>{using var image=ImageEngine.Read(path);image.Thumbnail(160,160);return PreviewBitmap.FromBytes(image.ToByteArray(ImageMagick.MagickFormat.Png));},lifetime.Token);
            if(!lifetime.IsCancellationRequested && path==paths.FirstOrDefault()){thumbnail=bitmap;Render();}
        }catch(Exception){/* A damaged thumbnail must never prevent a file drop. */}
    }
    void Entered(object sender,DragEventArgs e)
    {
        if(e.Data.GetData(DataFormats.FileDrop) is string[] incoming && !paths.SequenceEqual(incoming)){paths=incoming;page=0;thumbnail=null;Render();_ = LoadThumbnail();}
        Over(sender,e);
    }
    void Over(object sender,DragEventArgs e){Highlight(e.GetPosition(canvas));e.Effects=paths.Length>0?DragDropEffects.Copy:DragDropEffects.None;e.Handled=true;}
    void Dropped(object sender,DragEventArgs e)
    {
        e.Effects=DragDropEffects.Copy;e.Handled=true;Highlight(e.GetPosition(canvas));
        if(hovered is {} operation){finishing=true;Hide();_ = Dispatcher.InvokeAsync(async()=>await Apply(operation));}else Close();
    }
    async Task Apply(Operation op)
    {
        if(finishing&&IsVisible)return;
        finishing=true;var selected=paths.ToArray();Close();
        if(op.Id.StartsWith("action:"))await FileActions.Run(app,selected,op);else await app.Shell.StartOperation(selected,op,false);
    }
    static Point P(double r,double a)=>new(180+r*Math.Cos(a),180+r*Math.Sin(a));
    static Geometry Slice(double a,double b)
    {
        const double inner=58,outer=155;
        var geometry=new StreamGeometry();using(var c=geometry.Open()){
            c.BeginFigure(P(inner+7,a),true,true);c.LineTo(P(outer-12,a),true,false);
            c.QuadraticBezierTo(P(outer,a),P(outer,a+0.075),true,false);
            c.ArcTo(P(outer,b-0.075),new Size(outer,outer),0,b-a>Math.PI,SweepDirection.Clockwise,true,false);
            c.QuadraticBezierTo(P(outer,b),P(outer-12,b),true,false);c.LineTo(P(inner+7,b),true,false);
            c.QuadraticBezierTo(P(inner,b),P(inner,b-0.1),true,false);
            c.ArcTo(P(inner,a+0.1),new Size(inner,inner),0,b-a>Math.PI,SweepDirection.Counterclockwise,true,false);
            c.QuadraticBezierTo(P(inner,a),P(inner+7,a),true,false);
        }geometry.Freeze();return geometry;
    }
    static string ShortTitle(Operation op)=>op.Id.StartsWith("convert:")?op.Title:op.Id switch{
        "compress-image" or "compress-video"=>"COMPRESS","crop-image" or "crop-video"=>"CROP","adjust"=>"EDIT","annotate"=>"ANNOTATE",
        "redact-image" or "redact-video"=>"COVER","background"=>"ADD BG","strip-image" or "strip-media" or "strip-audio"=>"METADATA",
        "trim-video" or "trim-audio"=>"TRIM","split-video"=>"SPLIT","join-video"=>"JOIN","frame"=>"FRAME",
        _=>op.Title.ToUpperInvariant().Replace(" IMAGE","").Replace(" VIDEO","")};
    void Render()
    {
        if(centerLabel.Parent is Border oldCenter)oldCenter.Child=null;
        if(hint.Parent is Border oldHint)oldHint.Child=null;
        canvas.Children.Clear();sectors.Clear();hovered=null;
        var options=mode==2?FileActions.All.ToList():Catalog.ForSelection(paths,mode==1);
        var pages=Math.Max(1,(options.Count+7)/8);page%=pages;var visible=options.Skip(page*8).Take(8).ToArray();
        var background=new Ellipse{Width=330,Height=330,Fill=Theme.Brush("Subtle"),Stroke=Theme.Brush("Line"),StrokeThickness=1,Effect=new DropShadowEffect{BlurRadius=15,ShadowDepth=3,Opacity=0.15},IsHitTestVisible=false};
        Canvas.SetLeft(background,15);Canvas.SetTop(background,15);canvas.Children.Add(background);
        for(int i=0;i<visible.Length;i++){
            var step=2*Math.PI/Math.Max(visible.Length,3);var a=-Math.PI/2-step/2+i*step+0.015;var b=a+step-0.03;
            var shape=new System.Windows.Shapes.Path{Data=Slice(a,b),Fill=SectorFill(),Stroke=Theme.Brush("Line"),StrokeThickness=0.8,IsHitTestVisible=false};canvas.Children.Add(shape);
            var p=P(108,(a+b)/2);var label=new StackPanel{Width=86,IsHitTestVisible=false};
            if(mode!=0){var icon=Icons.Create(visible[i].Id,20);icon.HorizontalAlignment=HorizontalAlignment.Center;icon.Margin=new Thickness(0,0,0,5);label.Children.Add(icon);}
            label.Children.Add(new TextBlock{Text=ShortTitle(visible[i]),Foreground=Ink,FontSize=mode==0?15:9.5,FontWeight=FontWeights.SemiBold,TextAlignment=TextAlignment.Center});
            Canvas.SetLeft(label,p.X-43);Canvas.SetTop(label,p.Y-(mode==0?11:22));canvas.Children.Add(label);sectors.Add((shape,label,visible[i]));
        }
        var disk=new Border{Width=96,Height=96,CornerRadius=new CornerRadius(48),Background=Theme.Brush("Surface"),BorderBrush=Theme.Brush("Line"),BorderThickness=new Thickness(1),Clip=new EllipseGeometry(new Point(48,48),48,48),IsHitTestVisible=false};
        if(thumbnail!=null)disk.Child=new Image{Source=thumbnail,Stretch=Stretch.UniformToFill};
        Canvas.SetLeft(disk,132);Canvas.SetTop(disk,132);canvas.Children.Add(disk);
        var badge=new Border{Width=100,Padding=new Thickness(2,7,2,7),CornerRadius=new CornerRadius(15),Background=Theme.Brush("Surface"),Child=centerLabel,IsHitTestVisible=false};
        centerLabel.Text=paths.Length==0?"Drop here":paths.Length>1?$"{paths.Length} files":FormatSize(paths[0]);centerLabel.Foreground=Ink;
        Canvas.SetLeft(badge,130);Canvas.SetTop(badge,162);canvas.Children.Add(badge);
        var caption=new Border{Width=330,Background=Theme.Brush("Surface"),CornerRadius=new CornerRadius(15),Padding=new Thickness(10,9,10,9),Child=hint,IsHitTestVisible=false};
        hint.Text=visible.Length==0?(paths.Length==0?"Bring the dragged file over the wheel":"No common operations · try another mode"):$"{(clickable?"Click":"Drop")} to apply · tap {KeyName()} again to close";hint.Foreground=Ink;
        Canvas.SetLeft(caption,15);Canvas.SetTop(caption,354);canvas.Children.Add(caption);
        if(pages>1){var next=new Button{Content=$"‹  {page+1} / {pages}  ›",Padding=new Thickness(6,2,6,2),FontSize=10,Background=Theme.Brush("Surface"),ToolTip="Next page (Tab)"};next.Click+=(_,e)=>{NextPage();e.Handled=true;};Canvas.SetLeft(next,144);Canvas.SetTop(next,238);canvas.Children.Add(next);}
    }
    string KeyName()=>mode switch{0=>"Shift",1=>"Alt",_=>"Ctrl"};
    static Brush SectorFill()=>Theme.Brush("Surface");
    static string FormatSize(string path){try{var size=new FileInfo(path).Length;return size>=1024*1024?$"{size/1048576d:0.#} MB":$"{size/1024d:0.#} KB";}catch{return "Files";}}
    void Highlight(Point point)
    {
        hovered=null;
        foreach(var sector in sectors){var hit=sector.Shape.Data.FillContains(point);if(hit)hovered=sector.Operation;sector.Shape.Fill=hit?Accent:SectorFill();
            foreach(var label in sector.Label.Children.OfType<TextBlock>())label.Foreground=hit?Theme.Brush("AccentInk"):Ink;
            foreach(var icon in sector.Label.Children.OfType<Viewbox>())if(icon.Child is System.Windows.Shapes.Path path)path.Stroke=hit?Theme.Brush("AccentInk"):Ink;
        }
        centerLabel.Text=hovered==null?(paths.Length==0?"Drop here":paths.Length>1?$"{paths.Length} files":FormatSize(paths[0])):ShortTitle(hovered);
        hint.Text=hovered!=null?hovered.Title+(app.Engines.Missing(hovered).Length>0?" · setup needed":""):$"{(clickable?"Click":"Drop")} to apply · tap {KeyName()} again to close";
    }
}
