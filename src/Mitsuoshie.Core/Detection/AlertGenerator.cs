using System.Collections.Concurrent;
using Mitsuoshie.Core.Models;

namespace Mitsuoshie.Core.Detection;

public class AlertGenerator
{
    private int _counter;
    private readonly TimeSpan _suppressDuration;
    private readonly ConcurrentDictionary<string, DateTime> _lastAlertTimes = new();

    public AlertGenerator(TimeSpan? suppressDuration = null)
    {
        _suppressDuration = suppressDuration ?? TimeSpan.FromMinutes(5);
    }

    /// <summary>
    /// 一意のアラートIDを生成する。MITSUOSHIE-YYYY-NNNN 形式。
    /// </summary>
    public string GenerateAlertId()
    {
        var num = Interlocked.Increment(ref _counter);
        return $"MITSUOSHIE-{DateTime.UtcNow.Year}-{num:D4}";
    }

    /// <summary>
    /// アラートを抑制すべきかどうかを判定する。
    /// 同一プロセス + 同一ファイルの組み合わせで抑制期間内なら true。
    /// 初回呼び出し時は記録して false を返す。
    /// </summary>
    public bool ShouldSuppress(MitsuoshieAlert alert)
    {
        var key = $"{alert.ProcessName}|{alert.ProcessId}|{alert.EventType}|{alert.HoneyFile}";

        // 期限切れエントリの定期クリーンアップ（100件超で実行）
        if (_lastAlertTimes.Count > 100)
        {
            PruneExpiredEntries();
        }

        if (_lastAlertTimes.TryGetValue(key, out var lastTime))
        {
            if (DateTime.UtcNow - lastTime < _suppressDuration)
                return true;
        }

        _lastAlertTimes[key] = DateTime.UtcNow;
        return false;
    }

    private void PruneExpiredEntries()
    {
        var now = DateTime.UtcNow;
        foreach (var kvp in _lastAlertTimes)
        {
            if (now - kvp.Value >= _suppressDuration)
            {
                _lastAlertTimes.TryRemove(kvp.Key, out _);
            }
        }
    }
}
