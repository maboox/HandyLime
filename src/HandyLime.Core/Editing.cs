using System.Text.Json;

namespace HandyLime.Core;

public sealed record JobProgress(string Message, double? Fraction = null);

// The same document coordinates are used by the editor, preview and saved output.
public sealed record Mark(string Kind, double X, double Y, double X2, double Y2,
    string Color = "#F05A2A", double Width = 4, string Text = "", double FontSize = 36,
    double[]? Points = null);

public static class Editing
{
    public static Mark[] ReadMarks(string json, double width, double height)
    {
        var marks = JsonSerializer.Deserialize<Mark[]>(json) ?? [];
        if (marks.Length > 500) throw new ArgumentException("Use at most 500 annotations.");
        foreach (var m in marks) {
            if (m.Kind is not ("arrow" or "line" or "rectangle" or "ellipse" or "text" or "pen")) throw new ArgumentException("Unknown annotation tool.");
            if (!new[]{m.X,m.Y,m.X2,m.Y2,m.Width,m.FontSize}.All(double.IsFinite) || m.Width is < 1 or > 100 || m.FontSize is < 8 or > 400)
                throw new ArgumentException("Invalid annotation dimensions.");
            if (m.X < 0 || m.Y < 0 || m.X2 < 0 || m.Y2 < 0 || m.X > width || m.X2 > width || m.Y > height || m.Y2 > height)
                throw new ArgumentException("Annotation lies outside the image.");
            if (!System.Text.RegularExpressions.Regex.IsMatch(m.Color,"^#[0-9a-fA-F]{6}$") || m.Text.Length > 2000) throw new ArgumentException("Invalid annotation color or text.");
            if (m.Kind == "pen" && (m.Points == null || m.Points.Length < 4 || m.Points.Length > 20000 || m.Points.Length % 2 != 0)) throw new ArgumentException("Invalid pen stroke.");
            if (m.Points != null)
                for (int i=0; i<m.Points.Length; i++) if (!double.IsFinite(m.Points[i]) || m.Points[i] < 0 || m.Points[i] > (i%2==0?width:height)) throw new ArgumentException("Pen stroke lies outside the image.");
        }
        return marks;
    }
    public static double[] SplitPoints(string text, double duration)
    {
        if (!double.IsFinite(duration) || duration <= 0) throw new ArgumentException("The file has no usable duration.");
        var points = text.Split(',',StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s=>Values.Number(new(){["cut"]=s},"cut",0,0,duration)).Order().ToArray();
        if (points.Length is 0 or > 999) throw new ArgumentException("Add between 1 and 999 split markers.");
        var boundaries = new[]{0d}.Concat(points).Append(duration).ToArray();
        if (boundaries.Zip(boundaries.Skip(1),(a,b)=>b-a).Any(d=>d<0.05)) throw new ArgumentException("Split markers must be at least 0.05 seconds apart and away from the ends.");
        return boundaries;
    }
}
