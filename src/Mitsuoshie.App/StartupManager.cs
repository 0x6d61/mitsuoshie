using System.Runtime.Versioning;
using Microsoft.Win32;

namespace Mitsuoshie.App;

[SupportedOSPlatform("windows")]
public static class StartupManager
{
    private const string RegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "Mitsuoshie";

    /// <summary>
    /// スタートアップに登録する。
    /// </summary>
    public static void Register()
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath)) return;

        using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, writable: true);
        key?.SetValue(AppName, $"\"{exePath}\"");
    }

    /// <summary>
    /// スタートアップから解除する。
    /// </summary>
    public static void Unregister()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, writable: true);
        key?.DeleteValue(AppName, throwOnMissingValue: false);
    }

    /// <summary>
    /// スタートアップに登録されているか確認する。
    /// </summary>
    public static bool IsRegistered()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKey);
        return key?.GetValue(AppName) is not null;
    }
}
