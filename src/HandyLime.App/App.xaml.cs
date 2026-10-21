using System.Windows;
using HandyLime.Core;
using Forms = System.Windows.Forms;

namespace HandyLime.App;

public partial class App : System.Windows.Application
{
    public AppSettings Settings { get; } = AppSettings.Load();
    public Engines Engines { get; private set; } = null!;
    public MainWindow Shell { get; private set; } = null!;
    public bool Quitting { get; private set; }
    Forms.NotifyIcon? tray;
    DragTrigger? trigger;
    Mutex? mutex;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Theme.Apply(Settings.Theme);
        ImageEngine.EnsureInitialized();
        if(e.Args.Length==2&&e.Args[0]=="--export-brand"){Brand.Export(e.Args[1]);Shutdown();return;}
        EventManager.RegisterClassHandler(typeof(Window),FrameworkElement.LoadedEvent,new RoutedEventHandler((sender,_)=>WindowFrame.Attach((Window)sender)));
        Microsoft.Win32.SystemEvents.UserPreferenceChanged+=SystemThemeChanged;
        mutex = new Mutex(true,"Local\\HandyLime-"+Environment.UserName,out var first);
        if (!first) { if(!e.Args.Contains("--tray"))Dialogs.Show("HandyLime is already running. Open it from the system tray.","HandyLime"); Shutdown(); return; }
        Engines = new(Settings);
        Shell = new MainWindow(this); MainWindow = Shell;
        DispatcherUnhandledException += (_,args) => { Dialogs.Show(args.Exception.Message,"HandyLime — something went wrong",MessageBoxButton.OK,MessageBoxImage.Error); args.Handled = true; };
        tray = new Forms.NotifyIcon { Text = "HandyLime — drag a file, tap Shift", Visible = true };
        using (var resource = GetResourceStream(new Uri("pack://application:,,,/Assets/handylime.ico")).Stream)
            tray.Icon = new System.Drawing.Icon(resource);
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Open HandyLime",null,(_,_)=>Dispatcher.Invoke(ShowShell));
        menu.Items.Add("Pause / resume drag wheel",null,(_,_)=>Dispatcher.Invoke(()=> { Settings.DragWheelEnabled=!Settings.DragWheelEnabled; Settings.Save(); Shell.RefreshStatus(); }));
        menu.Items.Add("Quit",null,(_,_)=>Dispatcher.Invoke(Quit)); tray.ContextMenuStrip=menu;
        tray.DoubleClick += (_,_)=>Dispatcher.Invoke(ShowShell);
        trigger = new DragTrigger(this);
        var files = e.Args.Where(p=>File.Exists(p) || Directory.Exists(p)).ToArray(); if(files.Length>0) Shell.AddPaths(files);
        if(!e.Args.Contains("--tray")||files.Length>0)Shell.Show();
    }
    public void ShowShell() { Shell.Show(); Shell.WindowState=WindowState.Normal; Shell.Activate(); }
    public void Quit()
    {
        if (Shell.IsBusy && Dialogs.Show("Cancel unfinished jobs and quit?","HandyLime",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes) return;
        Quitting=true; Shell.CancelAll(); trigger?.Dispose(); tray?.Dispose(); Shutdown();
    }
    void SystemThemeChanged(object sender,Microsoft.Win32.UserPreferenceChangedEventArgs e){if(Settings.Theme=="System")Dispatcher.Invoke(()=>Theme.Apply("System"));}
    protected override void OnExit(ExitEventArgs e) { Microsoft.Win32.SystemEvents.UserPreferenceChanged-=SystemThemeChanged;trigger?.Dispose(); tray?.Dispose(); mutex?.Dispose(); base.OnExit(e); }
}
