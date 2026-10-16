using Virex.NET.Contracts;

namespace Virex.NET.Simulator.Core;

public sealed partial class SimulatorSession
{
    private const string CleanupAlarmName = "simulator.cleanupFailed";
    private const string AcquisitionAlarmName = "simulator.acquisitionFault";
    private const string PersistenceAlarmName = "simulator.persistenceFailed";
    private Task<CommandResponse>? _activeReset;
    private int _alarmFaultGeneration;
    private readonly Dictionary<string, AlarmSourceDescriptor> _alarmSources = new Dictionary<string, AlarmSourceDescriptor>(StringComparer.Ordinal)
    {
        [CleanupAlarmName] = new AlarmSourceDescriptor { Name = CleanupAlarmName, Source = "cleanup", Category = AlarmCategories.Fault },
        [AcquisitionAlarmName] = new AlarmSourceDescriptor { Name = AcquisitionAlarmName, Source = "Acquisition", Category = AlarmCategories.Fault },
        [PersistenceAlarmName] = new AlarmSourceDescriptor { Name = PersistenceAlarmName, Source = "artifactPersistence", Category = AlarmCategories.Fault },
    };

    /// <summary>
    /// Configures an explicit synthetic producer before initialization. Names and sources are exact strings;
    /// category controls fault/defect behavior. This session owns its copied descriptor and event registration.
    /// Invalid/colliding descriptors throw ArgumentException; active/closed sessions throw InvalidOperationException.
    /// Configuration and emission serialize through the session gate; cancellation before the gate has no effects.
    /// </summary>
    public async Task ConfigureAlarmSourceAsync(AlarmSourceDescriptor descriptor, CancellationToken cancellationToken = default)
    {
        if (descriptor is null) throw new ArgumentNullException(nameof(descriptor));
        var copy = new AlarmSourceDescriptor { Name = descriptor.Name, Source = descriptor.Source, Category = descriptor.Category };
        if (string.IsNullOrWhiteSpace(copy.Name) || string.IsNullOrWhiteSpace(copy.Source) || !AlarmCategories.IsValid(copy.Category))
            throw new ArgumentException("An exact Alarm name, source and valid category are required.", nameof(descriptor));
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (State != SimulatorState.Uninitialized || _shutdownRequested)
                throw new InvalidOperationException("Configure Alarm producers before initialization.");
            lock (_eventGate)
            {
                if (_eventNames.Contains(copy.Name)) throw new ArgumentException("The event name already has a producer.", nameof(descriptor));
                _alarmSources.Add(copy.Name, copy);
                _eventNames.Add(copy.Name);
            }
        }
        finally { _gate.Release(); }
    }

    /// <summary>
    /// Explicit synthetic occurrence, never inferred from logs or UI. Returns false for a stale expectedJobId,
    /// a result superseded by a new accepted job, or a closed host. Optional ResultId selects only a committed
    /// current result; unknown/noncommitted identifiers do not emit. Unknown names/blank arguments throw
    /// ArgumentException. Cancellation before admission has no effects. All state and identity changes use
    /// the existing session gate; the session retains timer/capture ownership.
    /// </summary>
    public async Task<bool> RaiseAlarmAsync(string name, string message, string? expectedJobId = null,
        string? resultId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(message)
            || expectedJobId is not null && string.IsNullOrWhiteSpace(expectedJobId)
            || resultId is not null && string.IsNullOrWhiteSpace(resultId))
            throw new ArgumentException("Nonempty Alarm name/message and optional identifiers are required.");
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_shutdownRequested) return false;
            if (!_alarmSources.TryGetValue(name, out var source)) throw new ArgumentException("Unknown Alarm producer.", nameof(name));
            if (name is CleanupAlarmName or AcquisitionAlarmName or PersistenceAlarmName)
                throw new ArgumentException("Built-in alarms are emitted by their actual producer, not by custom injection.", nameof(name));
            var work = SnapshotActiveWork();
            if (resultId is not null)
            {
                if (source.Category == AlarmCategories.Fault) throw new ArgumentException("A runtime fault does not select a result.", nameof(resultId));
                ResultSummary? result;
                lock (_results) result = _results.FirstOrDefault(x => x.ResultId == resultId);
                if (result is null || resultId != _currentResultId || result.JobId is null) return false;
                work = new EventWorkScope(_resultRequestIds[resultId], result.JobId, result.CaptureId,
                    _resultWorkspaceScopes[resultId].Snapshot(), _resultOperationContexts[resultId]);
            }
            if (expectedJobId is not null && work?.JobId != expectedJobId) return false;
            if (source.Category == AlarmCategories.Fault)
                await LatchAlarmFaultUnderGateAsync(source, message, work).ConfigureAwait(false);
            PublishAlarm(source, message, work, resultId);
            return true;
        }
        finally { _gate.Release(); }
    }

    private async Task LatchAlarmFaultUnderGateAsync(AlarmSourceDescriptor source, string message, EventWorkScope? work)
    {
        _alarmFaultGeneration++;
        if (State == SimulatorState.Faulted) return; // Retain the first fault until successful cleanup.
        if (State == SimulatorState.Running)
            await StopUnderGateAsync(new SystemStopRequest { Reason = "Alarm fault: " + source.Name }).ConfigureAwait(false);
        StopActiveRunTimers();
        InvalidateCapture();
        var safeMessage = RecoveryMessageSanitizer.Sanitize(message) ?? "Simulated Alarm fault.";
        BeginRecovery(safeMessage, source.Source, "Faulted", CommandErrorCodes.RequiresReset);
        _terminalEventScope = work;
        try
        {
            if (CanFire(SimulatorTrigger.Fault)) await FireAsync(SimulatorTrigger.Fault).ConfigureAwait(false);
            SetError(safeMessage);
        }
        finally { _terminalEventScope = null; }
    }

    /// <summary>
    /// External recovery command. Stops/invalidate work and reuses the session's cleanup owner.
    /// Concurrent callers join one cleanup; a repeated completed reset is idempotent.
    /// Pre-cancellation has no effects. After invocation cancellation ends only this caller's wait;
    /// admitted cleanup continues. Success is Uninitialized and never resumes old work.
    /// </summary>
    public Task<CommandResponse> ResetAsync(CancellationToken cancellationToken = default) => ResetAsync(null, cancellationToken);

    private Task<CommandResponse> ResetAsync(ControlTarget? expectedTarget, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return Task.FromCanceled<CommandResponse>(cancellationToken);
        if (CheckOperationSource("Reset", OperationSource.External) is { } denied) return Task.FromResult(denied);
        Task<CommandResponse> operation;
        TaskCompletionSource<CommandResponse>? completion = null;
        lock (_deinitializationGate)
        {
            if (_activeReset is not { IsCompleted: false })
            {
                if (_activeDeinitialization is { IsCompleted: false } cleanup)
                    _activeReset = JoinDeinitializeAsResetAsync(cleanup);
                else
                {
                    completion = new TaskCompletionSource<CommandResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
                    _activeReset = completion.Task;
                }
            }
            operation = _activeReset;
        }
        if (completion is not null) _ = CompleteResetAsync(completion, expectedTarget);
        return WaitForResetAsync(operation, cancellationToken);
    }

    private async Task<CommandResponse> JoinDeinitializeAsResetAsync(Task<CommandResponse> cleanup)
    {
        var response = ProtocolJson.Deserialize<CommandResponse>(ProtocolJson.Serialize(await cleanup.ConfigureAwait(false)))!;
        response.Command = "Reset";
        return ResetResponse(response);
    }

    private static async Task<CommandResponse> WaitForResetAsync(Task<CommandResponse> operation, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled) return await operation.ConfigureAwait(false);
        var cancelled = new TaskCompletionSource<CommandResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        using (cancellationToken.Register(() => cancelled.TrySetCanceled(cancellationToken)))
            return await (await Task.WhenAny(operation, cancelled.Task).ConfigureAwait(false)).ConfigureAwait(false);
    }

    private async Task CompleteResetAsync(TaskCompletionSource<CommandResponse> completion, ControlTarget? expectedTarget)
    {
        try { completion.TrySetResult(await ResetCoreAsync(expectedTarget).ConfigureAwait(false)); }
        catch (Exception ex) { completion.TrySetException(ex); }
    }

    private async Task<CommandResponse> ResetCoreAsync(ControlTarget? expectedTarget)
    {
        Task[] runs;
        EventWorkScope? work;
        int faultGeneration;
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (CheckControlTarget("Reset", expectedTarget) is { } targetDenied) return targetDenied;
            if (CheckOperationSource("Reset", OperationSource.External) is { } denied) return denied;
            if (State == SimulatorState.Uninitialized) return ResetResponse(Accept("Reset", "Already uninitialized."));
            work = SnapshotActiveWork();
            faultGeneration = _alarmFaultGeneration;
            if (State == SimulatorState.Running)
                await StopUnderGateAsync(new SystemStopRequest { Reason = "Reset requested." }).ConfigureAwait(false);
            StopActiveRunTimers();
            InvalidateCapture();
            runs = _runTasks.ToArray();
            if (!CanFire(SimulatorTrigger.Deinitialize)) return ResetResponse(Reject("Reset"));
            // Keep admission closed while joining cancelled callbacks outside _gate.
            await FireAsync(SimulatorTrigger.Deinitialize).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
        Exception? runFailure = null;
        try { await Task.WhenAll(runs).ConfigureAwait(false); }
        catch (Exception ex) { runFailure = ex; }
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (runFailure is not null)
            {
                var message = RecoveryMessageSanitizer.Sanitize(runFailure.Message) ?? "Simulator run cleanup failed.";
                BeginRecovery(message, "cleanup", "Faulted", CommandErrorCodes.CleanupFailed);
                if (CanFire(SimulatorTrigger.Fault)) await FireAsync(SimulatorTrigger.Fault).ConfigureAwait(false);
                SetError(message);
                if (!_shutdownRequested) PublishAlarm(_alarmSources[CleanupAlarmName], message, work);
                return ResetResponse(Reject("Reset", CommandErrorCodes.CleanupFailed, message));
            }
            // A new source fault during the join must survive this older recovery attempt.
            if (faultGeneration != _alarmFaultGeneration)
                return ResetResponse(Reject("Reset", CommandErrorCodes.RequiresReset, "A new fault occurred during reset."));
            var response = await HandleDeinitializeUnderGateAsync(CancellationToken.None, "Reset", transitionAlreadyStarted: true).ConfigureAwait(false);
            if (!response.Accepted && response.ErrorCode == CommandErrorCodes.CleanupFailed && !_shutdownRequested)
                PublishAlarm(_alarmSources[CleanupAlarmName], response.Message, work);
            return ResetResponse(response);
        }
        finally { _gate.Release(); }
    }

    private CommandResponse ResetResponse(CommandResponse response)
    {
        response.AppId = AppId; response.InstanceId = InstanceId;
        return response;
    }

    private void PublishAlarm(AlarmSourceDescriptor descriptor, string message, EventWorkScope? work,
        string? resultId = null)
    {
        var alarm = new AlarmNotification
        {
            Name = descriptor.Name, Source = descriptor.Source, Category = descriptor.Category,
            Message = RecoveryMessageSanitizer.Sanitize(message) ?? "Simulated Alarm.",
            State = SimulatorStateNames.ToDto(State), Timestamp = DateTimeOffset.UtcNow,
            AppId = AppId, InstanceId = InstanceId, RequestId = work?.RequestId,
            JobId = work?.JobId, CaptureId = work?.CaptureId, ResultId = resultId,
        };
        EmitEventCore(descriptor.Name, ProtocolJson.Serialize(alarm), alarm.RequestId, alarm.JobId, alarm.CaptureId,
            alarm.ResultId, alarmProducer: true, sourceScope: work?.WorkspaceScope,
            sourceOperationContext: work?.OperationContext);
    }

    /// <summary>Returns an owned catalog snapshot of exact producer names and categories.
    /// Reads serialize with configuration through the session gate. Caller cancellation stops waiting;
    /// no state changes occur. Callers own and may modify the returned descriptors.</summary>
    public async Task<AlarmCatalog> GetAlarmsAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return new AlarmCatalog
            {
                AppId = AppId, InstanceId = InstanceId, Timestamp = DateTimeOffset.UtcNow,
                Sources = _alarmSources.Values.OrderBy(x => x.Name, StringComparer.Ordinal).Select(x =>
                    new AlarmSourceDescriptor { Name = x.Name, Source = x.Source, Category = x.Category }).ToArray(),
            };
        }
        finally { _gate.Release(); }
    }
}
