using System.Security.Cryptography;
using Mitsuoshie.Core.Models;

namespace Mitsuoshie.Core.Monitoring;

public class SecurityEventSubscriber
{
    private readonly SettingsStore _store;
    private readonly SafeProcessFilter _filter;

    public event Action<MitsuoshieAlert>? AlertRaised;

    public SecurityEventSubscriber(SettingsStore store, SafeProcessFilter filter)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _filter = filter ?? throw new ArgumentNullException(nameof(filter));
    }

    /// <summary>
    /// Security Event を処理し、罠ファイルへのアクセスであればアラートを生成する。
    /// </summary>
    public void ProcessEvent(SecurityEventData evt)
    {
        // 罠ファイルでなければ無視
        if (!_store.ContainsPath(evt.ObjectName))
            return;

        // 安全プロセスは除外
        if (_filter.IsSafe(evt.ProcessName, evt.AccessMask, evt.ProcessId))
            return;

        // ハッシュ比較（ファイルが存在する場合のみ）
        var originalHash = _store.GetOriginalHash(evt.ObjectName) ?? "";
        var currentHash = ComputeCurrentHash(evt.ObjectName);
        var tampered = !string.IsNullOrEmpty(currentHash)
                       && !string.IsNullOrEmpty(originalHash)
                       && currentHash != originalHash;

        var eventType = tampered ? "Tampered" : GetAccessType(evt.AccessMask);

        var alert = new MitsuoshieAlert
        {
            Timestamp = evt.Timestamp ?? DateTime.UtcNow,
            HoneyFile = evt.ObjectName,
            HoneyType = _store.GetHoneyType(evt.ObjectName) ?? HoneyTokenType.AwsCredential,
            EventType = eventType,
            Tampered = tampered,
            OriginalHash = originalHash,
            CurrentHash = currentHash,
            AccessMask = evt.AccessMask,
            ProcessId = evt.ProcessId,
            ProcessName = Path.GetFileName(evt.ProcessName),
            ProcessPath = evt.ProcessName,
            User = evt.UserName,
            Severity = AlertSeverity.Critical
        };

        AlertRaised?.Invoke(alert);
    }

    internal static string GetAccessType(string accessMask)
    {
        return accessMask switch
        {
            "0x1" => "ReadData",
            "0x2" => "WriteData",
            "0x4" => "AppendData",
            "0x10000" => "Delete",
            "0x80" => "ReadAttributes",
            _ => $"Unknown({accessMask})"
        };
    }

    private static string ComputeCurrentHash(string filePath)
    {
        if (!File.Exists(filePath))
            return "";

        var bytes = File.ReadAllBytes(filePath);
        return Convert.ToHexStringLower(SHA256.HashData(bytes));
    }
}
