namespace Mitsuoshie.Core.Tests.Models;

using Mitsuoshie.Core.Models;

public class HoneyTokenTypeTests
{
    [Fact]
    public void HoneyTokenType_HasExpectedValues()
    {
        Assert.True(Enum.IsDefined(typeof(HoneyTokenType), HoneyTokenType.AwsCredential));
        Assert.True(Enum.IsDefined(typeof(HoneyTokenType), HoneyTokenType.SshKey));
        Assert.True(Enum.IsDefined(typeof(HoneyTokenType), HoneyTokenType.EnvFile));
        Assert.True(Enum.IsDefined(typeof(HoneyTokenType), HoneyTokenType.PasswordFile));
        Assert.True(Enum.IsDefined(typeof(HoneyTokenType), HoneyTokenType.CryptoWallet));
        Assert.True(Enum.IsDefined(typeof(HoneyTokenType), HoneyTokenType.BrowserLoginData));
        Assert.True(Enum.IsDefined(typeof(HoneyTokenType), HoneyTokenType.ConfidentialDocument));
    }
}
