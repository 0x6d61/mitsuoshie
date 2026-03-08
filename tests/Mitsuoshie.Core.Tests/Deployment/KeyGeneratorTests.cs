namespace Mitsuoshie.Core.Tests.Deployment;

using Mitsuoshie.Core.Deployment;

public class KeyGeneratorTests
{
    [Fact]
    public void GenerateAwsAccessKeyId_StartsWithAKIA_And_Has20Chars()
    {
        var key = KeyGenerator.GenerateAwsAccessKeyId();
        Assert.StartsWith("AKIA", key);
        Assert.Equal(20, key.Length);
    }

    [Fact]
    public void GenerateAwsSecretAccessKey_Has40Chars()
    {
        var key = KeyGenerator.GenerateAwsSecretAccessKey();
        Assert.Equal(40, key.Length);
    }

    [Fact]
    public void GenerateRandomHex_ReturnsCorrectLength()
    {
        var hex = KeyGenerator.GenerateRandomHex(32);
        Assert.Equal(64, hex.Length); // 32 bytes = 64 hex chars
        Assert.Matches("^[0-9a-f]+$", hex);
    }

    [Fact]
    public void GenerateRandomBase64_ReturnsNonEmpty()
    {
        var b64 = KeyGenerator.GenerateRandomBase64(48);
        Assert.NotEmpty(b64);
    }

    [Fact]
    public void GenerateRandomAlphanumeric_ReturnsCorrectLength()
    {
        var str = KeyGenerator.GenerateRandomAlphanumeric(32);
        Assert.Equal(32, str.Length);
        Assert.Matches("^[A-Za-z0-9]+$", str);
    }

    [Fact]
    public void GeneratedKeys_AreDifferentEachTime()
    {
        var key1 = KeyGenerator.GenerateAwsAccessKeyId();
        var key2 = KeyGenerator.GenerateAwsAccessKeyId();
        Assert.NotEqual(key1, key2);
    }
}
