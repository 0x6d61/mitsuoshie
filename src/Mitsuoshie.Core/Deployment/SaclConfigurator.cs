using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Mitsuoshie.Core.Deployment;

[SupportedOSPlatform("windows")]
public static class SaclConfigurator
{
    /// <summary>
    /// 罠ファイルに NTFS 監査 ACL を設定する。
    /// Everyone に対する ReadData, WriteData, Delete, AppendData を監査対象にする。
    /// </summary>
    public static void SetAuditRule(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentException.ThrowIfNullOrEmpty(filePath);

        if (!File.Exists(filePath))
            throw new FileNotFoundException("罠ファイルが見つかりません。", filePath);

        var fileInfo = new FileInfo(filePath);
        var security = fileInfo.GetAccessControl(AccessControlSections.Audit);

        var auditRule = new FileSystemAuditRule(
            identity: new SecurityIdentifier(WellKnownSidType.WorldSid, null),
            fileSystemRights: FileSystemRights.ReadData
                            | FileSystemRights.WriteData
                            | FileSystemRights.Delete
                            | FileSystemRights.AppendData,
            flags: AuditFlags.Success
        );

        security.AddAuditRule(auditRule);
        fileInfo.SetAccessControl(security);
    }

    /// <summary>
    /// ファイルシステム監査ポリシーを有効化する（管理者権限が必要）。
    /// auditpol /set /subcategory:"File System" /success:enable を実行。
    /// </summary>
    public static void EnableFileSystemAuditing()
    {
        if (!IsAdministrator())
            throw new InvalidOperationException(
                "ファイルシステム監査の有効化には管理者権限が必要です。");

        var psi = new ProcessStartInfo
        {
            FileName = "auditpol",
            Arguments = "/set /subcategory:\"File System\" /success:enable",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        process?.WaitForExit(TimeSpan.FromSeconds(30));

        if (process?.ExitCode != 0)
        {
            var error = process?.StandardError.ReadToEnd();
            throw new InvalidOperationException(
                $"監査ポリシーの有効化に失敗しました: {error}");
        }
    }

    /// <summary>
    /// 現在のプロセスが管理者権限で実行されているかを確認する。
    /// </summary>
    public static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}
