namespace Mitsuoshie.Core.Tests.Logging;

using Mitsuoshie.Core.Logging;
using Mitsuoshie.Core.Models;

public class WindowsEventLoggerTests
{
    [Fact]
    public void GetEventId_ReadData_Returns1000()
    {
        Assert.Equal(1000, WindowsEventLogger.GetEventId("ReadData"));
    }

    [Fact]
    public void GetEventId_WriteData_Returns1001()
    {
        Assert.Equal(1001, WindowsEventLogger.GetEventId("WriteData"));
    }

    [Fact]
    public void GetEventId_Delete_Returns1002()
    {
        Assert.Equal(1002, WindowsEventLogger.GetEventId("Delete"));
    }

    [Fact]
    public void GetEventId_Tampered_Returns1003()
    {
        Assert.Equal(1003, WindowsEventLogger.GetEventId("Tampered"));
    }

    [Fact]
    public void GetEventId_Deleted_Returns1002()
    {
        Assert.Equal(1002, WindowsEventLogger.GetEventId("Deleted"));
    }

    [Fact]
    public void GetEventId_Unknown_Returns1000()
    {
        Assert.Equal(1000, WindowsEventLogger.GetEventId("Unknown"));
    }

    [Fact]
    public void FormatMessage_ContainsAllFields()
    {
        var alert = new MitsuoshieAlert
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

        var message = WindowsEventLogger.FormatMessage(alert);

        Assert.Contains("Honeytoken access detected", message);
        Assert.Contains(@"C:\Users\test\.aws\credentials.bak", message);
        Assert.Contains("ReadData", message);
        Assert.Contains("stealer.exe", message);
        Assert.Contains("4832", message);
        Assert.Contains(@"DESKTOP\test", message);
        Assert.Contains("abc123", message);
    }

    [Fact]
    public void WriteAlert_DoesNotThrow_InNonAdminSession()
    {
        var logger = new WindowsEventLogger();
        var alert = new MitsuoshieAlert
        {
            Timestamp = DateTime.UtcNow,
            HoneyFile = "file",
            HoneyType = HoneyTokenType.SshKey,
            EventType = "ReadData",
            Tampered = false,
            OriginalHash = "hash",
            CurrentHash = "hash",
            AccessMask = "0x1",
            ProcessId = 123,
            ProcessName = "test.exe",
            ProcessPath = "",
            User = "user",
            Severity = AlertSeverity.Warning
        };

        // 非管理者環境では SecurityException が出るが、例外は投げない
        var ex = Record.Exception(() => logger.WriteAlert(alert));
        Assert.Null(ex);
    }

    [Fact]
    public void WriteServiceStart_DoesNotThrow_InNonAdminSession()
    {
        var logger = new WindowsEventLogger();
        var ex = Record.Exception(() => logger.WriteServiceStart());
        Assert.Null(ex);
    }

    [Fact]
    public void WriteServiceStop_DoesNotThrow_InNonAdminSession()
    {
        var logger = new WindowsEventLogger();
        var ex = Record.Exception(() => logger.WriteServiceStop());
        Assert.Null(ex);
    }

    [Fact]
    public void FormatMessage_TamperedAlert_ShowsTampered()
    {
        var alert = new MitsuoshieAlert
        {
            Timestamp = DateTime.UtcNow,
            HoneyFile = "file",
            HoneyType = HoneyTokenType.SshKey,
            EventType = "Tampered",
            Tampered = true,
            OriginalHash = "original",
            CurrentHash = "modified",
            AccessMask = "",
            ProcessId = 0,
            ProcessName = "unknown",
            ProcessPath = "",
            User = "",
            Severity = AlertSeverity.Critical
        };

        var message = WindowsEventLogger.FormatMessage(alert);

        Assert.Contains("Tampered: true", message);
    }
}
