namespace Virex.NET.Contracts;

public sealed class SystemStatus
{
    /// <summary>Receiver implements strict recovery Stop validation, atomic admission fencing and cleanup proof.
    /// Missing/false means unsupported; callers must check before mutation and must not fall back to ordinary Stop.</summary>
    public bool RecoveryStopSupported { get; set; }
    public string State { get; set; } = SystemStates.Uninitialized;

    public string? RecoveryAction { get; set; }

    public string? ErrorCode { get; set; }

    public DateTimeOffset? RecoveryStartedAt { get; set; }

    public string? RecoverySource { get; set; }

    public string? RecoveryPhase { get; set; }

    public string? RecoveryDetails { get; set; }
}
