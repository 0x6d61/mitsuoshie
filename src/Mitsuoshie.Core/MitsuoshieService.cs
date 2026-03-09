using Mitsuoshie.Core.Deployment;
using Mitsuoshie.Core.Detection;
using Mitsuoshie.Core.Logging;
using Mitsuoshie.Core.Models;
using Mitsuoshie.Core.Monitoring;

namespace Mitsuoshie.Core;

public class MitsuoshieService : IDisposable
{
    private readonly MitsuoshieServiceConfig _config;
    private readonly HoneyDeployer _deployer;
    private readonly AlertGenerator _alertGenerator;
    private readonly SysmonJsonLogger _sysmonLogger;
    private readonly WindowsEventLogger? _eventLogger;
    private SettingsStore _store;
    private SecurityEventSubscriber? _subscriber;
    private SafeProcessFilter? _filter;
    private HoneyFileWatcher? _fileWatcher;
    private Timer? _integrityTimer;

    /// <summary>
    /// 管理者権限で実行中かどうか。
    /// </summary>
    public bool IsElevated { get; private set; }

    public event Action<MitsuoshieAlert>? AlertRaised;

    public MitsuoshieService(MitsuoshieServiceConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _deployer = new HoneyDeployer(config.UserProfileDir);
        _alertGenerator = new AlertGenerator(TimeSpan.FromMinutes(config.SuppressDuplicateMinutes));
        _sysmonLogger = new SysmonJsonLogger(config.SysmonLogPath);
        _store = SettingsStore.Load(config.SettingsPath);

        if (config.EnableWindowsEventLog)
            _eventLogger = new WindowsEventLogger();
    }

    /// <summary>
    /// 罠ファイルを配置し、SACL を設定して設定を保存する。
    /// 戻り値は全トークン数（新規＋既存）。
    /// </summary>
    public int DeployTokens()
    {
        IsElevated = Deployment.SaclConfigurator.IsAdministrator();

        // 管理者権限がある場合、ファイルシステム監査を有効化 + SACL 設定
        if (IsElevated)
        {
            TryEnableAuditing();
        }

        var newTokens = _deployer.DeployAll();

        foreach (var token in newTokens)
        {
            _store.AddToken(token);
        }

        // 管理者の場合のみ SACL を設定
        if (IsElevated)
        {
            foreach (var path in _store.GetAllPaths())
            {
                TrySetSacl(path);
            }
        }

        _store.Save();

        // フィルタとサブスクライバを再構築
        RebuildSubscriber();

        // FileSystemWatcher を開始（管理者でなくても書き込み/削除を検知）
        StartFileWatcher();

        return _store.Tokens.Count;
    }

    /// <summary>
    /// Security Event を処理する。外部から EventLogWatcher 経由で呼び出される。
    /// </summary>
    public void ProcessSecurityEvent(SecurityEventData evt)
    {
        _subscriber?.ProcessEvent(evt);
    }

    /// <summary>
    /// 全罠ファイルの整合性チェックを実行する。
    /// </summary>
    public List<MitsuoshieAlert> CheckIntegrity()
    {
        var checker = new IntegrityChecker(_store);
        var alerts = checker.CheckAll();

        foreach (var alert in alerts)
        {
            HandleAlert(alert);
        }

        return alerts;
    }

    /// <summary>
    /// 定期整合性チェックを開始する。
    /// </summary>
    public void StartIntegrityTimer()
    {
        var interval = TimeSpan.FromMinutes(_config.IntegrityCheckIntervalMinutes);
        _integrityTimer = new Timer(_ => CheckIntegrity(), null, interval, interval);
    }

    /// <summary>
    /// サービスを停止する。
    /// </summary>
    public void Stop()
    {
        _integrityTimer?.Dispose();
        _integrityTimer = null;
        _fileWatcher?.Dispose();
        _fileWatcher = null;
        _eventLogger?.WriteServiceStop();
    }

    public void Dispose()
    {
        Stop();
    }

    private void StartFileWatcher()
    {
        _fileWatcher?.Dispose();
        _fileWatcher = new HoneyFileWatcher(_store);
        _fileWatcher.FileAccessed += evt => _subscriber?.ProcessEvent(evt);
        _fileWatcher.Start();
    }

    private void TryEnableAuditing()
    {
        try
        {
            if (SaclConfigurator.IsAdministrator())
                SaclConfigurator.EnableFileSystemAuditing();
        }
        catch
        {
            // 監査ポリシー有効化に失敗しても続行（インストーラーで設定済みの場合がある）
        }
    }

    private static void TrySetSacl(string filePath)
    {
        try
        {
            SaclConfigurator.SetAuditRule(filePath);
        }
        catch
        {
            // SACL 設定には管理者権限が必要。失敗しても続行。
        }
    }

    private void RebuildSubscriber()
    {
        _filter = new SafeProcessFilter(
            _config.SafeProcessNames,
            Environment.ProcessId
        );

        _subscriber = new SecurityEventSubscriber(_store, _filter);
        _subscriber.AlertRaised += OnSubscriberAlert;
    }

    private void OnSubscriberAlert(MitsuoshieAlert alert)
    {
        if (_alertGenerator.ShouldSuppress(alert))
            return;

        HandleAlert(alert);
    }

    private void HandleAlert(MitsuoshieAlert alert)
    {
        var alertId = _alertGenerator.GenerateAlertId();

        // Sysmon JSON ログ出力
        _sysmonLogger.WriteAlert(alert, alertId);

        // Windows Event Log 出力
        _eventLogger?.WriteAlert(alert);

        // 外部通知（UI等）
        AlertRaised?.Invoke(alert);
    }
}
