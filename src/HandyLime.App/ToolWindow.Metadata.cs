using System.Windows;
using System.Windows.Controls;
using HandyLime.Core;
namespace HandyLime.App;
public sealed partial class ToolWindow
{
    async Task ConfigureMetadata()
    {
        previewArea.Children.Clear();var rows=await MetadataEngine.Read(inputs[0],engines,lifetime.Token);
        var panel=new DockPanel();var search=new TextBox{Margin=new Thickness(0,0,0,12),Tag="Find a property or value",ToolTip="Filter metadata properties or values"};System.Windows.Automation.AutomationProperties.SetName(search,"Filter metadata");DockPanel.SetDock(search,Dock.Top);panel.Children.Add(search);
        var list=new StackPanel();var scroll=new ScrollViewer{Content=list,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};panel.Children.Add(scroll);previewArea.Children.Add(panel);
        void Show(string filter){list.Children.Clear();foreach(var row in rows.Where(x=>x.Property.Contains(filter,StringComparison.OrdinalIgnoreCase)||x.Value.Contains(filter,StringComparison.OrdinalIgnoreCase))){var grid=new Grid{Margin=new Thickness(0,0,0,1)};grid.SetResourceReference(BackgroundProperty,"Surface");grid.ColumnDefinitions.Add(new(){Width=new GridLength(210)});grid.ColumnDefinitions.Add(new());var key=new TextBlock{Text=row.Property,Margin=new Thickness(12),FontSize=12};key.SetResourceReference(TextBlock.ForegroundProperty,"Muted");grid.Children.Add(key);var value=new TextBox{Text=row.Value,IsReadOnly=true,BorderThickness=new Thickness(0),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(4),Background=System.Windows.Media.Brushes.Transparent};Grid.SetColumn(value,1);grid.Children.Add(value);list.Children.Add(grid);}}
        search.TextChanged+=(_,_)=>Show(search.Text);Show("");
        summary.Text=$"{rows.Length} properties · {inputs.Count} selected file(s)";
        controls.Children.Add(new TextBlock{Text="Inspect and copy the metadata values on the left. Filter by property or value above. Saving creates a copy with removable tags/profiles stripped. Technical codec properties remain necessary for playback.",Margin=new Thickness(0,12,0,12)});
        controls.Children.Add(new TextBlock{Text="For multiple inputs, this table describes the first file. Clean copies are saved separately for every selected file."});
        save.Content="Save clean copy";status.Text="Metadata loaded · original unchanged";
    }
}
