using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Linq;
namespace HandyLime.App;

public static class Icons
{
    static readonly Dictionary<string,Geometry> cache=[];
    public static string Name(string id)=>id switch{
        "adjust"=>"sliders-horizontal","annotate"=>"pencil","background"=>"image","collage"=>"images","rotate"=>"rotate-cw","flip"=>"arrow-left-right","resize"=>"expand","speed"=>"gauge","frame"=>"camera","join-video" or "pdf-merge"=>"combine","pdf-pages"=>"files","visualizer"=>"activity","normalize" or "channels" or "bleep"=>"volume-2","zip" or "tar" or "extract"=>"archive","action:float"=>"files","action:copy"=>"copy","action:copyto"=>"folder","action:move"=>"move","action:path"=>"clipboard","action:reveal"=>"folder-open","action:terminal"=>"terminal",
        var x when x.Contains("crop")=>"crop",var x when x.Contains("strip")=>"file-search",var x when x.Contains("compress")=>"shrink",var x when x.Contains("split")||x.Contains("trim")=>"scissors",var x when x.Contains("redact")=>"eye-off",_=>id
    };
    public static Viewbox Create(string id,double size=18)
    {
        var name=Name(id);if(!cache.TryGetValue(name,out var geometry)){
            var uri=new Uri($"pack://application:,,,/Assets/Lucide/{name}.svg");
            var resource=Application.GetResourceStream(uri)??Application.GetResourceStream(new Uri("pack://application:,,,/Assets/Lucide/file.svg"));
            using var stream=resource!.Stream;var doc=XDocument.Load(stream);var group=new GeometryGroup();
            foreach(var item in doc.Root!.Elements()){
                double N(string key,double fallback=0)=>double.TryParse((string?)item.Attribute(key),NumberStyles.Float,CultureInfo.InvariantCulture,out var value)?value:fallback;
                Geometry? part=item.Name.LocalName switch{
                    "path"=>Geometry.Parse((string)item.Attribute("d")!),
                    "circle"=>new EllipseGeometry(new Point(N("cx"),N("cy")),N("r"),N("r")),
                    "ellipse"=>new EllipseGeometry(new Point(N("cx"),N("cy")),N("rx"),N("ry")),
                    "rect"=>new RectangleGeometry(new Rect(N("x"),N("y"),N("width"),N("height")),N("rx"),N("ry",N("rx"))),
                    "line"=>new LineGeometry(new Point(N("x1"),N("y1")),new Point(N("x2"),N("y2"))),
                    "polyline" or "polygon"=>Geometry.Parse("M"+(string?)item.Attribute("points")+(item.Name.LocalName=="polygon"?"Z":"")),_=>null};
                if(part!=null)group.Children.Add(part);
            }
            group.Freeze();cache[name]=geometry=group;
        }
        var path=new System.Windows.Shapes.Path{Data=geometry,StrokeThickness=1.8,StrokeLineJoin=PenLineJoin.Round,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round,Width=24,Height=24};path.SetResourceReference(System.Windows.Shapes.Shape.StrokeProperty,"Ink");
        return new Viewbox{Width=size,Height=size,Child=path,Focusable=false,IsHitTestVisible=false};
    }
    public static StackPanel Label(string icon,string text,double size=17){var row=new StackPanel{Orientation=Orientation.Horizontal};var drawing=Create(icon,size);if(drawing.Child is System.Windows.Shapes.Path path)path.SetBinding(System.Windows.Shapes.Shape.StrokeProperty,ForegroundBinding());drawing.Margin=new Thickness(0,0,8,0);row.Children.Add(drawing);var label=new TextBlock{Text=text,VerticalAlignment=VerticalAlignment.Center};label.SetBinding(TextBlock.ForegroundProperty,ForegroundBinding());row.Children.Add(label);return row;}
    static System.Windows.Data.Binding ForegroundBinding()=>new("Foreground"){RelativeSource=new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.FindAncestor,typeof(Button),1)};
}
