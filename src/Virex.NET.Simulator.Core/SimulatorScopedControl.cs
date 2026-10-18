using Virex.NET.Contracts;

namespace Virex.NET.Simulator.Core;

public sealed partial class SimulatorSession
{
    /// <summary>Strict scoped control; validation and mutations share the existing session admission gate.</summary>
    public Task<CommandResponse> ExecuteScopedAsync(ScopedControlRequest request, CancellationToken cancellationToken = default)
    {
        // Own a snapshot before any asynchronous wait, including callers that bypass HTTP.
        var owned = ScopedControlJson.ReadRequest(ProtocolJson.Serialize(request));
        var target = owned.ExpectedTarget!;
        return owned.Command switch
        {
            ScopedControlCommands.Initialize => HandleInitializeAsync(cancellationToken, expectedTarget: target),
            ScopedControlCommands.Deinitialize => DeinitializeScopedCoreAsync(target, cancellationToken),
            ScopedControlCommands.Start => HandleStartAsync(ScopedControlJson.ReadPayload<SystemStartRequest>(owned), cancellationToken, expectedTarget: target, requireRemote: owned.RequireRemote),
            ScopedControlCommands.Stop => HandleStopAsync(ScopedControlJson.ReadPayload<SystemStopRequest>(owned), cancellationToken, expectedTarget: target),
            ScopedControlCommands.SetProductInfo => HandleSetProductInfoAsync(ScopedControlJson.ReadPayload<ProductInfo>(owned), cancellationToken, expectedTarget: target),
            ScopedControlCommands.SetOperationMode => SetOperationModeCoreAsync(ScopedControlJson.ReadPayload<SetOperationModeRequest>(owned), target, cancellationToken),
            ScopedControlCommands.Reset => ResetScopedCoreAsync(target, cancellationToken),
            _ => throw new System.Text.Json.JsonException("Unknown scoped command."),
        };
    }

    private CommandResponse? CheckControlTarget(string command, ControlTarget? expectedTarget, string? requestId = null) =>
        expectedTarget is null || expectedTarget.SameAs(new ControlTarget { AppId = AppId, InstanceId = InstanceId, WorkspaceScope = CurrentWorkspaceScope })
            ? null : Reject(command, CommandErrorCodes.TargetMismatch, "The addressed process or Workspace activation is no longer current.", requestId);

    private async Task<CommandResponse> DeinitializeScopedCoreAsync(ControlTarget target, CancellationToken cancellationToken)
    {
        Task<CommandResponse> operation;
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (CheckControlTarget(ScopedControlCommands.Deinitialize, target) is { } denied) return denied;
            operation = DeinitializeFromSourceCoreAsync(OperationSource.External, target, cancellationToken);
        }
        finally { _gate.Release(); }
        return await operation.ConfigureAwait(false);
    }

    private async Task<CommandResponse> ResetScopedCoreAsync(ControlTarget target, CancellationToken cancellationToken)
    {
        Task<CommandResponse> operation;
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (CheckControlTarget(ScopedControlCommands.Reset, target) is { } denied) return denied;
            // Cleanup starts through its original single owner and revalidates before the first mutation.
            operation = ResetAsync(target, cancellationToken);
        }
        finally { _gate.Release(); }
        return await operation.ConfigureAwait(false);
    }
}
