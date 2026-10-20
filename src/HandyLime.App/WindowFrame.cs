using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shell;
namespace HandyLime.App;

public static class WindowFrame
{
    static readonly HashSet<Window> framed=[];
    public static void Attach(Window window)
    {
        if(window is WheelWindow||!framed.Add(window))return;
        window.Closed+=(_,_)=>framed.Remove(window);
        window.WindowStyle=WindowStyle.None;
        WindowChrome.SetWindowChrome(window,new WindowChrome{CaptionHeight=46,ResizeBorderThickness=new Thickness(window.ResizeMode==ResizeMode.NoResize?0:6),CornerRadius=new CornerRadius(12),GlassFrameThickness=new Thickness(0),UseAeroCaptionButtons=false});
        var content=window.Content;window.Content=null;
        var root=new Grid();root.RowDefinitions.Add(new(){Height=new GridLength(46)});root.RowDefinitions.Add(new());
        var bar=new DockPanel{LastChildFill=true,Margin=new Thickness(14,0,6,0)};
        var actions=new StackPanel{Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Center};DockPanel.SetDock(actions,Dock.Right);bar.Children.Add(actions);
        void Command(string icon,string tip,Action action){var b=new Button{Content=Icons.Create(icon,15),Width=34,Height=32,Padding=new Thickness(0),BorderThickness=new Thickness(0),ToolTip=tip,Margin=new Thickness(2,0,0,0)};WindowChrome.SetIsHitTestVisibleInChrome(b,true);b.Click+=(_,_)=>action();actions.Children.Add(b);}
        if(window.ResizeMode!=ResizeMode.NoResize){Command("minus","Minimize",()=>window.WindowState=WindowState.Minimized);if(window.ResizeMode==ResizeMode.CanResize||window.ResizeMode==ResizeMode.CanResizeWithGrip)Command("square","Maximize / restore",()=>window.WindowState=window.WindowState==WindowState.Maximized?WindowState.Normal:WindowState.Maximized);}
        Command("x","Close",window.Close);
        var identity=new StackPanel{Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Center};identity.Children.Add(Brand.Mark(24));var title=new TextBlock{FontSize=12,FontWeight=FontWeights.SemiBold,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(10,0,0,0),TextTrimming=TextTrimming.CharacterEllipsis,TextWrapping=TextWrapping.NoWrap};title.Foreground=Theme.Brush("Ink");title.SetBinding(TextBlock.TextProperty,new System.Windows.Data.Binding(nameof(Window.Title)){Source=window});identity.Children.Add(title);bar.Children.Add(identity);
        root.Children.Add(bar);var presenter=new ContentPresenter{Content=content};Grid.SetRow(presenter,1);root.Children.Add(presenter);
        var border=new Border{CornerRadius=new CornerRadius(12),BorderThickness=new Thickness(1),Child=root};border.SetResourceReference(Border.BackgroundProperty,"Canvas");border.SetResourceReference(Border.BorderBrushProperty,"Line");window.Content=border;
        NameButtons(root);
    }
    static void NameButtons(DependencyObject root){for(int i=0;i<System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);i++){var child=System.Windows.Media.VisualTreeHelper.GetChild(root,i);if(child is Button button&&button.Content is StackPanel panel){var text=string.Join(" ",panel.Children.OfType<TextBlock>().Select(t=>t.Text));System.Windows.Automation.AutomationProperties.SetName(button,text);}NameButtons(child);}}
}
