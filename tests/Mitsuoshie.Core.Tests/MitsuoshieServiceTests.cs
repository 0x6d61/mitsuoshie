using System.Security.Cryptography;

namespace Mitsuoshie.Core.Tests;

using Mitsuoshie.Core.Detection;
using Mitsuoshie.Core.Logging;
using Mitsuoshie.Core.Models;
using Mitsuoshie.Core.Monitoring;

public class MitsuoshieServiceTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _settingsPath;
    private readonly string _logPath;

    public MitsuoshieServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "mitsuoshie_svc_test_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_testDir);
        _settingsPath = Path.Combine(_testDir, "settings.json");
        _logPath = Path.Combine(_testDir, "sysmon.jsonl");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, recursive: true);
    }

    [Fact]
    public void Constructor_CreatesInstance()
    {
        var config = CreateConfig();
        var service = new MitsuoshieService(config);

        Assert.NotNull(service);
    }

    [Fact]
    public void DeployTokens_CreatesHoneyFiles()
    {
        var config = CreateConfig();
        var service = new MitsuoshieService(config);

        var totalCount = service.DeployTokens();

        Assert.True(totalCount > 0);
        var loaded = SettingsStore.Load(_settingsPath);
        foreach (var token in loaded.Tokens)
        {
            Assert.True(File.Exists(token.FilePath));
        }
    }

    [Fact]
    public void DeployTokens_SavesSettings()
    {
        var config = CreateConfig();
        var service = new MitsuoshieService(config);

        service.DeployTokens();

        Assert.True(File.Exists(_settingsPath));
        var loaded = SettingsStore.Load(_settingsPath);
        Assert.NotEmpty(loaded.Tokens);
    }

    [Fact]
    public void DeployTokens_ReturnsTotalCount_IncludingExisting()
    {
        var config = CreateConfig();
        var service = new MitsuoshieService(config);

        // 1回目: 全罠ファイル配置
        var firstCount = service.DeployTokens();
        Assert.True(firstCount > 0);

        // 2回目: 既に存在するので新規は0だが、合計数は同じ
        var secondCount = service.DeployTokens();
        Assert.Equal(firstCount, secondCount);
    }

    [Fact]
    public void DeployTokens_ReregistersExistingFiles()
    {
        // 1つ先に配置（再インストールシナリオ）
        var awsPath = Path.Combine(_testDir, "profile", ".aws", "credentials.bak");
        Directory.CreateDirectory(Path.GetDirectoryName(awsPath)!);
        File.WriteAllText(awsPath, "existing");

        var config = CreateConfig();
        var service = new MitsuoshieService(config);
        var totalCount = service.DeployTokens();

        // 既存ファイルも監視対象として再登録される（再インストール対応）
        var loaded = SettingsStore.Load(_settingsPath);
        Assert.Contains(loaded.Tokens, t => t.HoneyType == HoneyTokenType.AwsCredential);
        Assert.Equal(loaded.Tokens.Count, totalCount);
        // 既存ファイルの内容は上書きされない
        Assert.Equal("existing", File.ReadAllText(awsPath));
    }

    [Fact]
    public void ProcessSecurityEvent_HoneyAccess_WritesJsonLog()
    {
        var config = CreateConfig();
        var service = new MitsuoshieService(config);
        service.DeployTokens();

        // 罠ファイルのパスを取得
        var store = SettingsStore.Load(_settingsPath);
        var token = store.Tokens[0];

        var evt = new SecurityEventData
        {
            ObjectName = token.FilePath,
            AccessMask = "0x1",
            ProcessId = 1234,
            ProcessName = @"C:\temp\evil.exe",
            UserName = @"DESKTOP\test"
        };

        service.ProcessSecurityEvent(evt);

        Assert.True(File.Exists(_logPath));
        var lines = File.ReadAllLines(_logPath);
        Assert.Single(lines);
    }

    [Fact]
    public void ProcessSecurityEvent_SafeProcess_NoLog()
    {
        var config = CreateConfig();
        var service = new MitsuoshieService(config);
        service.DeployTokens();

        var store = SettingsStore.Load(_settingsPath);
        var token = store.Tokens[0];

        var evt = new SecurityEventData
        {
            ObjectName = token.FilePath,
            AccessMask = "0x1",
            ProcessId = 1234,
            ProcessName = @"C:\Windows\System32\MsMpEng.exe",
            UserName = "SYSTEM"
        };

        service.ProcessSecurityEvent(evt);

        Assert.False(File.Exists(_logPath));
    }

    [Fact]
    public void CheckIntegrity_NoChanges_NoAlerts()
    {
        var config = CreateConfig();
        var service = new MitsuoshieService(config);
        service.DeployTokens();

        var alerts = service.CheckIntegrity();

        Assert.Empty(alerts);
    }

    [Fact]
    public void CheckIntegrity_TamperedFile_ReturnsAlert()
    {
        var config = CreateConfig();
        var service = new MitsuoshieService(config);
        service.DeployTokens();

        // ファイルを改ざん
        var store = SettingsStore.Load(_settingsPath);
        File.WriteAllText(store.Tokens[0].FilePath, "tampered!");

        var alerts = service.CheckIntegrity();

        Assert.NotEmpty(alerts);
        Assert.Contains(alerts, a => a.EventType == "Tampered");
    }

    [Fact]
    public void AlertRaised_Event_IsFired()
    {
        var config = CreateConfig();
        var service = new MitsuoshieService(config);
        service.DeployTokens();

        MitsuoshieAlert? capturedAlert = null;
        service.AlertRaised += alert => capturedAlert = alert;

        var store = SettingsStore.Load(_settingsPath);
        var token = store.Tokens[0];

        var evt = new SecurityEventData
        {
            ObjectName = token.FilePath,
            AccessMask = "0x1",
            ProcessId = 5555,
            ProcessName = @"C:\temp\stealer.exe",
            UserName = "user"
        };

        service.ProcessSecurityEvent(evt);

        Assert.NotNull(capturedAlert);
    }

    private MitsuoshieServiceConfig CreateConfig()
    {
        return new MitsuoshieServiceConfig
        {
            UserProfileDir = Path.Combine(_testDir, "profile"),
            SettingsPath = _settingsPath,
            SysmonLogPath = _logPath,
            SafeProcessNames = ["MsMpEng.exe", "SearchIndexer.exe"],
            IntegrityCheckIntervalMinutes = 30,
            SuppressDuplicateMinutes = 5,
            EnableWindowsEventLog = false // テスト時は無効（管理者権限不要）
        };
    }
}
