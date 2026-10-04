using Virex.NET.Contracts;

namespace Virex.NET.Simulator.Core;

// Assigned by the trusted host entry point, never by a network payload.
public enum OperationSource { Local, External }

public sealed partial class SimulatorSession
{
    private volatile string _operationMode = OperationModes.Local;
    private readonly bool _operationManagementEnabled;

    public SimulatorSession(string? resultRootDirectory, bool operationManagementEnabled)
        : this(resultRootDirectory)
    {
        _operationManagementEnabled = operationManagementEnabled;
    }

    public OperationModeInfo OperationMode => new OperationModeInfo
    {
        Mode = _operationMode,
        ManagementEnabled = _operationManagementEnabled,
    };

    public event EventHandler<OperationModeInfo>? OperationModeChanged;

    public async Task<CommandResponse> SetOperationModeAsync(SetOperationModeRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        var mode = request.Mode;
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_shutdownRequested)
                return Reject("SetOperationMode", CommandErrorCodes.InvalidState, "Simulator host is shutting down.");

            if (!OperationModes.IsValid(mode))
                return Reject("SetOperationMode", CommandErrorCodes.InvalidOperationMode, "Mode must be local or remote.");
            if (_operationMode != mode)
            {
                _operationMode = mode!;
                LogEvent("operationModeChanged", OperationMode);
                OperationModeChanged?.Invoke(this, OperationMode);
            }
            var response = Accept("SetOperationMode", "Operation mode applied.");
            response.OperationMode = OperationMode;
            return response;
        }
        finally { _gate.Release(); }
    }

    public Task<CommandResponse> InitializeFromSourceAsync(OperationSource source, CancellationToken cancellationToken = default) =>
        HandleInitializeAsync(cancellationToken, source);

    public Task<CommandResponse> DeinitializeFromSourceAsync(OperationSource source, CancellationToken cancellationToken = default)
    {
        if (CheckOperationSource("Deinitialize", source) is { } denied) return Task.FromResult(denied);
        lock (_deinitializationGate)
        {
            if (_activeDeinitialization is { IsCompleted: false } activeDeinitialization) return activeDeinitialization;
            // Cleanup remains a single shared operation, independent of caller cancellation.
            _activeDeinitialization = HandleDeinitializeAsync(CancellationToken.None, source);
            return _activeDeinitialization;
        }
    }

    public Task<CommandResponse> SetProductInfoFromSourceAsync(ProductInfo info, OperationSource source, CancellationToken cancellationToken = default) =>
        HandleSetProductInfoAsync(info, cancellationToken, source);

    public Task<CommandResponse> StartFromSourceAsync(SystemStartRequest request, OperationSource source, CancellationToken cancellationToken = default) =>
        HandleStartAsync(request, cancellationToken, source);

    public Task<CommandResponse> StopFromSourceAsync(SystemStopRequest request, OperationSource source, CancellationToken cancellationToken = default) =>
        HandleStopAsync(request, cancellationToken, source);

    private CommandResponse? CheckOperationSource(string command, OperationSource source)
    {
        if (_shutdownRequested)
            return Reject(command, CommandErrorCodes.InvalidState, "Simulator host is shutting down.");

        if (source is not OperationSource.Local and not OperationSource.External)
            return Reject(command, CommandErrorCodes.InvalidOperationSource, "Invalid operation source.");
        if (_operationManagementEnabled &&
            ((_operationMode == OperationModes.Local && source == OperationSource.External) ||
             (_operationMode == OperationModes.Remote && source == OperationSource.Local)))
            return Reject(command, CommandErrorCodes.OperationNotAllowed, "Operation source is not allowed in the applied mode.");
        return null;
    }
}
