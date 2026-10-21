using HandyLime.Core;
using Microsoft.Win32;

namespace HandyLime.App;

internal static class WindowsStartup
{
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public static string Executable => Path.Combine(AppContext.BaseDirectory, "HandyLime.exe");
    public static string? ReadCommand()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(StartupRegistration.ValueName) as string;
    }
    public static void SetEnabled(bool enabled)
    {
        if (enabled) {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey, true);
            StartupRegistration.Write(key, Executable, true);
        } else {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, true);
            if (key != null) StartupRegistration.Write(key, Executable, false);
        }
    }
}
