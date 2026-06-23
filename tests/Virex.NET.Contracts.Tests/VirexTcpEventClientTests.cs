using System.Net;
using System.Net.Sockets;
using System.Text;
using Virex.NET.Client;
using Virex.NET.Contracts;

namespace Virex.NET.Contracts.Tests;

public sealed class VirexTcpEventClientTests
{
    [Fact]
    public async Task RunAsyncTimesOutWhenPartialFrameIsNotCompleted()
    {
        using var listener = StartLoopbackListener(out var port);
        var client = new VirexTcpEventClient(new VirexClientOptions
        {
            TcpHost = "127.0.0.1",
            TcpPort = port,
            TcpFrameTimeoutMs = 100,
        });

        var runTask = client.RunAsync();
        using var accepted = await listener.AcceptTcpClientAsync();
        await accepted.GetStream().WriteAsync(Encoding.UTF8.GetBytes("{\"type\":\"status\""));

        await Assert.ThrowsAsync<TimeoutException>(() => runTask);
    }

    [Fact]
    public async Task RunAsyncAllowsLongGapBetweenCompleteFrames()
    {
        using var listener = StartLoopbackListener(out var port);
        using var cancellation = new CancellationTokenSource();
        var received = new TaskCompletionSource<VirexEvent>();
        var client = new VirexTcpEventClient(new VirexClientOptions
        {
            TcpHost = "127.0.0.1",
            TcpPort = port,
            TcpFrameTimeoutMs = 100,
        });
        client.EventReceived += (_, value) => received.TrySetResult(value);

        var runTask = client.RunAsync(cancellation.Token);
        using var accepted = await listener.AcceptTcpClientAsync();
        var frame = TcpSocketEventFormatter.FormatStatus(new StatusDto
        {
            Initialized = true,
            ProcessState = ProcessStates.Ready,
            Recipe = "Default",
        });
        await accepted.GetStream().WriteAsync(Encoding.UTF8.GetBytes(frame));

        var value = await received.Task;
        Assert.Equal("status", value.Type);

        await Task.Delay(250);
        Assert.False(runTask.IsCompleted);

        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
    }

    private static TcpListener StartLoopbackListener(out int port)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        port = ((IPEndPoint)listener.LocalEndpoint).Port;
        return listener;
    }
}
