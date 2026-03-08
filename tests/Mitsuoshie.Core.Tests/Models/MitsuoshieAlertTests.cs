namespace Mitsuoshie.Core.Tests.Models;

using Mitsuoshie.Core.Models;

public class MitsuoshieAlertTests
{
    [Fact]
    public void MitsuoshieAlert_CanBeCreated()
    {
        var alert = new MitsuoshieAlert
        {
            Timestamp = DateTime.UtcNow,
            HoneyFile = @"C:\Users\test\.aws\credentials.bak",
            HoneyType = HoneyTokenType.AwsCredential,
            EventType = "ReadData",
            Tampered = false,
            OriginalHash = "abc",
            CurrentHash = "abc",
            AccessMask = "0x1",
            ProcessId = 1234,
            ProcessName = "malware.exe",
            ProcessPath = @"C:\temp\malware.exe",
            User = @"DESKTOP\user",
            Severity = AlertSeverity.Critical
        };

        Assert.Equal("ReadData", alert.EventType);
        Assert.False(alert.Tampered);
        Assert.Equal(AlertSeverity.Critical, alert.Severity);
    }

    [Fact]
    public void MitsuoshieAlert_Tampered_WhenHashesDiffer()
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
            AccessMask = "0x2",
            ProcessId = 5678,
            ProcessName = "evil.exe",
            ProcessPath = @"C:\evil.exe",
            User = "user",
            Severity = AlertSeverity.Critical
        };

        Assert.True(alert.Tampered);
        Assert.NotEqual(alert.OriginalHash, alert.CurrentHash);
    }
}
