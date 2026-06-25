using System.Text.Json;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Virex.NET.Contracts;
using Virex.NET.Simulator.WPF.Services;

namespace Virex.NET.Contracts.Tests;

public sealed class ProtocolFormatterTests
{
    [Fact]
    public void StatusFrameUsesTypedNdjsonShape()
    {
        var frame = TcpSocketEventFormatter.FormatStatus(new StatusDto
        {
            Initialized = true,
            ProcessState = ProcessStates.Ready,
            Recipe = "Default",
        });

        Assert.EndsWith("\n", frame);
        using var doc = JsonDocument.Parse(frame);
        Assert.Equal("status", doc.RootElement.GetProperty("type").GetString());
        Assert.True(doc.RootElement.GetProperty("initialized").GetBoolean());
        Assert.Equal("ready", doc.RootElement.GetProperty("processState").GetString());
        Assert.Equal("Default", doc.RootElement.GetProperty("recipe").GetString());
    }

    [Fact]
    public void ResultFrameDoesNotIncludeDefectList()
    {
        var frame = TcpSocketEventFormatter.FormatResult(new ResultSummaryDto
        {
            ResultId = "RID-1",
            Timestamp = "2026-06-20T15:30:12+08:00",
            LotId = "LOT-1",
            WaferId = "W01",
            RecipeId = "RCP-A",
            Slot = "1",
            FoupId = "FOUP-A",
            ChamberId = "CH-1",
            OverallResult = "OK",
            DefectCount = 0,
            ImageRelativePath = "20260620/LOT-1/20260620_153012_W01.tiff",
            ResultRelativePath = "20260620/LOT-1/20260620_153012_W01.json",
            ImagePath = "/data/virex-results/20260620/LOT-1/20260620_153012_W01.tiff",
            PreviewImagePath = "/data/virex-results/20260620/LOT-1/20260620_153012_W01.jpg",
            ResultPath = "/data/virex-results/20260620/LOT-1/20260620_153012_W01.json",
        });

        using var doc = JsonDocument.Parse(frame);
        Assert.Equal("result", doc.RootElement.GetProperty("type").GetString());
        Assert.Equal("RID-1", doc.RootElement.GetProperty("resultId").GetString());
        Assert.Equal("/data/virex-results/20260620/LOT-1/20260620_153012_W01.jpg", doc.RootElement.GetProperty("previewImagePath").GetString());
        Assert.False(doc.RootElement.TryGetProperty("dieCount", out _));
        Assert.False(doc.RootElement.TryGetProperty("defectList", out _));
    }

    [Fact]
    public void SimulatorResultUsesTimestampWaferArtifactNamesAndPreviewImagePath()
    {
        var session = new SimulatorSession();

        var result = session.EmitResult("/data/virex-results");

        Assert.Matches(@"^\d{8}/LOT-001/\d{8}_\d{6}_W01\.tiff$", result.ImageRelativePath);
        Assert.Matches(@"^\d{8}/LOT-001/\d{8}_\d{6}_W01\.json$", result.ResultRelativePath);
        Assert.Matches(@"^/data/virex-results/\d{8}/LOT-001/\d{8}_\d{6}_W01\.tiff$", result.ImagePath);
        Assert.Matches(@"^/data/virex-results/\d{8}/LOT-001/\d{8}_\d{6}_W01\.jpg$", result.PreviewImagePath);
        Assert.Matches(@"^/data/virex-results/\d{8}/LOT-001/\d{8}_\d{6}_W01\.json$", result.ResultPath);
    }

    [Fact]
    public void WaferInfoParserAcceptsNumberOrStringSlot()
    {
        Assert.True(WaferInfoJsonParser.TryParse(
            """{"lotId":"LOT-1","waferId":"W01","recipeId":"RCP-A","slot":1,"foupId":"FOUP-A","chamberId":"CH-1"}""",
            out var numberSlot,
            out _));
        Assert.Equal("1", numberSlot.Slot);

        Assert.True(WaferInfoJsonParser.TryParse(
            """{"lotId":"LOT-1","waferId":"W01","recipeId":"RCP-A","slot":"A01","foupId":"FOUP-A","chamberId":"CH-1"}""",
            out var stringSlot,
            out _));
        Assert.Equal("A01", stringSlot.Slot);
    }

