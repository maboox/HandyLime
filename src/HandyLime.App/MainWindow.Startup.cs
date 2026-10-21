using System.Windows;
using System.Windows.Controls;
using HandyLime.Core;

namespace HandyLime.App;

public partial class MainWindow
{
    void ConfigureStartupSettings()
    {
        OtherContent.Children.Add(new TextBlock { Text="Windows startup", FontWeight=FontWeights.SemiBold, Margin=new Thickness(0,18,0,8) });
        var startup = new CheckBox { Content="Start HandyLime with Windows" };
        var hint = new TextBlock { Text="Start minimized in the tray when you sign in. Keep HandyLime in a permanent folder.",
            TextWrapping=TextWrapping.Wrap, FontSize=12, Foreground=Theme.Brush("Muted"), Margin=new Thickness(0,5,0,8) };
        var update = new Button { Content="Use this copy for Windows startup", HorizontalAlignment=HorizontalAlignment.Left,
            Margin=new Thickness(0,0,0,8), Visibility=Visibility.Collapsed };
        OtherContent.Children.Add(startup); OtherContent.Children.Add(hint); OtherContent.Children.Add(update);
        bool enabled = false;
        void Refresh()
        {
            var command = WindowsStartup.ReadCommand();
            enabled = !string.IsNullOrWhiteSpace(command); startup.IsChecked = enabled;
            update.Visibility = enabled && !string.Equals(command,StartupRegistration.CommandFor(WindowsStartup.Executable),StringComparison.OrdinalIgnoreCase)
                ? Visibility.Visible : Visibility.Collapsed;
        }
        try { Refresh(); }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException or ArgumentException) {
            startup.IsEnabled = false; hint.Text = "Windows startup settings are unavailable: " + ex.Message;
        }
        void Apply(bool value)
        {
            try { WindowsStartup.SetEnabled(value); Refresh(); }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException or ArgumentException) {
                startup.IsChecked = enabled;
                Dialogs.Show(this,"Windows startup could not be changed.\n\n"+ex.Message,"HandyLime — Windows startup",MessageBoxButton.OK,MessageBoxImage.Error);
            }
        }
        startup.Click += (_,_) => Apply(startup.IsChecked == true);
        update.Click += (_,_) => Apply(true);
    }
}
