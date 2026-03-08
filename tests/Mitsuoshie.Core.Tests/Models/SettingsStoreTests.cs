using System.Text.Json;

namespace Mitsuoshie.Core.Tests.Models;

using Mitsuoshie.Core.Models;

public class SettingsStoreTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _settingsPath;

    public SettingsStoreTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "mitsuoshie_settings_test_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_testDir);
        _settingsPath = Path.Combine(_testDir, "settings.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, recursive: true);
    }

    [Fact]
    public void Save_CreatesJsonFile()
    {
        var store = new SettingsStore(_settingsPath);
        var token = new DeployedToken("path", HoneyTokenType.AwsCredential, "hash", DateTime.UtcNow);

        store.AddToken(token);
        store.Save();

        Assert.True(File.Exists(_settingsPath));
    }

    [Fact]
    public void Save_And_Load_PreservesTokens()
    {
        var now = DateTime.UtcNow;
        var store = new SettingsStore(_settingsPath);
        store.AddToken(new DeployedToken(@"C:\test\.aws\credentials.bak", HoneyTokenType.AwsCredential, "hash1", now));
        store.AddToken(new DeployedToken(@"C:\test\.ssh\id_rsa.old", HoneyTokenType.SshKey, "hash2", now));
        store.Save();

        var loaded = SettingsStore.Load(_settingsPath);

        Assert.Equal(2, loaded.Tokens.Count);
        Assert.Contains(loaded.Tokens, t => t.HoneyType == HoneyTokenType.AwsCredential);
        Assert.Contains(loaded.Tokens, t => t.HoneyType == HoneyTokenType.SshKey);
    }

    [Fact]
    public void Load_ReturnsEmpty_WhenFileDoesNotExist()
    {
        var loaded = SettingsStore.Load(Path.Combine(_testDir, "nonexistent.json"));

        Assert.Empty(loaded.Tokens);
    }

    [Fact]
    public void GetOriginalHash_ReturnsHash_ForKnownPath()
    {
        var store = new SettingsStore(_settingsPath);
        store.AddToken(new DeployedToken("path1", HoneyTokenType.AwsCredential, "abc123", DateTime.UtcNow));

        Assert.Equal("abc123", store.GetOriginalHash("path1"));
    }

    [Fact]
    public void GetOriginalHash_ReturnsNull_ForUnknownPath()
    {
        var store = new SettingsStore(_settingsPath);

        Assert.Null(store.GetOriginalHash("unknown"));
    }

    [Fact]
    public void GetHoneyType_ReturnsType_ForKnownPath()
    {
        var store = new SettingsStore(_settingsPath);
        store.AddToken(new DeployedToken("path1", HoneyTokenType.SshKey, "hash", DateTime.UtcNow));

        Assert.Equal(HoneyTokenType.SshKey, store.GetHoneyType("path1"));
    }

    [Fact]
    public void ContainsPath_ReturnsTrue_ForKnownPath()
    {
        var store = new SettingsStore(_settingsPath);
        store.AddToken(new DeployedToken("path1", HoneyTokenType.EnvFile, "hash", DateTime.UtcNow));

        Assert.True(store.ContainsPath("path1"));
        Assert.False(store.ContainsPath("unknown"));
    }

    [Fact]
    public void Save_ProducesValidJson()
    {
        var store = new SettingsStore(_settingsPath);
        store.AddToken(new DeployedToken("path", HoneyTokenType.AwsCredential, "hash", DateTime.UtcNow));
        store.Save();

        var json = File.ReadAllText(_settingsPath);
        var doc = JsonDocument.Parse(json); // パースできれば有効なJSON
        Assert.NotNull(doc);
    }
}
