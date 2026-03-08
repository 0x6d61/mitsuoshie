using System.Text.Json;
using System.Text.Json.Serialization;
using Mitsuoshie.Core.Models;

namespace Mitsuoshie.Core.Logging;

public class SysmonJsonLogger
{
    private readonly string _logPath;
    private readonly object _writeLock = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null, // PascalCase のまま
        Converters = { new JsonStringEnumConverter() }
    };

    public SysmonJsonLogger(string logPath)
    {
        _logPath = logPath ?? throw new ArgumentNullException(nameof(logPath));
    }

    /// <summary>
    /// アラートを Sysmon 互換 JSON 形式でファイルに追記する（JSONL形式）。
    /// </summary>
    public void WriteAlert(MitsuoshieAlert alert, string alertId)
    {
        var entry = new SysmonEntry
        {
            EventId = 11,
            EventType = "FileAccess",
            RuleName = "Mitsuoshie:HoneytokenAccess",
            UtcTime = alert.Timestamp.ToString("o"),
            ProcessId = alert.ProcessId,
            Image = alert.ProcessPath,
            TargetFilename = alert.HoneyFile,
            User = alert.User,
            AccessMask = alert.AccessMask,
            AccessType = alert.EventType,
            Hashes = $"SHA256={alert.CurrentHash}",
            MitsuoshieMetadata = new SysmonMetadata
            {
                HoneyType = alert.HoneyType.ToString(),
                HoneyFile = alert.HoneyFile,
                Severity = alert.Severity.ToString().ToLower(),
                Tampered = alert.Tampered,
                OriginalHash = $"SHA256={alert.OriginalHash}",
                CurrentHash = $"SHA256={alert.CurrentHash}",
                AlertId = alertId,
                Description = $"{alert.HoneyType} honeytoken {alert.EventType.ToLower()} by {alert.ProcessName}"
            }
        };

        var json = JsonSerializer.Serialize(entry, JsonOptions);

        var dir = Path.GetDirectoryName(_logPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        lock (_writeLock)
        {
            File.AppendAllText(_logPath, json + Environment.NewLine);
        }
    }

    private record SysmonEntry
    {
        public int EventId { get; init; }
        public string EventType { get; init; } = "";
        public string RuleName { get; init; } = "";
        public string UtcTime { get; init; } = "";
        public int ProcessId { get; init; }
        public string Image { get; init; } = "";
        public string TargetFilename { get; init; } = "";
        public string User { get; init; } = "";
        public string AccessMask { get; init; } = "";
        public string AccessType { get; init; } = "";
        public string Hashes { get; init; } = "";
        public SysmonMetadata MitsuoshieMetadata { get; init; } = new();
    }

    private record SysmonMetadata
    {
        public string HoneyType { get; init; } = "";
        public string HoneyFile { get; init; } = "";
        public string Severity { get; init; } = "";
        public bool Tampered { get; init; }
        public string OriginalHash { get; init; } = "";
        public string CurrentHash { get; init; } = "";
        public string AlertId { get; init; } = "";
        public string Description { get; init; } = "";
    }
}
