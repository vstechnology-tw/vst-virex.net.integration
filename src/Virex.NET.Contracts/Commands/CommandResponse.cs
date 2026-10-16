namespace Virex.NET.Contracts;

public sealed class CommandResponse
{
    public bool Accepted { get; set; }
    public string? AppId { get; set; }
    public string? InstanceId { get; set; }
    public WorkspaceScopeIdentity? WorkspaceScope { get; set; }
    /// <summary>Context captured when this work was accepted; null means unknown or legacy producer.</summary>
    public string? OperationContext { get; set; }
    public string? JobId { get; set; }
    public string? CaptureId { get; set; }
    public OperationModeInfo? OperationMode { get; set; }

    public string State { get; set; } = SystemStates.Uninitialized;

    public string Command { get; set; } = string.Empty;
    public string? RequestId { get; set; }

    public string? ErrorCode { get; set; }

    public string? RecoveryAction { get; set; }

    public DateTimeOffset? RecoveryStartedAt { get; set; }

    public string? RecoverySource { get; set; }

    public string? RecoveryPhase { get; set; }

    public string? RecoveryDetails { get; set; }

    public string Message { get; set; } = string.Empty;
}
