namespace Mitsuoshie.Core.Tests.Deployment;

using Mitsuoshie.Core.Deployment;

public class SaclConfiguratorTests : IDisposable
{
    private readonly string _testDir;

    public SaclConfiguratorTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "mitsuoshie_sacl_test_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, recursive: true);
    }

    [Fact]
    public void IsAdministrator_ReturnsBool()
    {
        // 実行環境によって true/false は変わるが、例外は出ない
        var result = SaclConfigurator.IsAdministrator();
        Assert.IsType<bool>(result);
    }

    [Fact]
    public void SetAuditRule_ThrowsFileNotFoundException_ForMissingFile()
    {
        var missingFile = Path.Combine(_testDir, "nonexistent.txt");

        Assert.Throws<FileNotFoundException>(() =>
            SaclConfigurator.SetAuditRule(missingFile));
    }

    [Fact]
    public void SetAuditRule_ThrowsArgumentNullException_ForNullPath()
    {
        Assert.Throws<ArgumentNullException>(() =>
            SaclConfigurator.SetAuditRule(null!));
    }

    [Fact]
    public void SetAuditRule_ThrowsArgumentException_ForEmptyPath()
    {
        Assert.Throws<ArgumentException>(() =>
            SaclConfigurator.SetAuditRule(""));
    }

    [Fact]
    public void EnableFileSystemAuditing_DoesNotThrow_WhenNotAdmin()
    {
        // 管理者でない場合は InvalidOperationException
        if (!SaclConfigurator.IsAdministrator())
        {
            Assert.Throws<InvalidOperationException>(() =>
                SaclConfigurator.EnableFileSystemAuditing());
        }
    }
}
