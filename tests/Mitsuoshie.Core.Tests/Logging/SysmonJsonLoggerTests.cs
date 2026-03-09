using System.Text.Json;

namespace Mitsuoshie.Core.Tests.Logging;

using Mitsuoshie.Core.Logging;
using Mitsuoshie.Core.Models;

public class SysmonJsonLoggerTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _logPath;

    public SysmonJsonLoggerTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "mitsuoshie_sysmon_test_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_testDir);
        _logPath = Path.Combine(_testDir, "mitsuoshie_sysmon.jsonl");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, recursive: true);
    }

    [Fact]
    public void WriteAlert_CreatesLogFile()
    {
        var logger = new SysmonJsonLogger(_logPath);
        logger.WriteAlert(CreateAlert(), "MITSUOSHIE-2026-0001");

        Assert.True(File.Exists(_logPath));
    }

    [Fact]
    public void WriteAlert_AppendsJsonLine()
    {
        var logger = new SysmonJsonLogger(_logPath);
        logger.WriteAlert(CreateAlert(), "MITSUOSHIE-2026-0001");
        logger.WriteAlert(CreateAlert(), "MITSUOSHIE-2026-0002");

        var lines = File.ReadAllLines(_logPath);
        Assert.Equal(2, lines.Length);
    }

    [Fact]
    public void WriteAlert_ProducesValidJson()
    {
        var logger = new SysmonJsonLogger(_logPath);
        logger.WriteAlert(CreateAlert(), "MITSUOSHIE-2026-0001");

        var line = File.ReadAllLines(_logPath)[0];
        var doc = JsonDocument.Parse(line);
        Assert.NotNull(doc);
    }

    [Fact]
    public void WriteAlert_ContainsSysmonFields()
    {
        var logger = new SysmonJsonLogger(_logPath);
        var alert = CreateAlert();
        logger.WriteAlert(alert, "MITSUOSHIE-2026-0001");

        var line = File.ReadAllLines(_logPath)[0];
        var doc = JsonDocument.Parse(line);
        var root = doc.RootElement;

        Assert.Equal(11, root.GetProperty("EventId").GetInt32());
        Assert.Equal("FileAccess", root.GetProperty("EventType").GetString());
        Assert.Equal("Mitsuoshie:HoneytokenAccess", root.GetProperty("RuleName").GetString());
        Assert.True(root.TryGetProperty("UtcTime", out _));
        Assert.True(root.TryGetProperty("ProcessId", out _));
        Assert.True(root.TryGetProperty("Image", out _));
        Assert.True(root.TryGetProperty("TargetFilename", out _));
        Assert.True(root.TryGetProperty("User", out _));
        Assert.True(root.TryGetProperty("AccessMask", out _));
        Assert.True(root.TryGetProperty("AccessType", out _));
    }

    [Fact]
    public void WriteAlert_ContainsMitsuoshieMetadata()
    {
        var logger = new SysmonJsonLogger(_logPath);
        logger.WriteAlert(CreateAlert(), "MITSUOSHIE-2026-0001");

        var line = File.ReadAllLines(_logPath)[0];
        var doc = JsonDocument.Parse(line);
        var metadata = doc.RootElement.GetProperty("MitsuoshieMetadata");

        Assert.Equal("AwsCredential", metadata.GetProperty("HoneyType").GetString());
        Assert.Equal("critical", metadata.GetProperty("Severity").GetString());
        Assert.Equal("MITSUOSHIE-2026-0001", metadata.GetProperty("AlertId").GetString());
        Assert.True(metadata.TryGetProperty("Tampered", out _));
        Assert.True(metadata.TryGetProperty("OriginalHash", out _));
        Assert.True(metadata.TryGetProperty("CurrentHash", out _));
    }

    [Fact]
    public void WriteAlert_CreatesDirectoryIfNeeded()
    {
        var deepPath = Path.Combine(_testDir, "sub", "dir", "log.jsonl");
        var logger = new SysmonJsonLogger(deepPath);
        logger.WriteAlert(CreateAlert(), "MITSUOSHIE-2026-0001");

        Assert.True(File.Exists(deepPath));
    }

    [Fact]
    public void WriteAlert_RotatesLogWhenExceedingMaxSize()
    {
        // 小さい上限（100バイト）でテスト
        var logger = new SysmonJsonLogger(_logPath, maxLogSizeBytes: 100);

        // 1行書くと100バイトを超える
        logger.WriteAlert(CreateAlert(), "MITSUOSHIE-2026-0001");
        Assert.True(new FileInfo(_logPath).Length > 100);

        // 2行目を書くとローテーションが発生
        logger.WriteAlert(CreateAlert(), "MITSUOSHIE-2026-0002");

        Assert.True(File.Exists(_logPath + ".bak"));
        // 新しいログファイルには最新の1行のみ
        var lines = File.ReadAllLines(_logPath);
        Assert.Single(lines);
    }

    private static MitsuoshieAlert CreateAlert()
    {
        return new MitsuoshieAlert
        {
            Timestamp = new DateTime(2026, 3, 9, 12, 0, 0, DateTimeKind.Utc),
            HoneyFile = @"C:\Users\test\.aws\credentials.bak",
            HoneyType = HoneyTokenType.AwsCredential,
            EventType = "ReadData",
            Tampered = false,
            OriginalHash = "abc123",
            CurrentHash = "abc123",
            AccessMask = "0x1",
            ProcessId = 4832,
            ProcessName = "stealer.exe",
            ProcessPath = @"C:\temp\stealer.exe",
            User = @"DESKTOP\test",
            Severity = AlertSeverity.Critical
        };
    }
}
