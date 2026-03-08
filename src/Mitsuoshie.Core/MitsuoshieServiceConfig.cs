namespace Mitsuoshie.Core;

public record MitsuoshieServiceConfig
{
    public required string UserProfileDir { get; init; }
    public required string SettingsPath { get; init; }
    public required string SysmonLogPath { get; init; }
    public required IReadOnlyList<string> SafeProcessNames { get; init; }
    public int IntegrityCheckIntervalMinutes { get; init; } = 30;
    public int SuppressDuplicateMinutes { get; init; } = 5;
    public bool EnableWindowsEventLog { get; init; } = true;
}
