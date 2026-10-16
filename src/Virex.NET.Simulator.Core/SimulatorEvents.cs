using Virex.NET.Contracts;

namespace Virex.NET.Simulator.Core;

public sealed partial class SimulatorSession
{
    private readonly object _eventGate = new object();
    private readonly HashSet<string> _eventNames = new HashSet<string>(StringComparer.Ordinal)
    {
        "statusChanged", "productInfoChanged", "operationModeChanged", "captureReady",
        "captureCompleted", "imageGrabbed", "runStarted", "runCompleted", "resultCreated",
        "errorChanged", "commandRejected", CleanupAlarmName, AcquisitionAlarmName, PersistenceAlarmName,
    };
    public string AppId { get; private set; } = "simulator";
    public string InstanceId { get; } = Guid.NewGuid().ToString("N");
    private WorkspaceScopeIdentity _workspaceScope = new()
    {
        WorkspaceId = Guid.NewGuid(), RegistrationId = Guid.NewGuid(), ActivationId = Guid.NewGuid(),
    };
    private WorkspaceScopeIdentity? _activeWorkspaceScope;
    private string? _activeRequestId;
    private string? _activeCaptureId;
    private EventWorkScope? _terminalEventScope;
    public event EventHandler<EventEnvelope>? EventProduced;
    public event EventHandler<WorkspaceScopeIdentity>? WorkspaceScopeChanged;

    // Metadata only: the snapshot cannot admit source callbacks or restart a capture.
    private sealed class EventWorkScope
    {
        public EventWorkScope(string? requestId, string jobId, string? captureId, WorkspaceScopeIdentity workspaceScope)
        { RequestId = requestId; JobId = jobId; CaptureId = captureId; WorkspaceScope = workspaceScope; }
        public string? RequestId { get; }
        public string JobId { get; }
        public string? CaptureId { get; }
        public WorkspaceScopeIdentity WorkspaceScope { get; }
    }

    private EventWorkScope? SnapshotActiveWork() => _activeJobId is null ? null
        : new EventWorkScope(_activeRequestId, _activeJobId, _activeCaptureId,
            _activeWorkspaceScope?.Snapshot() ?? CurrentWorkspaceScope);

    public WorkspaceScopeIdentity CurrentWorkspaceScope
    {
        get { lock (_eventGate) return _workspaceScope.Snapshot(); }
    }

    /// <summary>Simulator-only station selection. One process keeps its InstanceId but gets a fresh activation.</summary>
    public async Task SelectWorkspaceForTestsAsync(Guid workspaceId, Guid registrationId,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty || registrationId == Guid.Empty)
            throw new ArgumentException("A synthetic Workspace and registration are required.");
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (State != SimulatorState.Uninitialized || _shutdownRequested)
                throw new InvalidOperationException("Switch only after simulated work and resources are released.");
            lock (_eventGate)
                _workspaceScope = new()
                {
                    WorkspaceId = workspaceId, RegistrationId = registrationId, ActivationId = Guid.NewGuid(),
                };
            _currentResultId = null;
            WorkspaceScopeChanged?.Invoke(this, CurrentWorkspaceScope);
        }
        finally { _gate.Release(); }
    }

    // Called under the session gate. Invalidate first; expose identity only while producing
    // the terminal transition, then discard it before a subsequent work can be admitted.
    private async Task EndRunTransitionAsync(SimulatorTrigger trigger)
    {
        var ending = SnapshotActiveWork();
        InvalidateCapture();
        _terminalEventScope = ending;
        try { await FireAsync(trigger).ConfigureAwait(false); }
        finally { _terminalEventScope = null; }
    }

    public EventCatalog GetEventCatalog()
    {
        lock (_eventGate)
            return new EventCatalog { AppId = AppId, InstanceId = InstanceId,
                WorkspaceScope = _workspaceScope.Snapshot(), Names = _eventNames.OrderBy(x => x, StringComparer.Ordinal).ToArray() };
    }

    /// <summary>Host configuration before use; synthetic custom producers call EmitEvent.</summary>
    public void ConfigureEvents(string appId, IEnumerable<string> customNames)
    {
        if (string.IsNullOrWhiteSpace(appId)) throw new ArgumentException("App identity is required.", nameof(appId));
        if (customNames is null) throw new ArgumentNullException(nameof(customNames));
        var names = customNames.ToArray();
        if (names.Any(string.IsNullOrWhiteSpace) || names.Distinct(StringComparer.Ordinal).Count() != names.Length)
            throw new ArgumentException("Custom event names must be nonempty and unique.", nameof(customNames));
        lock (_eventGate)
        {
            if (State != SimulatorState.Uninitialized || _shutdownRequested)
                throw new InvalidOperationException("Configure events before initialization.");
            AppId = appId;
            foreach (var name in names) _eventNames.Add(name);
        }
    }

    /// <summary>Explicit synthetic event producer. Alarm names require their classified producer;
    /// unknown/Alarm names or non-object payloads throw ArgumentException. The caller owns its input;
    /// the session clones payloads and serializes delivery under the event gate.</summary>
    public void EmitEvent(string name, string payloadJson, string? requestId = null,
        string? jobId = null, string? captureId = null, string? resultId = null,
        WorkspaceScopeIdentity? sourceScope = null)
        => EmitEventCore(name, payloadJson, requestId, jobId, captureId, resultId,
            alarmProducer: false, sourceScope: sourceScope);

    private void EmitEventCore(string name, string payloadJson, string? requestId,
        string? jobId, string? captureId, string? resultId, bool alarmProducer,
        WorkspaceScopeIdentity? sourceScope = null)
    {
        using var document = System.Text.Json.JsonDocument.Parse(payloadJson);
        if (document.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object)
            throw new ArgumentException("Event payload must be a JSON object.", nameof(payloadJson));
        lock (_eventGate)
        {
            if (_shutdownRequested) throw new InvalidOperationException("Simulator host is shutting down.");
            if (!_eventNames.Contains(name)) throw new ArgumentException("Unknown event name.", nameof(name));
            if (!alarmProducer && _alarmSources.ContainsKey(name))
                throw new ArgumentException("Alarm names require their classified producer.", nameof(name));
            EventProduced?.Invoke(this, new EventEnvelope
            {
                Name = name, EventId = Guid.NewGuid().ToString("N"), Timestamp = DateTimeOffset.UtcNow,
                AppId = AppId, InstanceId = InstanceId,
                WorkspaceScope = (sourceScope ?? _workspaceScope).Snapshot(), RequestId = requestId,
                JobId = jobId, CaptureId = captureId, ResultId = resultId,
                Payload = document.RootElement.Clone(),
            });
        }
    }
}
