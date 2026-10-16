using System.Text.Json;

namespace Virex.NET.Contracts;

/// <summary>Producer-supported exact names. AppId is stable; InstanceId changes on App restart.</summary>
public sealed class EventCatalog
{
    public string AppId { get; set; } = string.Empty;
    public string InstanceId { get; set; } = string.Empty;
    public WorkspaceScopeIdentity? WorkspaceScope { get; set; }
    public string[] Names { get; set; } = [];
}

/// <summary>An object payload from an actual producer. Optional IDs describe its exact scope.</summary>
public sealed class EventEnvelope
{
    public string Type { get; set; } = "event";
    public string Name { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public string AppId { get; set; } = string.Empty;
    public string InstanceId { get; set; } = string.Empty;
    public WorkspaceScopeIdentity? WorkspaceScope { get; set; }
    public string ConnectionGeneration { get; set; } = string.Empty;
    /// <summary>Original accepted work context, independent of the current Workspace context.</summary>
    public string? OperationContext { get; set; }
    public string? RequestId { get; set; }
    public string? JobId { get; set; }
    public string? CaptureId { get; set; }
    public string? ResultId { get; set; }
    public JsonElement Payload { get; set; }
}

/// <summary>Atomic subscription command; names use ordinal equality, without patterns.</summary>
public sealed class EventSubscriptionRequest
{
    public string Type { get; set; } = "subscribeEvents";
    public string RequestId { get; set; } = string.Empty;
    public WorkspaceScopeIdentity? WorkspaceScope { get; set; }
    public string[] Names { get; set; } = [];
}

public sealed class EventSubscriptionResponse
{
    public string Type { get; set; } = "eventSubscription";
    public string RequestId { get; set; } = string.Empty;
    public bool Accepted { get; set; }
    public string? ErrorCode { get; set; }
    public string AppId { get; set; } = string.Empty;
    public string InstanceId { get; set; } = string.Empty;
    public WorkspaceScopeIdentity? WorkspaceScope { get; set; }
    public string ConnectionGeneration { get; set; } = string.Empty;
    public string[] Names { get; set; } = [];
}

public static class EventSubscriptionErrorCodes
{
    public const string UnknownName = "unknown_event_name";
    public const string DuplicateName = "duplicate_event_subscription";
    public const string InvalidPayload = "invalid_event_payload";
    public const string NotSubscribed = "event_not_subscribed";
    public const string StaleWorkspaceScope = "stale_workspace_scope";
}
