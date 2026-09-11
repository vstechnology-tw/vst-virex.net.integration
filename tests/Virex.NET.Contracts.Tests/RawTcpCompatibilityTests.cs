using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Virex.NET.Simulator.Core;
using Virex.NET.Simulator.WPF.Services;

namespace Virex.NET.Contracts.Tests;

public sealed class RawTcpCompatibilityTests
{
    [Theory]
    [InlineData("\"type\":\"productInfo\",")]
    [InlineData("\"type\":\"productInfoChanged\",")]
    [InlineData("")]
    public async Task ExistingStringCommandsWorkAfterSingleWithoutRequestIds(string typeField)
    {
        var folder = Path.Combine(Path.GetTempPath(), "VirexRawCompatibility", Guid.NewGuid().ToString("N"));
        using var reservation = new TcpListener(IPAddress.Loopback, 0);
        reservation.Start();
        var port = ((IPEndPoint)reservation.LocalEndpoint).Port;
        reservation.Stop();
        var server = new TcpSimulatorServer(new SimulatorSession(folder), port);
        await server.StartAsync();
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, port);
            using var reader = new StreamReader(client.GetStream(), Encoding.UTF8, false, 4096, true);
            using var writer = new StreamWriter(client.GetStream(), new UTF8Encoding(false), 4096, true)
            { AutoFlush = true, NewLine = "\n" };

            await writer.WriteLineAsync("{\"type\":\"initialize\"}");
            await ReadUntilAsync(reader, "statusChanged", "state", "Ready");
            await writer.WriteLineAsync("{\"type\":\"start\",\"runMode\":\"single\"}");
            await ReadUntilAsync(reader, "runCompleted");

            await writer.WriteLineAsync("{" + typeField + "\"lotID\":\"RAW-LOT\",\"waferID\":\"RAW-WAFER\",\"recipe\":\"RAW-RECIPE\",\"slot\":\"1\",\"foupID\":\"F\",\"chamberID\":\"C\"}");
            await ReadUntilAsync(reader, "productInfoChanged", "waferID", "RAW-WAFER");
            await writer.WriteLineAsync("{\"type\":\"status\"}");
            var status = await ReadUntilAsync(reader, "status");
            Assert.Equal("Ready", status.GetProperty("state").GetString());
            Assert.False(status.TryGetProperty("requestId", out _));
            await writer.WriteLineAsync("{\"type\":\"getProductInfo\"}");
            var product = await ReadUntilAsync(reader, "productInfo");
            Assert.Equal("RAW-WAFER", product.GetProperty("waferID").GetString());

            await writer.WriteLineAsync("{\"type\":\"start\",\"runMode\":\"single\"}");
            var result = await ReadUntilAsync(reader, "resultCreated");
            Assert.Equal("RAW-LOT", result.GetProperty("lotID").GetString());
            Assert.Equal("RAW-WAFER", result.GetProperty("waferID").GetString());
            await ReadUntilAsync(reader, "runCompleted");
            await writer.WriteLineAsync("{\"type\":\"results\",\"lotID\":\"RAW-LOT\",\"waferID\":\"RAW-WAFER\"}");
            var results = await ReadUntilAsync(reader, "results");
            Assert.Equal(1, results.GetProperty("count").GetInt32());
            Assert.Equal(result.GetProperty("resultId").GetString(), results.GetProperty("items")[0].GetProperty("resultId").GetString());

            await writer.WriteLineAsync("{\"type\":\"start\",\"runMode\":\"continue\"}");
            await ReadUntilAsync(reader, "runStarted");
            await writer.WriteLineAsync("{\"type\":\"stop\"}");
            await ReadUntilAsync(reader, "runCompleted");
            await writer.WriteLineAsync("{\"type\":\"deinitialize\"}");
            await ReadUntilAsync(reader, "statusChanged", "state", "Uninitialized");
        }
        finally
        {
            await server.StopAsync();
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
    }

    private static async Task<JsonElement> ReadUntilAsync(StreamReader reader, string expectedType, string? property = null, string? expectedValue = null)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        while (true)
        {
            var line = await reader.ReadLineAsync(deadline.Token);
            Assert.NotNull(line);
            using var document = JsonDocument.Parse(line);
            var frame = document.RootElement;
            var type = frame.GetProperty("type").GetString();
            Assert.NotEqual("commandRejected", type);
            if (type == expectedType && (property is null || frame.GetProperty(property).GetString() == expectedValue))
                return frame.Clone();
        }
    }
}
