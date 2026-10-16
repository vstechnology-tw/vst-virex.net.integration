using System.Text.Json;
using System.Text.Json.Serialization;

namespace Virex.NET.Contracts;

/// <summary>Exact addressed process and Workspace activation. This is an isolation precondition, not authentication.</summary>
public sealed class ControlTarget
{
    public string AppId { get; set; } = string.Empty;
    public string InstanceId { get; set; } = string.Empty;
    public WorkspaceScopeIdentity? WorkspaceScope { get; set; }
    [JsonIgnore] public bool IsValid => !string.IsNullOrWhiteSpace(AppId) && !string.IsNullOrWhiteSpace(InstanceId) && WorkspaceScope?.IsValid == true;
    public ControlTarget Snapshot() => new() { AppId = AppId, InstanceId = InstanceId, WorkspaceScope = WorkspaceScope?.Snapshot() };
    public bool SameAs(ControlTarget? other) => IsValid && other?.IsValid == true &&
        string.Equals(AppId, other.AppId, StringComparison.Ordinal) && string.Equals(InstanceId, other.InstanceId, StringComparison.Ordinal) &&
        WorkspaceScope!.SameAs(other.WorkspaceScope);
}

public static class ScopedControlCommands
{
    public const string Initialize = "Initialize";
    public const string Start = "Start";
    public const string Stop = "Stop";
    public const string Reset = "Reset";
    public const string Deinitialize = "Deinitialize";
    public const string SetOperationMode = "SetOperationMode";
    public const string SetProductInfo = "SetProductInfo";
}

/// <summary>Additive scoped-control envelope. Payload uses the existing command DTO, or null for bodyless commands.</summary>
public sealed class ScopedControlRequest
{
    public ControlTarget? ExpectedTarget { get; set; }
    public string Command { get; set; } = string.Empty;
    public JsonElement? Payload { get; set; }
}
