using Mitsuoshie.Core.Models;

namespace Mitsuoshie.Core.Deployment;

public static class HoneyTemplates
{
    public static string GenerateContent(HoneyTokenType type)
    {
        return type switch
        {
            HoneyTokenType.AwsCredential => GenerateAwsCredential(),
            HoneyTokenType.SshKey => GenerateSshKey(),
            HoneyTokenType.EnvFile => GenerateEnvFile(),
            HoneyTokenType.PasswordFile => GeneratePasswordFile(),
            HoneyTokenType.CryptoWallet => GenerateCryptoWallet(),
            HoneyTokenType.BrowserLoginData => GenerateBrowserLoginData(),
            HoneyTokenType.ConfidentialDocument => GenerateConfidentialDocument(),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    public static string GetRelativePath(HoneyTokenType type)
    {
        return type switch
        {
            HoneyTokenType.AwsCredential => @".aws\credentials.bak",
            HoneyTokenType.SshKey => @".ssh\id_rsa.old",
            HoneyTokenType.EnvFile => @".config\.env.production",
            HoneyTokenType.PasswordFile => @"Documents\.secure\passwords.xlsx",
            HoneyTokenType.CryptoWallet => @"AppData\Roaming\Bitcoin\wallet.dat.bak",
            HoneyTokenType.BrowserLoginData => @"AppData\Local\Google\Chrome\User Data\Login Data.bak",
            HoneyTokenType.ConfidentialDocument => @"Desktop\.confidential\重要_機密情報.docx",
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    private static string GenerateAwsCredential()
    {
        return $"""
            [default]
            aws_access_key_id = {KeyGenerator.GenerateAwsAccessKeyId()}
            aws_secret_access_key = {KeyGenerator.GenerateAwsSecretAccessKey()}
            region = ap-northeast-1
            """;
    }

    private static string GenerateSshKey()
    {
        var keyData = KeyGenerator.GenerateRandomBase64(192);
        var lines = new List<string> { "-----BEGIN OPENSSH PRIVATE KEY-----" };

        for (var i = 0; i < keyData.Length; i += 70)
        {
            lines.Add(keyData.Substring(i, Math.Min(70, keyData.Length - i)));
        }

        lines.Add("-----END OPENSSH PRIVATE KEY-----");
        return string.Join(Environment.NewLine, lines);
    }

    private static string GenerateEnvFile()
    {
        var dbPassword = KeyGenerator.GenerateRandomAlphanumeric(24);
        var apiSecret = KeyGenerator.GenerateRandomAlphanumeric(32);
        var stripeKey = KeyGenerator.GenerateRandomAlphanumeric(32);

        return $"""
            DATABASE_URL=postgresql://admin:{dbPassword}@db.internal.local:5432/prod
            API_SECRET=sk_live_{apiSecret}
            STRIPE_KEY=pk_live_{stripeKey}
            REDIS_URL=redis://cache.internal.local:6379/0
            JWT_SECRET={KeyGenerator.GenerateRandomHex(32)}
            """;
    }

    private static string GeneratePasswordFile()
    {
        // CSV形式のパスワードリストを装う
        var lines = new List<string>
        {
            "Service,Username,Password,URL,Notes"
        };

        var services = new[]
        {
            ("Gmail", "user@gmail.com", "https://mail.google.com"),
            ("AWS Console", "admin", "https://console.aws.amazon.com"),
            ("GitHub", "developer", "https://github.com"),
            ("Slack", "user@company.com", "https://company.slack.com"),
            ("VPN", "vpn_user", "https://vpn.company.local"),
        };

        foreach (var (service, username, url) in services)
        {
            var password = KeyGenerator.GenerateRandomAlphanumeric(16);
            lines.Add($"{service},{username},{password},{url},");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string GenerateCryptoWallet()
    {
        // Bitcoin wallet.dat のヘッダーを模倣したバイナリ風データ
        return $"""
            # Bitcoin Core wallet backup
            # Generated: {DateTime.UtcNow:yyyy-MM-dd}
            # WARNING: Keep this file secure

            wallet_key={KeyGenerator.GenerateRandomHex(32)}
            master_seed={KeyGenerator.GenerateRandomHex(64)}
            encryption_iv={KeyGenerator.GenerateRandomHex(16)}
            checksum={KeyGenerator.GenerateRandomHex(8)}
            """;
    }

    private static string GenerateBrowserLoginData()
    {
        // SQLite のヘッダーを模倣（Chrome Login Data はSQLite DB）
        var header = "SQLite format 3\0";
        var dummyData = KeyGenerator.GenerateRandomBase64(256);
        return $"{header}{Environment.NewLine}{dummyData}";
    }

    private static string GenerateConfidentialDocument()
    {
        // Office Open XML (docx) のヘッダー風データ
        return $"""
            PK\x03\x04
            [Content_Types].xml
            word/document.xml

            社外秘 - 人事評価資料
            2026年度 部門別業績評価

            管理番号: DOC-{KeyGenerator.GenerateRandomAlphanumeric(8)}
            最終更新: {DateTime.UtcNow:yyyy-MM-dd}

            {KeyGenerator.GenerateRandomBase64(128)}
            """;
    }
}
