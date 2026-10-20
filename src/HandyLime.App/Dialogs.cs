using System.Windows;
using System.Windows.Controls;
namespace HandyLime.App;
public static class Dialogs
{
    public static MessageBoxResult Show(string text,string caption)=>Show(null,text,caption,MessageBoxButton.OK);
    public static MessageBoxResult Show(string text,string caption,MessageBoxButton buttons,MessageBoxImage image=MessageBoxImage.None)=>Show(null,text,caption,buttons,image);
    public static MessageBoxResult Show(Window? owner,string text,string caption,MessageBoxButton buttons=MessageBoxButton.OK,MessageBoxImage image=MessageBoxImage.None)
    {
        var window=new Window{Title=caption,Width=510,MinHeight=210,MaxHeight=650,SizeToContent=SizeToContent.Height,ResizeMode=ResizeMode.NoResize,WindowStartupLocation=owner==null?WindowStartupLocation.CenterScreen:WindowStartupLocation.CenterOwner};if(owner?.IsVisible==true)window.Owner=owner;
        var body=new DockPanel{Margin=new Thickness(24)};window.Content=body;var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,22,0,0)};DockPanel.SetDock(actions,Dock.Bottom);body.Children.Add(actions);
        body.Children.Add(new ScrollViewer{MaxHeight=440,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Content=new TextBlock{Text=text,TextWrapping=TextWrapping.Wrap}});
        var result=MessageBoxResult.Cancel;
        void Add(string label,MessageBoxResult value,bool primary=false){var b=new Button{Content=label,Margin=new Thickness(8,0,0,0),IsDefault=primary,IsCancel=value==MessageBoxResult.Cancel};if(primary)b.Style=(Style)Application.Current.FindResource("Primary");b.Click+=(_,_)=>{result=value;window.Close();};actions.Children.Add(b);}
        if(buttons is MessageBoxButton.YesNo or MessageBoxButton.YesNoCancel){Add("No",MessageBoxResult.No);Add("Yes",MessageBoxResult.Yes,true);if(buttons==MessageBoxButton.YesNoCancel)Add("Cancel",MessageBoxResult.Cancel);}
        else{if(buttons==MessageBoxButton.OKCancel)Add("Cancel",MessageBoxResult.Cancel);Add("OK",MessageBoxResult.OK,true);}
        window.ShowDialog();return result;
    }
}
