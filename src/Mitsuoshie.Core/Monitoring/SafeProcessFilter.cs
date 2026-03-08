namespace Mitsuoshie.Core.Monitoring;

public class SafeProcessFilter
{
    private const string ReadAttributesMask = "0x80";

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
        if (accessMask == ReadAttributesMask)
            return true;

        var processName = Path.GetFileName(processPath);

        // 常時除外リストに含まれるプロセス
        if (_safeProcessNames.Contains(processName))
            return true;

        // explorer.exe は ReadAttributes のみ除外（上で処理済み）
        // それ以外のアクセスマスクならアラート対象
        return false;
    }
}
