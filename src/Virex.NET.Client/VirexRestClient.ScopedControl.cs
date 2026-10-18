using System.Text.Json;
using Virex.NET.Contracts;

namespace Virex.NET.Client;

public sealed partial class VirexRestClient
{
    /// <summary>Targets one exact process/activation. Unsupported servers fail without falling back to legacy control.
    /// Each invocation owns its serialized target/payload; concurrent calls never modify HttpClient headers.
    /// Cancellation stops the caller's wait and cannot undo work already admitted by the server.</summary>
    public Task<CommandResponse> InitializeScopedAsync(ControlTarget target, CancellationToken cancellationToken = default) => SendScopedAsync(target, ScopedControlCommands.Initialize, null, cancellationToken);
    public Task<CommandResponse> DeinitializeScopedAsync(ControlTarget target, CancellationToken cancellationToken = default) => SendScopedAsync(target, ScopedControlCommands.Deinitialize, null, cancellationToken);
    public Task<CommandResponse> ResetScopedAsync(ControlTarget target, CancellationToken cancellationToken = default) => SendScopedAsync(target, ScopedControlCommands.Reset, null, cancellationToken);
    public Task<CommandResponse> StartScopedAsync(ControlTarget target, SystemStartRequest request, CancellationToken cancellationToken = default) =>
        StartScopedAsync(target, request, false, cancellationToken);
    /// <summary>Starts through the scoped route with an optional Remote precondition. The restriction is sent only when true;
    /// old callers and false produce the original envelope with no requireRemote field.</summary>
    public Task<CommandResponse> StartScopedAsync(ControlTarget target, SystemStartRequest request, bool requireRemote, CancellationToken cancellationToken = default) =>
        SendScopedAsync(target, ScopedControlCommands.Start, request ?? throw new ArgumentNullException(nameof(request)), cancellationToken, requireRemote);
    public Task<CommandResponse> StopScopedAsync(ControlTarget target, SystemStopRequest request, CancellationToken cancellationToken = default) => SendScopedAsync(target, ScopedControlCommands.Stop, request ?? throw new ArgumentNullException(nameof(request)), cancellationToken);
    public Task<CommandResponse> SetOperationModeScopedAsync(ControlTarget target, SetOperationModeRequest request, CancellationToken cancellationToken = default) => SendScopedAsync(target, ScopedControlCommands.SetOperationMode, request ?? throw new ArgumentNullException(nameof(request)), cancellationToken);
    public Task<CommandResponse> SetProductInfoScopedAsync(ControlTarget target, ProductInfo info, CancellationToken cancellationToken = default) => SendScopedAsync(target, ScopedControlCommands.SetProductInfo, info ?? throw new ArgumentNullException(nameof(info)), cancellationToken);

    private Task<CommandResponse> SendScopedAsync(ControlTarget target, string command, object? payload, CancellationToken cancellationToken, bool requireRemote = false)
    {
        if (target?.IsValid != true) throw new ArgumentException("A valid exact control target is required.", nameof(target));
        cancellationToken.ThrowIfCancellationRequested();
        JsonElement? snapshot = null;
        if (payload is not null)
        {
            using var document = JsonDocument.Parse(ProtocolJson.Serialize(payload));
            snapshot = document.RootElement.Clone();
        }
        var request = new ScopedControlRequest { ExpectedTarget = target.Snapshot(), Command = command, Payload = snapshot, RequireRemote = requireRemote };
        ScopedControlJson.Validate(request);
        return PostAsync(RestRoutes.ApiScopedControl, request, cancellationToken);
    }
}
