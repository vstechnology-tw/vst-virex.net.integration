using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Virex.NET.Client;
using Virex.NET.Contracts;
using Virex.NET.Simulator.Core;
using Virex.NET.Simulator.WPF.Services;

namespace Virex.NET.Contracts.Tests;

public sealed class InspectionModeTests
{
    [Theory]
    [InlineData(null)]
    [InlineData(InspectionModes.CaptureOnly)]
    [InlineData(InspectionModes.CaptureAndInspect)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("CaptureOnly")]
    [InlineData("captureOnly ")]
    [InlineData("unknown")]
    public void AllPayloadsPreserveSuppliedModeForValidation(string? mode)
    {
        var request = new SystemStartRequest { Condition = "sample", RunMode = ControlRunModes.SingleRun, InspectionMode = mode };
        var json = ProtocolJson.Serialize(request);
        Assert.Equal(mode, CommandPayloadJson.ReadObject<SystemStartRequest>(json)!.InspectionMode);
        Assert.Equal(mode, CommandPayloadJson.ReadObject<MqttCommandRequest>(json)!.InspectionMode);
        Assert.True(TcpSocketMessageParser.TryParse(TcpSocketEventFormatter.FormatStartCommand(request.Condition, request.RunMode, mode), out var frame, out _));
        Assert.Equal(mode, frame.InspectionMode);
        Assert.Equal(ControlRunModes.SingleRun, frame.RunMode);
        Assert.Equal(mode is null or InspectionModes.CaptureOnly or InspectionModes.CaptureAndInspect, InspectionModes.IsValid(mode));
        if (mode is null) Assert.DoesNotContain("inspectionMode", json);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("1")]
    [InlineData("{}")]
    [InlineData("[]")]
    public void InvalidJsonTypesCannotBecomeLegacyMode(string value)
    {
        var json = "{\"type\":\"start\",\"inspectionMode\":" + value + "}";
        Assert.Throws<JsonException>(() => CommandPayloadJson.ReadObject<SystemStartRequest>(json));
        Assert.Throws<JsonException>(() => CommandPayloadJson.ReadObject<MqttCommandRequest>(json));
        Assert.False(TcpSocketMessageParser.TryParse(json, out _, out _));
    }

    [Theory]
    [InlineData("rest")]
    [InlineData("tcp")]
    [InlineData("mqtt")]
    public async Task RealTransportsPreserveModesRejectInvalidAndKeepRunsIndependent(string transport)
    {
        await using var fixture = await Fixture.CreateAsync(transport);
        foreach (var invalid in new[] { "", " ", "unknown", "CaptureOnly", "captureOnly " })
        {
            var rejection = await fixture.StartAsync(new SystemStartRequest { InspectionMode = invalid });
            Assert.False(rejection.Accepted);
            Assert.Equal(CommandErrorCodes.InvalidInspectionMode, rejection.ErrorCode);
            Assert.Equal(SimulatorState.Ready, fixture.Session.State);
        }

        var recipe = ProtocolJson.Serialize(fixture.Session.GetCurrentRecipeParameters());
        var captures = new List<ImageGrabbedInfo>();
        var results = new List<ResultSummary>();
        fixture.Session.ImageGrabbed += (_, capture) => captures.Add(capture);
        fixture.Session.ResultCreated += (_, result) => results.Add(result);
        foreach (var runMode in new[] { ControlRunModes.SingleRun, ControlRunModes.Continue })
        {
            foreach (var inspectionMode in new[] { InspectionModes.CaptureOnly, InspectionModes.CaptureAndInspect, null })
            {
                var before = results.Count;
                var request = new SystemStartRequest { Condition = "same-recipe", RunMode = runMode, InspectionMode = inspectionMode };
                Assert.True((await fixture.StartAsync(request)).Accepted);
                request.InspectionMode = "unknown"; // Caller mutation cannot change an accepted run.
                Assert.True((await fixture.Session.RunCompletedAsync()).Accepted);
                Assert.Equal(SimulatorState.Ready, fixture.Session.State);
                var capture = captures[^1];
                Assert.Single(Directory.GetFiles(fixture.Root, capture.CaptureId + ".bmp", SearchOption.AllDirectories));
                Assert.Single(Directory.GetFiles(fixture.Root, capture.CaptureId + ".jpg", SearchOption.AllDirectories));
                if (inspectionMode == InspectionModes.CaptureOnly)
                {
                    Assert.Equal(before, results.Count);
                    var path = Assert.Single(Directory.GetFiles(fixture.Root, capture.CaptureId + ".capture.json", SearchOption.AllDirectories));
                    using var diagnostic = JsonDocument.Parse(await File.ReadAllTextAsync(path));
                    Assert.Equal(InspectionModes.CaptureOnly, diagnostic.RootElement.GetProperty("inspectionMode").GetString());
                    Assert.False(diagnostic.RootElement.TryGetProperty("overallResult", out _));
                    Assert.Empty(Directory.GetFiles(fixture.Root, capture.CaptureId + ".json", SearchOption.AllDirectories));
                }
                else
                {
                    Assert.Equal(before + 1, results.Count);
                    Assert.Equal("OK", results[^1].OverallResult);
                    Assert.Equal(capture.CaptureId, results[^1].CaptureId);
                    Assert.Equal("same-recipe", results[^1].Condition);
                    Assert.Equal(capture.CaptureId, (await fixture.Session.GetResultDetailAsync(capture.CaptureId)).ResultId);
                }
                Assert.Equal(recipe, ProtocolJson.Serialize(fixture.Session.GetCurrentRecipeParameters()));
            }
        }
        Assert.Equal(6, captures.Count);
        Assert.Equal(4, results.Count);
    }

