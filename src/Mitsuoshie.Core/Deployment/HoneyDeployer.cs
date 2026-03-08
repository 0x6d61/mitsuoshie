using System.Security.Cryptography;
using System.Text;
using Mitsuoshie.Core.Models;

namespace Mitsuoshie.Core.Deployment;

public class HoneyDeployer
{
    private readonly string _userProfileDir;

    // 隠しフォルダ属性を付与するディレクトリ名
    private static readonly HashSet<string> HiddenFolderNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ".secure",
        ".confidential"
    };

    public HoneyDeployer(string userProfileDir)
    {
        _userProfileDir = userProfileDir ?? throw new ArgumentNullException(nameof(userProfileDir));
    }

    public DeployedToken? DeploySingle(HoneyTokenType type)
    {
        var relativePath = HoneyTemplates.GetRelativePath(type);
        var fullPath = Path.Combine(_userProfileDir, relativePath);

        // 既存ファイルは絶対に上書きしない
        if (File.Exists(fullPath))
        {
            return null;
        }

        // ディレクトリ作成
        var dirPath = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(dirPath);

        // 隠しフォルダ属性の付与
        SetHiddenAttributeIfNeeded(dirPath);

        // コンテンツ生成・書き込み
        var content = HoneyTemplates.GenerateContent(type);
        File.WriteAllText(fullPath, content, Encoding.UTF8);

        // ファイルの更新日時を過去に設定（3〜12ヶ月前のランダムな日時）
        var daysAgo = RandomNumberGenerator.GetInt32(90, 365);
        var pastDate = DateTime.UtcNow.AddDays(-daysAgo);
        File.SetLastWriteTimeUtc(fullPath, pastDate);
        File.SetCreationTimeUtc(fullPath, pastDate.AddDays(-RandomNumberGenerator.GetInt32(1, 30)));

        // SHA256 ハッシュ計算
        var hash = ComputeSHA256(fullPath);

        return new DeployedToken(
            FilePath: fullPath,
            HoneyType: type,
            OriginalHash: hash,
            DeployedAt: DateTime.UtcNow
        );
    }

    public List<DeployedToken> DeployAll()
    {
        var results = new List<DeployedToken>();

        foreach (var type in Enum.GetValues<HoneyTokenType>())
        {
            var result = DeploySingle(type);
            if (result is not null)
            {
                results.Add(result);
            }
        }

        return results;
    }

    private void SetHiddenAttributeIfNeeded(string dirPath)
    {
        // ディレクトリ階層を走査して、隠しフォルダ対象を探す
        var dir = new DirectoryInfo(dirPath);
        while (dir is not null && dir.FullName != _userProfileDir)
        {
            if (HiddenFolderNames.Contains(dir.Name))
            {
                dir.Attributes |= FileAttributes.Hidden;
            }
            dir = dir.Parent;
        }
    }

    private static string ComputeSHA256(string filePath)
    {
        var fileBytes = File.ReadAllBytes(filePath);
        return Convert.ToHexStringLower(SHA256.HashData(fileBytes));
    }
}
