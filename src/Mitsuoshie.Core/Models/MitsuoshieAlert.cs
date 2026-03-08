namespace Mitsuoshie.Core.Models;

public record MitsuoshieAlert
{
    public required DateTime Timestamp { get; init; }
    public required string HoneyFile { get; init; }
    public required HoneyTokenType HoneyType { get; init; }
    public required string EventType { get; init; }
    public required bool Tampered { get; init; }
    public required string OriginalHash { get; init; }
    public required string CurrentHash { get; init; }
    public required string AccessMask { get; init; }
    public required int ProcessId { get; init; }
    public required string ProcessName { get; init; }
    public required string ProcessPath { get; init; }
    public required string User { get; init; }
    public required AlertSeverity Severity { get; init; }
}
