using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Globalization;
using HandyLime.Core;

namespace HandyLime.App;

internal static class PreviewBitmap
{
    public static BitmapSource FromBytes(byte[] bytes){using var stream=new MemoryStream(bytes);var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;image.StreamSource=stream;image.EndInit();image.Freeze();return image;}
    public static BitmapSource FromFile(string path)=>FromBytes(File.ReadAllBytes(path));
    public static BitmapSource FromPixels(PreviewPixels pixels){var image=BitmapSource.Create(pixels.Width,pixels.Height,96,96,PixelFormats.Bgra32,null,pixels.Bgra,pixels.Width*4);image.Freeze();return image;}
    public static PreviewPixels ToPixels(BitmapSource image){var converted=new FormatConvertedBitmap(image,PixelFormats.Bgra32,null,0);var bytes=new byte[converted.PixelWidth*converted.PixelHeight*4];converted.CopyPixels(bytes,converted.PixelWidth*4,0);return new(converted.PixelWidth,converted.PixelHeight,bytes);}
}

public sealed class EditorCanvas : FrameworkElement
{
    public BitmapSource? Picture {get;set;}
    public double DocumentWidth {get;set;}=1;
    public double DocumentHeight {get;set;}=1;
    public bool ShowPicture {get;set;}=true;
    public bool RegionEnabled {get;set;}
    public bool CoverRegion {get;set;}
    public bool Annotating {get;set;}
    public double AspectRatio {get;set;}
    public string DrawingTool {get;set;}="arrow";
    public string MarkColor {get;set;}="#B9E44D";
    public double StrokeWidth {get;set;}=4;
    public double FontSizeValue {get;set;}=36;
    public string MarkText {get;set;}="Your label";
    public List<Mark> Marks {get;}=[];
    public int SelectedMark {get;private set;}=-1;
    Rect region;
    public Rect Region {get=>region;set{region=value;InvalidateVisual();}}
    public event Action? Changed;
    readonly Stack<Mark[]> history=[];
    Point down;
    Rect before;
    Mark? beforeMark;
    int handle=-2;
    List<double>? stroke;
    bool drawing;
    public EditorCanvas(){Focusable=true;ClipToBounds=true;MinHeight=180;Cursor=Cursors.Cross;System.Windows.Automation.AutomationProperties.SetName(this,"Image preview. Drag a crop handle or draw an annotation.");}
    Rect ImageRect{
        get{var scale=Math.Min(Math.Max(1,ActualWidth-32)/Math.Max(1,DocumentWidth),Math.Max(1,ActualHeight-32)/Math.Max(1,DocumentHeight));var w=DocumentWidth*scale;var h=DocumentHeight*scale;return new((ActualWidth-w)/2,(ActualHeight-h)/2,w,h);}
    }
    double Scale=>ImageRect.Width/DocumentWidth;
    Point Screen(Point p)=>new(ImageRect.Left+p.X*Scale,ImageRect.Top+p.Y*Scale);
    Point Doc(Point p)=>new(Math.Clamp((p.X-ImageRect.Left)/Scale,0,DocumentWidth),Math.Clamp((p.Y-ImageRect.Top)/Scale,0,DocumentHeight));
    Rect ScreenRect(Rect r)=>new(Screen(r.TopLeft),Screen(r.BottomRight));
    static Brush Orange=>Theme.Brush("Accent");
    Point[] Handles()=>[region.TopLeft,new(region.Left+region.Width/2,region.Top),region.TopRight,new(region.Right,region.Top+region.Height/2),region.BottomRight,new(region.Left+region.Width/2,region.Bottom),region.BottomLeft,new(region.Left,region.Top+region.Height/2)];
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);if(ShowPicture)dc.DrawRoundedRectangle(Theme.Brush("Stage"),null,new Rect(RenderSize),16,16);
        if(Picture!=null && ShowPicture)dc.DrawImage(Picture,ImageRect);
        if(RegionEnabled && region.Width>0){
            var screen=ScreenRect(region);
            if(CoverRegion)dc.DrawRectangle(Brushes.Black,null,screen);
            else dc.DrawGeometry(new SolidColorBrush(Color.FromArgb(125,0,0,0)),null,new CombinedGeometry(GeometryCombineMode.Exclude,new RectangleGeometry(ImageRect),new RectangleGeometry(screen)));
            dc.DrawRectangle(null,new Pen(Orange,2),screen);
            if(!CoverRegion)for(int i=1;i<3;i++){dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(100,255,255,255)),1),new(screen.Left+screen.Width*i/3,screen.Top),new(screen.Left+screen.Width*i/3,screen.Bottom));dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(100,255,255,255)),1),new(screen.Left,screen.Top+screen.Height*i/3),new(screen.Right,screen.Top+screen.Height*i/3));}
            foreach(var point in Handles()){var p=Screen(point);dc.DrawRoundedRectangle(Orange,new Pen(Brushes.White,0.6),new Rect(p.X-5,p.Y-5,10,10),2,2);}
        }
        dc.PushClip(new RectangleGeometry(ImageRect));
        for(var i=0;i<Marks.Count;i++){DrawMark(dc,Marks[i]);if(i==SelectedMark){var bounds=ScreenRect(Bounds(Marks[i]));bounds.Inflate(5,5);dc.DrawRectangle(null,new Pen(Brushes.White,1){DashStyle=DashStyles.Dash},bounds);}}
        dc.Pop();
    }
    void DrawMark(DrawingContext dc,Mark m)
    {
        var brush=(Brush)new BrushConverter().ConvertFromString(m.Color)!;var pen=new Pen(brush,Math.Max(1,m.Width*Scale)){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round};
        var a=Screen(new(m.X,m.Y));var b=Screen(new(m.X2,m.Y2));var r=new Rect(a,b);
        switch(m.Kind){
            case "rectangle":dc.DrawRectangle(null,pen,r);break;
            case "ellipse":dc.DrawEllipse(null,pen,new(r.Left+r.Width/2,r.Top+r.Height/2),r.Width/2,r.Height/2);break;
            case "text":dc.DrawText(new FormattedText(m.Text,CultureInfo.CurrentUICulture,FlowDirection.LeftToRight,new Typeface("Segoe UI"),Math.Max(1,m.FontSize*Scale),brush,VisualTreeHelper.GetDpi(this).PixelsPerDip),a);break;
            case "pen":if(m.Points!=null)for(var i=2;i<m.Points.Length;i+=2)dc.DrawLine(pen,Screen(new(m.Points[i-2],m.Points[i-1])),Screen(new(m.Points[i],m.Points[i+1])));break;
            default:
                dc.DrawLine(pen,a,b);
                if(m.Kind=="arrow"){var angle=Math.Atan2(b.Y-a.Y,b.X-a.X);var size=Math.Max(12,m.Width*4)*Scale;dc.DrawLine(pen,b,new(b.X-size*Math.Cos(angle-0.45),b.Y-size*Math.Sin(angle-0.45)));dc.DrawLine(pen,b,new(b.X-size*Math.Cos(angle+0.45),b.Y-size*Math.Sin(angle+0.45)));}break;
        }
    }
    static Rect Bounds(Mark m){if(m.Kind=="pen"&&m.Points is {} p)return new(new Point(p.Where((_,i)=>i%2==0).Min(),p.Where((_,i)=>i%2==1).Min()),new Point(p.Where((_,i)=>i%2==0).Max(),p.Where((_,i)=>i%2==1).Max()));return m.Kind=="text"?new Rect(m.X,m.Y,Math.Max(m.FontSize,m.Text.Length*m.FontSize*0.55),m.FontSize*1.3):new Rect(new Point(m.X,m.Y),new Point(m.X2,m.Y2));}
    public void Undo(){if(history.TryPop(out var old)){Marks.Clear();Marks.AddRange(old);SelectedMark=-1;Changed?.Invoke();InvalidateVisual();}}
    public void DeleteSelected(){if(SelectedMark>=0&&SelectedMark<Marks.Count){history.Push(Marks.ToArray());Marks.RemoveAt(SelectedMark);SelectedMark=-1;Changed?.Invoke();InvalidateVisual();}}
    public void ResetMarks(){history.Push(Marks.ToArray());Marks.Clear();SelectedMark=-1;Changed?.Invoke();InvalidateVisual();}
    public void ApplyAspect(double ratio){AspectRatio=ratio;if(ratio>0){var w=Math.Min(DocumentWidth,DocumentHeight*ratio);var h=w/ratio;Region=new((DocumentWidth-w)/2,(DocumentHeight-h)/2,w,h);}Changed?.Invoke();}
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);Focus();var p=e.GetPosition(this);if(!ImageRect.Contains(p))return;down=Doc(p);before=region;handle=-2;drawing=true;CaptureMouse();
        if(RegionEnabled){
            var points=Handles();for(var i=0;i<points.Length;i++)if((Screen(points[i])-p).Length<12){handle=i;break;}
            if(handle==-2)handle=region.Contains(down)?-1:8;
        }else if(Annotating){
            history.Push(Marks.ToArray());
            if(DrawingTool=="select"){
                SelectedMark=-1;for(int i=Marks.Count-1;i>=0;i--){var r=Bounds(Marks[i]);r.Inflate(10/Scale,10/Scale);if(r.Contains(down)){SelectedMark=i;break;}}
                beforeMark=SelectedMark>=0?Marks[SelectedMark]:null;
            }else{
                stroke=DrawingTool=="pen"?[down.X,down.Y]:null;
                Marks.Add(new(DrawingTool,down.X,down.Y,down.X,down.Y,MarkColor,StrokeWidth,MarkText,FontSizeValue,stroke?.ToArray()));SelectedMark=Marks.Count-1;
            }
        }
        InvalidateVisual();e.Handled=true;
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);if(!drawing||e.LeftButton!=MouseButtonState.Pressed)return;var p=Doc(e.GetPosition(this));
        if(RegionEnabled){
            if(handle==-1){Region=new(Math.Clamp(before.X+p.X-down.X,0,DocumentWidth-before.Width),Math.Clamp(before.Y+p.Y-down.Y,0,DocumentHeight-before.Height),before.Width,before.Height);}
            else{
                var left=before.Left;var top=before.Top;var right=before.Right;var bottom=before.Bottom;
                if(handle==8){left=down.X;top=down.Y;right=p.X;bottom=p.Y;}
                else{if(handle is 0 or 6 or 7)left=p.X;if(handle is 0 or 1 or 2)top=p.Y;if(handle is 2 or 3 or 4)right=p.X;if(handle is 4 or 5 or 6)bottom=p.Y;}
                var r=new Rect(new Point(Math.Min(left,right),Math.Min(top,bottom)),new Point(Math.Max(left,right),Math.Max(top,bottom)));
                if(AspectRatio>0)r=AspectSelection(p);
                if(r.Width>=1&&r.Height>=1)Region=r;
            }
        }else if(Annotating&&SelectedMark>=0){
            var m=Marks[SelectedMark];
            if(DrawingTool=="select"&&beforeMark is {} original){
                var r=Bounds(original);var dx=Math.Clamp(p.X-down.X,-r.Left,Math.Max(-r.Left,DocumentWidth-r.Right));var dy=Math.Clamp(p.Y-down.Y,-r.Top,Math.Max(-r.Top,DocumentHeight-r.Bottom));
                Marks[SelectedMark]=original with{X=original.X+dx,Y=original.Y+dy,X2=original.X2+dx,Y2=original.Y2+dy,Points=original.Points?.Select((v,i)=>v+(i%2==0?dx:dy)).ToArray()};
            }else if(DrawingTool=="pen"){if(stroke!.Count<20000){stroke.Add(p.X);stroke.Add(p.Y);Marks[SelectedMark]=m with{X2=p.X,Y2=p.Y,Points=stroke.ToArray()};}}
            else Marks[SelectedMark]=m with{X2=p.X,Y2=p.Y};
        }
        Changed?.Invoke();InvalidateVisual();
    }
    Rect AspectSelection(Point point)
    {
        var ratio=AspectRatio;
        if(handle is 1 or 5){
            var anchor=handle==1?before.Bottom:before.Top;var center=before.Left+before.Width/2;
            var height=Math.Min(Math.Abs(point.Y-anchor),Math.Min(handle==1?anchor:DocumentHeight-anchor,2*Math.Min(center,DocumentWidth-center)/ratio));
            return new Rect(center-height*ratio/2,handle==1?anchor-height:anchor,height*ratio,height);
        }
        if(handle is 3 or 7){
            var anchor=handle==7?before.Right:before.Left;var center=before.Top+before.Height/2;
            var width=Math.Min(Math.Abs(point.X-anchor),Math.Min(handle==7?anchor:DocumentWidth-anchor,2*Math.Min(center,DocumentHeight-center)*ratio));
            return new Rect(handle==7?anchor-width:anchor,center-width/ratio/2,width,width/ratio);
        }
        var fixedPoint=handle switch{0=>before.BottomRight,2=>before.BottomLeft,4=>before.TopLeft,6=>before.TopRight,_=>down};
        var left=point.X<fixedPoint.X;var up=point.Y<fixedPoint.Y;
        var wanted=Math.Max(Math.Abs(point.X-fixedPoint.X),Math.Abs(point.Y-fixedPoint.Y)*ratio);
        var w=Math.Min(wanted,Math.Min(left?fixedPoint.X:DocumentWidth-fixedPoint.X,(up?fixedPoint.Y:DocumentHeight-fixedPoint.Y)*ratio));
        return new Rect(left?fixedPoint.X-w:fixedPoint.X,up?fixedPoint.Y-w/ratio:fixedPoint.Y,w,w/ratio);
    }
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e){base.OnMouseLeftButtonUp(e);if(!drawing)return;drawing=false;ReleaseMouseCapture();if(Annotating&&SelectedMark>=0&&Marks[SelectedMark].Kind=="pen"&&Marks[SelectedMark].Points!.Length<4)Marks.RemoveAt(SelectedMark);Changed?.Invoke();InvalidateVisual();e.Handled=true;}
    protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(e.Key==Key.Delete){DeleteSelected();e.Handled=true;}if(e.Key==Key.Z&&Keyboard.Modifiers==ModifierKeys.Control){Undo();e.Handled=true;}}
}
