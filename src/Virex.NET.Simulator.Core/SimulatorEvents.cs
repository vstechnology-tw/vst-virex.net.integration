using Virex.NET.Contracts;

namespace Virex.NET.Simulator.Core;

public sealed partial class SimulatorSession
{
    private readonly object _eventGate = new object();
    private readonly HashSet<string> _eventNames = new HashSet<string>(StringComparer.Ordinal)
    {
        "statusChanged", "productInfoChanged", "operationModeChanged", "captureReady",
        "captureCompleted", "imageGrabbed", "runStarted", "runCompleted", "resultCreated",
        "errorChanged", "commandRejected",
    };
    public string AppId { get; private set; } = "simulator";
    public string InstanceId { get; } = Guid.NewGuid().ToString("N");
    private string? _activeRequestId;
    public event EventHandler<EventEnvelope>? EventProduced;

    public EventCatalog GetEventCatalog()
    {
        lock (_eventGate)
            return new EventCatalog { AppId = AppId, InstanceId = InstanceId, Names = _eventNames.OrderBy(x => x, StringComparer.Ordinal).ToArray() };
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

    /// <summary>Explicit synthetic producer. No unknown name or non-object payload is emitted.</summary>
    public void EmitEvent(string name, string payloadJson, string? requestId = null,
        string? jobId = null, string? captureId = null, string? resultId = null)
    {
        using var document = System.Text.Json.JsonDocument.Parse(payloadJson);
        if (document.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object)
            throw new ArgumentException("Event payload must be a JSON object.", nameof(payloadJson));
        lock (_eventGate)
        {
            if (_shutdownRequested) throw new InvalidOperationException("Simulator host is shutting down.");
            if (!_eventNames.Contains(name)) throw new ArgumentException("Unknown event name.", nameof(name));
            EventProduced?.Invoke(this, new EventEnvelope
            {
                Name = name, EventId = Guid.NewGuid().ToString("N"), Timestamp = DateTimeOffset.UtcNow,
                AppId = AppId, InstanceId = InstanceId, RequestId = requestId,
                JobId = jobId, CaptureId = captureId, ResultId = resultId,
                Payload = document.RootElement.Clone(),
            });
        }
    }
}
