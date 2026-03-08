using System.Security.Cryptography;

namespace Mitsuoshie.Core.Tests.Monitoring;

using Mitsuoshie.Core.Models;
using Mitsuoshie.Core.Monitoring;

public class IntegrityCheckerTests : IDisposable
{
    private readonly string _testDir;
    private readonly SettingsStore _store;

    public IntegrityCheckerTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "mitsuoshie_integrity_test_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_testDir);
        _store = new SettingsStore(Path.Combine(_testDir, "settings.json"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, recursive: true);
    }

    [Fact]
    public void CheckAll_NoChanges_ReturnsEmpty()
    {
        var filePath = CreateTestFile("test.txt", "original content");
        var hash = ComputeHash(filePath);
        _store.AddToken(new DeployedToken(filePath, HoneyTokenType.AwsCredential, hash, DateTime.UtcNow));

        var checker = new IntegrityChecker(_store);
        var alerts = checker.CheckAll();

        Assert.Empty(alerts);
    }

    [Fact]
    public void CheckAll_TamperedFile_ReturnsTamperedAlert()
    {
        var filePath = CreateTestFile("test.txt", "original content");
        var hash = ComputeHash(filePath);
        _store.AddToken(new DeployedToken(filePath, HoneyTokenType.SshKey, hash, DateTime.UtcNow));

        // ファイルを改ざん
        File.WriteAllText(filePath, "tampered content");

        var checker = new IntegrityChecker(_store);
        var alerts = checker.CheckAll();

        Assert.Single(alerts);
        Assert.Equal("Tampered", alerts[0].EventType);
        Assert.True(alerts[0].Tampered);
        Assert.Equal(HoneyTokenType.SshKey, alerts[0].HoneyType);
        Assert.NotEqual(alerts[0].OriginalHash, alerts[0].CurrentHash);
    }

    [Fact]
    public void CheckAll_DeletedFile_ReturnsDeletedAlert()
    {
        var filePath = Path.Combine(_testDir, "deleted.txt");
        _store.AddToken(new DeployedToken(filePath, HoneyTokenType.EnvFile, "somehash", DateTime.UtcNow));

        // ファイルは存在しない

        var checker = new IntegrityChecker(_store);
        var alerts = checker.CheckAll();

        Assert.Single(alerts);
        Assert.Equal("Deleted", alerts[0].EventType);
        Assert.Equal(filePath, alerts[0].HoneyFile);
    }

    [Fact]
    public void CheckAll_MultipleFiles_ChecksAll()
    {
        var file1 = CreateTestFile("file1.txt", "content1");
        var file2 = CreateTestFile("file2.txt", "content2");
        _store.AddToken(new DeployedToken(file1, HoneyTokenType.AwsCredential, ComputeHash(file1), DateTime.UtcNow));
        _store.AddToken(new DeployedToken(file2, HoneyTokenType.SshKey, ComputeHash(file2), DateTime.UtcNow));

        // file2 を改ざん
        File.WriteAllText(file2, "tampered");

        var checker = new IntegrityChecker(_store);
        var alerts = checker.CheckAll();

        Assert.Single(alerts);
        Assert.Equal(file2, alerts[0].HoneyFile);
    }

    [Fact]
    public void CheckAll_AllFilesIntact_ReturnsEmpty()
    {
        var file1 = CreateTestFile("a.txt", "aaa");
        var file2 = CreateTestFile("b.txt", "bbb");
        var file3 = CreateTestFile("c.txt", "ccc");
        _store.AddToken(new DeployedToken(file1, HoneyTokenType.AwsCredential, ComputeHash(file1), DateTime.UtcNow));
        _store.AddToken(new DeployedToken(file2, HoneyTokenType.SshKey, ComputeHash(file2), DateTime.UtcNow));
        _store.AddToken(new DeployedToken(file3, HoneyTokenType.EnvFile, ComputeHash(file3), DateTime.UtcNow));

        var checker = new IntegrityChecker(_store);
        var alerts = checker.CheckAll();

        Assert.Empty(alerts);
    }

    private string CreateTestFile(string name, string content)
    {
        var path = Path.Combine(_testDir, name);
        File.WriteAllText(path, content);
        return path;
    }

    private static string ComputeHash(string filePath)
    {
        var bytes = File.ReadAllBytes(filePath);
        return Convert.ToHexStringLower(SHA256.HashData(bytes));
    }
}
