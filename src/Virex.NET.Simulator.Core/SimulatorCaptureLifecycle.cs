using Virex.NET.Contracts;

namespace Virex.NET.Simulator.Core;

public sealed partial class SimulatorSession
{
    private string[] _captureSources = ["simulator"];
    private int _framesPerSource = 1;
    private bool _failCapturePreparation;
    private string? _activeJobId;
    private SimulatorCaptureCycle? _captureCycle;

    public event EventHandler<CaptureReadyInfo>? CaptureReady;
    public event EventHandler<CaptureCompletedInfo>? CaptureCompleted;

    public async Task ConfigureCaptureSimulationAsync(string[] sourceIds, int framesPerSource = 1,
        bool failPreparation = false, CancellationToken cancellationToken = default)
    {
        // Validate/copy before waiting; caller mutation cannot alter a configured cycle.
        var copy = sourceIds is null ? null : (string[])sourceIds.Clone();
        _ = new SimulatorCaptureCycle("validation", "validation", copy!);
        if (framesPerSource < 1) throw new ArgumentOutOfRangeException(nameof(framesPerSource));
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (State is not SimulatorState.Ready and not SimulatorState.Uninitialized)
                throw new InvalidOperationException("Capture simulation can only be configured while inactive.");
            _captureSources = copy!;
            _framesPerSource = framesPerSource;
            _failCapturePreparation = failPreparation;
        }
        finally { _gate.Release(); }
    }

    private bool PrepareCapture()
    {
        if (_failCapturePreparation) return false;
        var info = _activeRunProductInfo ?? ProductInfo;
        var sequence = Interlocked.Increment(ref _resultSequence);
        var captureId = $"{SanitizePathSegment(info.LotIDOr, "LOT-UNKNOWN")}-{SanitizePathSegment(info.WaferIDOr, "WAFER-UNKNOWN")}-{SanitizePathSegment(info.SlotOr, "SLOT-UNKNOWN")}-{DateTime.Now:yyyyMMdd_HHmmss}_{sequence:000}";
        _captureCycle = new SimulatorCaptureCycle(_activeJobId!, captureId, _captureSources);
        return true;
    }

    private void AnnounceCaptureReady()
    {
        var cycle = _captureCycle!;
        foreach (var source in _captureSources)
        {
            if (cycle.MarkSourceReady(source, out var ready) && ready is not null)
            {
                LogEvent("captureReady", ready);
                CaptureReady?.Invoke(this, ready);
            }
        }
    }

    private void InvalidateCapture()
    {
        _captureCycle?.Cancel();
        _captureCycle = null;
        _activeJobId = null;
    }
}
