namespace Mitsuoshie.Core.Monitoring;

public class SafeProcessFilter
{
    private const int ReadAttributesFlag = 0x80;

    private readonly HashSet<string> _safeProcessNames;
    private readonly int _currentProcessId;

    public SafeProcessFilter(IEnumerable<string> safeProcessNames, int currentProcessId)
    {
        _safeProcessNames = new HashSet<string>(
            safeProcessNames,
            StringComparer.OrdinalIgnoreCase
        );
        _currentProcessId = currentProcessId;
    }

    /// <summary>
    /// プロセスが安全（アラート不要）かどうかを判定する。
    /// </summary>
    public bool IsSafe(string processPath, string accessMask, int processId)
    {
        // Mitsuoshie 自身のアクセスは除外
        if (processId == _currentProcessId)
            return true;

        // ReadAttributes (0x80) のみのアクセスは常に除外（フォルダ表示等）
        if (IsReadAttributesOnly(accessMask))
            return true;

        var processName = Path.GetFileName(processPath);

        // 常時除外リストに含まれるプロセス
        if (_safeProcessNames.Contains(processName))
            return true;

        return false;
    }

    /// <summary>
    /// AccessMask が ReadAttributes (0x80) のみかどうかを判定する。
    /// 複合フラグにも対応。
    /// </summary>
    private static bool IsReadAttributesOnly(string accessMask)
    {
        if (string.IsNullOrEmpty(accessMask)) return false;

        int mask;
        if (accessMask.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            if (!int.TryParse(accessMask.AsSpan(2),
                System.Globalization.NumberStyles.HexNumber, null, out mask))
                return false;
        }
        else if (!int.TryParse(accessMask, out mask))
        {
            return false;
        }

        // ReadAttributes ビットが立っていて、かつデータアクセスビットが立っていない
        const int dataAccessBits = 0x1 | 0x2 | 0x4 | 0x10000; // Read|Write|Append|Delete
        return (mask & ReadAttributesFlag) != 0 && (mask & dataAccessBits) == 0;
    }
}
