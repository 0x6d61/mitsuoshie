namespace Mitsuoshie.Core.Tests.Deployment;

using Mitsuoshie.Core.Deployment;
using Mitsuoshie.Core.Models;

public class HoneyTemplatesTests
{
    [Fact]
    public void GenerateContent_AwsCredential_ContainsAwsFormat()
    {
        var content = HoneyTemplates.GenerateContent(HoneyTokenType.AwsCredential);
        Assert.Contains("[default]", content);
        Assert.Contains("aws_access_key_id", content);
        Assert.Contains("aws_secret_access_key", content);
        Assert.Contains("AKIA", content);
    }

    [Fact]
    public void GenerateContent_SshKey_ContainsKeyFormat()
    {
        var content = HoneyTemplates.GenerateContent(HoneyTokenType.SshKey);
        Assert.Contains("BEGIN OPENSSH PRIVATE KEY", content);
        Assert.Contains("END OPENSSH PRIVATE KEY", content);
    }

    [Fact]
    public void GenerateContent_EnvFile_ContainsEnvFormat()
    {
        var content = HoneyTemplates.GenerateContent(HoneyTokenType.EnvFile);
        Assert.Contains("DATABASE_URL=", content);
        Assert.Contains("API_SECRET=", content);
    }

    [Fact]
    public void GenerateContent_CryptoWallet_ContainsBinaryLikeData()
    {
        var content = HoneyTemplates.GenerateContent(HoneyTokenType.CryptoWallet);
        Assert.NotEmpty(content);
    }

    [Fact]
    public void GenerateContent_PasswordFile_ReturnsNonEmpty()
    {
        var content = HoneyTemplates.GenerateContent(HoneyTokenType.PasswordFile);
        Assert.NotEmpty(content);
    }

    [Fact]
    public void GenerateContent_BrowserLoginData_ReturnsNonEmpty()
    {
        var content = HoneyTemplates.GenerateContent(HoneyTokenType.BrowserLoginData);
        Assert.NotEmpty(content);
    }

    [Fact]
    public void GenerateContent_ConfidentialDocument_ReturnsNonEmpty()
    {
        var content = HoneyTemplates.GenerateContent(HoneyTokenType.ConfidentialDocument);
        Assert.NotEmpty(content);
    }

    [Fact]
    public void GenerateContent_ProducesDifferentContentEachTime()
    {
        var content1 = HoneyTemplates.GenerateContent(HoneyTokenType.AwsCredential);
        var content2 = HoneyTemplates.GenerateContent(HoneyTokenType.AwsCredential);
        Assert.NotEqual(content1, content2);
    }

    [Fact]
    public void GetRelativePath_ReturnsExpectedPaths()
    {
        Assert.Equal(@".aws\credentials.bak",
            HoneyTemplates.GetRelativePath(HoneyTokenType.AwsCredential));
        Assert.Equal(@".ssh\id_rsa.old",
            HoneyTemplates.GetRelativePath(HoneyTokenType.SshKey));
        Assert.Equal(@".config\.env.production",
            HoneyTemplates.GetRelativePath(HoneyTokenType.EnvFile));
    }
}
