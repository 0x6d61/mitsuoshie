using Mitsuoshie.Core.Models;

namespace Mitsuoshie.Core.Monitoring;

/// <summary>
/// FileSystemWatcher を使った罠ファイル監視。
/// 管理者権限不要で書き込み・削除・リネームを検知する。
/// SACL + Event ID 4663 が使えない場合のフォールバック。
/// </summary>
public class HoneyFileWatcher : IDisposable
{
    private readonly SettingsStore _store;
    private readonly List<FileSystemWatcher> _watchers = [];

    public event Action<SecurityEventData>? FileAccessed;

    public HoneyFileWatcher(SettingsStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    /// <summary>
    /// 全罠ファイルの親ディレクトリに FileSystemWatcher を設定する。
    /// </summary>
    public void Start()
    {
        Stop();

        // 罠ファイルのディレクトリをグルーピング（同じディレクトリに複数ある場合）
        var dirToFiles = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in _store.GetAllPaths())
        {
            var dir = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                continue;

            if (!dirToFiles.ContainsKey(dir))
                dirToFiles[dir] = [];
            dirToFiles[dir].Add(Path.GetFileName(path));
        }

        foreach (var (dir, fileNames) in dirToFiles)
        {
            foreach (var fileName in fileNames)
            {
                try
                {
                    var watcher = new FileSystemWatcher(dir, fileName)
                    {
                        NotifyFilter = NotifyFilters.LastWrite
                                     | NotifyFilters.Size
                                     | NotifyFilters.FileName,
                        EnableRaisingEvents = true
                    };

                    watcher.Changed += (_, e) => OnFileEvent(e.FullPath, "WriteData");
                    watcher.Deleted += (_, e) => OnFileEvent(e.FullPath, "Delete");
                    watcher.Renamed += (_, e) => OnFileEvent(e.OldFullPath, "Delete");

                    _watchers.Add(watcher);
                }
                catch
                {
                    // ディレクトリが存在しない等のエラーは無視
                }
            }
        }
    }

    public void Stop()
    {
        foreach (var w in _watchers)
            w.Dispose();
        _watchers.Clear();
    }

    public void Dispose() => Stop();

    private int _eventSequence;

    private void OnFileEvent(string filePath, string accessType)
    {
        if (!_store.ContainsPath(filePath))
            return;

        // 各イベントに一意のシーケンス番号を付与（重複抑制で別イベントが
        // 同一キーにならないようにする）
        var seq = Interlocked.Increment(ref _eventSequence);

        var evt = new SecurityEventData
        {
            ObjectName = filePath,
            AccessMask = accessType switch
            {
                "WriteData" => "0x2",
                "Delete" => "0x10000",
                _ => "0x1"
            },
            ProcessId = seq,
            ProcessName = "FileSystemWatcher",
            UserName = Environment.UserName,
            Timestamp = DateTime.UtcNow
        };

        FileAccessed?.Invoke(evt);
    }
}
