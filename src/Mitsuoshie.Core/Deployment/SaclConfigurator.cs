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
    private static readonly SecurityIdentifier EveryoneSid =
        new(WellKnownSidType.WorldSid, null);

    private static readonly FileSystemRights AuditRights =
        FileSystemRights.ReadData
        | FileSystemRights.WriteData
        | FileSystemRights.Delete
        | FileSystemRights.AppendData;

    public static void SetAuditRule(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentException.ThrowIfNullOrEmpty(filePath);

        if (!File.Exists(filePath))
            throw new FileNotFoundException("罠ファイルが見つかりません。", filePath);

        var fileInfo = new FileInfo(filePath);
        var security = fileInfo.GetAccessControl(AccessControlSections.Audit);

        // 既に同等のルールがあればスキップ（べき等）
        if (HasMatchingAuditRule(security))
            return;

        var auditRule = new FileSystemAuditRule(
            identity: EveryoneSid,
            fileSystemRights: AuditRights,
            flags: AuditFlags.Success
        );

        security.AddAuditRule(auditRule);
        fileInfo.SetAccessControl(security);
    }

    /// <summary>
    /// 既に同等の監査ルールが設定されているかチェックする。
    /// </summary>
    private static bool HasMatchingAuditRule(FileSystemSecurity security)
    {
        var rules = security.GetAuditRules(
            includeExplicit: true,
            includeInherited: false,
            targetType: typeof(SecurityIdentifier));

        foreach (FileSystemAuditRule rule in rules)
        {
            if (rule.IdentityReference is SecurityIdentifier sid
                && sid == EveryoneSid
                && (rule.FileSystemRights & AuditRights) == AuditRights
                && rule.AuditFlags.HasFlag(AuditFlags.Success))
            {
                return true;
            }
        }

        return false;
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
            // GUID指定でロケール非依存（"File System" は日本語版では "ファイル システム"）
            Arguments = "/set /subcategory:\"{0CCE921D-69AE-11D9-BED3-505054503030}\" /success:enable",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process is null) return;

        // stdout/stderr を先に読み切ってから WaitForExit（デッドロック防止）
        var error = process.StandardError.ReadToEnd();
        process.StandardOutput.ReadToEnd();
        process.WaitForExit(TimeSpan.FromSeconds(30));

        if (process.ExitCode != 0)
        {
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
