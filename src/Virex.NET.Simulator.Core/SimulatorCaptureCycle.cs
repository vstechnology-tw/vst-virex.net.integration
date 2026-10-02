using Virex.NET.Contracts;

namespace Virex.NET.Simulator.Core;

// Simulator callback accounting; this does not establish any hardware readiness guarantee.
public sealed class SimulatorCaptureCycle
{
    private readonly object _sync = new object();
    private readonly string[] _sources;
    private readonly HashSet<string> _ready = new HashSet<string>(StringComparer.Ordinal);
    private readonly HashSet<string> _finished = new HashSet<string>(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> _frames = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
    private bool _cancelled;
    private bool _completed;

    public SimulatorCaptureCycle(string jobId, string captureId, string[] sourceIds)
    {
        if (string.IsNullOrWhiteSpace(jobId)) throw new ArgumentException("A job ID is required.", nameof(jobId));
        if (string.IsNullOrWhiteSpace(captureId)) throw new ArgumentException("A capture ID is required.", nameof(captureId));
        if (sourceIds is null || sourceIds.Length == 0 || sourceIds.Any(string.IsNullOrWhiteSpace) ||
            sourceIds.Distinct(StringComparer.Ordinal).Count() != sourceIds.Length)
            throw new ArgumentException("Unique, nonblank sources are required.", nameof(sourceIds));
        JobId = jobId;
        CaptureId = captureId;
        _sources = (string[])sourceIds.Clone();
        foreach (var source in _sources) _frames.Add(source, new HashSet<string>(StringComparer.Ordinal));
    }

    public string JobId { get; }
    public string CaptureId { get; }

    public bool MarkSourceReady(string sourceId, out CaptureReadyInfo? ready)
    {
        lock (_sync)
        {
            ready = null;
            if (_cancelled || _completed || string.IsNullOrWhiteSpace(sourceId) || !_frames.ContainsKey(sourceId) || !_ready.Add(sourceId)) return false;
            if (_ready.Count == _sources.Length) ready = SnapshotReady();
            return true;
        }
    }

    public bool AddImage(ImageGrabbedInfo image)
    {
        if (image is null) throw new ArgumentNullException(nameof(image));
        lock (_sync)
        {
            if (_cancelled || _completed || _ready.Count != _sources.Length || image.JobId != JobId ||
                image.CaptureId != CaptureId || image.SourceId is null || string.IsNullOrWhiteSpace(image.FrameId) ||
                !_frames.TryGetValue(image.SourceId, out var frames) || _finished.Contains(image.SourceId)) return false;
            return frames.Add(image.FrameId!);
        }
    }

    public bool CompleteSource(string jobId, string captureId, string sourceId, out CaptureCompletedInfo? completed)
    {
        lock (_sync)
        {
            completed = null;
            if (_cancelled || _completed || jobId != JobId || captureId != CaptureId || string.IsNullOrWhiteSpace(sourceId) || _ready.Count != _sources.Length ||
                !_frames.TryGetValue(sourceId, out var frames) || frames.Count == 0 || !_finished.Add(sourceId)) return false;
            if (_finished.Count == _sources.Length)
            {
                _completed = true;
                completed = new CaptureCompletedInfo
                {
                    JobId = JobId,
                    CaptureId = CaptureId,
                    Timestamp = Timestamp(),
                    SourceIds = (string[])_sources.Clone(),
                    ImageCount = _frames.Values.Sum(x => x.Count),
                };
            }
            return true;
        }
    }

    public void Cancel()
    {
        lock (_sync) _cancelled = true;
    }

    private CaptureReadyInfo SnapshotReady() => new CaptureReadyInfo
    {
        JobId = JobId,
        CaptureId = CaptureId,
        Timestamp = Timestamp(),
        SourceIds = (string[])_sources.Clone(),
    };

    private static string Timestamp() => DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");
}
