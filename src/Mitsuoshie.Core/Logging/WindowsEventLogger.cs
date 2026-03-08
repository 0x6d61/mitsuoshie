using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using Mitsuoshie.Core.Models;

namespace Mitsuoshie.Core.Logging;

[SupportedOSPlatform("windows")]
public class WindowsEventLogger
{
    private const string SourceName = "Mitsuoshie";
    private const string LogName = "Application";

    /// <summary>
    /// Windows Event Log にアラートを書き込む。
    /// EventSource の登録には管理者権限が必要。
    /// </summary>
    public void WriteAlert(MitsuoshieAlert alert)
    {
        EnsureSourceExists();

        var eventId = GetEventId(alert.EventType);
        var message = FormatMessage(alert);
        var entryType = EventLogEntryType.Warning;

        EventLog.WriteEntry(SourceName, message, entryType, eventId);
    }

    /// <summary>
    /// サービス開始イベントを書き込む。
    /// </summary>
    public void WriteServiceStart()
    {
        EnsureSourceExists();
        EventLog.WriteEntry(SourceName, "Mitsuoshie service started.", EventLogEntryType.Information, 2000);
    }

    /// <summary>
    /// サービス停止イベントを書き込む。
    /// </summary>
    public void WriteServiceStop()
    {
        EnsureSourceExists();
        EventLog.WriteEntry(SourceName, "Mitsuoshie service stopped.", EventLogEntryType.Information, 2001);
    }

    /// <summary>
    /// アラートの EventType からイベントIDを返す。
    /// </summary>
    public static int GetEventId(string eventType)
    {
        return eventType switch
        {
            "ReadData" => 1000,
            "WriteData" => 1001,
            "Delete" or "Deleted" => 1002,
            "Tampered" => 1003,
            _ => 1000
        };
    }

    /// <summary>
    /// アラートを Windows Event Log 用のメッセージ文字列にフォーマットする。
    /// </summary>
    public static string FormatMessage(MitsuoshieAlert alert)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Honeytoken access detected.");
        sb.AppendLine($"File: {alert.HoneyFile}");
        sb.AppendLine($"Type: {alert.HoneyType}");
        sb.AppendLine($"Access: {alert.EventType} ({alert.AccessMask})");
        sb.AppendLine($"Tampered: {alert.Tampered.ToString().ToLower()}");
        sb.AppendLine($"Process: {alert.ProcessName} (PID: {alert.ProcessId})");
        sb.AppendLine($"ProcessPath: {alert.ProcessPath}");
        sb.AppendLine($"User: {alert.User}");
        sb.AppendLine($"OriginalHash: SHA256={alert.OriginalHash}");
        sb.AppendLine($"CurrentHash: SHA256={alert.CurrentHash}");
        sb.AppendLine($"Timestamp: {alert.Timestamp:o}");
        return sb.ToString();
    }

    private static void EnsureSourceExists()
    {
        if (!EventLog.SourceExists(SourceName))
        {
            EventLog.CreateEventSource(SourceName, LogName);
        }
    }
}
