using System.Text.Json;
using Virex.NET.Contracts;

namespace Virex.NET.Simulator.Core;

public sealed partial class SimulatorSession
{
    private readonly Dictionary<string, string> _resultDataSources = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["result.findings"] = "findings",
        ["result.detail"] = "detail",
    };

    /// <summary>Defines a synthetic result source over the existing public detail, summary or findings projection.</summary>
    public async Task ConfigureResultDataSourceAsync(string name, string projection, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name) || name == "system.state") throw new ArgumentException("A nonreserved source name is required.", nameof(name));
        if (projection is not "detail" and not "summary" and not "findings") throw new ArgumentException("Use a public detail, summary or findings projection.", nameof(projection));
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_shutdownRequested || State is not SimulatorState.Uninitialized and not SimulatorState.Ready)
                throw new InvalidOperationException("Configure synthetic sources while inactive.");
            if (_liveDataSources.ContainsKey(name)) throw new ArgumentException("The name already identifies a live source.", nameof(name));
            _resultDataSources[name] = projection;
        }
        finally { _gate.Release(); }
    }
    private readonly Dictionary<string, string?> _resultRequestIds = new Dictionary<string, string?>(StringComparer.Ordinal);
    private string? _currentResultId;

    public async Task<ResultReference> GetCurrentResultAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            lock (_results)
            {
                var result = _results.FirstOrDefault(item => item.ResultId == _currentResultId);
                if (result is null) throw new SimulatorQueryException(409, QueryErrorCodes.NoCurrentResult, "The current work has no committed result.");
                return new ResultReference
                {
                    AppId = AppId, InstanceId = InstanceId, Timestamp = DateTimeOffset.UtcNow,
                    RequestId = _resultRequestIds[result.ResultId], JobId = result.JobId!, CaptureId = result.CaptureId, ResultId = result.ResultId,
                };
            }
        }
        finally { _gate.Release(); }
    }
    private readonly HashSet<string> _uncommittedResultIds = new HashSet<string>(StringComparer.Ordinal);
    private readonly Queue<string> _knownCaptureIds = new Queue<string>();

    private void TrackUncommittedResult(string captureId)
    {
        lock (_results)
        {
            _uncommittedResultIds.Add(captureId);
            _knownCaptureIds.Enqueue(captureId);
            if (_knownCaptureIds.Count > 100) _uncommittedResultIds.Remove(_knownCaptureIds.Dequeue());
        }
    }
    private readonly Dictionary<string, SyntheticLiveSource> _liveDataSources = new Dictionary<string, SyntheticLiveSource>(StringComparer.Ordinal);

    private sealed class SyntheticLiveSource
    {
        public string Type { get; set; } = string.Empty;
        public JsonElement? Value { get; set; }
        public bool Available { get; set; }
        public string Availability => !Available ? DataSourceAvailability.Unavailable : Value is null ? DataSourceAvailability.MissingValue : DataSourceAvailability.Available;
    }

    /// <summary>Configures an explicitly synthetic live source while inactive. The JSON snapshot is copied.</summary>
    public async Task ConfigureLiveDataSourceAsync(string name, string type, JsonElement? value, bool available = true, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name) || name == "system.state") throw new ArgumentException("A nonreserved source name is required.", nameof(name));
        if (!DataSourceJson.IsType(type)) throw new ArgumentException("Unsupported JSON type.", nameof(type));
        var copy = value?.Clone();
        if (copy is { } actual && !DataSourceJson.MatchesType(type, actual)) throw new ArgumentException("The value must match its declared type.", nameof(value));
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_shutdownRequested || State is not SimulatorState.Uninitialized and not SimulatorState.Ready)
                throw new InvalidOperationException("Configure synthetic sources while inactive.");
            if (_resultDataSources.ContainsKey(name)) throw new ArgumentException("The name already identifies a result source.", nameof(name));
            _liveDataSources[name] = new SyntheticLiveSource { Type = type, Value = copy, Available = available };
        }
        finally { _gate.Release(); }
    }

    public async Task<DataSourceValue> ReadDataSourceAsync(string name, string? resultId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name) || (resultId is not null && string.IsNullOrWhiteSpace(resultId)))
            throw new SimulatorQueryException(400, QueryErrorCodes.InvalidQuery, "A nonempty source name and nonblank explicit result identifier are required.");
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_liveDataSources.TryGetValue(name, out var source))
            {
                if (resultId is not null) throw new SimulatorQueryException(400, QueryErrorCodes.InvalidQuery, "Live sources do not accept a result identifier.");
                if (!source.Available) throw new SimulatorQueryException(409, DataSourceAvailability.Unavailable, "The source is unavailable.");
                if (source.Value is null) throw new SimulatorQueryException(409, DataSourceAvailability.MissingValue, "The source has no value.");
                return new DataSourceValue
                {
                    AppId = AppId, InstanceId = InstanceId, Name = name, Kind = DataSourceKinds.Live, Type = source.Type,
                    State = Status.State, Timestamp = DateTimeOffset.UtcNow, Value = source.Value.Value.Clone(),
                };
            }
            if (_resultDataSources.TryGetValue(name, out var projection))
            {
                if (string.IsNullOrWhiteSpace(resultId)) throw new SimulatorQueryException(400, QueryErrorCodes.InvalidQuery, "Result sources require an explicit result identifier.");
                var detail = await GetResultDetailAsync(resultId!, cancellationToken).ConfigureAwait(false);
                object projectionValue = projection switch { "detail" => detail, "summary" => detail.Summary, _ => detail.Findings };
                using var findings = JsonDocument.Parse(ProtocolJson.Serialize(projectionValue));
                string? requestId;
                lock (_results) _resultRequestIds.TryGetValue(resultId!, out requestId);
                return new DataSourceValue
                {
                    AppId = AppId, InstanceId = InstanceId, Name = name, Kind = DataSourceKinds.Result, Type = projection == "findings" ? "array" : "object",
                    State = Status.State, Timestamp = DateTimeOffset.UtcNow, Value = findings.RootElement.Clone(),
                    RequestId = requestId, JobId = detail.Summary.JobId, CaptureId = detail.Summary.CaptureId, ResultId = detail.ResultId,
                };
            }
            if (name != "system.state") throw new SimulatorQueryException(404, QueryErrorCodes.UnknownSource, "Unknown source name.");
            if (resultId is not null) throw new SimulatorQueryException(400, QueryErrorCodes.InvalidQuery, "Live sources do not accept a result identifier.");
            using var document = JsonDocument.Parse(ProtocolJson.Serialize(Status.State));
            return new DataSourceValue
            {
                AppId = AppId, InstanceId = InstanceId, Name = name, Kind = DataSourceKinds.Live, Type = "string",
                State = Status.State, Timestamp = DateTimeOffset.UtcNow, Value = document.RootElement.Clone(),
            };
        }
        finally { _gate.Release(); }
    }

    public async Task<DataSourceCatalog> GetDataSourcesAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return new DataSourceCatalog
            {
                AppId = AppId, InstanceId = InstanceId, Timestamp = DateTimeOffset.UtcNow,
                Sources = new[] {
                    new DataSourceDescriptor { Name = "system.state", Kind = DataSourceKinds.Live, Type = "string", Availability = DataSourceAvailability.Available },
                }.Concat(_liveDataSources.Select(pair => new DataSourceDescriptor
                {
                    Name = pair.Key, Kind = DataSourceKinds.Live, Type = pair.Value.Type, Availability = pair.Value.Availability,
                })).Concat(_resultDataSources.Select(pair => new DataSourceDescriptor
                {
                    Name = pair.Key, Kind = DataSourceKinds.Result, Type = pair.Value == "findings" ? "array" : "object",
                    Availability = _currentResultId is null ? DataSourceAvailability.NoCurrentResult : DataSourceAvailability.Available,
                })).OrderBy(source => source.Name, StringComparer.Ordinal).ToArray(),
            };
        }
        finally { _gate.Release(); }
    }
}
