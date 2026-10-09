using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Virex.NET.Contracts;

namespace Virex.NET.Client;

/// <summary>Optional exact identity/work filter. Mismatches are reported and never delivered.</summary>
public sealed class EventSubscriptionScope
{
    public string? AppId { get; set; }
    public string? InstanceId { get; set; }
    public string? RequestId { get; set; }
    public string? JobId { get; set; }
}

public sealed class EventStreamIssue
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string RawJson { get; set; } = string.Empty;
}

public sealed class VirexEventSubscriptionException : Exception
{
    public VirexEventSubscriptionException(EventSubscriptionResponse response)
        : base("Event subscription rejected: " + response.ErrorCode) => Response = response;
    public EventSubscriptionResponse Response { get; }
}

public sealed partial class VirexTcpEventClient
{
    /// <summary>
    /// Opens an owned TCP subscription. Names use complete ordinal matching. The callback is
    /// installed before acknowledgement and executes serially on the receive loop, not a UI
    /// dispatcher. The token owns the subscription lifetime. Await CloseAsync to join cleanup.
    /// Callback exceptions fault Completion and close the stream. No reconnect or replay occurs.
    /// </summary>
    public Task<VirexEventSubscription> SubscribeAsync(IEnumerable<string> names, Action<EventEnvelope> onEvent,
        Action<EventStreamIssue>? onIssue = null, EventSubscriptionScope? scope = null,
        CancellationToken cancellationToken = default) =>
        VirexEventSubscription.OpenAsync(this, _options, names, onEvent, onIssue, scope, cancellationToken);
}

/// <summary>
/// Owns one connection and its callbacks. Dispose stops delivery synchronously; CloseAsync also
/// joins the receive loop. UnsubscribeAsync is an acknowledgement barrier for specified names.
/// Callbacks may call Dispose; do not synchronously wait for CloseAsync/UnsubscribeAsync inside a callback.
/// </summary>
public sealed class VirexEventSubscription : IDisposable
{
    private readonly VirexTcpEventClient _reader;
    private readonly TcpClient _client = new TcpClient();
    private readonly CancellationTokenSource _stop = new CancellationTokenSource();
    private readonly object _deliveryGate = new object();
    private readonly SemaphoreSlim _commands = new SemaphoreSlim(1, 1);
    private readonly HashSet<string> _names;
    private readonly HashSet<string> _seen = new HashSet<string>(StringComparer.Ordinal);
    private readonly Queue<string> _seenOrder = new Queue<string>();
    private readonly Action<EventEnvelope> _onEvent;
    private readonly Action<EventStreamIssue>? _onIssue;
    private readonly EventSubscriptionScope _scope;
    private readonly int _timeoutMs;
    private NetworkStream? _stream;
    private CancellationTokenRegistration _registration;
    private TaskCompletionSource<EventSubscriptionResponse>? _reply;
    private string? _replyId;
    private bool _disposed;
    private Task _completion = Task.CompletedTask;
    private EventSubscriptionResponse _identity = new EventSubscriptionResponse();
    private int _pendingOperations;
    private TaskCompletionSource<object?>? _drained;
    private Task? _closeTask;

    private VirexEventSubscription(VirexTcpEventClient reader, VirexClientOptions options, string[] names,
        Action<EventEnvelope> onEvent, Action<EventStreamIssue>? onIssue, EventSubscriptionScope? scope)
    {
        _reader = reader; _names = new HashSet<string>(names, StringComparer.Ordinal);
        _onEvent = onEvent; _onIssue = onIssue;
        _timeoutMs = options.TimeoutMs <= 0 ? 5000 : options.TimeoutMs;
        _scope = new EventSubscriptionScope { AppId = scope?.AppId, InstanceId = scope?.InstanceId,
            RequestId = scope?.RequestId, JobId = scope?.JobId };
    }

    public string AppId => _identity.AppId;
    public string InstanceId => _identity.InstanceId;
    public string ConnectionGeneration => _identity.ConnectionGeneration;
    /// <summary>Completes on disconnect/cancellation; faults for transport/callback failure.</summary>
    public Task Completion => _completion;
    public EventStreamIssue? LastIssue { get; private set; }

