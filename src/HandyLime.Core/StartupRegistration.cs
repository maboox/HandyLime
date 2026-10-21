using Microsoft.Win32;

namespace HandyLime.Core;

public static class StartupRegistration
{
    public const string ValueName = "HandyLime";
    public static string CommandFor(string executable)
    {
        if (string.IsNullOrWhiteSpace(executable) || executable.IndexOfAny(['"', '\r', '\n', '\0']) >= 0 ||
            !Path.IsPathFullyQualified(executable) || !Path.GetExtension(executable).Equals(".exe", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Choose a permanent folder and run HandyLime.exe before enabling Windows startup.");
        var command = $"\"{Path.GetFullPath(executable)}\" --tray";
        if (command.Length > 260) throw new ArgumentException("The startup path is too long. Move HandyLime to a shorter permanent path.");
        return command;
    }
    // Production supplies HKCU's Run key; tests supply an isolated fixture key.
    public static void Write(RegistryKey storage, string executable, bool enabled)
    {
        if (!enabled) { storage.DeleteValue(ValueName, false); return; }
        var command = CommandFor(executable);
        if (!File.Exists(executable)) throw new FileNotFoundException("The HandyLime executable no longer exists.", executable);
        storage.SetValue(ValueName, command, RegistryValueKind.String);
    }
}
