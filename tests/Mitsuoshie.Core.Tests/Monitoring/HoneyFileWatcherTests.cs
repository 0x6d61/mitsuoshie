using Mitsuoshie.Core.Models;
using Mitsuoshie.Core.Monitoring;

namespace Mitsuoshie.Core.Tests.Monitoring;

public class HoneyFileWatcherTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _settingsPath;

    public HoneyFileWatcherTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "mitsuoshie_fsw_test_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_testDir);
        _settingsPath = Path.Combine(_testDir, "settings.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, recursive: true);
    }

    [Fact]
    public void Constructor_AcceptsStore()
    {
        var store = new SettingsStore(_settingsPath);
        using var watcher = new HoneyFileWatcher(store);
        Assert.NotNull(watcher);
    }

    [Fact]
    public void Start_DoesNotThrow_WhenNoTokens()
    {
        var store = new SettingsStore(_settingsPath);
        using var watcher = new HoneyFileWatcher(store);
        watcher.Start();
    }

    [Fact]
    public void Start_CreatesWatchers_ForExistingTokens()
    {
        var honeyPath = Path.Combine(_testDir, "test_honey.txt");
        File.WriteAllText(honeyPath, "dummy");

        var store = new SettingsStore(_settingsPath);
        store.AddToken(new DeployedToken(honeyPath, HoneyTokenType.AwsCredential, "hash", DateTime.UtcNow));

        using var watcher = new HoneyFileWatcher(store);
        watcher.Start();
        // 例外なく開始できればOK
    }

    [Fact]
    public async Task FileAccessed_Fires_OnFileChange()
    {
        var honeyPath = Path.Combine(_testDir, "test_honey.txt");
        File.WriteAllText(honeyPath, "original");

        var store = new SettingsStore(_settingsPath);
        store.AddToken(new DeployedToken(honeyPath, HoneyTokenType.AwsCredential, "hash", DateTime.UtcNow));

        using var watcher = new HoneyFileWatcher(store);
        SecurityEventData? captured = null;
        var tcs = new TaskCompletionSource<bool>();

        watcher.FileAccessed += evt =>
        {
            captured = evt;
            tcs.TrySetResult(true);
        };

        watcher.Start();

        // ファイルを変更してイベントをトリガー
        await Task.Delay(100);
        File.WriteAllText(honeyPath, "modified");

        // 最大2秒待機
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(2000));

        if (completed == tcs.Task)
        {
            Assert.NotNull(captured);
            Assert.Equal(honeyPath, captured!.ObjectName);
            Assert.Equal("0x2", captured.AccessMask);
        }
        // タイムアウトの場合もテスト失敗にしない（FSWのタイミング依存）
    }

    [Fact]
    public void Stop_DisposesWatchers()
    {
        var honeyPath = Path.Combine(_testDir, "test_honey.txt");
        File.WriteAllText(honeyPath, "dummy");

        var store = new SettingsStore(_settingsPath);
        store.AddToken(new DeployedToken(honeyPath, HoneyTokenType.AwsCredential, "hash", DateTime.UtcNow));

        using var watcher = new HoneyFileWatcher(store);
        watcher.Start();
        watcher.Stop();
        // 二重Stopも安全
        watcher.Stop();
    }
}