    internal static async Task<VirexEventSubscription> OpenAsync(VirexTcpEventClient reader, VirexClientOptions options,
        IEnumerable<string> names, Action<EventEnvelope> onEvent, Action<EventStreamIssue>? onIssue,
        EventSubscriptionScope? scope, CancellationToken token)
    {
        if (names is null) throw new ArgumentNullException(nameof(names));
        if (onEvent is null) throw new ArgumentNullException(nameof(onEvent));
        var copy = names.ToArray();
        if (copy.Length == 0 || copy.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("At least one complete event name is required.", nameof(names));
        var subscription = new VirexEventSubscription(reader, options, copy, onEvent, onIssue, scope);
        // Lifetime cancellation closes .NET Framework blocking reads too.
        subscription._registration = token.Register(subscription.Dispose);
        using var timeout = new CancellationTokenSource(subscription._timeoutMs);
        using var closeOnTimeout = timeout.Token.Register(subscription._client.Dispose);
        try
        {
            token.ThrowIfCancellationRequested();
            await subscription._client.ConnectAsync(options.TcpHost, options.TcpPort).ConfigureAwait(false);
            subscription._stream = subscription._client.GetStream();
            var request = new EventSubscriptionRequest { RequestId = Guid.NewGuid().ToString("N"), Names = copy };
            await subscription.WriteAsync(request, token).ConfigureAwait(false);
            while (true)
            {
                var line = await reader.ReadFrameAsync(subscription._stream, token).ConfigureAwait(false);
                if (line is null) throw new EndOfStreamException("Connection closed before subscription acknowledgement.");
                using var doc = JsonDocument.Parse(line);
                if (!doc.RootElement.TryGetProperty("type", out var type) || type.GetString() != "eventSubscription") continue;
                var response = ProtocolJson.Deserialize<EventSubscriptionResponse>(line)!;
                if (response.RequestId != request.RequestId) throw new InvalidDataException("Mismatched subscription acknowledgement.");
                if (!response.Accepted) throw new VirexEventSubscriptionException(response);
                if (string.IsNullOrWhiteSpace(response.AppId) || string.IsNullOrWhiteSpace(response.InstanceId) || string.IsNullOrWhiteSpace(response.ConnectionGeneration)
                    || response.Names is null || !new HashSet<string>(response.Names, StringComparer.Ordinal).SetEquals(copy))
                    throw new InvalidDataException("Invalid subscription acknowledgement.");
                if ((scope?.AppId is not null && scope.AppId != response.AppId) || (scope?.InstanceId is not null && scope.InstanceId != response.InstanceId))
                    throw new InvalidDataException("Subscription App identity does not match the requested scope.");
                subscription._identity = response;
                break;
            }
            token.ThrowIfCancellationRequested();
            timeout.Token.ThrowIfCancellationRequested();
            closeOnTimeout.Dispose();
            subscription._completion = subscription.ReceiveAsync();
            return subscription;
        }
        catch (Exception ex)
        {
            await subscription.CloseAsync().ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            if (timeout.IsCancellationRequested) throw new TimeoutException("Event subscription acknowledgement timed out.", ex);
            throw;
        }
    }

    public async Task UnsubscribeAsync(IEnumerable<string> names, CancellationToken cancellationToken = default)
    {
        if (names is null) throw new ArgumentNullException(nameof(names));
        var copy = names.ToArray();
        var acquired = false;
        lock (_deliveryGate)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(VirexEventSubscription));
            _pendingOperations++;
        }
        try
        {
            await _commands.WaitAsync(cancellationToken).ConfigureAwait(false);
            acquired = true;
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _stop.Token);
            timeout.CancelAfter(_timeoutMs);
            var reply = new TaskCompletionSource<EventSubscriptionResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            var request = new EventSubscriptionRequest { Type = "unsubscribeEvents", RequestId = Guid.NewGuid().ToString("N"), Names = copy };
            lock (_deliveryGate)
            {
                if (_disposed) throw new ObjectDisposedException(nameof(VirexEventSubscription));
                _reply = reply; _replyId = request.RequestId;
            }
            using var cancel = timeout.Token.Register(() => reply.TrySetCanceled());
            await WriteAsync(request, timeout.Token).ConfigureAwait(false);
            EventSubscriptionResponse response;
            try { response = await reply.Task.ConfigureAwait(false); }
            catch (OperationCanceledException)
            {
                var closed = _stop.IsCancellationRequested;
                Dispose(); // Ambiguous wire outcome must not leave callbacks active.
                cancellationToken.ThrowIfCancellationRequested();
                if (closed) throw new OperationCanceledException("Subscription closed.");
                throw new TimeoutException("Unsubscribe acknowledgement timed out.");
            }
            if (!response.Accepted) throw new VirexEventSubscriptionException(response);
            lock (_deliveryGate) foreach (var name in copy) _names.Remove(name);
        }
        catch (Exception ex) when (acquired && ex is IOException or SocketException or ObjectDisposedException or OperationCanceledException)
        {
            Dispose();
            throw;
        }
        finally
        {
            if (acquired)
            {
                lock (_deliveryGate) { _reply = null; _replyId = null; }
                _commands.Release();
            }
            lock (_deliveryGate)
            {
                _pendingOperations--;
                if (_pendingOperations == 0) _drained?.TrySetResult(null);
            }
        }
    }

    private async Task ReceiveAsync()
    {
        try
        {
            while (!_stop.IsCancellationRequested)
            {
                string? line;
                try { line = await _reader.ReadFrameAsync(_stream!, _stop.Token).ConfigureAwait(false); }
                catch (Exception ex) when (_stop.IsCancellationRequested && ex is IOException or SocketException or ObjectDisposedException or OperationCanceledException)
                { return; }
                if (line is null) return;
                lock (_deliveryGate)
                {
                    if (_disposed) return;
                    try
                    {
                        using var doc = JsonDocument.Parse(line);
                        if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("type", out var type)
                            && type.ValueKind == JsonValueKind.String && type.GetString() == "eventSubscription")
                        {
                            var response = ProtocolJson.Deserialize<EventSubscriptionResponse>(line)!;
                            if (response.AppId != AppId || response.InstanceId != InstanceId || response.ConnectionGeneration != ConnectionGeneration)
                                throw new InvalidDataException("Unsubscribe acknowledgement identity changed.");
                            if (response.RequestId == _replyId) _reply?.TrySetResult(response);
                            continue;
                        }
                    }
                    catch (JsonException ex) { Issue(EventSubscriptionErrorCodes.InvalidPayload, ex.Message, line); continue; }
                    if (!EventEnvelopeJson.TryParse(line, out var value, out var error)) { Issue(EventSubscriptionErrorCodes.InvalidPayload, error, line); continue; }
                    if (!_names.Contains(value.Name)) { Issue(EventSubscriptionErrorCodes.UnknownName, "Event name is not subscribed.", line); continue; }
                    if (value.AppId != AppId || value.InstanceId != InstanceId || value.ConnectionGeneration != ConnectionGeneration)
                    { Issue("stale_event_generation", "Event identity or connection generation does not match.", line); continue; }
                    if ((_scope.RequestId is not null && value.RequestId != _scope.RequestId) || (_scope.JobId is not null && value.JobId != _scope.JobId))
                    { Issue("stale_event_work", "Event does not belong to the requested work.", line); continue; }
                    if (!_seen.Add(value.EventId)) { Issue("duplicate_event", "Duplicate event identifier.", line); continue; }
                    _seenOrder.Enqueue(value.EventId);
                    if (_seenOrder.Count > 4096) _seen.Remove(_seenOrder.Dequeue());
                    _onEvent(value);
                }
            }
        }
        finally { Dispose(); }
    }

    private void Issue(string code, string message, string raw)
    {
        LastIssue = new EventStreamIssue { Code = code, Message = message, RawJson = raw };
        _onIssue?.Invoke(LastIssue);
    }
    private async Task WriteAsync(EventSubscriptionRequest request, CancellationToken token)
    {
        var bytes = Encoding.UTF8.GetBytes(ProtocolJson.Serialize(request) + "\n");
        await _stream!.WriteAsync(bytes, 0, bytes.Length, token).ConfigureAwait(false);
        await _stream.FlushAsync(token).ConfigureAwait(false);
    }

    public void Dispose()
    {
        var insideCallback = Monitor.IsEntered(_deliveryGate);
        lock (_deliveryGate)
        {
            if (!_disposed)
            {
                _disposed = true;
                _stop.Cancel();
                _client.Dispose();
                _reply?.TrySetCanceled();
            }
        }
        // A cancellation callback on another thread may be waiting for the delivery lock.
        // Reentrant disposal defers registration cleanup to the receive loop's finally block.
        if (!insideCallback) _registration.Dispose();
    }

    public Task CloseAsync()
    {
        Dispose();
        lock (_deliveryGate) return _closeTask ??= JoinAndReleaseAsync();
    }

    private async Task JoinAndReleaseAsync()
    {
        try { await _completion.ConfigureAwait(false); }
        finally
        {
            Task drained;
            lock (_deliveryGate)
                drained = _pendingOperations == 0 ? Task.CompletedTask
                    : (_drained ??= new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously)).Task;
            await drained.ConfigureAwait(false);
            _registration.Dispose();
            _commands.Dispose();
            _stop.Dispose();
        }
    }
}
