using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HandyLime.App;

public sealed class TimelineControl : FrameworkElement
{
    public double Duration {get;set;}=1;
    public double Start {get;set;}
    public double End {get;set;}=1;
    public double Position {get;set;}
    public bool SelectRange {get;set;}
    public bool Splitting {get;set;}
    public List<double> Cuts {get;}=[];
    public int SelectedCut {get;private set;}=-1;
    public List<BitmapSource> Frames {get;}=[];
    public event Action? Seek;
    public event Action? Changed;
    int drag=-4;
    public TimelineControl(){Height=126;MinWidth=200;Focusable=true;Cursor=Cursors.Hand;System.Windows.Automation.AutomationProperties.SetName(this,"Timeline. Drag the playhead, orange range handles, or split markers.");}
    double X(double time)=>14+Math.Clamp(time/Math.Max(0.001,Duration),0,1)*Math.Max(1,ActualWidth-28);
    double Time(double x)=>Math.Clamp((x-14)/Math.Max(1,ActualWidth-28)*Duration,0,Duration);
    static Brush Accent=>Theme.Brush("Accent");
    public static string Stamp(double seconds)=>TimeSpan.FromSeconds(Math.Max(0,seconds)).ToString(seconds>=3600?"h\\:mm\\:ss\\.ff":"mm\\:ss\\.ff",CultureInfo.InvariantCulture);
    void Text(DrawingContext dc,string value,Point point,Brush? brush=null)=>dc.DrawText(new FormattedText(value,CultureInfo.CurrentUICulture,FlowDirection.LeftToRight,new Typeface("Segoe UI"),11,brush??Theme.Brush("Muted"),VisualTreeHelper.GetDpi(this).PixelsPerDip),point);
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);dc.DrawRectangle(Brushes.Transparent,null,new Rect(RenderSize));
        var track=new Rect(14,27,Math.Max(1,ActualWidth-28),58);dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(39,46,44)),null,track,8,8);
        dc.PushClip(new RectangleGeometry(track,8,8));
        if(Frames.Count>0)for(int i=0;i<Frames.Count;i++)dc.DrawImage(Frames[i],new Rect(14+track.Width*i/Frames.Count,27,track.Width/Frames.Count+1,58));
        if(SelectRange){dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(170,0,0,0)),null,new Rect(14,27,Math.Max(0,X(Start)-14),58));dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(170,0,0,0)),null,new Rect(X(End),27,Math.Max(0,ActualWidth-14-X(End)),58));}
        dc.Pop();
        if(SelectRange){dc.DrawRectangle(null,new Pen(Accent,2),new Rect(X(Start),27,Math.Max(1,X(End)-X(Start)),58));foreach(var time in new[]{Start,End}){dc.DrawRoundedRectangle(Accent,null,new Rect(X(time)-5,26,10,60),3,3);dc.DrawLine(new Pen(Theme.Brush("AccentInk"),1),new(X(time),47),new(X(time),65));}}
        for(int i=0;i<Cuts.Count;i++){var x=X(Cuts[i]);var brush=i==SelectedCut?Accent:Brushes.DeepSkyBlue;dc.DrawLine(new Pen(brush,2),new(x,17),new(x,88));dc.DrawRoundedRectangle(brush,null,new Rect(x-8,6,16,17),3,3);Text(dc,(i+1).ToString(),new(x-3,6),i==SelectedCut?Theme.Brush("AccentInk"):Brushes.Black);}
        var pos=X(Position);dc.DrawLine(new Pen(Brushes.White,4),new(pos,25),new(pos,87));dc.DrawLine(new Pen(Brushes.Black,1),new(pos,25),new(pos,87));
        dc.DrawEllipse(Accent,new Pen(Brushes.White,1),new(pos,92),5,5);
        Text(dc,"0:00",new(14,105));Text(dc,Stamp(Position)+" / "+Stamp(Duration),new(Math.Max(70,ActualWidth-160),105));
    }
    public void AddCut(){if(Position>0.05&&Position<Duration-0.05&&!Cuts.Any(c=>Math.Abs(c-Position)<0.05)){Cuts.Add(Position);Cuts.Sort();SelectedCut=Cuts.IndexOf(Position);Changed?.Invoke();InvalidateVisual();}}
    public void RemoveCut(){if(SelectedCut>=0&&SelectedCut<Cuts.Count){Cuts.RemoveAt(SelectedCut);SelectedCut=-1;Changed?.Invoke();InvalidateVisual();}}
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);Focus();var p=e.GetPosition(this);drag=-1;
        if(SelectRange&&p.Y>=25&&p.Y<=86){if(Math.Abs(p.X-X(Start))<12)drag=-2;else if(Math.Abs(p.X-X(End))<12)drag=-3;}
        if(Splitting&&p.Y<26){for(int i=0;i<Cuts.Count;i++)if(Math.Abs(p.X-X(Cuts[i]))<12){drag=i;SelectedCut=i;break;}}
        CaptureMouse();Move(p.X);e.Handled=true;
    }
    void Move(double x)
    {
        var value=Time(x);
        if(drag==-2){Start=Math.Min(value,Math.Max(0,End-0.05));Position=Start;}
        else if(drag==-3){End=Math.Max(value,Math.Min(Duration,Start+0.05));Position=Math.Max(0,End-0.03);}
        else if(drag>=0){Cuts[drag]=Math.Clamp(value,0.05,Math.Max(0.05,Duration-0.05));Position=Cuts[drag];}
        else Position=Math.Min(value,Math.Max(0,Duration-0.03));
        InvalidateVisual();Seek?.Invoke();Changed?.Invoke();
    }
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(IsMouseCaptured&&e.LeftButton==MouseButtonState.Pressed)Move(e.GetPosition(this).X);}
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e){base.OnMouseLeftButtonUp(e);ReleaseMouseCapture();if(drag>=0){var selected=Cuts[drag];Cuts.Sort();SelectedCut=Cuts.IndexOf(selected);}drag=-4;InvalidateVisual();Seek?.Invoke();}
    protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(e.Key==Key.Delete)RemoveCut();else if(e.Key is Key.Left or Key.Right){Position=Math.Clamp(Position+(e.Key==Key.Left?-1:1)*(Keyboard.Modifiers==ModifierKeys.Shift?1:0.04),0,Math.Max(0,Duration-0.03));Seek?.Invoke();InvalidateVisual();e.Handled=true;}}
}
