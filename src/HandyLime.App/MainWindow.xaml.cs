using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using HandyLime.Core;
using Microsoft.Win32;

namespace HandyLime.App;

public record FileItem(string Path)
{
    public string Name => System.IO.Path.GetFileName(Path);
    public string Badge => Directory.Exists(Path) ? "DIR" : Catalog.Extension(Path).ToUpperInvariant()[..Math.Min(4,Catalog.Extension(Path).Length)];
    public string Detail => Directory.Exists(Path) ? "Folder" : $"{Catalog.Kind(Path)} · {new FileInfo(Path).Length / 1024d:N1} KB";
}
public class ActivityItem
{
    public string Title {get;set;} = "";
    public string Status {get;set;} = "Queued";
    public string[] Outputs {get;set;} = [];
    public string? Error {get;set;}
    public string Detail {get;set;}="Waiting to start…";
    public double? Fraction {get;set;}
}
public partial class MainWindow : Window
{
    readonly App app;
    readonly ObservableCollection<FileItem> files = [];
    readonly List<ActivityItem> activity = [];
    readonly SemaphoreSlim serial = new(1,1);
    ProgressWindow? notifications;
    readonly System.Windows.Threading.DispatcherTimer activityRefresh=new(){Interval=TimeSpan.FromMilliseconds(120)};
    CancellationTokenSource cancellation = new();
    int pending;
    bool toolsMode;
    string currentPage = "convert";
    Operation? selected;
    public bool IsBusy => pending > 0;
    public MainWindow(App app)
    {
        InitializeComponent(); this.app=app; FileList.ItemsSource=files; RefreshStatus();
        activityRefresh.Tick+=(_,_)=>{activityRefresh.Stop();if(currentPage=="activity")ActivityTab(this,new());};
        BrandMark.Content=Brand.Mark(38);AddFilesButton.Content=Icons.Label("plus","Add files");MoveUpButton.Content=Icons.Create("arrow-up",14);MoveDownButton.Content=Icons.Create("arrow-down",14);ConvertNav.Content=Icons.Label("arrow-left-right","Convert");ToolsNav.Content=Icons.Label("sliders-horizontal","File tools");ActivityNav.Content=Icons.Label("activity","Activity");SettingsNav.Content=Icons.Label("settings","Settings");
        Closing += OnClosing;
        PreviewKeyDown += (_,e)=> {
            if(e.Key == Key.O && Keyboard.Modifiers == ModifierKeys.Control) { AddFiles(this,new()); e.Handled=true; }
            if(e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control && Keyboard.FocusedElement is not TextBox && Clipboard.ContainsFileDropList()) { AddPaths(Clipboard.GetFileDropList().Cast<string>()); e.Handled=true; }
        };
    }
    void OnClosing(object? sender,CancelEventArgs e)
    {
        if(app.Quitting) return;
        e.Cancel=true;
        if(app.Settings.MinimizeToTray) Hide(); else app.Quit();
    }
    public void RefreshStatus()
    {
        WheelStatus.Text = app.Settings.DragWheelEnabled ? "●  Drag wheel on" : "○  Drag wheel paused";
        DestinationLabel.Text = app.Settings.OutputDirectory is {Length:>0} d ? "↳  " + d : "↳  Save beside the original";
    }
    public void AddPaths(IEnumerable<string> paths)
    {
        foreach(var path in paths.Where(p=>File.Exists(p)||Directory.Exists(p))) {
            var full=Path.GetFullPath(path); if(!files.Any(f=>f.Path.Equals(full,StringComparison.OrdinalIgnoreCase))) files.Add(new(full));
        }
        if(files.Count>1000) { while(files.Count>1000) files.RemoveAt(files.Count-1); StatusText.Text="At most 1,000 files can be queued at a time."; }
        RefreshOptions();
    }
    public string[] Paths => files.Select(f=>f.Path).ToArray();
    void FilesDragged(object sender,DragEventArgs e) { e.Effects=e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None; e.Handled=true; }
    void FilesDropped(object sender,DragEventArgs e) { if(e.Data.GetData(DataFormats.FileDrop) is string[] paths) AddPaths(paths); e.Effects=DragDropEffects.Copy; e.Handled=true; }
    void AddFiles(object sender,RoutedEventArgs e) { var dialog=new OpenFileDialog { Multiselect=true,Title="Add files to HandyLime",CheckFileExists=true }; if(dialog.ShowDialog(this)==true) AddPaths(dialog.FileNames); }
    void AddFolder(object sender,RoutedEventArgs e) { var dialog=new OpenFolderDialog {Title="Choose a folder"}; if(dialog.ShowDialog(this)==true) AddPaths([dialog.FolderName]); }
    void ClearFiles(object sender,RoutedEventArgs e) { files.Clear(); OptionSearch.Clear(); RefreshOptions(); }
    void RemoveFile(object sender,RoutedEventArgs e) { if(FileList.SelectedItem is FileItem item) files.Remove(item); RefreshOptions(); }
    void MoveUp(object sender,RoutedEventArgs e) { var i=FileList.SelectedIndex; if(i>0) files.Move(i,i-1); }
    void MoveDown(object sender,RoutedEventArgs e) { var i=FileList.SelectedIndex; if(i>=0 && i<files.Count-1) files.Move(i,i+1); }
    void ConvertTab(object sender,RoutedEventArgs e) => SetMode(false);
    void ToolsTab(object sender,RoutedEventArgs e) => SetMode(true);
    void SetMode(bool tools)
    {
        toolsMode=tools; OptionSearch.Clear(); currentPage=tools?"tools":"convert"; Workspace.Visibility=Visibility.Visible; OtherPage.Visibility=Visibility.Collapsed;
        Heading.Text=tools?"Small tools. Big difference.":"New format. Same flow.";
        Subheading.Text=tools?"A practical toolkit for the files already on your desktop.":"Drop your files, pick a format, and get back to what matters.";
        OptionsTitle.Text=tools?"PICK A TOOL":"MAKE IT YOURS"; RefreshOptions();
    }
    void RefreshOptions()
    {
        FileCount.Text=$"YOUR FILES  /  {files.Count}"; EmptyState.Visibility=files.Count==0?Visibility.Visible:Visibility.Collapsed;
        selected=null; RunButton.IsEnabled=false; RunButton.Content=toolsMode?"Choose a tool":"Choose a format"; OptionsPanel.Children.Clear();
        var options=Catalog.ForSelection(Paths,toolsMode);if(!string.IsNullOrWhiteSpace(OptionSearch.Text))options=options.Where(o=>o.Title.Contains(OptionSearch.Text,StringComparison.OrdinalIgnoreCase)||o.Description.Contains(OptionSearch.Text,StringComparison.OrdinalIgnoreCase)).ToList();
        OptionsHint.Text=files.Count==0?"Add files to see available options.":options.Count==0?(!string.IsNullOrWhiteSpace(OptionSearch.Text)?"No matches. Clear the filter or search another name.":"No common operations. Select compatible files."):$"{options.Count} options for this selection";
        foreach(var op in options) {
            var ready=app.Engines.Missing(op).Length==0;
            var button=new Button { Content=toolsMode?Icons.Label(Icons.Name(op.Id),op.Title,14):op.Title,Margin=new Thickness(0,0,6,8),Padding=new Thickness(11,8,11,8),FontSize=12,ToolTip=op.Description+(ready?"":"\nRequires "+op.Engine+" — see Settings") };
            button.Click+=(_,_)=> {
                selected=op; foreach(var child in OptionsPanel.Children.OfType<Button>()) { child.Background=Theme.Brush("Surface"); child.Foreground=(Brush)FindResource("Ink"); }
                button.Background=(Brush)FindResource("Accent"); button.Foreground=Theme.Brush("AccentInk");
                RunButton.IsEnabled=true; RunButton.Content=ready?(toolsMode?"Open tool →":"Convert files →"):"Set up "+op.Engine;
                OptionsHint.Text=op.Description;
            };
            OptionsPanel.Children.Add(button);
        }
    }
    void FilterOptions(object sender,TextChangedEventArgs e){if(app!=null)RefreshOptions();}
    async void RunSelected(object sender,RoutedEventArgs e) { if(selected!=null) await StartOperation(Paths,selected,true); }
    public async Task StartOperation(string[] paths,Operation op,bool configure)
    {
        if(paths.Length==0) { StatusText.Text="Add a file first."; return; }
        var missing=app.Engines.Missing(op);
        if(missing.Length>0) { app.ShowShell(); SettingsTab(this,new()); StatusText.Text=$"Install or locate {string.Join(" and ",missing)} to enable this operation.";
            Dialogs.Show(this,$"This operation needs {string.Join(" and ",missing)}.\n\nOpen the setup guide or choose the executable under Conversion engines. No conversion has started.","Conversion engine needed",MessageBoxButton.OK,MessageBoxImage.Information);return; }
        var options=op.Fields.ToDictionary(f=>f.Key,f=>f.Default);
        if(configure && op.Fields.Length>0 || !op.Id.StartsWith("convert:") || op.Id=="convert:gif" && Catalog.Kind(paths[0])==FileKind.Video) {
            notifications?.Hide();
            app.ShowShell(); var dialog=new ToolWindow(paths,op,app.Engines) {Owner=this};
            if(dialog.ShowDialog()!=true) return; options=dialog.Options; paths=dialog.Inputs;
        }
        var batches=op.IsBatch?new[]{paths}:paths.Select(p=>new[]{p}).ToArray();
        var jobs=batches.Select(batch=>RunJob(new JobRequest(batch,op,new(options),app.Settings.OutputDirectory))).ToArray();
        await Task.WhenAll(jobs);
    }
    async Task RunJob(JobRequest request)
    {
        var item=new ActivityItem {Title=$"{request.Operation.Title} · {Path.GetFileName(request.Inputs[0])}"}; activity.Insert(0,item);
        using var jobCancellation=CancellationTokenSource.CreateLinkedTokenSource(cancellation.Token);
        var token=jobCancellation.Token; pending++; UpdateBusy(); RefreshActivityIfVisible();
        notifications??=new ProgressWindow(this);notifications.Add(item,()=>jobCancellation.Cancel());
        var entered=false;
        try {
            await serial.WaitAsync(token); entered=true; item.Status="Working";item.Detail="Starting conversion…";notifications.Refresh(item);RefreshActivityIfVisible();
            var progress=new Progress<JobProgress>(update=>{if(item.Status!="Working")return;item.Detail=update.Message;item.Fraction=update.Fraction;StatusText.Text=update.Message;notifications.Refresh(item);});
            var result=await Task.Run(()=>new JobExecutor(app.Engines).Execute(request,token,progress),token);
            item.Status="Done";item.Fraction=1;item.Detail=result.Message; item.Outputs=result.Outputs; StatusText.Text=result.Message;
        } catch(OperationCanceledException) { item.Status="Cancelled";item.Fraction=null;item.Detail="Cancelled. The original is unchanged."; StatusText.Text="Cancelled. Completed outputs are kept; unfinished temporary files are removed."; }
        catch(Exception ex) { item.Status="Failed"; item.Fraction=null;item.Error=ex.Message; StatusText.Text="Conversion failed: "+ex.Message;notifications.ShowFailure(item); }
        finally { if(entered) serial.Release(); pending--; UpdateBusy();notifications.Refresh(item); RefreshActivityIfVisible(); }
    }
    void UpdateBusy() { BusyBar.Visibility=IsBusy?Visibility.Visible:Visibility.Collapsed; CancelButton.Visibility=BusyBar.Visibility; }
    public void CancelAll() { var previous=cancellation;cancellation=new();previous.Cancel();previous.Dispose(); }
    void CancelJobs(object sender,RoutedEventArgs e)=>CancelAll();
    void OpenWheel(object sender,RoutedEventArgs e) { if(files.Count==0) { AddFiles(sender,e); if(files.Count==0)return; } new WheelWindow(app,toolsMode?1:0,Paths).OpenAtPointer(); }
    void ActionsClick(object sender,RoutedEventArgs e) { if(files.Count>0) new WheelWindow(app,2,Paths).OpenAtPointer(); }
    void ShowOther(string page,string title,string description)
    {
        currentPage=page; Workspace.Visibility=Visibility.Collapsed; OtherPage.Visibility=Visibility.Visible; OtherContent.Children.Clear();
        OtherContent.Children.Add(new TextBlock {Text=title,FontSize=32,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,0,0,10)});
        OtherContent.Children.Add(new TextBlock {Text=description,Foreground=(Brush)FindResource("Muted"),Margin=new Thickness(0,0,0,24)});
    }
    void RefreshActivityIfVisible() { if(currentPage=="activity"&&!activityRefresh.IsEnabled)activityRefresh.Start(); }
    void ActivityTab(object sender,RoutedEventArgs e)
    {
        ShowOther("activity","A little work, done.","This session’s jobs. History is held in memory and cleared when you quit.");
        if(activity.Count==0) OtherContent.Children.Add(new TextBlock {Text="Nothing here yet. Your first conversion will appear here."});
        foreach(var item in activity.Take(200)) {
            var stack=new StackPanel(); stack.Children.Add(new TextBlock {Text=item.Title,FontWeight=FontWeights.SemiBold});
            stack.Children.Add(new TextBlock {Text=item.Status,Foreground=item.Status=="Failed"?Theme.Brush("Danger"):Theme.Brush("Ink"),Margin=new Thickness(0,8,0,4)});
            if(item.Error!=null) stack.Children.Add(new TextBlock {Text=item.Error,FontSize=12,TextWrapping=TextWrapping.Wrap});
            foreach(var output in item.Outputs) { var b=new Button {Content="Show in Explorer →",HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,8,0,0),ToolTip=output}; b.Click+=(_,_)=>FileActions.Reveal(output); stack.Children.Add(b); }
            OtherContent.Children.Add(new Border {Style=(Style)FindResource("Card"),Child=stack,Margin=new Thickness(0,0,0,12)});
        }
    }
    void SettingsTab(object sender,RoutedEventArgs e)
    {
        ShowOther("settings","Make yourself at home.","Set up optional engines once. Conversions then run on your computer.");
        OtherContent.Children.Add(new TextBlock{Text="Appearance",FontSize=22,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,0,0,12)});
        var themes=new WrapPanel{Margin=new Thickness(0,0,0,20)};OtherContent.Children.Add(themes);
        foreach(var choice in new[]{"Light","Dark","System"}){var button=new Button{Content=Icons.Label(choice=="Light"?"sun":choice=="Dark"?"moon":"monitor",choice),Margin=new Thickness(0,0,8,0)};if(app.Settings.Theme==choice)button.SetResourceReference(Button.BackgroundProperty,"Selected");button.Click+=(_,_)=>{app.Settings.Theme=choice;app.Settings.Save();Theme.Apply(choice);SettingsTab(this,new());};themes.Children.Add(button);}
        var wheel=new CheckBox {Content="Enable Shift / Alt / Ctrl drag wheel",IsChecked=app.Settings.DragWheelEnabled};
        wheel.Click+=(_,_)=> {app.Settings.DragWheelEnabled=wheel.IsChecked==true; app.Settings.Save(); RefreshStatus();}; OtherContent.Children.Add(wheel);
        var tray=new CheckBox {Content="Keep running in the system tray when the window is closed",IsChecked=app.Settings.MinimizeToTray};
        tray.Click+=(_,_)=> {app.Settings.MinimizeToTray=tray.IsChecked==true; app.Settings.Save();}; OtherContent.Children.Add(tray);
        var output=new Button {Content="Choose output folder…",HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,16,0,6)};
        output.Click+=(_,_)=> {var d=new OpenFolderDialog(); if(d.ShowDialog(this)==true) {app.Settings.OutputDirectory=d.FolderName; app.Settings.Save(); RefreshStatus();}}; OtherContent.Children.Add(output);
        var reset=new Button {Content="Use source folder",HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,0,0,24)};
        reset.Click+=(_,_)=> {app.Settings.OutputDirectory=null; app.Settings.Save(); RefreshStatus();}; OtherContent.Children.Add(reset);
        OtherContent.Children.Add(new TextBlock {Text="Conversion engines",FontSize=22,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,0,0,15)});
        foreach(var engine in new[]{"FFmpeg","FFprobe","LibreOffice","Pandoc"}) {
            var stack=new StackPanel(); var path=app.Engines.Find(engine);
            stack.Children.Add(new TextBlock {Text=engine+"  ·  "+(path==null?"Not installed":"Ready"),FontWeight=FontWeights.SemiBold});
            stack.Children.Add(new TextBlock {Text=path??"Optional. Choose an installed executable or read the setup guide.",FontSize=12,Foreground=(Brush)FindResource("Muted"),Margin=new Thickness(0,8,0,12)});
            var row=new WrapPanel {Orientation=Orientation.Horizontal};
            var browse=new Button {Content="Locate .exe…",FontSize=12}; browse.Click+=(_,_)=> {var d=new OpenFileDialog {Filter="Executable|*.exe",Title="Locate "+engine}; if(d.ShowDialog(this)==true) {app.Settings.EnginePaths[engine]=d.FileName; app.Settings.Save(); SettingsTab(this,new());}};
            row.Children.Add(browse);
            if(path==null && engine!="FFprobe"){
                var install=new Button{Content="Install with winget",FontSize=12,Margin=new Thickness(8,0,0,0)};
                install.Click+=async(_,_)=>{install.IsEnabled=false;await InstallEngine(engine);if(currentPage=="settings")SettingsTab(this,new());};row.Children.Add(install);
            }
            var link=new Button {Content="Official website ↗",FontSize=12,Margin=new Thickness(8,0,0,0)}; link.Click+=(_,_)=>Process.Start(new ProcessStartInfo(engine switch {"FFmpeg" or "FFprobe"=>"https://ffmpeg.org/download.html","LibreOffice"=>"https://www.libreoffice.org/download/download-libreoffice/",_=>"https://pandoc.org/installing.html"}){UseShellExecute=true}); row.Children.Add(link);
            stack.Children.Add(row); OtherContent.Children.Add(new Border {Style=(Style)FindResource("Card"),Child=stack,Margin=new Thickness(0,0,0,12)});
        }
        OtherContent.Children.Add(new TextBlock {Text="Images, PDF tools, archives and subtitles work without these engines. Install with winget downloads the publisher’s package and accepts its package/source agreements. FFmpeg includes FFprobe. You can also install manually and use Locate .exe. See docs/ENGINES.md.",Foreground=(Brush)FindResource("Muted"),Margin=new Thickness(0,12,0,20)});
        var quit=new Button {Content="Quit HandyLime",HorizontalAlignment=HorizontalAlignment.Left}; quit.Click+=(_,_)=>app.Quit(); OtherContent.Children.Add(quit);
    }
    async Task InstallEngine(string engine)
    {
        var package=engine switch{"FFmpeg"=>"Gyan.FFmpeg","LibreOffice"=>"TheDocumentFoundation.LibreOffice","Pandoc"=>"JohnMacFarlane.Pandoc",_=>throw new ArgumentException("Unknown engine")};
        var item=new ActivityItem{Title="Install "+engine,Status="Working",Detail="Downloading and installing the publisher’s package…"};activity.Insert(0,item);
        using var cancel=CancellationTokenSource.CreateLinkedTokenSource(cancellation.Token);
        pending++;UpdateBusy();notifications??=new ProgressWindow(this);notifications.Add(item,()=>cancel.Cancel());
        try{
            await ProcessRunner.Run("winget.exe",["install","--id",package,"--exact","--source","winget","--silent","--accept-package-agreements","--accept-source-agreements","--disable-interactivity"],cancel.Token);
            if(app.Engines.Find(engine)==null)throw new InvalidOperationException("Installation finished, but the executable was not found. Restart HandyLime or use Locate .exe.");
            item.Status="Done";item.Fraction=1;item.Detail=engine+" is ready. Start the conversion again.";
        }catch(OperationCanceledException){item.Status="Cancelled";item.Detail="Installation cancelled. Check Settings before trying again.";}
        catch(Exception ex){item.Status="Failed";item.Error="Install manually using the Official website button, or install Windows App Installer if winget is unavailable.\n\n"+ex.Message;notifications.ShowFailure(item);}
        finally{pending--;UpdateBusy();notifications.Refresh(item);RefreshActivityIfVisible();}
    }
}
