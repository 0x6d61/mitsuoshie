namespace Mitsuoshie.Core.Tests.Models;

using Mitsuoshie.Core.Models;

public class DeployedTokenTests
{
    [Fact]
    public void DeployedToken_CanBeCreated()
    {
        var now = DateTime.UtcNow;
        var token = new DeployedToken(
            FilePath: @"C:\Users\test\.aws\credentials.bak",
            HoneyType: HoneyTokenType.AwsCredential,
            OriginalHash: "abc123",
            DeployedAt: now
        );

        Assert.Equal(@"C:\Users\test\.aws\credentials.bak", token.FilePath);
        Assert.Equal(HoneyTokenType.AwsCredential, token.HoneyType);
        Assert.Equal("abc123", token.OriginalHash);
        Assert.Equal(now, token.DeployedAt);
    }

    [Fact]
    public void DeployedToken_IsImmutable_Record()
    {
        var token = new DeployedToken("path", HoneyTokenType.SshKey, "hash", DateTime.UtcNow);
        var copy = token with { FilePath = "new_path" };

        Assert.NotEqual(token.FilePath, copy.FilePath);
        Assert.Equal(token.HoneyType, copy.HoneyType);
    }
}
