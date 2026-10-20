using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HandyLime.App;

public sealed class ProgressWindow : Window
{
    readonly StackPanel rows=new();
    readonly Dictionary<ActivityItem,Action> updates=[];
    public ProgressWindow(MainWindow shell)
    {
        Title="HandyLime · Activity";Width=460;Height=400;MinWidth=360;MinHeight=250;
        ShowInTaskbar=true;Topmost=true;
        var area=SystemParameters.WorkArea;Left=Math.Max(area.Left,area.Right-Width-20);Top=Math.Max(area.Top,area.Bottom-Height-20);
        var root=new DockPanel {Margin=new Thickness(22)};Content=root;
        var header=new DockPanel {Margin=new Thickness(0,0,0,18)};DockPanel.SetDock(header,Dock.Top);root.Children.Add(header);
        var button=new Button {Content="Workspace",Padding=new Thickness(10,6,10,6),FontSize=11};DockPanel.SetDock(button,Dock.Right);button.Click+=(_,_)=>((App)Application.Current).ShowShell();header.Children.Add(button);
        header.Children.Add(new TextBlock{Text="Your file, on its way.",FontSize=20,FontWeight=FontWeights.SemiBold,VerticalAlignment=VerticalAlignment.Center});
        root.Children.Add(new ScrollViewer{Content=rows,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});
        Closing+=(_,e)=>{if(!((App)Application.Current).Quitting){e.Cancel=true;Hide();}};
    }
    public void Add(ActivityItem item,Action cancel)
    {
        var stack=new StackPanel();var title=new TextBlock{Text=item.Title,FontWeight=FontWeights.SemiBold,FontSize=14};stack.Children.Add(title);
        var status=new TextBlock{Margin=new Thickness(0,8,0,8),FontSize=12};stack.Children.Add(status);
        var progress=new ProgressBar{Height=6,Foreground=(Brush)FindResource("Accent"),Maximum=1};stack.Children.Add(progress);
        var detail=new TextBlock{FontSize=11,Foreground=Theme.Brush("Muted"),Margin=new Thickness(0,8,0,6)};stack.Children.Add(detail);
        var commands=new WrapPanel();stack.Children.Add(commands);
        var stop=new Button{Content="Cancel",Padding=new Thickness(10,5,10,5),FontSize=12};stop.Click+=(_,_)=>cancel();commands.Children.Add(stop);
        var card=new Border{Style=(Style)FindResource("Card"),Padding=new Thickness(16),Margin=new Thickness(0,0,0,12),Child=stack,Tag=item};rows.Children.Insert(0,card);
        void Update() {
            status.Text=item.Status+(item.Fraction is double value?$" · {value:P0}":"");
            status.Foreground=item.Status=="Failed"?Theme.Brush("Danger"):Theme.Brush("Ink");
            detail.Text=item.Error??item.Detail;
            progress.IsIndeterminate=item.Fraction==null && item.Status is "Queued" or "Working";
            progress.Value=item.Fraction??0;
            stop.Visibility=item.Status is "Queued" or "Working"?Visibility.Visible:Visibility.Collapsed;
            if(item.Status=="Done" && commands.Children.Count==1) {
                foreach(var output in item.Outputs) {
                    var open=new Button{Content="Open result",ToolTip=output,Padding=new Thickness(10,5,10,5),FontSize=12,Margin=new Thickness(0,0,6,0)};
                    open.Click+=(_,_)=>{try{Process.Start(new ProcessStartInfo(output){UseShellExecute=true});}catch(Exception ex){Dialogs.Show(this,ex.Message,"Open result");}};
                    var reveal=new Button{Content="Show in folder",ToolTip=output,Padding=new Thickness(10,5,10,5),FontSize=12};reveal.Click+=(_,_)=>FileActions.Reveal(output);
                    commands.Children.Add(open);commands.Children.Add(reveal);
                }
            }
        }
        updates[item]=Update;Update();Show();
        while(rows.Children.Count>50){if(rows.Children[^1] is Border old&&old.Tag is ActivityItem previous)updates.Remove(previous);rows.Children.RemoveAt(rows.Children.Count-1);}
    }
    public void Refresh(ActivityItem item){if(updates.TryGetValue(item,out var update))update();}
    public void ShowFailure(ActivityItem item){Refresh(item);Show();Activate();}
}

