using System.Security.Cryptography;
using System.Text;

namespace Mitsuoshie.Core.Tests.Deployment;

using Mitsuoshie.Core.Deployment;
using Mitsuoshie.Core.Models;

public class HoneyDeployerTests : IDisposable
{
    private readonly string _testDir;
    private readonly HoneyDeployer _deployer;

    public HoneyDeployerTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "mitsuoshie_test_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_testDir);
        _deployer = new HoneyDeployer(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }

    [Fact]
    public void DeploySingle_CreatesFileAtExpectedPath()
    {
        var result = _deployer.DeploySingle(HoneyTokenType.AwsCredential);

        Assert.NotNull(result);
        var expectedPath = Path.Combine(_testDir, @".aws\credentials.bak");
        Assert.Equal(expectedPath, result.FilePath);
        Assert.True(File.Exists(result.FilePath));
    }

    [Fact]
    public void DeploySingle_CreatesDirectoryIfNotExists()
    {
        _deployer.DeploySingle(HoneyTokenType.AwsCredential);

        var awsDir = Path.Combine(_testDir, ".aws");
        Assert.True(Directory.Exists(awsDir));
    }

    [Fact]
    public void DeploySingle_DoesNotOverwriteExistingFile()
    {
        var filePath = Path.Combine(_testDir, @".aws\credentials.bak");
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath, "existing content");

        var result = _deployer.DeploySingle(HoneyTokenType.AwsCredential);

        // 既存ファイルの内容は上書きされない
        Assert.Equal("existing content", File.ReadAllText(filePath));
        // 再登録用トークンが返される（再インストールシナリオ対応）
        Assert.NotNull(result);
        Assert.Equal(HoneyTokenType.AwsCredential, result.HoneyType);
        Assert.NotEmpty(result.OriginalHash);
    }

    [Fact]
    public void DeploySingle_RecordsSHA256Hash()
    {
        var result = _deployer.DeploySingle(HoneyTokenType.AwsCredential);

        Assert.NotNull(result);
        Assert.NotEmpty(result.OriginalHash);

        // ハッシュを検証
        var fileBytes = File.ReadAllBytes(result.FilePath);
        var expectedHash = Convert.ToHexStringLower(SHA256.HashData(fileBytes));
        Assert.Equal(expectedHash, result.OriginalHash);
    }

    [Fact]
    public void DeploySingle_SetsCorrectHoneyType()
    {
        var result = _deployer.DeploySingle(HoneyTokenType.SshKey);

        Assert.NotNull(result);
        Assert.Equal(HoneyTokenType.SshKey, result.HoneyType);
    }

    [Fact]
    public void DeploySingle_SetsFileTimestampInPast()
    {
        var result = _deployer.DeploySingle(HoneyTokenType.AwsCredential);

        Assert.NotNull(result);
        var lastWrite = File.GetLastWriteTimeUtc(result.FilePath);
        Assert.True(lastWrite < DateTime.UtcNow.AddDays(-30),
            "File timestamp should be set to a past date");
    }

    [Fact]
    public void DeployAll_DeploysAllTokenTypes()
    {
        var results = _deployer.DeployAll();

        var tokenTypes = Enum.GetValues<HoneyTokenType>();
        Assert.Equal(tokenTypes.Length, results.Count);

        foreach (var type in tokenTypes)
        {
            Assert.Contains(results, r => r.HoneyType == type);
        }
    }

    [Fact]
    public void DeployAll_ReregistersExistingFiles()
    {
        // 1つだけ既存ファイルを配置
        var awsPath = Path.Combine(_testDir, @".aws\credentials.bak");
        Directory.CreateDirectory(Path.GetDirectoryName(awsPath)!);
        File.WriteAllText(awsPath, "existing");

        var results = _deployer.DeployAll();

        // 全トークンが返される（既存ファイルも再登録）
        var tokenTypes = Enum.GetValues<HoneyTokenType>();
        Assert.Equal(tokenTypes.Length, results.Count);
        Assert.Contains(results, r => r.HoneyType == HoneyTokenType.AwsCredential);
        // 既存ファイルの内容は上書きされない
        Assert.Equal("existing", File.ReadAllText(awsPath));
    }

    [Fact]
    public void DeploySingle_HiddenFolders_AreHidden()
    {
        // Documents\.secure\ と Desktop\.confidential\ は隠しフォルダ
        _deployer.DeploySingle(HoneyTokenType.PasswordFile);

        var secureDir = Path.Combine(_testDir, @"Documents\.secure");
        Assert.True(Directory.Exists(secureDir));

        var attrs = File.GetAttributes(secureDir);
        Assert.True((attrs & FileAttributes.Hidden) != 0,
            ".secure folder should have Hidden attribute");
    }
}
