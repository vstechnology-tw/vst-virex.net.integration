using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Virex.NET.Contracts;
using Virex.NET.Simulator.Core;

namespace Virex.NET.Simulator.WPF.Services;

public sealed class TcpSimulatorServer
{
    private readonly SimulatorSession _session;
    private readonly int _port;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _loop;
    private readonly object _clientsGate = new object();
    private readonly List<Task> _clients = new List<Task>();

    public TcpSimulatorServer(SimulatorSession session, int port)
    {
        _session = session;
        _port = port;
    }

    public Task StartAsync()
    {
        _cts = new CancellationTokenSource();
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();
        _loop = Task.Run(() => AcceptLoopAsync(_cts.Token));
        _session.WriteLog("TCP listening on port " + _port);
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        _cts?.Cancel();
        _listener?.Stop();
        if (_loop is not null)
        {
            try { await _loop.ConfigureAwait(false); } catch { }
        }
        Task[] clients;
        lock (_clientsGate) clients = _clients.ToArray();
        await Task.WhenAll(clients).ConfigureAwait(false);
    }

    private async Task AcceptLoopAsync(CancellationToken token)
    {
        if (_listener is null)
            return;

        while (!token.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await AcceptTcpClientAsync(token).ConfigureAwait(false);
            }
            catch when (token.IsCancellationRequested)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            lock (_clientsGate)
            {
                _clients.RemoveAll(task => task.IsCompleted);
                _clients.Add(HandleClientAsync(client, token));
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken token)
    {
        using (client)
        using (token.Register(client.Dispose))
        using (var stream = client.GetStream())
        using (var reader = new StreamReader(stream, Encoding.UTF8, false, 4096, true))
        using (var writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, true) { AutoFlush = true, NewLine = "\n" })
        {
            var generation = Guid.NewGuid().ToString("N");
            var explicitSubscription = false;
            var names = new HashSet<string>(StringComparer.Ordinal);
            WorkspaceScopeIdentity? pinnedScope = null;
            void WriteLegacy(string frame)
            {
                lock (writer) if (!explicitSubscription) SafeWrite(writer, frame);
            }
            void OnEvent(object? sender, EventEnvelope value)
            {
                lock (writer)
                {
                    if (!explicitSubscription || !names.Contains(value.Name) || token.IsCancellationRequested ||
                        (pinnedScope is not null && !pinnedScope.SameAs(_session.CurrentWorkspaceScope))) return;
                    // The shared producer envelope is immutable on this path; stamp a per-connection copy.
                    var frame = ProtocolJson.Deserialize<EventEnvelope>(ProtocolJson.Serialize(value))!;
                    frame.ConnectionGeneration = generation;
                    SafeWrite(writer, ProtocolJson.Serialize(frame) + "\n");
                }
            }
            void OnWorkspaceScopeChanged(object? sender, WorkspaceScopeIdentity scope)
            {
                lock (writer)
                    if (pinnedScope is not null && !pinnedScope.SameAs(scope)) client.Dispose();
            }
            var runActive = false;
            void OnStatus(object? sender, SystemStatus status)
            {
                WriteLegacy(TcpSocketEventFormatter.FormatStatus(status));
                if (string.Equals(status.State, SystemStates.Running, StringComparison.OrdinalIgnoreCase))
                {
                    runActive = true;
                    WriteLegacy(TcpSocketEventFormatter.FormatRunStarted(status));
                }
                if (runActive && string.Equals(status.State, SystemStates.Ready, StringComparison.OrdinalIgnoreCase))
                {
                    runActive = false;
                    WriteLegacy(TcpSocketEventFormatter.FormatRunCompleted(status));
                }
            }
            void OnProductInfo(object? sender, ProductInfo info) => WriteLegacy(TcpSocketEventFormatter.FormatProductInfo(info));
            void OnOperationMode(object? sender, OperationModeInfo mode) => WriteLegacy(TcpSocketEventFormatter.FormatOperationMode(mode));
            void OnCaptureReady(object? sender, CaptureReadyInfo ready) => WriteLegacy(TcpSocketEventFormatter.FormatCaptureReady(ready));
            void OnCaptureCompleted(object? sender, CaptureCompletedInfo completed) => WriteLegacy(TcpSocketEventFormatter.FormatCaptureCompleted(completed));
            void OnImageGrabbed(object? sender, ImageGrabbedInfo image) => WriteLegacy(TcpSocketEventFormatter.FormatImageGrabbed(image));

            void OnResult(object? sender, ResultSummary result) => WriteLegacy(TcpSocketEventFormatter.FormatResult(result));
            void OnError(object? sender, ErrorInfo error) => WriteLegacy(TcpSocketEventFormatter.FormatError(error));
            void OnRejected(object? sender, CommandResponse response) => WriteLegacy(TcpSocketEventFormatter.FormatCommandRejected(response));

            _session.ImageGrabbed += OnImageGrabbed;
            _session.OperationModeChanged += OnOperationMode;
            _session.CaptureReady += OnCaptureReady;
            _session.CaptureCompleted += OnCaptureCompleted;
            _session.StatusChanged += OnStatus;
            _session.ProductInfoChanged += OnProductInfo;
            _session.ResultCreated += OnResult;
            _session.ErrorChanged += OnError;
            _session.CommandRejected += OnRejected;
            _session.EventProduced += OnEvent;
            _session.WorkspaceScopeChanged += OnWorkspaceScopeChanged;

            try
            {
                SafeWrite(writer, TcpSocketEventFormatter.FormatStatus(_session.Status));
                SafeWrite(writer, TcpSocketEventFormatter.FormatProductInfo(_session.ProductInfo));
                while (!token.IsCancellationRequested)
                {
                    var line = await ReadLineAsync(reader, token).ConfigureAwait(false);
                    if (line is null)
                        return;

                    if (TrySubscription(line, out var subscription))
                    {
                        var catalog = _session.GetEventCatalog();
                        lock (writer)
                        {
                            explicitSubscription = true;
                            var requested = subscription.Names;
                            var unsubscribe = subscription.Type == "unsubscribeEvents";
                            var code = string.IsNullOrWhiteSpace(subscription.RequestId) || requested is null || requested.Length == 0 || requested.Any(string.IsNullOrWhiteSpace)
                                ? EventSubscriptionErrorCodes.InvalidPayload
                                : requested.Distinct(StringComparer.Ordinal).Count() != requested.Length
                                    ? EventSubscriptionErrorCodes.DuplicateName
                                : requested.Any(name => !catalog.Names.Contains(name, StringComparer.Ordinal))
                                    ? EventSubscriptionErrorCodes.UnknownName
                                : !unsubscribe && requested.Any(names.Contains)
                                    ? EventSubscriptionErrorCodes.DuplicateName
                                : unsubscribe && requested.Any(name => !names.Contains(name))
                                    ? EventSubscriptionErrorCodes.NotSubscribed
                                : pinnedScope is not null && subscription.WorkspaceScope is not null &&
                                    !pinnedScope.SameAs(subscription.WorkspaceScope)
                                    ? EventSubscriptionErrorCodes.StaleWorkspaceScope
                                : subscription.WorkspaceScope is { IsValid: false }
                                    ? EventSubscriptionErrorCodes.InvalidPayload
                                : subscription.WorkspaceScope is not null &&
                                    !subscription.WorkspaceScope.SameAs(catalog.WorkspaceScope)
                                    ? EventSubscriptionErrorCodes.StaleWorkspaceScope : null;
                            if (code is null)
                            {
                                foreach (var name in requested!) { if (unsubscribe) names.Remove(name); else names.Add(name); }
                                if (!unsubscribe)
                                    pinnedScope ??= catalog.WorkspaceScope?.Snapshot();
                            }
                            SafeWrite(writer, ProtocolJson.Serialize(new EventSubscriptionResponse
                            {
                                RequestId = subscription.RequestId, Accepted = code is null, ErrorCode = code,
                                AppId = catalog.AppId, InstanceId = catalog.InstanceId,
                                WorkspaceScope = catalog.WorkspaceScope?.Snapshot(), ConnectionGeneration = generation,
                                Names = names.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
                            }) + "\n");
                        }
                        continue;
                    }

                    if (!TcpSocketMessageParser.TryParse(line, out var message, out var error))
                    {
                        _session.WriteLog("Invalid TCP frame: " + error);
                        _session.ReportFailure("Unknown", CommandErrorCodes.InvalidPayload, "Invalid TCP command frame.", CommandPayloadJson.TryReadRequestId(line));
                        continue;
                    }

                    _session.WriteLog("TCP inbound: " + message.Type);
                    try
                    {
                        if (message.Type == "operationmode")
                            SafeWrite(writer, CommandPayloadJson.WithRequestId(TcpSocketEventFormatter.FormatOperationMode(_session.OperationMode, "operationMode"), message.RequestId));
                        else if (message.Type == "setoperationmode")
                        {
                            var response = await _session.SetOperationModeAsync(new SetOperationModeRequest { Mode = message.Mode }, token).ConfigureAwait(false);
                            response.RequestId = message.RequestId;
                            SafeWrite(writer, TcpSocketEventFormatter.FormatCommandResponse(response));
                        }
                        else if (message.Type == "status")
                            SafeWrite(writer, CommandPayloadJson.WithRequestId(TcpSocketEventFormatter.FormatStatusResponse(_session.Status), message.RequestId));
                        else if (message.Type == "error")
                            SafeWrite(writer, CommandPayloadJson.WithRequestId(TcpSocketEventFormatter.FormatErrorResponse(_session.Error), message.RequestId));
                        else if (message.Type == "getProductInfo")
                            SafeWrite(writer, CommandPayloadJson.WithRequestId(TcpSocketEventFormatter.FormatProductInfoResponse(_session.ProductInfo), message.RequestId));
                        else if (message.Type == "results")
                        {
                            var items = _session.QueryResults(message.LotID, message.WaferID, message.Recipe);
                            SafeWrite(writer, CommandPayloadJson.WithRequestId(TcpSocketEventFormatter.FormatResults(new ResultList
                            {
                                Items = items,
                                Count = items.Length,
                            }), message.RequestId));
                        }
                        else if (message.Type == "initialize")
                            await _session.InitializeAsync(token).ConfigureAwait(false);
                        else if (message.Type == "deinitialize")
                            await _session.DeinitializeAsync(token).ConfigureAwait(false);
                        else if (message.Type == "productInfo" && message.ProductInfo is not null)
                            await _session.SetProductInfoAsync(message.ProductInfo, token).ConfigureAwait(false);
                        else if (message.Type == "start")
                            await _session.StartAsync(new SystemStartRequest { RequestId = message.RequestId ?? Guid.NewGuid().ToString("N"), Condition = message.Condition, RunMode = message.RunMode, InspectionMode = message.InspectionMode, ProductInfo = message.ProductInfo }, token).ConfigureAwait(false);
                        else if (message.Type == "stop")
                            await _session.StopAsync(new SystemStopRequest { Reason = message.Reason }, token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested) { return; }
                    catch (Exception ex)
                    {
                        _session.WriteLog("TCP operation failed: " + ex.Message);
                        _session.ReportFailure(message.Type, CommandErrorCodes.CommandFailed, "The TCP operation could not be completed.", message.RequestId);
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException or OperationCanceledException)
            {
                if (!token.IsCancellationRequested) _session.WriteLog("TCP client disconnected: " + ex.Message);
            }
            finally
            {
                _session.EventProduced -= OnEvent;
                _session.WorkspaceScopeChanged -= OnWorkspaceScopeChanged;
                _session.StatusChanged -= OnStatus;
                _session.OperationModeChanged -= OnOperationMode;
                _session.CaptureReady -= OnCaptureReady;
                _session.CaptureCompleted -= OnCaptureCompleted;
                _session.ProductInfoChanged -= OnProductInfo;
                _session.ImageGrabbed -= OnImageGrabbed;
                _session.ResultCreated -= OnResult;
                _session.ErrorChanged -= OnError;
                _session.CommandRejected -= OnRejected;
            }
        }
    }

    private static bool TrySubscription(string line, out EventSubscriptionRequest value)
    {
        value = new EventSubscriptionRequest();
        try
        {
            using var doc = JsonDocument.Parse(line);
            if (doc.RootElement.ValueKind != JsonValueKind.Object || !doc.RootElement.TryGetProperty("type", out var type)
                || type.ValueKind != JsonValueKind.String || type.GetString() is not ("subscribeEvents" or "unsubscribeEvents")) return false;
            value.Type = type.GetString()!;
            value.RequestId = CommandPayloadJson.TryReadRequestId(line) ?? string.Empty;
            try { value = ProtocolJson.Deserialize<EventSubscriptionRequest>(line)!; }
            catch (JsonException) { value.Names = []; }
            return true;
        }
        catch (JsonException) { return false; }
    }

    private void SafeWrite(StreamWriter writer, string frame)
    {
        try
        {
            lock (writer) writer.Write(frame);
        }
        catch (IOException ex)
        {
            _session.WriteLog("TCP write failed: " + ex.Message);
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private Task<TcpClient> AcceptTcpClientAsync(CancellationToken token)
    {
#if NET48
        return _listener!.AcceptTcpClientAsync();
#else
        return _listener!.AcceptTcpClientAsync(token).AsTask();
#endif
    }

    private static Task<string?> ReadLineAsync(StreamReader reader, CancellationToken token)
    {
#if NET48
        return reader.ReadLineAsync();
#else
        return reader.ReadLineAsync(token).AsTask();
#endif
    }
}
