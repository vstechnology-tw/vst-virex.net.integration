using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;
using Virex.NET.Client;
using Virex.NET.Contracts;
using Virex.NET.Simulator.Core;
using Virex.NET.Simulator.WPF.Services;

namespace Virex.NET.Contracts.Tests;

public sealed class OperationModeTests
{
    [Theory]
    [InlineData("rest")]
    [InlineData("tcp")]
    [InlineData("mqtt")]
    public async Task AppliedModeAndSourcePolicyAreConsistentAcrossRealTransports(string transport)
    {
        await using var fixture = await PublicTransportFixture.CreateAsync(true);
        var session = fixture.Session;
        var changes = new List<OperationModeInfo>();
        session.OperationModeChanged += (_, mode) => changes.Add(mode);
        Assert.Equal(OperationModes.Local, (await fixture.GetModeAsync(transport)).Mode);
        Assert.True((await fixture.GetModeAsync(transport)).ManagementEnabled);
        Assert.Equal(CommandErrorCodes.OperationNotAllowed, (await fixture.StartAsync(transport)).ErrorCode);
        Assert.Equal(CommandErrorCodes.OperationNotAllowed, (await session.InitializeAsync()).ErrorCode);
        Assert.True((await session.InitializeFromSourceAsync(OperationSource.Local)).Accepted);
        Assert.Equal(CommandErrorCodes.OperationNotAllowed, (await fixture.StartAsync(transport)).ErrorCode);
        foreach (var mode in new[] { "", " ", "Remote", "remote ", "bad" })
        {
            var invalid = await fixture.SetModeAsync(transport, mode);
            Assert.False(invalid.Accepted);
            Assert.Equal(CommandErrorCodes.InvalidOperationMode, invalid.ErrorCode);
            Assert.Equal(OperationModes.Local, (await fixture.GetModeAsync(transport)).Mode);
        }
        var applied = await fixture.SetModeAsync(transport, OperationModes.Remote);
        Assert.True(applied.Accepted);
        Assert.Equal(OperationModes.Remote, applied.OperationMode!.Mode);
        Assert.Equal(SimulatorState.Ready, session.State);
        Assert.Equal(OperationModes.Remote, (await fixture.GetModeAsync(transport)).Mode);
        Assert.True((await fixture.SetModeAsync(transport, OperationModes.Remote)).Accepted);
        Assert.Single(changes);
        Assert.Equal(CommandErrorCodes.OperationNotAllowed, (await session.StartFromSourceAsync(new SystemStartRequest(), OperationSource.Local)).ErrorCode);
        Assert.Equal(CommandErrorCodes.InvalidOperationSource, (await session.StartFromSourceAsync(new SystemStartRequest(), (OperationSource)123)).ErrorCode);
        Assert.True((await fixture.StartAsync(transport)).Accepted);
        Assert.Equal(CommandErrorCodes.OperationNotAllowed, (await session.StopFromSourceAsync(new SystemStopRequest(), OperationSource.Local)).ErrorCode);
        Assert.True((await session.StopAsync()).Accepted);
        Assert.True((await fixture.SetModeAsync(transport, OperationModes.Local)).Accepted);
        Assert.True((await session.StartFromSourceAsync(new SystemStartRequest { RunMode = ControlRunModes.Continue }, OperationSource.Local)).Accepted);
        Assert.Equal(CommandErrorCodes.OperationNotAllowed, (await session.StopAsync()).ErrorCode);
        Assert.True((await session.StopFromSourceAsync(new SystemStopRequest(), OperationSource.Local)).Accepted);
        Assert.True((await session.DeinitializeFromSourceAsync(OperationSource.Local)).Accepted);
        Assert.Equal(OperationModes.Local, session.OperationMode.Mode);
        Assert.Equal(2, changes.Count);
    }

