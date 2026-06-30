using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Virex.NET.Contracts;
using Virex.NET.Simulator.Core;
using Virex.NET.Simulator.WPF.Services;

namespace Virex.NET.Contracts.Tests;

public sealed class TcpSimulatorServerTests
{
    [Fact]
    public async Task TcpInitializeAndDeinitializeCommandsChangeState()
    {
        var session = new SimulatorSession();
        var port = GetFreeTcpPort();
        var server = new TcpSimulatorServer(session, port);
        await server.StartAsync();

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, port);
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, false, 4096, true);
            using var writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, true) { AutoFlush = true, NewLine = "\n" };

            await writer.WriteLineAsync("""{"type":"initialize"}""");
            Assert.Equal(SystemStates.Ready, await ReadStatusStateAsync(reader, SystemStates.Ready));

            await writer.WriteLineAsync("""{"type":"deinitialize"}""");
            Assert.Equal(SystemStates.Uninitialized, await ReadStatusStateAsync(reader, SystemStates.Uninitialized));
        }
        finally
        {
            await server.StopAsync();
        }
    }

    private static async Task<string> ReadStatusStateAsync(StreamReader reader, string expectedState)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        while (!timeout.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(timeout.Token);
            if (line is null)
                throw new EndOfStreamException("TCP stream closed before the expected statusChanged frame was received.");

            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (root.TryGetProperty("type", out var type) &&
                type.GetString() == "statusChanged" &&
                root.TryGetProperty("state", out var state) &&
                state.GetString() == expectedState)
            {
                return expectedState;
            }
        }

        throw new TimeoutException("Timed out waiting for the expected statusChanged frame.");
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