    [Fact]
    public async Task CaptureOnlyContinueCapturesWithoutInspectionUntilStopAndOmissionResetsSelection()
    {
        await using var fixture = await Fixture.CreateAsync("rest");
        var captured = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Session.ImageGrabbed += (_, _) => captured.TrySetResult();
        Assert.True((await fixture.StartAsync(new SystemStartRequest { RunMode = ControlRunModes.Continue, InspectionMode = InspectionModes.CaptureOnly })).Accepted);
        await captured.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(SimulatorState.Running, fixture.Session.State);
        Assert.True((await fixture.Session.StopAsync()).Accepted);
        Assert.Empty(fixture.Session.Results);
        Assert.True((await fixture.StartAsync(new SystemStartRequest())).Accepted);
        await fixture.Session.RunCompletedAsync();
        Assert.Single(fixture.Session.Results);
    }

    [Fact]
    public async Task CapturePersistenceFailureDoesNotPublishNormalResultAndNextRunUsesLegacyMode()
    {
        await using var fixture = await Fixture.CreateAsync("rest");
        Directory.Delete(fixture.Root);
        await File.WriteAllTextAsync(fixture.Root, "block directory creation");
        Assert.True((await fixture.StartAsync(new SystemStartRequest { InspectionMode = InspectionModes.CaptureOnly })).Accepted);
        await fixture.Session.RunCompletedAsync();
        Assert.True(fixture.Session.Error.HasError);
        Assert.Empty(fixture.Session.Results);
        File.Delete(fixture.Root);
        Directory.CreateDirectory(fixture.Root);
        Assert.True((await fixture.StartAsync(new SystemStartRequest())).Accepted);
        await fixture.Session.RunCompletedAsync();
        Assert.Single(fixture.Session.Results);
    }

