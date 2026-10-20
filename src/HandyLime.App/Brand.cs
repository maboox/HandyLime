using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace HandyLime.App;
public static class Brand
{
    public static void Export(string directory)
    {
        Directory.CreateDirectory(directory);var data=new List<(int Size,byte[] Png)>();
        foreach(var size in new[]{16,24,32,48,64,128,256}){
            var visual=new DrawingVisual();using(var dc=visual.RenderOpen())dc.DrawImage(Drawing(),new Rect(0,0,size,size));
            var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap(size,size,96,96,PixelFormats.Pbgra32);bitmap.Render(visual);
            var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));using var stream=new MemoryStream();encoder.Save(stream);data.Add((size,stream.ToArray()));
        }
        File.WriteAllBytes(Path.Combine(directory,"handylime.png"),data[^1].Png);
        using var icon=File.Create(Path.Combine(directory,"handylime.ico"));using var writer=new BinaryWriter(icon);writer.Write((ushort)0);writer.Write((ushort)1);writer.Write((ushort)data.Count);var offset=6+16*data.Count;
        foreach(var item in data){writer.Write((byte)(item.Size==256?0:item.Size));writer.Write((byte)(item.Size==256?0:item.Size));writer.Write((byte)0);writer.Write((byte)0);writer.Write((ushort)1);writer.Write((ushort)32);writer.Write(item.Png.Length);writer.Write(offset);offset+=item.Png.Length;}
        foreach(var item in data)writer.Write(item.Png);
    }
    // Original HandyLime fruit mark, distinct from the third-party UI icon set.
    public static Image Mark(double size=40)=>new(){Width=size,Height=size,Source=Drawing(),Stretch=Stretch.Uniform};
    public static DrawingImage Drawing()
    {
        var group=new DrawingGroup();using(var dc=group.Open()){
            var dark=new SolidColorBrush(Color.FromRgb(29,49,12));var lime=new SolidColorBrush(Color.FromRgb(193,236,98));
            dc.DrawRoundedRectangle(dark,null,new Rect(0,0,64,64),17,17);
            dc.DrawGeometry(lime,null,Geometry.Parse("M14,35 C14,20 26,14 38,18 C50,22 54,34 47,45 C40,56 23,54 17,44 Z"));
            dc.DrawGeometry(new SolidColorBrush(Color.FromRgb(232,255,180)),null,Geometry.Parse("M20,35 C20,25 28,21 36,24 C45,27 47,35 42,42 C37,49 27,49 23,42 Z"));
            var pen=new Pen(dark,2.8){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round};
            dc.DrawLine(pen,new Point(32,35),new Point(22,30));dc.DrawLine(pen,new Point(32,35),new Point(34,24));dc.DrawLine(pen,new Point(32,35),new Point(44,35));dc.DrawLine(pen,new Point(32,35),new Point(35,46));dc.DrawLine(pen,new Point(32,35),new Point(24,43));
            dc.DrawGeometry(lime,null,Geometry.Parse("M34,16 C36,7 46,6 51,10 C47,18 41,21 34,16 Z"));
        }group.Freeze();var image=new DrawingImage(group);image.Freeze();return image;
    }
}
