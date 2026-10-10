using Virex.NET.Contracts;

namespace Virex.NET.Simulator.Core;

public sealed partial class SimulatorSession
{
    private bool _recoveryStopRequired;
    private readonly HashSet<string> _revokedStartRequestIds = new HashSet<string>(StringComparer.Ordinal);

    private async Task<CommandResponse> RecoveryStopAsync(SystemStopRequest request, OperationSource source, CancellationToken cancellationToken)
    {
        Task[] runs;
        CommandResponse response;
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (CheckOperationSource("Stop", source) is { } denied) return denied;
            if (!string.Equals(request.ExpectedAppId, AppId, StringComparison.Ordinal)
                || !string.Equals(request.ExpectedInstanceId, InstanceId, StringComparison.Ordinal))
                return Reject("Stop", CommandErrorCodes.OperationNotAllowed, "Expected App instance does not match.");

            // Same gate as Start admission. Install first, before cancelling any admitted work.
            _recoveryStopRequired = true;
            _alarmFaultGeneration++;
            foreach (var id in request.CancelStartRequestIds) _revokedStartRequestIds.Add(id);
            if (!string.IsNullOrWhiteSpace(_activeRequestId)) _revokedStartRequestIds.Add(_activeRequestId!);
            if (State == SimulatorState.Running)
                await StopUnderGateAsync(request).ConfigureAwait(false);
            StopActiveRunTimers();
            InvalidateCapture();
            BeginRecovery("Recovery Stop requires explicit Reset.", "recoveryStop", "Stopped", CommandErrorCodes.RequiresReset);
            if (CanFire(SimulatorTrigger.Fault)) await FireAsync(SimulatorTrigger.Fault).ConfigureAwait(false);
            SetError("Recovery Stop requires explicit Reset.");
            runs = _runTasks.ToArray();
            response = Accept("Stop", "Recovery barrier installed and work revoked.");
            response.AppId = AppId; response.InstanceId = InstanceId;
            response.RecoveryAction = RecoveryActions.Reset;
        }
        finally { _gate.Release(); }

        // A cancelled run can be waiting for _gate. Join outside it, never create another run owner.
        try { await Task.WhenAll(runs).ConfigureAwait(false); }
        catch (Exception)
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try { return Reject("Stop", CommandErrorCodes.CleanupFailed, "Recovery Stop run cleanup failed."); }
            finally { _gate.Release(); }
        }
        response.RecoveryStopApplied = true;
        return response;
    }
}
