using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace HandyLime.Core;

public static partial class SubtitleEngine
{
    public record Cue(TimeSpan Start, TimeSpan End, string Text);
    [GeneratedRegex(@"^(?<start>(?:\d{2,}:)?\d{2}:\d{2}[.,]\d{3})\s+-->\s+(?<end>(?:\d{2,}:)?\d{2}:\d{2}[.,]\d{3})(?:\s+.*)?$")]
    private static partial Regex Timing();
    [GeneratedRegex("<[^>]*>")]
    private static partial Regex Tags();
    static TimeSpan ParseTime(string time)
    {
        var parts = time.Replace(',','.').Split(':');
        if (parts.Length == 2) parts = ["0",..parts];
        var hours = int.Parse(parts[0],CultureInfo.InvariantCulture); var minutes = int.Parse(parts[1],CultureInfo.InvariantCulture); var seconds = double.Parse(parts[2],CultureInfo.InvariantCulture);
        if (minutes > 59 || seconds >= 60) throw new InvalidDataException("Invalid subtitle timestamp.");
        return TimeSpan.FromHours(hours)+TimeSpan.FromMinutes(minutes)+TimeSpan.FromSeconds(seconds);
    }
    public static List<Cue> Parse(string text)
    {
        var cues = new List<Cue>();
        var blocks = Regex.Split(text.TrimStart('\uFEFF').Replace("\r\n","\n").Replace('\r','\n').Trim(),@"\n\s*\n");
        foreach (var block in blocks) {
            var lines = block.Split('\n');
            if (lines[0].StartsWith("WEBVTT") || lines[0].StartsWith("NOTE") || lines[0] is "STYLE" or "REGION") continue;
            var index = Array.FindIndex(lines,l => Timing().IsMatch(l));
            if (index < 0 || index+1 >= lines.Length) throw new InvalidDataException("Malformed subtitle cue.");
            var match = Timing().Match(lines[index]);
            var start = ParseTime(match.Groups["start"].Value); var end = ParseTime(match.Groups["end"].Value);
            if (end <= start) throw new InvalidDataException("Subtitle cue end must follow start.");
            cues.Add(new(start,end,string.Join('\n',lines.Skip(index+1))));
        }
        if (cues.Count == 0) throw new InvalidDataException("No subtitle cues found.");
        return cues;
    }
    static string Stamp(TimeSpan time, char separator) => $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}{separator}{time.Milliseconds:000}";
    public static string Convert(string text, string format)
    {
        var cues = Parse(text);
        if (format == "txt") return string.Join(Environment.NewLine,cues.Select(c => System.Net.WebUtility.HtmlDecode(Tags().Replace(c.Text,""))));
        var result = new StringBuilder(format == "vtt" ? "WEBVTT\n\n" : "");
        for (var i=0;i<cues.Count;i++) {
            var cue = cues[i]; if (format == "srt") result.AppendLine((i+1).ToString(CultureInfo.InvariantCulture));
            result.AppendLine($"{Stamp(cue.Start,format == "srt" ? ',' : '.')} --> {Stamp(cue.End,format == "srt" ? ',' : '.')}");
            result.AppendLine(cue.Text).AppendLine();
        }
        return result.ToString();
    }
}
