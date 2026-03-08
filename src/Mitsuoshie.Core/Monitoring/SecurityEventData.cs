namespace Mitsuoshie.Core.Monitoring;

/// <summary>
/// Security Event Log の Event ID 4663 から抽出したデータ。
/// EventLogWatcher からの生イベントをこの形式に変換して処理する。
/// </summary>
public record SecurityEventData
{
    /// <summary>アクセスされたオブジェクトのパス（Object Name）</summary>
    public required string ObjectName { get; init; }

    /// <summary>アクセスマスク（0x1=ReadData, 0x2=WriteData, 0x10000=Delete 等）</summary>
    public required string AccessMask { get; init; }

    /// <summary>アクセスしたプロセスのID</summary>
    public required int ProcessId { get; init; }

    /// <summary>アクセスしたプロセスのフルパス</summary>
    public required string ProcessName { get; init; }

    /// <summary>ユーザー名</summary>
    public required string UserName { get; init; }

    /// <summary>イベント発生日時（省略時は現在時刻）</summary>
    public DateTime? Timestamp { get; init; }
}
