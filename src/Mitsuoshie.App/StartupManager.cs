using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace Mitsuoshie.App;

[SupportedOSPlatform("windows")]
public static class StartupManager
{
    private const string TaskName = "Mitsuoshie";
    private const string RegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>
    /// スタートアップに登録する。
    /// 管理者権限がある場合は Task Scheduler（最高権限）で登録。
    /// ない場合はレジストリ Run キーにフォールバック。
    /// </summary>
    public static void Register()
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath)) return;

        if (Mitsuoshie.Core.Deployment.SaclConfigurator.IsAdministrator())
        {
            RegisterTaskScheduler(exePath);
        }
        else
        {
            RegisterRegistry(exePath);
        }
    }

    /// <summary>
    /// スタートアップから解除する。
    /// </summary>
    public static void Unregister()
    {
        UnregisterTaskScheduler();
        UnregisterRegistry();
    }

    /// <summary>
    /// スタートアップに登録されているか確認する。
    /// </summary>
    public static bool IsRegistered()
    {
        return IsTaskSchedulerRegistered() || IsRegistryRegistered();
    }

    private static void RegisterTaskScheduler(string exePath)
    {
        // 既存タスクを削除してから再作成
        UnregisterTaskScheduler();

        var psi = new ProcessStartInfo
        {
            FileName = "schtasks",
            Arguments = $"/Create /TN \"{TaskName}\" /TR \"\\\"{exePath}\\\"\" "
                      + "/SC ONLOGON /RL HIGHEST /F",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        process?.WaitForExit(TimeSpan.FromSeconds(10));
    }

    private static void UnregisterTaskScheduler()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "schtasks",
            Arguments = $"/Delete /TN \"{TaskName}\" /F",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        process?.WaitForExit(TimeSpan.FromSeconds(10));
    }

    private static bool IsTaskSchedulerRegistered()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "schtasks",
            Arguments = $"/Query /TN \"{TaskName}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        process?.WaitForExit(TimeSpan.FromSeconds(10));
        return process?.ExitCode == 0;
    }

    private static void RegisterRegistry(string exePath)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, writable: true);
        key?.SetValue(TaskName, $"\"{exePath}\"");
    }

    private static void UnregisterRegistry()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, writable: true);
        key?.DeleteValue(TaskName, throwOnMissingValue: false);
    }

    private static bool IsRegistryRegistered()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKey);
        return key?.GetValue(TaskName) is not null;
    }
}
