namespace Virex.NET.Contracts;

/// <summary>Whole accepted job quiescence, emitted only by its actual runtime owner after preparation,
/// capture, inspection, persistence and owned cleanup have completed. Identifiers are exact, nonblank
/// and match the accepted work; Timestamp is the actual completion time. Event name is configurable,
/// not a fixed event name. CaptureCompleted, ResultCreated and Ready alone do not prove this fact.
/// App/instance scope comes from the enclosing EventEnvelope. The Simulator has no built-in producer.</summary>
public sealed class WorkCompletedInfo
{
    public string RequestId { get; set; } = string.Empty;
    public string JobId { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
}