    [Theory]
    [InlineData("rest")]
    [InlineData("tcp")]
    [InlineData("mqtt")]
    public async Task DefaultOptOutKeepsOldCommandsAndPrecedingCards(string transport)
    {
        await using var fixture = await PublicTransportFixture.CreateAsync(false);
        Assert.False((await fixture.GetModeAsync(transport)).ManagementEnabled);
        Assert.True((await fixture.Session.InitializeAsync()).Accepted);
        Assert.NotEmpty((await fixture.Client.GetRecipesAsync()).Items);
        Assert.Equal(fixture.Session.ProductInfo.RecipeOr, (await fixture.Client.GetCurrentRecipeAsync()).Recipe);
        Assert.True((await fixture.StartAsync(transport)).Accepted);
        Assert.True((await fixture.Session.RunCompletedAsync()).Accepted);
        var result = Assert.Single(fixture.Session.Results);
        Assert.Equal(result.ResultId, (await fixture.Client.GetResultDetailAsync(result.ResultId)).ResultId);
        Assert.True((await fixture.SetModeAsync(transport, OperationModes.Remote)).Accepted);
        Assert.True((await fixture.Client.StartAsync(new SystemStartRequest { InspectionMode = InspectionModes.CaptureOnly })).Accepted);
        await fixture.Session.RunCompletedAsync();
        Assert.Single(fixture.Session.Results);
        Assert.True((await fixture.StartAsync(transport)).Accepted);
        Assert.True((await fixture.Session.StopAsync()).Accepted);
        Assert.True((await fixture.Session.DeinitializeAsync()).Accepted);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("1")]
    [InlineData("{}")]
    [InlineData("[]")]
    public async Task WrongModeTypesAndSpoofedSourceAreRejected(string value)
    {
        await using var fixture = await PublicTransportFixture.CreateAsync(true);
        using var invalid = await fixture.Http.PostAsync(RestRoutes.ApiOperationMode, new StringContent("{\"mode\":" + value + "}", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.False(TcpSocketMessageParser.TryParse("{\"type\":\"setOperationMode\",\"mode\":" + value + "}", out _, out _));
        Assert.Throws<JsonException>(() => CommandPayloadJson.ReadObject<MqttCommandRequest>("{\"mode\":" + value + "}"));
        using var spoofed = await fixture.Http.PostAsync(RestRoutes.ApiSystemStart, new StringContent("{\"source\":\"local\"}", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, spoofed.StatusCode);
        Assert.False(TcpSocketMessageParser.TryParse("{\"type\":\"start\",\"operationSource\":\"local\"}", out _, out _));
        Assert.Throws<JsonException>(() => CommandPayloadJson.ReadObject<MqttCommandRequest>("{\"source\":\"local\"}"));
        Assert.Equal(CommandErrorCodes.InvalidPayload, (await fixture.RawTcpAsync("{\"type\":\"setOperationMode\",\"mode\":" + value + "}")).ErrorCode);
        Assert.Equal(CommandErrorCodes.InvalidPayload, (await fixture.RawMqttAsync("{\"mode\":" + value + "}")).ErrorCode);
        Assert.Equal(CommandErrorCodes.InvalidPayload, (await fixture.RawTcpAsync("{\"type\":\"start\",\"source\":\"local\"}")).ErrorCode);
        Assert.Equal(CommandErrorCodes.InvalidPayload, (await fixture.RawMqttAsync("{\"source\":\"local\"}")).ErrorCode);
        Assert.Equal(OperationModes.Local, fixture.Session.OperationMode.Mode);
    }

    [Fact]
    public void ModeEventsRoundTripSeparatelyFromLifecycle()
    {
        var json = TcpSocketEventFormatter.FormatOperationMode(new OperationModeInfo { Mode = OperationModes.Remote, ManagementEnabled = true });
        Assert.True(VirexEventParser.TryParse(json, out var value, out _));
        Assert.Null(value.Status);
        Assert.Equal(OperationModes.Remote, value.OperationMode!.Mode);
        Assert.True(value.OperationMode.ManagementEnabled);
        Assert.True(VirexEventParser.TryParse(TcpSocketEventFormatter.FormatStatus(new SystemStatus()), out var legacy, out _));
        Assert.Null(legacy.OperationMode);
    }

    [Theory]
    [InlineData("tcp")]
    [InlineData("mqtt")]
    public async Task SdkObservesLiveAppliedModeEvent(string transport)
    {
        await using var fixture = await PublicTransportFixture.CreateAsync(true);
        var received = new TaskCompletionSource<OperationModeInfo>(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnEvent(object? sender, VirexEvent value)
        {
            if (value.OperationMode?.Mode == OperationModes.Remote) received.TrySetResult(value.OperationMode);
        }
        fixture.Client.TcpEvents.EventReceived += OnEvent;
        fixture.Client.MqttEvents.EventReceived += OnEvent;
        using var stop = new CancellationTokenSource();
        var watcher = transport == "tcp" ? fixture.Client.TcpEvents.RunAsync(stop.Token) : fixture.Client.MqttEvents.RunAsync(stop.Token);
        try
        {
            // A subscriber has no replay. Toggle until its connection is subscribed.
            for (var attempt = 0; attempt < 30 && !received.Task.IsCompleted; attempt++)
            {
                await fixture.Client.SetOperationModeAsync(OperationModes.Local);
                await fixture.Client.SetOperationModeAsync(OperationModes.Remote);
                await Task.WhenAny(received.Task, Task.Delay(100));
            }
            Assert.True((await received.Task.WaitAsync(TimeSpan.FromSeconds(3))).ManagementEnabled);
        }
        finally
        {
            stop.Cancel();
            try { await watcher; } catch (OperationCanceledException) { }
        }
    }
}

internal sealed class PublicTransportFixture : IAsyncDisposable
{
    private readonly RestSimulatorServer _rest;
    private readonly TcpSimulatorServer _tcp;
    private readonly EmbeddedMqttBroker _broker;
    private readonly MqttSimulatorPublisher _publisher;

    private PublicTransportFixture(bool managed)
    {
        Root = Path.Combine(Path.GetTempPath(), "virex-public-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
        Session = new SimulatorSession(Root, managed);
        var options = new VirexClientOptions { RestBaseUrl = "http://127.0.0.1:" + FreePort() + "/", TcpPort = FreePort(), MqttPort = FreePort(), MqttTopic = "test-" + Guid.NewGuid().ToString("N") };
        Client = new VirexClient(options);
        Http = new HttpClient { BaseAddress = new Uri(options.RestBaseUrl) };
        _rest = new RestSimulatorServer(Session, options.RestBaseUrl);
        _tcp = new TcpSimulatorServer(Session, options.TcpPort);
        _broker = new EmbeddedMqttBroker(options.MqttPort);
        _publisher = new MqttSimulatorPublisher(Session, "127.0.0.1", options.MqttPort, options.MqttTopic);
    }

    public string Root { get; }
    public SimulatorSession Session { get; }
    public VirexClient Client { get; }
    public HttpClient Http { get; }

    public static async Task<PublicTransportFixture> CreateAsync(bool managed)
    {
        var fixture = new PublicTransportFixture(managed);
        await fixture._rest.StartAsync();
        await fixture._tcp.StartAsync();
        await fixture._broker.StartAsync();
        await fixture._publisher.StartAsync();
        return fixture;
    }

    public Task<OperationModeInfo> GetModeAsync(string transport) => transport switch
    {
        "tcp" => Client.TcpEvents.GetOperationModeAsync(),
        "mqtt" => Client.MqttCommands.GetOperationModeAsync(),
        _ => Client.GetOperationModeAsync(),
    };

    public async Task<CommandResponse> SetModeAsync(string transport, string mode)
    {
        try
        {
            return transport switch
            {
                "tcp" => await Client.TcpEvents.SetOperationModeAsync(mode),
                "mqtt" => await Client.MqttCommands.SetOperationModeAsync(mode),
                _ => await Client.SetOperationModeAsync(mode),
            };
        }
        catch (VirexClientException ex) { return ProtocolJson.Deserialize<CommandResponse>(ex.ResponseBody)!; }
        catch (VirexCommandException ex) { return new CommandResponse { Accepted = false, ErrorCode = ex.ErrorCode }; }
    }

    public async Task<CommandResponse> StartAsync(string transport)
    {
        try
        {
            if (transport == "rest") return await Client.StartAsync(new SystemStartRequest { RunMode = ControlRunModes.Continue });
            if (transport == "mqtt") return await Client.MqttCommands.StartAsync(runMode: ControlRunModes.Continue);
            using var socket = new TcpClient();
            await socket.ConnectAsync(IPAddress.Loopback, Client.Options.TcpPort);
            using var reader = new StreamReader(socket.GetStream());
            using var writer = new StreamWriter(socket.GetStream(), new UTF8Encoding(false)) { AutoFlush = true };
            await writer.WriteAsync(TcpSocketEventFormatter.FormatStartCommand(null, ControlRunModes.Continue));
            while (true)
            {
                var json = (await reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(5)))!;
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.GetProperty("type").GetString() == "commandRejected") return ProtocolJson.Deserialize<CommandResponse>(json)!;
                if (doc.RootElement.GetProperty("type").GetString() == "runStarted") return new CommandResponse { Accepted = true };
            }
        }
        catch (VirexClientException ex) { return ProtocolJson.Deserialize<CommandResponse>(ex.ResponseBody)!; }
    }

    public async Task<CommandResponse> RawTcpAsync(string json)
    {
        using var socket = new TcpClient();
        await socket.ConnectAsync(IPAddress.Loopback, Client.Options.TcpPort);
        using var reader = new StreamReader(socket.GetStream());
        using var writer = new StreamWriter(socket.GetStream(), new UTF8Encoding(false)) { AutoFlush = true };
        await writer.WriteLineAsync(json);
        while (true)
        {
            var frame = (await reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(5)))!;
            using var doc = JsonDocument.Parse(frame);
            if (doc.RootElement.GetProperty("type").GetString() is "commandRejected" or "commandResponse")
                return ProtocolJson.Deserialize<CommandResponse>(frame)!;
        }
    }

    public async Task<CommandResponse> RawMqttAsync(string json)
    {
        var factory = new MqttFactory();
        using var client = factory.CreateMqttClient();
        var id = Guid.NewGuid().ToString("N");
        var received = new TaskCompletionSource<CommandResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.ApplicationMessageReceivedAsync += e =>
        {
            var envelope = ProtocolJson.Deserialize<MqttCommandResponse>(Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment.ToArray()))!;
            received.TrySetResult(envelope.CommandResponse!);
            return Task.CompletedTask;
        };
        await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", Client.Options.MqttPort).Build());
        await client.SubscribeAsync(factory.CreateSubscribeOptionsBuilder().WithTopicFilter(f => f.WithTopic(MqttTopics.ResponseTopic(Client.Options.MqttTopic, id))).Build());
        json = "{\"correlationId\":\"" + id + "\"," + json.Substring(1);
        await client.PublishAsync(new MqttApplicationMessageBuilder().WithTopic(MqttTopics.Combine(Client.Options.MqttTopic, MqttTopics.CommandOperationModeSet)).WithPayload(json).Build());
        return await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    public async ValueTask DisposeAsync()
    {
        if (Session.State == SimulatorState.Running)
            await Session.StopFromSourceAsync(new SystemStopRequest(), Session.OperationMode.Mode == OperationModes.Local ? OperationSource.Local : OperationSource.External);
        await _publisher.StopAsync();
        await _broker.DisposeAsync();
        await _tcp.StopAsync();
        await _rest.StopAsync();
        Http.Dispose();
        Client.Dispose();
        Directory.Delete(Root, true);
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
