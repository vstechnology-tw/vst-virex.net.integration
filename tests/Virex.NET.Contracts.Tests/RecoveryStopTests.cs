using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;
using Virex.NET.Client;
using Virex.NET.Simulator.Core;
using Virex.NET.Simulator.WPF.Services;

namespace Virex.NET.Contracts.Tests;

public sealed class RecoveryStopTests
{
    [Theory]
    [InlineData(SystemStates.Running)]
    [InlineData(SystemStates.Initializing)]
    [InlineData(SystemStates.UpdatingProductInfo)]
    [InlineData(SystemStates.Deinitializing)]
    public async Task SdkRecoveryProofRejectsWorkOrCleanupStillInProgress(string state)
    {
        using var handler = new ReplyHandler(RecoveryProof(state));
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1/") };
        var client = new VirexRestClient(http);
        await Assert.ThrowsAsync<JsonException>(() => client.StopAsync(new SystemStopRequest { RequireReset = true, ExpectedAppId = "A", ExpectedInstanceId = "I" }));
    }

    [Theory]
    [InlineData(SystemStates.Ready)]
    [InlineData(SystemStates.Uninitialized)]
    [InlineData(SystemStates.Faulted)]
    public async Task SdkRecoveryProofAcceptsOnlyQuiescentResultStates(string state)
    {
        using var handler = new ReplyHandler(RecoveryProof(state));
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1/") };
        var client = new VirexRestClient(http);
        var response = await client.StopAsync(new SystemStopRequest { RequireReset = true, ExpectedAppId = "A", ExpectedInstanceId = "I" });
        Assert.True(response.RecoveryStopApplied);
        Assert.Equal(state, response.State);
    }

    private static string RecoveryProof(string state) => "{\"accepted\":true,\"recoveryStopApplied\":true,\"command\":\"Stop\",\"appId\":\"A\",\"instanceId\":\"I\",\"recoveryAction\":\"Reset\",\"state\":\"" + state + "\"}";

    [Theory]
    [InlineData("{\"accepted\":true,\"command\":\"Stop\",\"state\":\"Ready\"}")]
    [InlineData("{\"accepted\":true,\"recoveryStopApplied\":false,\"command\":\"Stop\",\"appId\":\"A\",\"instanceId\":\"I\",\"recoveryAction\":\"Reset\",\"state\":\"Faulted\"}")]
    [InlineData("{\"accepted\":true,\"recoveryStopApplied\":true,\"command\":\"Stop\",\"appId\":\"A\",\"instanceId\":\"old-I\",\"recoveryAction\":\"Reset\",\"state\":\"Faulted\"}")]
    [InlineData("{\"accepted\":true,\"recoveryStopApplied\":true,\"command\":\"Stop\",\"appId\":\"A\",\"instanceId\":\"I\",\"recoveryAction\":\"Deinitialize\",\"state\":\"Faulted\"}")]
    public async Task SdkRecoveryRequiresCompleteAppliedIdentityAndResetProof(string json)
    {
        using var handler = new ReplyHandler(json);
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1/") };
        var client = new VirexRestClient(http);
        await Assert.ThrowsAsync<JsonException>(() => client.StopAsync(new SystemStopRequest { RequireReset = true, ExpectedAppId = "A", ExpectedInstanceId = "I" }));
    }

    [Fact]
    public async Task SdkRecoveryReplyCannotForgeAppliedProofWithDuplicateCaseAliases()
    {
        using var handler = new ReplyHandler("{\"accepted\":false,\"Accepted\":true,\"recoveryStopApplied\":true,\"command\":\"Stop\",\"appId\":\"A\",\"instanceId\":\"I\",\"recoveryAction\":\"Reset\",\"state\":\"Faulted\"}");
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1/") };
        var client = new VirexRestClient(http);
        await Assert.ThrowsAsync<JsonException>(() => client.StopAsync(new SystemStopRequest { RequireReset = true, ExpectedAppId = "A", ExpectedInstanceId = "I" }));
    }