    [Fact]
    public void TcpMessageParserRoutesCommandsAndWaferInfo()
    {
        Assert.True(TcpSocketMessageParser.TryParse("""{"type":"start"}""", out var start, out _));
        Assert.Equal("start", start.Type);
        Assert.Null(start.Condition);
        Assert.Equal(ControlRunModes.Continue, start.RunMode);

        Assert.True(TcpSocketMessageParser.TryParse(
            """{"type":"waferInfo","lotId":"LOT-1","waferId":"W01","recipeId":"RCP-A","slot":"1","foupId":"FOUP-A","chamberId":"CH-1"}""",
            out var wafer,
            out _));
        Assert.Equal("waferInfo", wafer.Type);
        Assert.Equal("LOT-1", wafer.WaferInfo?.LotId);
    }

    [Fact]
    public void TcpCommandPayloadsIncludeOptionalStartConditionRunModeAndStopReason()
    {
        var startFrame = TcpSocketEventFormatter.FormatStartCommand("golden-sample", ControlRunModes.SingleRun);
        var stopFrame = TcpSocketEventFormatter.FormatStopCommand("operator-request");

        using var startDoc = JsonDocument.Parse(startFrame);
        Assert.Equal("start", startDoc.RootElement.GetProperty("type").GetString());
        Assert.Equal("golden-sample", startDoc.RootElement.GetProperty("condition").GetString());
        Assert.Equal("single", startDoc.RootElement.GetProperty("runMode").GetString());

        using var stopDoc = JsonDocument.Parse(stopFrame);
        Assert.Equal("stop", stopDoc.RootElement.GetProperty("type").GetString());
        Assert.Equal("operator-request", stopDoc.RootElement.GetProperty("reason").GetString());
    }

    [Fact]
    public void TcpMessageParserReadsOptionalStartConditionRunModeAndStopReason()
    {
        Assert.True(TcpSocketMessageParser.TryParse(
            """{"type":"start","condition":"golden-sample","runMode":"single"}""",
            out var start,
            out _));
        Assert.Equal("start", start.Type);
        Assert.Equal("golden-sample", start.Condition);
        Assert.Equal(ControlRunModes.SingleRun, start.RunMode);

        Assert.True(TcpSocketMessageParser.TryParse(
            """{"type":"stop","reason":"operator-request"}""",
            out var stop,
            out _));
        Assert.Equal("stop", stop.Type);
        Assert.Equal("operator-request", stop.Reason);
    }

    [Fact]
    public void TcpMessageParserDefaultsBlankStartRunModeToContinue()
    {
        Assert.True(TcpSocketMessageParser.TryParse(
            """{"type":"start","runMode":"   "}""",
            out var start,
            out _));

        Assert.Equal(ControlRunModes.Continue, start.RunMode);
    }

    [Fact]
    public void EventParserReadsStatusEvent()
    {
        var json = TcpSocketEventFormatter.FormatStatus(new StatusDto
        {
            Initialized = true,
            ProcessState = ProcessStates.Capturing,
            Recipe = "Default",
        });

        Assert.True(VirexEventParser.TryParse(json, out var value, out _));
        Assert.Equal("status", value.Type);
        Assert.Equal(ProcessStates.Capturing, value.Status?.ProcessState);
    }

    [Fact]
    public async Task TcpServerProcessesStopReasonWhileStartCommandIsRunning()
    {
        var session = new SimulatorSession();
        var messages = new List<string>();
        session.Log += (_, message) => messages.Add(message);
        session.Initialize("Default");
        var port = GetFreeTcpPort();
        var server = new TcpSimulatorServer(session, port);
        await server.StartAsync();

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync("127.0.0.1", port);
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, false, 4096, true);
            await reader.ReadLineAsync();
            await reader.ReadLineAsync();

            await WriteFrameAsync(stream, TcpSocketEventFormatter.FormatStartCommand("golden-sample"));
            Assert.Contains("capturing", await ReadLineWithTimeoutAsync(reader));
            await WriteFrameAsync(stream, TcpSocketEventFormatter.FormatStopCommand("operator-request"));
            Assert.Contains("ready", await ReadLineWithTimeoutAsync(reader));

            Assert.Contains("Stopped. reason=operator-request", messages);
        }
        finally
        {
            await server.StopAsync();
        }
    }

    private static async Task WriteFrameAsync(NetworkStream stream, string frame)
    {
        var bytes = Encoding.UTF8.GetBytes(frame);
        await stream.WriteAsync(bytes);
        await stream.FlushAsync();
    }

    private static async Task<string> ReadLineWithTimeoutAsync(StreamReader reader)
    {
        var read = reader.ReadLineAsync();
        var completed = await Task.WhenAny(read, Task.Delay(1500));
        if (completed != read)
            throw new TimeoutException("Timed out waiting for TCP frame.");

        return await read ?? string.Empty;
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
