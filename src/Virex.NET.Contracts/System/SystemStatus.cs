namespace Virex.NET.Contracts;

public sealed class SystemStatus
{
    public string State { get; set; } = SystemStates.Uninitialized;

    public string? RecoveryAction { get; set; }

    public DateTimeOffset? RecoveryStartedAt { get; set; }

    public string? RecoverySource { get; set; }

    public string? RecoveryPhase { get; set; }

    public string? RecoveryDetails { get; set; }
}
