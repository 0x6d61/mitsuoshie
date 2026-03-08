using System.Security.Cryptography;
using System.Text;

namespace Mitsuoshie.Core.Deployment;

public static class KeyGenerator
{
    private const string AlphanumericChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private const string UpperAlphanumericChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public static string GenerateAwsAccessKeyId()
    {
        // AWS Access Key ID: AKIA + 16 uppercase alphanumeric chars
        return "AKIA" + GenerateRandomFromChars(UpperAlphanumericChars, 16);
    }

    public static string GenerateAwsSecretAccessKey()
    {
        // AWS Secret Access Key: 40 alphanumeric + special chars
        return GenerateRandomAlphanumeric(40);
    }

    public static string GenerateRandomHex(int byteCount)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteCount);
        return Convert.ToHexStringLower(bytes);
    }

    public static string GenerateRandomBase64(int byteCount)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteCount);
        return Convert.ToBase64String(bytes);
    }

    public static string GenerateRandomAlphanumeric(int length)
    {
        return GenerateRandomFromChars(AlphanumericChars, length);
    }

    private static string GenerateRandomFromChars(string chars, int length)
    {
        var sb = new StringBuilder(length);
        for (var i = 0; i < length; i++)
        {
            sb.Append(chars[RandomNumberGenerator.GetInt32(chars.Length)]);
        }
        return sb.ToString();
    }
}