    [Fact]
    public async Task RawRestMalformedModeIs400AndOldEmptyBodyStillRuns()
    {
        await using var fixture = await Fixture.CreateAsync("rest");
        using var http = new HttpClient { BaseAddress = new Uri(fixture.Prefix) };
        using var invalid = await http.PostAsync(RestRoutes.ApiSystemStart, new StringContent("{\"inspectionMode\":true}", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(CommandErrorCodes.InvalidPayload, ProtocolJson.Deserialize<CommandResponse>(await invalid.Content.ReadAsStringAsync())!.ErrorCode);
        using var legacy = await http.PostAsync(RestRoutes.ApiSystemStart, null);
        Assert.Equal(HttpStatusCode.OK, legacy.StatusCode);
        await fixture.Session.RunCompletedAsync();
        Assert.Single(fixture.Session.Results);
        using var api = await http.GetAsync("openapi/v1.json");
        using var document = JsonDocument.Parse(await api.Content.ReadAsStringAsync());
        var property = document.RootElement.GetProperty("components").GetProperty("schemas").GetProperty("SystemStartRequest").GetProperty("properties").GetProperty("inspectionMode");
        Assert.Equal(2, property.GetProperty("enum").GetArrayLength());
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly string _transport;
        private readonly RestSimulatorServer _rest;
        private readonly TcpSimulatorServer _tcp;
        private readonly EmbeddedMqttBroker _broker;
        private readonly MqttSimulatorPublisher _publisher;
        private readonly VirexClient _client;
        private TcpClient? _socket;
        private StreamReader? _reader;
        private StreamWriter? _writer;

        private Fixture(string transport)
        {
            _transport = transport;
            Root = Path.Combine(Path.GetTempPath(), "virex-mode-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
            Session = new SimulatorSession(Root);
            Prefix = "http://127.0.0.1:" + FreePort() + "/";
            var tcpPort = FreePort();
            var mqttPort = FreePort();
            _rest = new RestSimulatorServer(Session, Prefix);
            _tcp = new TcpSimulatorServer(Session, tcpPort);
            _broker = new EmbeddedMqttBroker(mqttPort);
            var topic = "mode-" + Guid.NewGuid().ToString("N");
            _publisher = new MqttSimulatorPublisher(Session, "127.0.0.1", mqttPort, topic);
            _client = new VirexClient(new VirexClientOptions { RestBaseUrl = Prefix, TcpPort = tcpPort, MqttPort = mqttPort, MqttTopic = topic });
        }

        public string Root { get; }
        public string Prefix { get; }
        public SimulatorSession Session { get; }

        public static async Task<Fixture> CreateAsync(string transport)
        {
            var fixture = new Fixture(transport);
            await fixture._rest.StartAsync();
            if (transport == "tcp")
            {
                await fixture._tcp.StartAsync();
                fixture._socket = new TcpClient();
                await fixture._socket.ConnectAsync(IPAddress.Loopback, fixture._client.Options.TcpPort);
                fixture._reader = new StreamReader(fixture._socket.GetStream(), Encoding.UTF8);
                fixture._writer = new StreamWriter(fixture._socket.GetStream(), new UTF8Encoding(false)) { AutoFlush = true };
            }
            if (transport == "mqtt")
            {
                await fixture._broker.StartAsync();
                await fixture._publisher.StartAsync();
            }
            await fixture.Session.InitializeAsync();
            return fixture;
        }

        public async Task<CommandResponse> StartAsync(SystemStartRequest request)
        {
            if (_transport == "mqtt") return request.InspectionMode is null
                ? await _client.MqttCommands.StartAsync(request.Condition, request.RunMode)
                : await _client.MqttCommands.StartWithOptionsAsync(request);
            if (_transport == "rest")
            {
                try { return request.InspectionMode is null ? await _client.StartAsync(request.Condition, request.RunMode) : await _client.StartAsync(request); }
                catch (VirexClientException error)
                {
                    Assert.Equal(400, error.StatusCode);
                    return ProtocolJson.Deserialize<CommandResponse>(error.ResponseBody)!;
                }
            }
            await _writer!.WriteAsync(request.InspectionMode is null
                ? TcpSocketEventFormatter.FormatStartCommand(request.Condition, request.RunMode)
                : TcpSocketEventFormatter.FormatStartCommand(request.Condition, request.RunMode, request.InspectionMode));
            while (true)
            {
                using var frame = JsonDocument.Parse((await _reader!.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(5)))!);
                var root = frame.RootElement;
                if (root.GetProperty("type").GetString() == "commandRejected") return ProtocolJson.Deserialize<CommandResponse>(root.GetRawText())!;
                if (root.GetProperty("type").GetString() == "statusChanged" && root.GetProperty("state").GetString() == SystemStates.Running)
                    return new CommandResponse { Accepted = true, State = SystemStates.Running };
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (Session.State == SimulatorState.Running) await Session.StopAsync();
            _socket?.Dispose();
            if (_transport == "tcp") await _tcp.StopAsync();
            if (_transport == "mqtt") await _publisher.StopAsync();
            await _broker.DisposeAsync();
            await _rest.StopAsync();
            _client.Dispose();
            if (Directory.Exists(Root)) Directory.Delete(Root, true);
            else if (File.Exists(Root)) File.Delete(Root);
        }

        private static int FreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}
