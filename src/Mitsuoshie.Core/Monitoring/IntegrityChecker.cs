using System.Security.Cryptography;
using Mitsuoshie.Core.Models;

namespace Mitsuoshie.Core.Monitoring;

public class IntegrityChecker
{
    private readonly SettingsStore _store;

    public IntegrityChecker(SettingsStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    /// <summary>
    /// 全罠ファイルのハッシュを再計算し、改ざん・削除を検知する。
    /// </summary>
    public List<MitsuoshieAlert> CheckAll()
    {
        var alerts = new List<MitsuoshieAlert>();

        foreach (var token in _store.Tokens)
        {
            var alert = CheckSingle(token);
            if (alert is not null)
                alerts.Add(alert);
        }

        return alerts;
    }

    private static MitsuoshieAlert? CheckSingle(DeployedToken token)
    {
        // ファイルが削除されている
        if (!File.Exists(token.FilePath))
        {
            return new MitsuoshieAlert
            {
                Timestamp = DateTime.UtcNow,
                HoneyFile = token.FilePath,
                HoneyType = token.HoneyType,
                EventType = "Deleted",
                Tampered = false,
                OriginalHash = token.OriginalHash,
                CurrentHash = "",
                AccessMask = "",
                ProcessId = 0,
                ProcessName = "unknown",
                ProcessPath = "",
                User = "",
                Severity = AlertSeverity.Critical
            };
        }

        // ハッシュ比較
        var currentHash = ComputeHash(token.FilePath);
        if (currentHash != token.OriginalHash)
        {
            return new MitsuoshieAlert
            {
                Timestamp = DateTime.UtcNow,
                HoneyFile = token.FilePath,
                HoneyType = token.HoneyType,
                EventType = "Tampered",
                Tampered = true,
                OriginalHash = token.OriginalHash,
                CurrentHash = currentHash,
                AccessMask = "",
                ProcessId = 0,
                ProcessName = "unknown",
                ProcessPath = "",
                User = "",
                Severity = AlertSeverity.Critical
            };
        }

        return null; // 整合性OK
    }

    private static string ComputeHash(string filePath)
    {
        var bytes = File.ReadAllBytes(filePath);
        return Convert.ToHexStringLower(SHA256.HashData(bytes));
    }
}
