namespace Mitsuoshie.Core.Tests.Monitoring;

using Mitsuoshie.Core.Models;
using Mitsuoshie.Core.Monitoring;

public class SecurityEventSubscriberTests
{
    private readonly SettingsStore _store;
    private readonly SafeProcessFilter _filter;

    public SecurityEventSubscriberTests()
    {
        var tmpPath = Path.Combine(Path.GetTempPath(), "test_settings.json");
        _store = new SettingsStore(tmpPath);
        _store.AddToken(new DeployedToken(
            @"C:\Users\test\.aws\credentials.bak",
            HoneyTokenType.AwsCredential,
            "abc123",
            DateTime.UtcNow
        ));

        _filter = new SafeProcessFilter(
            ["MsMpEng.exe", "SearchIndexer.exe"],
            currentProcessId: 9999
        );
    }

    [Fact]
    public void ProcessEvent_HoneyFileAccess_RaisesAlert()
    {
        var subscriber = new SecurityEventSubscriber(_store, _filter);
        MitsuoshieAlert? capturedAlert = null;
        subscriber.AlertRaised += alert => capturedAlert = alert;

        var evt = new SecurityEventData
        {
            ObjectName = @"C:\Users\test\.aws\credentials.bak",
            AccessMask = "0x1",
            ProcessId = 1234,
            ProcessName = @"C:\temp\stealer.exe",
            UserName = @"DESKTOP\test"
        };

        subscriber.ProcessEvent(evt);

        Assert.NotNull(capturedAlert);
        Assert.Equal(@"C:\Users\test\.aws\credentials.bak", capturedAlert.HoneyFile);
        Assert.Equal(HoneyTokenType.AwsCredential, capturedAlert.HoneyType);
        Assert.Equal("ReadData", capturedAlert.EventType);
        Assert.Equal(1234, capturedAlert.ProcessId);
        Assert.Equal("stealer.exe", capturedAlert.ProcessName);
        Assert.Equal(AlertSeverity.Critical, capturedAlert.Severity);
    }

    [Fact]
    public void ProcessEvent_NonHoneyFile_DoesNotRaiseAlert()
    {
        var subscriber = new SecurityEventSubscriber(_store, _filter);
        MitsuoshieAlert? capturedAlert = null;
        subscriber.AlertRaised += alert => capturedAlert = alert;

        var evt = new SecurityEventData
        {
            ObjectName = @"C:\Users\test\Documents\normal.txt",
            AccessMask = "0x1",
            ProcessId = 1234,
            ProcessName = @"C:\notepad.exe",
            UserName = @"DESKTOP\test"
        };

        subscriber.ProcessEvent(evt);

        Assert.Null(capturedAlert);
    }

    [Fact]
    public void ProcessEvent_SafeProcess_DoesNotRaiseAlert()
    {
        var subscriber = new SecurityEventSubscriber(_store, _filter);
        MitsuoshieAlert? capturedAlert = null;
        subscriber.AlertRaised += alert => capturedAlert = alert;

        var evt = new SecurityEventData
        {
            ObjectName = @"C:\Users\test\.aws\credentials.bak",
            AccessMask = "0x1",
            ProcessId = 1234,
            ProcessName = @"C:\Windows\System32\MsMpEng.exe",
            UserName = @"SYSTEM"
        };

        subscriber.ProcessEvent(evt);

        Assert.Null(capturedAlert);
    }

    [Fact]
    public void ProcessEvent_ReadAttributesOnly_DoesNotRaiseAlert()
    {
        var subscriber = new SecurityEventSubscriber(_store, _filter);
        MitsuoshieAlert? capturedAlert = null;
        subscriber.AlertRaised += alert => capturedAlert = alert;

        var evt = new SecurityEventData
        {
            ObjectName = @"C:\Users\test\.aws\credentials.bak",
            AccessMask = "0x80",
            ProcessId = 1234,
            ProcessName = @"C:\Windows\explorer.exe",
            UserName = @"DESKTOP\test"
        };

        subscriber.ProcessEvent(evt);

        Assert.Null(capturedAlert);
    }

    [Theory]
    [InlineData("0x1", "ReadData")]
    [InlineData("0x2", "WriteData")]
    [InlineData("0x4", "AppendData")]
    [InlineData("0x10000", "Delete")]
    [InlineData("0x81", "ReadData")]     // ReadAttributes + ReadData → ReadData
    [InlineData("0x10002", "Delete")]    // Delete + WriteData → Delete（優先度順）
    [InlineData("0x3", "WriteData")]     // ReadData + WriteData → WriteData（優先度順）
    public void ProcessEvent_CorrectAccessType(string accessMask, string expectedEventType)
    {
        var subscriber = new SecurityEventSubscriber(_store, _filter);
        MitsuoshieAlert? capturedAlert = null;
        subscriber.AlertRaised += alert => capturedAlert = alert;

        var evt = new SecurityEventData
        {
            ObjectName = @"C:\Users\test\.aws\credentials.bak",
            AccessMask = accessMask,
            ProcessId = 1234,
            ProcessName = @"C:\temp\evil.exe",
            UserName = @"DESKTOP\test"
        };

        subscriber.ProcessEvent(evt);

        Assert.NotNull(capturedAlert);
        Assert.Equal(expectedEventType, capturedAlert.EventType);
    }

    [Fact]
    public void ProcessEvent_OwnProcess_DoesNotRaiseAlert()
    {
        var subscriber = new SecurityEventSubscriber(_store, _filter);
        MitsuoshieAlert? capturedAlert = null;
        subscriber.AlertRaised += alert => capturedAlert = alert;

        var evt = new SecurityEventData
        {
            ObjectName = @"C:\Users\test\.aws\credentials.bak",
            AccessMask = "0x1",
            ProcessId = 9999, // 自プロセス
            ProcessName = @"C:\Mitsuoshie\Mitsuoshie.exe",
            UserName = @"DESKTOP\test"
        };

        subscriber.ProcessEvent(evt);

        Assert.Null(capturedAlert);
    }
}