    private sealed class ReplyHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
    }

    [Fact]
    public void WholeWorkDtoPreservesExactIdentityTimestampAndOlderRepliesNeverProveRecovery()
    {
        const string json = "{\"requestId\":\" R \u0022,\"jobId\":\"Job-A\",\"timestamp\":\"2026-10-10T02:00:00+00:00\"}";
        var completed = ProtocolJson.Deserialize<WorkCompletedInfo>(json)!;
        Assert.Equal(" R ", completed.RequestId);
        Assert.Equal("Job-A", completed.JobId);
        Assert.Equal(new DateTimeOffset(2026, 10, 10, 2, 0, 0, TimeSpan.Zero), completed.Timestamp);
        using var roundTrip = JsonDocument.Parse(ProtocolJson.Serialize(completed));
        Assert.Equal(3, roundTrip.RootElement.EnumerateObject().Count());
        Assert.Equal(" R ", roundTrip.RootElement.GetProperty("requestId").GetString());
        Assert.False(ProtocolJson.Deserialize<SystemStatus>("{\"state\":\"Ready\"}")!.RecoveryStopSupported);
        Assert.False(ProtocolJson.Deserialize<CommandResponse>("{\"accepted\":true,\"command\":\"Stop\",\"state\":\"Ready\"}")!.RecoveryStopApplied);
        Assert.DoesNotContain("workCompleted", new SimulatorSession().GetEventCatalog().Names);
    }

    [Theory]
    [InlineData("{\"type\":\"stop\",\"requireReset\":false}")]
    [InlineData("{\"type\":\"stop\",\"CancelStartRequestIds\":[]}")]
    [InlineData("{\"type\":\"stop\",\"expectedAppId\":null}")]
    [InlineData("{\"type\":\"stop\",\"reason\":true}")]
    [InlineData("{\"type\":\"stop\",\"Reason\":true}")]
    [InlineData("{\"type\":\"stop\",\"reason\":\"a\",\"Reason\":\"b\"}")]
    public void TcpStopStrictlyRejectsRecoveryOrMalformedFields(string frame)
    {
        Assert.False(TcpSocketMessageParser.TryParse(frame, out _, out _));
        Assert.True(TcpSocketMessageParser.TryParse(TcpSocketEventFormatter.FormatStopCommand("ordinary"), out var ordinary, out _));
        Assert.Equal("ordinary", ordinary.Reason);
    }

    [Fact]
    public async Task TcpUnsupportedRecoveryIsTypedRejectionWithoutOrdinaryStopEffects()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.Client.InitializeAsync();
        await fixture.Client.StartAsync(new SystemStartRequest { RequestId = "tcp-run", RunMode = ControlRunModes.Continue });
        var port = FreePort();
        var server = new TcpSimulatorServer(fixture.Session, port);
        await server.StartAsync();
        try
        {
            using var socket = new TcpClient();
            await socket.ConnectAsync(IPAddress.Loopback, port);
            using var reader = new StreamReader(socket.GetStream(), Encoding.UTF8, leaveOpen: true);
            using var writer = new StreamWriter(socket.GetStream(), new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
            await writer.WriteLineAsync("{\"type\":\"stop\",\"requestId\":\"tcp-scope\",\"requireReset\":false}");
            while (true)
            {
                using var frame = JsonDocument.Parse((await reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(5)))!);
                if (frame.RootElement.GetProperty("type").GetString() != "commandRejected") continue;
                Assert.Equal(CommandErrorCodes.InvalidPayload, frame.RootElement.GetProperty("errorCode").GetString());
                Assert.Equal("tcp-scope", frame.RootElement.GetProperty("requestId").GetString());
                break;
            }
            Assert.Equal(SystemStates.Running, (await fixture.Client.GetStatusAsync()).State);
            await fixture.Client.StopAsync();
        }
        finally { await server.StopAsync(); }
    }

    [Fact]
    public async Task MqttUnsupportedRecoveryIsCorrelatedTypedRejectionAndSdkOrdinaryStopStillWorks()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.Client.InitializeAsync();
        await fixture.Client.StartAsync(new SystemStartRequest { RequestId = "mqtt-run", RunMode = ControlRunModes.Continue });
        var port = FreePort();
        await using var broker = new EmbeddedMqttBroker(port);
        await broker.StartAsync();
        var topic = "recovery-" + Guid.NewGuid().ToString("N");
        var publisher = new MqttSimulatorPublisher(fixture.Session, "127.0.0.1", port, topic);
        await publisher.StartAsync();
        try
        {
            var factory = new MqttFactory();
            using var socket = factory.CreateMqttClient();
            var received = new TaskCompletionSource<MqttCommandResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            socket.ApplicationMessageReceivedAsync += frame =>
            {
                if (frame.ApplicationMessage.Topic == MqttTopics.ResponseTopic(topic, "reject-recovery"))
                    received.TrySetResult(ProtocolJson.Deserialize<MqttCommandResponse>(Encoding.UTF8.GetString(frame.ApplicationMessage.PayloadSegment.ToArray()))!);
                return Task.CompletedTask;
            };
            await socket.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", port).Build());
            await socket.SubscribeAsync(factory.CreateSubscribeOptionsBuilder().WithTopicFilter(f => f.WithTopic(MqttTopics.ResponseTopic(topic, "reject-recovery"))).Build());
            await socket.PublishAsync(new MqttApplicationMessageBuilder().WithTopic(MqttTopics.Combine(topic, MqttTopics.CommandSystemStop))
                .WithPayload("{\"correlationId\":\"reject-recovery\",\"expectedInstanceId\":null}").Build());
            var rejection = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.False(rejection.Accepted);
            Assert.Equal(CommandErrorCodes.InvalidPayload, rejection.CommandResponse!.ErrorCode);
            Assert.Equal(SystemStates.Running, (await fixture.Client.GetStatusAsync()).State);
            var sdk = new VirexMqttCommandClient(new VirexClientOptions { MqttPort = port, MqttTopic = topic });
            Assert.True((await sdk.StopAsync("ordinary regression")).Accepted);
            Assert.Null((await fixture.Client.GetStatusAsync()).RecoveryAction);
            await socket.DisconnectAsync();
        }
        finally { await publisher.StopAsync(); }
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    [Fact]
    public async Task ExplicitResetPublishesReleasedBarrierAndJoiningDeinitializeCountsAsExplicitReset()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.Client.InitializeAsync();
        await fixture.Client.StopAsync(Recovery(fixture));
        var reset = await fixture.Client.ResetAsync();
        Assert.Null(reset.RecoveryAction);
        await fixture.Client.InitializeAsync();
        await fixture.Client.StopAsync(Recovery(fixture));
        var deinitializing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Session.StatusChanged += (_, state) => { if (state.State == SystemStates.Deinitializing) deinitializing.TrySetResult(); };
        var deinitialize = fixture.Client.DeinitializeAsync();
        await deinitializing.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(SystemStates.Uninitialized, (await fixture.Client.ResetAsync()).State);
        await deinitialize;
        Assert.True((await fixture.Client.InitializeAsync()).Accepted);
    }

    [Fact]
    public void CapabilityDuplicateCaseAliasCannotForgeRecoverySupport()
    {
        using var document = JsonDocument.Parse("{\"state\":\"Ready\",\"recoveryStopSupported\":false,\"RecoveryStopSupported\":true}");
        Assert.Throws<JsonException>(() => QueryPayloadJson.ReadSystemStatus(document.RootElement));
    }

    [Fact]
    public async Task NewerRecoveryDuringHeldResetJoinCannotBeClearedByOlderReset()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.Client.InitializeAsync();
        // A controlled synthetic owned task makes this exact join schedule deterministic.
        // Reflect the existing test seam; do not add a public product timing hook.
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var gate = (SemaphoreSlim)typeof(SimulatorSession).GetField("_gate", flags)!.GetValue(fixture.Session)!;
        var runs = (List<Task>)typeof(SimulatorSession).GetField("_runTasks", flags)!.GetValue(fixture.Session)!;
        var heldOwner = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await gate.WaitAsync();
        try { runs.Add(heldOwner.Task); }
        finally { gate.Release(); }
        var oldResetEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var newerRecoveryEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Session.StatusChanged += (_, status) =>
        {
            if (status.State == SystemStates.Deinitializing) oldResetEntered.TrySetResult();
            if (status.State == SystemStates.Faulted) newerRecoveryEntered.TrySetResult();
        };
        var oldReset = fixture.Client.ResetAsync();
        try
        {
            await oldResetEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.False(oldReset.IsCompleted);
            var newerRecovery = fixture.Client.StopAsync(Recovery(fixture, ["newer-revoked"]));
            await newerRecoveryEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.False(oldReset.IsCompleted);
            Assert.False(newerRecovery.IsCompleted);
            Assert.Equal(RecoveryActions.Reset, fixture.Session.Status.RecoveryAction);
            heldOwner.TrySetResult();
            var rejected = await Assert.ThrowsAsync<VirexClientException>(() => oldReset.WaitAsync(TimeSpan.FromSeconds(5)));
            Assert.Equal(CommandErrorCodes.RequiresReset, ProtocolJson.Deserialize<CommandResponse>(rejected.ResponseBody)!.ErrorCode);
            Assert.True((await newerRecovery.WaitAsync(TimeSpan.FromSeconds(5))).RecoveryStopApplied);
            Assert.Equal(SystemStates.Faulted, (await fixture.Client.GetStatusAsync()).State);
            Assert.Equal(RecoveryActions.Reset, (await fixture.Client.GetStatusAsync()).RecoveryAction);
            await Assert.ThrowsAsync<VirexClientException>(() => fixture.Client.InitializeAsync());
            Assert.Equal(SystemStates.Uninitialized, (await fixture.Client.ResetAsync()).State);
            await fixture.Client.InitializeAsync();
            await Assert.ThrowsAsync<VirexClientException>(() => fixture.Client.StartAsync(new SystemStartRequest { RequestId = "newer-revoked" }));
            Assert.True((await fixture.Client.StartAsync(new SystemStartRequest { RequestId = "fresh-after-newer-reset", RunMode = ControlRunModes.Continue })).Accepted);
        }
        finally { heldOwner.TrySetResult(); }
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("{\"requireReset\":null}")]
    [InlineData("{\"requireReset\":\"true\"}")]
    [InlineData("{\"requireReset\":true,\"RequireReset\":false}")]
    [InlineData("{\"reason\":\"a\",\"Reason\":\"b\"}")]
    [InlineData("{\"reason\":15}")]
    [InlineData("{\"source\":\"local\"}")]
    [InlineData("{\"OperationSource\":\"local\"}")]
    [InlineData("{\"expectedAppId\":null}")]
    [InlineData("{\"cancelStartRequestIds\":[\"R\"]}")]
    [InlineData("{\"cancelStartRequestIds\":null}")]
    [InlineData("{\"cancelStartRequestIds\":[null]}")]
    [InlineData("{\"requireReset\":true}")]
    [InlineData("{\"requireReset\":true,\"expectedAppId\":\"A\",\"expectedInstanceId\":\"I\",\"cancelStartRequestIds\":[\"R\",\"R\"]}")]
    [InlineData("{\"requireReset\":true,\"expectedAppId\":\"A\",\"expectedInstanceId\":\"I\",\"cancelStartRequestIds\":[\"R1\",\"R2\",\"R3\"]}")]
    [InlineData("{\"requireReset\":true,\"expectedAppId\":\"A\",\"expectedInstanceId\":\"I\",\"cancelStartRequestIds\":[\" \" ]}")]
    public async Task MalformedStopIsTyped400BeforeAnyEffect(string json)
    {
        await using var fixture = await Fixture.CreateAsync();
        using var http = new HttpClient { BaseAddress = new Uri(fixture.Prefix) };
        using var reply = await http.PostAsync(RestRoutes.ApiSystemStop, new StringContent(json, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, reply.StatusCode);
        var failure = ProtocolJson.Deserialize<CommandResponse>(await reply.Content.ReadAsStringAsync())!;
        Assert.Equal(CommandErrorCodes.InvalidPayload, failure.ErrorCode);
        Assert.False(failure.RecoveryStopApplied);
        Assert.Equal(SystemStates.Uninitialized, (await fixture.Client.GetStatusAsync()).State);
        Assert.Null((await fixture.Client.GetStatusAsync()).RecoveryAction);
    }

    [Fact]
    public async Task SdkRejectsMalformedTypedStopBeforeSendingAndCopiesExactIds()
    {
        await using var fixture = await Fixture.CreateAsync();
        var invalid = new[]
        {
            new SystemStopRequest { RequireReset = true },
            new SystemStopRequest { ExpectedAppId = "A" },
            new SystemStopRequest { CancelStartRequestIds = ["R"] },
            new SystemStopRequest { CancelStartRequestIds = null! },
            Recovery(fixture, [null!]), Recovery(fixture, [""]), Recovery(fixture, [" "]),
            Recovery(fixture, ["R", "R"]), Recovery(fixture, ["1", "2", "3"]),
        };
        foreach (var request in invalid) Assert.Throws<ArgumentException>(() => { _ = fixture.Client.StopAsync(request); });
        Assert.Equal(SystemStates.Uninitialized, (await fixture.Client.GetStatusAsync()).State);
        var valid = Recovery(fixture, ["R", "r"]);
        var snapshot = StopPayloadJson.ValidateAndSnapshot(valid);
        valid.CancelStartRequestIds[0] = "mutated";
        Assert.Equal("R", snapshot.CancelStartRequestIds[0]);
        Assert.True((await fixture.Client.StopAsync(snapshot)).RecoveryStopApplied);
    }

    [Fact]
    public async Task IdentityMismatchCannotLatchOrRevokeAndLocalExternalAuthorityStillApplies()
    {
        await using var fixture = await Fixture.CreateAsync(managed: true);
        var request = Recovery(fixture, ["keep-R"]);
        var localDenied = await Assert.ThrowsAsync<VirexClientException>(() => fixture.Client.StopAsync(request));
        Assert.Equal(CommandErrorCodes.OperationNotAllowed, ProtocolJson.Deserialize<CommandResponse>(localDenied.ResponseBody)!.ErrorCode);
        await fixture.Client.SetOperationModeAsync(OperationModes.Remote);
        request.ExpectedInstanceId = "previous-instance";
        var mismatch = await Assert.ThrowsAsync<VirexClientException>(() => fixture.Client.StopAsync(request));
        Assert.Equal(CommandErrorCodes.OperationNotAllowed, ProtocolJson.Deserialize<CommandResponse>(mismatch.ResponseBody)!.ErrorCode);
        Assert.Null((await fixture.Client.GetStatusAsync()).RecoveryAction);
        await fixture.Client.InitializeAsync();
        Assert.True((await fixture.Client.StartAsync(new SystemStartRequest { RequestId = "keep-R", RunMode = ControlRunModes.Continue })).Accepted);
        await fixture.Client.StopAsync();
    }

    [Fact]
    public async Task RecoveryCleanupFailureStaysLatchedAndTombstonesSurviveExplicitReset()
    {
        await using var fixture = await Fixture.CreateAsync();
        Assert.True((await fixture.Client.GetStatusAsync()).RecoveryStopSupported);
        await fixture.Client.InitializeAsync();
        await fixture.Client.StartAsync(new SystemStartRequest { RequestId = "active-R", RunMode = ControlRunModes.Continue });
        var captures = 0;
        fixture.Session.CaptureCompleted += (_, _) => Interlocked.Increment(ref captures);
        var replies = await Task.WhenAll(fixture.Client.StopAsync(Recovery(fixture, ["late-R"])), fixture.Client.StopAsync(Recovery(fixture, ["later-R"])));
        Assert.All(replies, reply => Assert.True(reply.RecoveryStopApplied));
        await Task.Delay(1100); // Cross the cancelled run's synthetic cadence: no callback survives joined stop.
        Assert.Equal(0, Volatile.Read(ref captures));
        fixture.Session.ConfigureDeinitializeFailures(1);
        var failure = await Assert.ThrowsAsync<VirexClientException>(() => fixture.Client.ResetAsync());
        Assert.Equal(CommandErrorCodes.CleanupFailed, ProtocolJson.Deserialize<CommandResponse>(failure.ResponseBody)!.ErrorCode);
        Assert.Equal(RecoveryActions.Reset, (await fixture.Client.GetStatusAsync()).RecoveryAction);
        await Assert.ThrowsAsync<VirexClientException>(() => fixture.Client.InitializeAsync());
        await fixture.Client.ResetAsync();
        await fixture.Client.InitializeAsync();
        foreach (var id in new[] { "late-R", "later-R", "active-R" })
            await Assert.ThrowsAsync<VirexClientException>(() => fixture.Client.StartAsync(new SystemStartRequest { RequestId = id }));
        Assert.True((await fixture.Client.StartAsync(new SystemStartRequest { RequestId = "fresh", RunMode = ControlRunModes.Continue })).Accepted);
        Assert.False((await fixture.Client.StopAsync("RequireReset=true is diagnostic only")).RecoveryStopApplied);
        Assert.Null((await fixture.Client.GetStatusAsync()).RecoveryAction);
    }

    [Fact]
    public async Task StartHeldAtHttpTransportCannotEnterAfterRecoveryResetInitial()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.Client.InitializeAsync();
        using var handler = new HeldStartHandler();
        using var http = new HttpClient(handler);
        using var pendingClient = new VirexClient(new VirexClientOptions { RestBaseUrl = fixture.Prefix, TimeoutMs = 15000 }, http);
        var start = pendingClient.StartAsync(new SystemStartRequest { RequestId = "in-flight", RunMode = ControlRunModes.Continue });
        await handler.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        try
        {
            Assert.True((await fixture.Client.StopAsync(Recovery(fixture, ["in-flight"]))).RecoveryStopApplied);
            await fixture.Client.ResetAsync();
            await fixture.Client.InitializeAsync();
            handler.Release.TrySetResult();
            var rejected = await Assert.ThrowsAsync<VirexClientException>(() => start);
            Assert.Equal(CommandErrorCodes.RequiresReset, ProtocolJson.Deserialize<CommandResponse>(rejected.ResponseBody)!.ErrorCode);
            Assert.Equal(SystemStates.Ready, (await fixture.Client.GetStatusAsync()).State);
            Assert.Empty(fixture.Session.Results);
            Assert.True((await fixture.Client.StartAsync(new SystemStartRequest { RequestId = "new-flight", RunMode = ControlRunModes.Continue })).Accepted);
        }
        finally { handler.Release.TrySetResult(); }
    }

    private static SystemStopRequest Recovery(Fixture fixture, string[]? ids = null) => new()
    {
        RequireReset = true,
        CancelStartRequestIds = ids ?? [],
        ExpectedAppId = fixture.Session.AppId,
        ExpectedInstanceId = fixture.Session.InstanceId,
    };

    private sealed class HeldStartHandler : DelegatingHandler
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public HeldStartHandler() : base(new HttpClientHandler()) { }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri!.AbsolutePath == RestRoutes.ApiSystemStart)
            {
                Entered.TrySetResult();
                await Release.Task.WaitAsync(cancellationToken);
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }
    [Fact]
    public async Task MisspelledRecoveryFieldCannotDowngradeIntoOrdinaryRunningStop()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.Client.InitializeAsync();
        await fixture.Client.StartAsync(new SystemStartRequest { RequestId = "running", RunMode = ControlRunModes.Continue });
        using var http = new HttpClient { BaseAddress = new Uri(fixture.Prefix) };
        using var reply = await http.PostAsync(RestRoutes.ApiSystemStop,
            new StringContent("{\"requiereReset\":true}", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, reply.StatusCode);
        Assert.Equal(CommandErrorCodes.InvalidPayload, ProtocolJson.Deserialize<CommandResponse>(await reply.Content.ReadAsStringAsync())!.ErrorCode);
        Assert.Equal(SystemStates.Running, (await fixture.Client.GetStatusAsync()).State);
    }

    [Fact]
    public async Task SdkRecoveryStopOnReadyRequiresResetAndRevokesLateStartAcrossResetInitial()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.Client.InitializeAsync();
        var request = ProtocolJson.Deserialize<SystemStopRequest>(
            "{\"requireReset\":true,\"cancelStartRequestIds\":[\"late-R\"],\"expectedAppId\":\"" + fixture.Session.AppId
            + "\",\"expectedInstanceId\":\"" + fixture.Session.InstanceId + "\"}")!;
        var stopped = await fixture.Client.StopAsync(request);
        Assert.True(stopped.Accepted);
        Assert.Equal(RecoveryActions.Reset, stopped.RecoveryAction);
        Assert.Equal(fixture.Session.AppId, stopped.AppId);
        Assert.Equal(fixture.Session.InstanceId, stopped.InstanceId);
        await Assert.ThrowsAsync<VirexClientException>(() => fixture.Client.StartAsync(new SystemStartRequest { RequestId = "other-before-reset" }));
        Assert.Equal(SystemStates.Uninitialized, (await fixture.Client.ResetAsync()).State);
        Assert.Equal(SystemStates.Ready, (await fixture.Client.InitializeAsync()).State);
        await Assert.ThrowsAsync<VirexClientException>(() => fixture.Client.StartAsync(new SystemStartRequest { RequestId = "late-R" }));
        Assert.True((await fixture.Client.StartAsync(new SystemStartRequest { RequestId = "fresh-R", RunMode = ControlRunModes.Continue })).Accepted);
        Assert.True((await fixture.Client.StopAsync()).Accepted);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly RestSimulatorServer _server;
        private readonly string _directory;
        public SimulatorSession Session { get; }
        public VirexClient Client { get; }
        public string Prefix { get; }
        private Fixture(bool managed)
        {
            _directory = Path.Combine(Path.GetTempPath(), "virex-recovery-" + Guid.NewGuid().ToString("N"));
            Session = new SimulatorSession(_directory, managed);
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            Prefix = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port + "/";
            listener.Stop();
            _server = new RestSimulatorServer(Session, Prefix);
            Client = new VirexClient(new VirexClientOptions { RestBaseUrl = Prefix });
        }
        public static async Task<Fixture> CreateAsync(bool managed = false)
        {
            var fixture = new Fixture(managed);
            await fixture._server.StartAsync();
            return fixture;
        }
        public async ValueTask DisposeAsync()
        {
            await Session.ShutdownAsync();
            await _server.StopAsync();
            Client.Dispose();
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }
    }
}
