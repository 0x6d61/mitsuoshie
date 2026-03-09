using System.Diagnostics.Eventing.Reader;
using System.Reflection;
using Mitsuoshie.Core;
using Mitsuoshie.Core.Models;
using Mitsuoshie.Core.Monitoring;

namespace Mitsuoshie.App;

public class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly MitsuoshieService _service;
    private readonly Icon _appIcon;
    private readonly Icon _alertIcon;
    private EventLogWatcher? _eventWatcher;

    public TrayApplicationContext(MitsuoshieServiceConfig config)
    {
        _service = new MitsuoshieService(config);
        _service.AlertRaised += OnAlertRaised;

        _appIcon = LoadAppIcon();
        _alertIcon = CreateAlertIcon();

        _notifyIcon = new NotifyIcon
        {
            Icon = _appIcon,
            Text = "Mitsuoshie — 監視中",
            Visible = true,
            ContextMenuStrip = CreateContextMenu()
        };

        Initialize();
    }

    private void Initialize()
    {
        try
        {
            // 罠ファイル配置 + SACL設定（管理者時）+ FileSystemWatcher開始
            var totalTokens = _service.DeployTokens();

            // 整合性チェック開始
            _service.StartIntegrityTimer();

            // 管理者権限がある場合のみ Security Event Log を購読
            if (_service.IsElevated)
            {
                StartEventLogWatcher();
            }

            var mode = _service.IsElevated ? "完全監視" : "簡易監視";
            UpdateStatus($"{mode}（罠ファイル: {totalTokens}個）");
        }
        catch (Exception ex)
        {
            ShowErrorNotification("初期化エラー", ex.Message);
        }
    }

    private void StartEventLogWatcher()
    {
        try
        {
            var query = new EventLogQuery(
                "Security",
                PathType.LogName,
                "*[System[EventID=4663]]"
            );

            _eventWatcher = new EventLogWatcher(query);
            _eventWatcher.EventRecordWritten += OnSecurityEventReceived;
            _eventWatcher.Enabled = true;
        }
        catch (Exception ex)
        {
            // Security Log の読み取りには権限が必要な場合がある
            ShowErrorNotification("イベントログ購読エラー",
                $"Security Event Log の購読に失敗しました: {ex.Message}");
        }
    }

    private void OnSecurityEventReceived(object? sender, EventRecordWrittenEventArgs e)
    {
        if (e.EventRecord is null) return;

        try
        {
            var record = e.EventRecord;
            // Event ID 4663 のプロパティインデックス:
            //  [0] SubjectUserSid     [1] SubjectUserName
            //  [2] SubjectDomainName  [3] SubjectLogonId
            //  [4] ObjectServer       [5] ObjectType
            //  [6] ObjectName         [7] HandleId
            //  [8] AccessList         [9] AccessMask
            // [10] ProcessId         [11] ProcessName
            var evt = new SecurityEventData
            {
                ObjectName = record.Properties[6]?.Value?.ToString() ?? "",
                AccessMask = record.Properties[9]?.Value?.ToString() ?? "",
                ProcessId = ParseProcessId(record.Properties[10]?.Value),
                ProcessName = record.Properties[11]?.Value?.ToString() ?? "",
                UserName = record.Properties[1]?.Value?.ToString() ?? "",
                Timestamp = record.TimeCreated?.ToUniversalTime()
            };

            _service.ProcessSecurityEvent(evt);
        }
        catch
        {
            // イベント解析エラーは無視（壊れたイベントをスキップ）
        }
    }

    private void OnAlertRaised(MitsuoshieAlert alert)
    {
        // バックグラウンドスレッドから呼ばれるため、UIスレッドにマーシャリング
        if (_notifyIcon.ContextMenuStrip?.InvokeRequired == true)
        {
            _notifyIcon.ContextMenuStrip.BeginInvoke(() => OnAlertRaisedOnUiThread(alert));
        }
        else
        {
            OnAlertRaisedOnUiThread(alert);
        }
    }

    private void OnAlertRaisedOnUiThread(MitsuoshieAlert alert)
    {
        _notifyIcon.Icon = _alertIcon;
        _notifyIcon.Text = $"Mitsuoshie — 検知あり！ {alert.ProcessName}";

        ShowAlertNotification(alert);

        // 10秒後に通常アイコンに戻す
        var timer = new System.Windows.Forms.Timer { Interval = 10000 };
        timer.Tick += (_, _) =>
        {
            _notifyIcon.Icon = _appIcon;
            _notifyIcon.Text = "Mitsuoshie — 監視中";
            timer.Stop();
            timer.Dispose();
        };
        timer.Start();
    }

    private void ShowAlertNotification(MitsuoshieAlert alert)
    {
        var title = "Mitsuoshie — ハニートークン検知";
        var text = $"罠ファイルが{GetAccessDescription(alert.EventType)}されました\n"
                 + $"ファイル: {Path.GetFileName(alert.HoneyFile)}\n"
                 + $"プロセス: {alert.ProcessName} (PID: {alert.ProcessId})";

        _notifyIcon.ShowBalloonTip(10000, title, text, ToolTipIcon.Warning);
    }

    private void ShowErrorNotification(string title, string message)
    {
        _notifyIcon.ShowBalloonTip(5000, $"Mitsuoshie — {title}", message, ToolTipIcon.Error);
    }

    private ContextMenuStrip CreateContextMenu()
    {
        var menu = new ContextMenuStrip();

        var statusItem = new ToolStripMenuItem("ステータス: 初期化中...")
        {
            Name = "statusItem",
            Enabled = false
        };
        menu.Items.Add(statusItem);
        menu.Items.Add(new ToolStripSeparator());

        var redeployItem = new ToolStripMenuItem("罠を再配置", null, (_, _) =>
        {
            var totalTokens = _service.DeployTokens();
            var mode = _service.IsElevated ? "完全監視" : "簡易監視";
            UpdateStatus($"{mode}（罠ファイル: {totalTokens}個 再配置）");
        });
        menu.Items.Add(redeployItem);

        var checkItem = new ToolStripMenuItem("整合性チェック", null, (_, _) =>
        {
            var alerts = _service.CheckIntegrity();
            if (alerts.Count == 0)
            {
                _notifyIcon.ShowBalloonTip(3000, "Mitsuoshie", "全罠ファイルの整合性OK", ToolTipIcon.Info);
            }
            else
            {
                _notifyIcon.ShowBalloonTip(3000, "Mitsuoshie",
                    $"異常検知: {alerts.Count}件", ToolTipIcon.Warning);
            }
        });
        menu.Items.Add(checkItem);

        menu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("終了", null, (_, _) =>
        {
            _service.Stop();
            _notifyIcon.Visible = false;
            Application.Exit();
        });
        menu.Items.Add(exitItem);

        return menu;
    }

    private void UpdateStatus(string status)
    {
        var statusItem = _notifyIcon.ContextMenuStrip?.Items.Find("statusItem", false).FirstOrDefault();
        if (statusItem is not null)
            statusItem.Text = $"ステータス: {status}";
    }

    private static string GetAccessDescription(string eventType)
    {
        return eventType switch
        {
            "ReadData" => "読み取り",
            "WriteData" => "書き込み",
            "Delete" or "Deleted" => "削除",
            "Tampered" => "改ざん",
            "AppendData" => "追記",
            _ => "アクセス"
        };
    }

    /// <summary>
    /// Event ID 4663 の ProcessId を安全にパースする。
    /// 値は int、long、または "0x1A4" のような hex 文字列の場合がある。
    /// </summary>
    private static int ParseProcessId(object? value)
    {
        if (value is null) return 0;
        if (value is int i) return i;
        if (value is long l) return (int)l;
        if (value is ulong ul) return (int)ul;

        var str = value.ToString() ?? "";
        if (str.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return int.TryParse(str.AsSpan(2), System.Globalization.NumberStyles.HexNumber, null, out var hex)
                ? hex : 0;
        }

        return int.TryParse(str, out var dec) ? dec : 0;
    }

    /// <summary>
    /// 埋め込みリソースからアプリアイコンを読み込む。
    /// 読み込みに失敗した場合はフォールバックアイコンを生成する。
    /// </summary>
    private static Icon LoadAppIcon()
    {
        try
        {
            using var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("Mitsuoshie.app.ico");
            if (stream is not null)
                return new Icon(stream, 16, 16);
        }
        catch
        {
            // フォールバック
        }

        // フォールバック: 緑丸アイコン
        var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);
        g.FillEllipse(Brushes.Green, 1, 1, 14, 14);
        return Icon.FromHandle(bmp.GetHicon());
    }

    /// <summary>
    /// アラート時の赤い警告アイコンを生成する。
    /// </summary>
    private static Icon CreateAlertIcon()
    {
        var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);
        g.FillEllipse(Brushes.Red, 1, 1, 14, 14);
        g.DrawString("!", new Font("Arial", 10, FontStyle.Bold), Brushes.White, 2, 0);
        return Icon.FromHandle(bmp.GetHicon());
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _eventWatcher?.Dispose();
            _service.Dispose();
            _notifyIcon.Dispose();
        }
        base.Dispose(disposing);
    }
}
