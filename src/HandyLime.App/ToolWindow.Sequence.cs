using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HandyLime.Core;
namespace HandyLime.App;
public sealed partial class ToolWindow
{
    sealed record SequenceClip(string Path,MediaEngine.Info Info,BitmapSource Picture);
    readonly Dictionary<string,SequenceClip> clips=new(StringComparer.OrdinalIgnoreCase);
    ListBox? clipList;
    string? activeClip;
    string PreviewInput=>activeClip??inputs[0];
    bool sequencePlaying;
    async Task ConfigureSequence()
    {
        if(inputs.Count is <2 or >100)throw new ArgumentException("Select 2–100 different videos in the workspace before opening Join.");
        Height=900;MinHeight=760;
        foreach(var path in inputs){
            lifetime.Token.ThrowIfCancellationRequested();status.Text=$"Loading clip {clips.Count+1}/{inputs.Count}…";
            var details=await new MediaEngine(engines).Probe(path,lifetime.Token);if(details.Width<=0||details.Duration<=0)throw new InvalidDataException("Video has no usable frames: "+Path.GetFileName(path));
            var thumbnail=Path.Combine(temp,Guid.NewGuid().ToString("N")+".png");await PreviewEngine.Frame(engines,path,Math.Min(0.1,details.Duration/2),thumbnail,lifetime.Token);
            clips[path]=new(path,details,PreviewBitmap.FromFile(thumbnail));File.Delete(thumbnail);
        }
        activeClip=inputs[0];info=clips[activeClip].Info;canvas.DocumentWidth=info.Width;canvas.DocumentHeight=info.Height;timeline.Duration=timeline.End=info.Duration;
        ConfigureMedia();
        foreach(UIElement child in previewArea.Children)Grid.SetRow(child,Grid.GetRow(child)+1);
        previewArea.RowDefinitions.Insert(0,new(){Height=new GridLength(172)});
        clipList=new ListBox{HorizontalContentAlignment=HorizontalAlignment.Left,BorderThickness=new Thickness(0),Background=Brushes.Transparent,Margin=new Thickness(0,0,0,12)};
        clipList.ItemsPanel=new ItemsPanelTemplate(new FrameworkElementFactory(typeof(WrapPanel)));ScrollViewer.SetHorizontalScrollBarVisibility(clipList,ScrollBarVisibility.Disabled);previewArea.Children.Add(clipList);
        clipList.SelectionChanged+=(_,_)=>{if(clipList.SelectedItem is ListBoxItem item&&item.Tag is string path&&path!=activeClip)ActivateClip(path);};
        Section("Output order");controls.Children.Add(new TextBlock{Text="Read the numbered cards left to right. Select any clip to scrub or play it. Move it with the arrows below; saved output follows these numbers.",Margin=new Thickness(0,0,0,12)});
        var move=new WrapPanel();controls.Children.Add(move);ActionButton(move,"Move earlier",()=>MoveClip(-1));ActionButton(move,"Move later",()=>MoveClip(1));
        var sequence=new Button{Content=Icons.Label("play","Play sequence"),Margin=new Thickness(0,12,0,0)};sequence.Click+=(_,_)=>{StopPlayback();sequencePlaying=true;timeline.Position=0;TogglePlayback();};controls.Children.Add(sequence);
        controls.Children.Add(new TextBlock{Text="Output: 1280 × 720 · 30 fps · stereo. Clips fit without stretching; empty space is padded. Silent clips receive silence.",FontSize=12,Margin=new Thickness(0,14,0,0)});
        RefreshClipCards();summary.Text=$"{inputs.Count} clips · total {TimelineControl.Stamp(clips.Values.Sum(c=>c.Info.Duration))}";
    }
    void RefreshClipCards()
    {
        if(clipList==null)return;var active=activeClip;clipList.Items.Clear();
        for(int i=0;i<inputs.Count;i++){
            var clip=clips[inputs[i]];var card=new StackPanel{Width=194,Margin=new Thickness(3)};
            card.Children.Add(new Image{Source=clip.Picture,Height=87,Stretch=Stretch.Uniform,HorizontalAlignment=HorizontalAlignment.Center});
            card.Children.Add(new TextBlock{Text=$"{i+1:00}  {Path.GetFileName(clip.Path)}",FontWeight=FontWeights.SemiBold,FontSize=11,TextTrimming=TextTrimming.CharacterEllipsis,TextWrapping=TextWrapping.NoWrap,Margin=new Thickness(4,5,4,2)});
            card.Children.Add(new TextBlock{Text=$"{TimelineControl.Stamp(clip.Info.Duration)} · {clip.Info.Width} × {clip.Info.Height}",FontSize=10,Foreground=Theme.Brush("Muted"),Margin=new Thickness(4,0,4,2)});
            var item=new ListBoxItem{Content=card,Tag=clip.Path,ToolTip=clip.Path};clipList.Items.Add(item);if(clip.Path==active)clipList.SelectedItem=item;
        }
    }
    void MoveClip(int delta)
    {
        var index=inputs.IndexOf(activeClip??inputs[0]);var next=index+delta;if(next<0||next>=inputs.Count)return;
        (inputs[index],inputs[next])=(inputs[next],inputs[index]);RefreshClipCards();UpdateSummary();
    }
    void ActivateClip(string path,bool continueSequence=false)
    {
        if(!continueSequence)sequencePlaying=false;
        StopPlayback();refresh?.Cancel();proxyCancellation?.Cancel();playRequested=false;activeClip=path;info=clips[path].Info;timeline.Position=timeline.Start=0;timeline.Duration=timeline.End=info.Duration;timeline.Frames.Clear();timeline.Frames.Add(clips[path].Picture);timeline.InvalidateVisual();
        canvas.DocumentWidth=info.Width;canvas.DocumentHeight=info.Height;canvas.Picture=clips[path].Picture;canvas.InvalidateVisual();mediaOpened=false;proxyAttempted=playbackUnavailable=false;player.Close();player.Source=new Uri(path);player.Pause();SchedulePreview();filmstripTask=LoadFilmstrip();
        summary.Text=$"Clip {inputs.IndexOf(path)+1}/{inputs.Count} · {TimelineControl.Stamp(info.Duration)}\nTotal {TimelineControl.Stamp(clips.Values.Sum(c=>c.Info.Duration))}";
    }
    void PlaybackEnded()
    {
        if(!playing)return;
        var advance=sequencePlaying&&operation.Id=="join-video";StopPlayback();
        if(advance){var index=inputs.IndexOf(PreviewInput)+1;if(index<inputs.Count){ActivateClip(inputs[index],true);if(clipList!=null)clipList.SelectedIndex=index;sequencePlaying=true;resumeSequence=true;}else sequencePlaying=false;}
    }
    bool resumeSequence,playRequested,playbackUnavailable;
}
