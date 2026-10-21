using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using HandyLime.Core;

namespace HandyLime.App;

public sealed partial class ToolWindow
{
    bool CachedAdjustment => kind == FileKind.Image || operation.Id == "adjust-video";
    BitmapSource? adjustmentFrame;
    PreviewPixels? adjustmentPixels;
    string PlayCaption => operation.Id == "adjust-video" ? "Play original" : "Play";

    void ConfigureAdjustments()
    {
        var compareBox = new CheckBox { Content = "Show original (compare)" };
        compareBox.Click += (_, _) => { compare = compareBox.IsChecked == true; SchedulePreview(); };
        controls.Children.Add(compareBox);
        string? section = null;
        foreach (var p in ColorAdjustments.Parameters) {
            if (p.Group != section) { Section(p.Group); section = p.Group; }
            SliderField(p.Key, p.Label, p.Minimum, p.Maximum, p.Default, p.Step);
        }
        var actions = new WrapPanel(); controls.Children.Add(actions);
        ActionButton(actions, "Reset", () => {
            foreach (var p in ColorAdjustments.Parameters) Set(p.Key, p.Default);
            compare = false; compareBox.IsChecked = false; SchedulePreview();
        });
    }

    async Task RefreshVideoAdjustment(string input, double position, Dictionary<string,string> options, CancellationToken token)
    {
        BitmapSource frame;
        if (timeline.IsMouseCaptured && timeline.Frames.Count > 0) {
            frame = timeline.Frames[Math.Min(timeline.Frames.Count - 1,
                (int)(position / Math.Max(0.01,timeline.Duration) * timeline.Frames.Count))];
        } else {
            var key = (input, (long)Math.Round(position * 1000));
            if (!frameCache.TryGetValue(key, out var cached)) {
                var path = Path.Combine(temp, Guid.NewGuid().ToString("N") + ".png");
                try {
                    await PreviewEngine.Frame(engines,input,Math.Min(position,Math.Max(0,timeline.Duration-0.04)),path,token);
                    token.ThrowIfCancellationRequested();cached = PreviewBitmap.FromFile(path);
                    if (frameCache.Count >= 32) frameCache.Remove(frameCache.Keys.First());
                    frameCache[key] = cached;
                } finally { if (File.Exists(path)) File.Delete(path); }
            }
            frame = cached;
        }
        if (compare) { canvas.Picture = frame; status.Text = "Original frame · comparison"; }
        else {
            if (!ReferenceEquals(adjustmentFrame,frame)) {
                adjustmentFrame = frame; adjustmentPixels = PreviewBitmap.ToPixels(frame);
            }
            var pixels = adjustmentPixels!;
            var interactive = Mouse.LeftButton == MouseButtonState.Pressed;
            var watch = Stopwatch.StartNew();
            var result = await Task.Run(() => PreviewBitmap.FromPixels(VideoAdjustments.Preview(pixels,options,token,interactive)),token);
            token.ThrowIfCancellationRequested();canvas.Picture = result;
            status.Text = $"Frame preview · {watch.ElapsedMilliseconds} ms · save edits the whole video";
        }
        status.SetResourceReference(TextBlock.ForegroundProperty,"Muted");
        canvas.ShowPicture = true; canvas.InvalidateVisual();
    }
}
