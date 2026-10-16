using System.Text.Json.Serialization;

namespace Virex.NET.Contracts;

/// <summary>
/// Public identity of the Workspace bound to one Runtime operation or event.
/// ActivationId changes when the same App process selects another Workspace,
/// including when it later returns to the original Workspace.
/// </summary>
public sealed class WorkspaceScopeIdentity
{
    public Guid WorkspaceId { get; set; }
    public Guid RegistrationId { get; set; }
    public Guid ActivationId { get; set; }

    [JsonIgnore]
    public bool IsValid => WorkspaceId != Guid.Empty && RegistrationId != Guid.Empty && ActivationId != Guid.Empty;

    public WorkspaceScopeIdentity Snapshot() => new()
    {
        WorkspaceId = WorkspaceId,
        RegistrationId = RegistrationId,
        ActivationId = ActivationId,
    };

    public bool SameAs(WorkspaceScopeIdentity? other) =>
        IsValid && other?.IsValid == true && WorkspaceId == other.WorkspaceId &&
        RegistrationId == other.RegistrationId && ActivationId == other.ActivationId;
}
