using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
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

            _ = Task.Run(() => HandleClientAsync(client, token), token);
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken token)
    {
        using (client)
        using (var stream = client.GetStream())
        using (var reader = new StreamReader(stream, Encoding.UTF8, false, 4096, true))
        using (var writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, true) { AutoFlush = true, NewLine = "\n" })
        {
            void OnStatus(object? sender, SystemStatus status) => SafeWrite(writer, TcpSocketEventFormatter.FormatStatus(status));
            void OnProductInfo(object? sender, ProductInfo info) => SafeWrite(writer, TcpSocketEventFormatter.FormatProductInfo(info));
            void OnResult(object? sender, ResultSummary result) => SafeWrite(writer, TcpSocketEventFormatter.FormatResult(result));
            void OnError(object? sender, ErrorInfo error) => SafeWrite(writer, TcpSocketEventFormatter.FormatError(error));
            void OnRejected(object? sender, CommandResponse response) => SafeWrite(writer, TcpSocketEventFormatter.FormatCommandRejected(response));

            _session.StatusChanged += OnStatus;
            _session.ProductInfoChanged += OnProductInfo;
            _session.ResultCreated += OnResult;
            _session.ErrorChanged += OnError;
            _session.CommandRejected += OnRejected;

            try
            {
                SafeWrite(writer, TcpSocketEventFormatter.FormatStatus(_session.Status));
                SafeWrite(writer, TcpSocketEventFormatter.FormatProductInfo(_session.ProductInfo));
                while (!token.IsCancellationRequested)
                {
                    var line = await ReadLineAsync(reader, token).ConfigureAwait(false);
                    if (line is null)
                        return;

                    if (!TcpSocketMessageParser.TryParse(line, out var message, out var error))
                    {
                        _session.WriteLog("Invalid TCP frame: " + error);
                        continue;
                    }

                    _session.WriteLog("TCP inbound: " + message.Type);
                    if (message.Type == "status")
                        SafeWrite(writer, TcpSocketEventFormatter.FormatStatusResponse(_session.Status));
                    else if (message.Type == "error")
                        SafeWrite(writer, TcpSocketEventFormatter.FormatErrorResponse(_session.Error));
                    else if (message.Type == "getProductInfo")
                        SafeWrite(writer, TcpSocketEventFormatter.FormatProductInfoResponse(_session.ProductInfo));
                    else if (message.Type == "results")
                    {
                        var items = _session.QueryResults(message.LotID, message.WaferID, message.Recipe);
                        SafeWrite(writer, TcpSocketEventFormatter.FormatResults(new ResultList
                        {
                            Items = items,
                            Count = items.Length,
                        }));
                    }
                    else if (message.Type == "initialize")
                        await _session.InitializeAsync(token).ConfigureAwait(false);
                    else if (message.Type == "deinitialize")
                        await _session.DeinitializeAsync(token).ConfigureAwait(false);
                    else if (message.Type == "productInfo" && message.ProductInfo is not null)
                        await _session.SetProductInfoAsync(message.ProductInfo, token).ConfigureAwait(false);
                    else if (message.Type == "start")
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await _session.StartAsync(new SystemStartRequest { Condition = message.Condition, RunMode = message.RunMode }, token).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                _session.WriteLog("TCP start failed: " + ex.Message);
                            }
                        }, token);
                    else if (message.Type == "stop")
                        await _session.StopAsync(new SystemStopRequest { Reason = message.Reason }, token).ConfigureAwait(false);
                }
            }
            finally
            {
                _session.StatusChanged -= OnStatus;
                _session.ProductInfoChanged -= OnProductInfo;
                _session.ResultCreated -= OnResult;
                _session.ErrorChanged -= OnError;
                _session.CommandRejected -= OnRejected;
            }
        }
    }

    private void SafeWrite(StreamWriter writer, string frame)
    {
        try
        {
            writer.Write(frame);
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
