using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using Virex.NET.Client;
using Virex.NET.Contracts;
using Virex.NET.Simulator.Core;
using Virex.NET.Simulator.WPF.Services;

namespace Virex.NET.Contracts.Tests;

public sealed class EventSubscriptionTests
{
    [Fact]
    public async Task CloseJoinsActiveAndQueuedUnsubscribeBeforeReleasingSynchronizationResources()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var commandRead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var server = Task.Run(async () =>
        {
            using var socket = await listener.AcceptTcpClientAsync();
            using var reader = new StreamReader(socket.GetStream());
            using var writer = new StreamWriter(socket.GetStream()) { AutoFlush = true };
            var request = ProtocolJson.Deserialize<EventSubscriptionRequest>((await reader.ReadLineAsync())!)!;
            await writer.WriteLineAsync(ProtocolJson.Serialize(new EventSubscriptionResponse { RequestId = request.RequestId, Accepted = true,
                AppId = "app", InstanceId = "instance", ConnectionGeneration = "generation", Names = ["extension.AnyName"] }));
            await reader.ReadLineAsync();
            commandRead.TrySetResult();
            Assert.Null(await reader.ReadLineAsync());
        });
        try
        {
            var client = new VirexTcpEventClient(new VirexClientOptions { TcpPort = ((IPEndPoint)listener.LocalEndpoint).Port });
            var subscription = await client.SubscribeAsync(["extension.AnyName"], _ => Assert.Fail("No event was sent."));
            var first = subscription.UnsubscribeAsync(["extension.AnyName"]);
            var second = subscription.UnsubscribeAsync(["extension.AnyName"]);
            await commandRead.Task.WaitAsync(TimeSpan.FromSeconds(3));
            var close = subscription.CloseAsync();
            await Assert.ThrowsAsync<OperationCanceledException>(() => first.WaitAsync(TimeSpan.FromSeconds(3)));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => second.WaitAsync(TimeSpan.FromSeconds(3)));
            await close.WaitAsync(TimeSpan.FromSeconds(3));
            await subscription.CloseAsync();
            await Assert.ThrowsAsync<ObjectDisposedException>(() => subscription.UnsubscribeAsync(["extension.AnyName"]));
        }
        finally { listener.Stop(); await server; }
    }

    [Fact]
    public async Task AppIdentityPreventsCrossAppSubscriptionAndRestartChangesInstance()
    {
        await using var first = await Fixture.CreateAsync();
        await using var second = await Fixture.CreateAsync();
        first.Session.ConfigureEvents("app-A", []);
        second.Session.ConfigureEvents("app-B", []);
        var before = await first.Client.GetEventCatalogAsync();
        await Assert.ThrowsAsync<InvalidDataException>(() => second.Client.TcpEvents.SubscribeAsync(["captureReady"], _ => Assert.Fail("Wrong App callback."),
            scope: new EventSubscriptionScope { AppId = before.AppId, InstanceId = before.InstanceId }));
        var restarted = new SimulatorSession();
        restarted.ConfigureEvents("app-A", []);
        Assert.Equal(before.AppId, restarted.GetEventCatalog().AppId);
        Assert.NotEqual(before.InstanceId, restarted.GetEventCatalog().InstanceId);
        await restarted.ShutdownAsync();
    }
    [Fact]
    public async Task DisposeInsideCallbackDoesNotHideConsumerIOException()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Session.ConfigureEvents("synthetic-app", ["custom.Alpha"]);
        VirexEventSubscription? subscription = null;
        var failure = new IOException("consumer callback failed after disposal");
        subscription = await fixture.Client.TcpEvents.SubscribeAsync(["custom.Alpha"], _ =>
        {
            subscription!.Dispose();
            throw failure;
        });
        fixture.Session.EmitEvent("custom.Alpha", "{}");
        Assert.Same(failure, await Assert.ThrowsAsync<IOException>(() => subscription.Completion.WaitAsync(TimeSpan.FromSeconds(3))));
        Assert.Same(failure, await Assert.ThrowsAsync<IOException>(subscription.CloseAsync));
    }
    [Fact]
    public async Task SdkReceivesOnlySelectedCompleteNamesAndUnsubscribeIsDeliveryBarrier()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Session.ConfigureEvents("synthetic-app", ["custom.Alpha", "custom.Beta", "custom.Alpha.more"]);
        var catalog = await fixture.Client.Rest.GetEventCatalogAsync();
        Assert.Equal("synthetic-app", catalog.AppId);
        Assert.Contains("custom.Alpha.more", catalog.Names);
        var observed = new ConcurrentQueue<EventEnvelope>();
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var subscription = await fixture.Client.TcpEvents.SubscribeAsync(["custom.Alpha", "custom.Beta"], value =>
        {
            observed.Enqueue(value); if (observed.Count == 2) received.TrySetResult();
        });
        try
        {
            fixture.Session.EmitEvent("custom.Alpha.more", "{}");
            fixture.Session.EmitEvent("custom.Alpha", "{\"value\":42}");
            fixture.Session.EmitEvent("custom.Beta", "{\"value\":43}");
            await received.Task.WaitAsync(TimeSpan.FromSeconds(3));
            Assert.Equal(new[] { "custom.Alpha", "custom.Beta" }, observed.Select(value => value.Name));
            Assert.All(observed, value => Assert.Equal(subscription.ConnectionGeneration, value.ConnectionGeneration));
            await subscription.UnsubscribeAsync(["custom.Alpha"]);
            fixture.Session.EmitEvent("custom.Alpha", "{}");
            fixture.Session.EmitEvent("custom.Beta", "{}");
            for (var attempt = 0; attempt < 30 && observed.Count < 3; attempt++) await Task.Delay(10);
            Assert.Equal(new[] { "custom.Alpha", "custom.Beta", "custom.Beta" }, observed.Select(value => value.Name));
            await subscription.UnsubscribeAsync(["custom.Beta"]);
            fixture.Session.EmitEvent("custom.Beta", "{}");
            await Task.Delay(100);
            Assert.Equal(3, observed.Count);
        }
        finally { await subscription.CloseAsync(); }
        fixture.Session.EmitEvent("custom.Beta", "{}");
        Assert.Equal(3, observed.Count);
    }

    [Theory]
    [InlineData("unknown", "unknown_event_name")]
    [InlineData("duplicate", "duplicate_event_subscription")]
    [InlineData("case", "unknown_event_name")]
    public async Task SdkReportsAtomicUnknownAndDuplicateSubscriptionRejection(string scenario, string code)
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Session.ConfigureEvents("synthetic-app", ["custom.Alpha"]);
        string[] names = scenario == "duplicate" ? ["custom.Alpha", "custom.Alpha"]
            : scenario == "case" ? ["custom.alpha"] : ["custom.Alpha", "missing"];
        var ex = await Assert.ThrowsAsync<VirexEventSubscriptionException>(() => fixture.Client.TcpEvents.SubscribeAsync(names, _ => Assert.Fail("Rejected subscription delivered a callback.")));
        Assert.Equal(code, ex.Response.ErrorCode);
        Assert.Empty(ex.Response.Names);
    }

    [Fact]
    public async Task RestAndTcpPreserveRequestJobCaptureAndResultAcrossRacingAcceptance()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.Client.InitializeAsync();
        var request = new SystemStartRequest { RequestId = "request-before-rest", RunMode = ControlRunModes.Continue };
        var observed = new ConcurrentQueue<EventEnvelope>();
        var complete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var subscription = await fixture.Client.TcpEvents.SubscribeAsync(["captureReady", "captureCompleted", "resultCreated"], value =>
        {
            observed.Enqueue(value); if (value.Name == "resultCreated") complete.TrySetResult();
        }, scope: new EventSubscriptionScope { RequestId = request.RequestId });
        try
        {
            var start = await fixture.Client.StartAsync(request);
            await fixture.Session.RunCompletedAsync();
            await complete.Task.WaitAsync(TimeSpan.FromSeconds(3));
            var events = observed.ToArray();
            Assert.Equal(new[] { "captureReady", "captureCompleted", "resultCreated" }, events.Select(value => value.Name));
            Assert.Equal(request.RequestId, start.RequestId);
            Assert.All(events, value =>
            {
                Assert.Equal(start.RequestId, value.RequestId);
                Assert.Equal(start.JobId, value.JobId);
                Assert.Equal(start.CaptureId, value.CaptureId);
                Assert.Equal(start.AppId, value.AppId);
                Assert.Equal(start.InstanceId, value.InstanceId);
            });
            Assert.Equal(events[2].ResultId, events[2].Payload.GetProperty("resultId").GetString());
            Assert.NotNull(events[2].ResultId);
        }
        finally { await subscription.CloseAsync(); }
    }

    [Fact]
    public async Task WorkScopeMakesLateEventsObservableAndCancellationStopsCallbacks()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Session.ConfigureEvents("synthetic-app", ["custom.Alpha"]);
        var events = new ConcurrentQueue<EventEnvelope>();
        var stale = new TaskCompletionSource<EventStreamIssue>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancel = new CancellationTokenSource();
        var subscription = await fixture.Client.TcpEvents.SubscribeAsync(["custom.Alpha"], events.Enqueue,
            issue => stale.TrySetResult(issue), new EventSubscriptionScope { JobId = "new-job" }, cancel.Token);
        fixture.Session.EmitEvent("custom.Alpha", "{}", jobId: "old-job");
        Assert.Equal("stale_event_work", (await stale.Task.WaitAsync(TimeSpan.FromSeconds(3))).Code);
        cancel.Cancel();
        await subscription.CloseAsync();
        fixture.Session.EmitEvent("custom.Alpha", "{}", jobId: "new-job");
        Assert.Empty(events);
        var next = await fixture.Client.TcpEvents.SubscribeAsync(["custom.Alpha"], _ => { });
        Assert.NotEqual(subscription.ConnectionGeneration, next.ConnectionGeneration);
        Assert.Equal(subscription.InstanceId, next.InstanceId);
        await next.CloseAsync();
    }

    [Fact]
    public async Task WireParserReportsMalformedDuplicateUnknownAndPreviousGenerationWithoutCallbacks()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var send = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var server = Task.Run(async () =>
        {
            using var socket = await listener.AcceptTcpClientAsync();
            using var reader = new StreamReader(socket.GetStream());
            using var writer = new StreamWriter(socket.GetStream()) { AutoFlush = true };
            var request = ProtocolJson.Deserialize<EventSubscriptionRequest>((await reader.ReadLineAsync())!)!;
            await writer.WriteLineAsync(ProtocolJson.Serialize(new EventSubscriptionResponse { RequestId = request.RequestId, Accepted = true,
                AppId = "app", InstanceId = "instance", ConnectionGeneration = "generation", Names = ["extension.AnyName"] }));
            await send.Task;
            var envelope = new EventEnvelope { Name = "extension.AnyName", AppId = "app", InstanceId = "instance", ConnectionGeneration = "generation",
                EventId = "event-1", Timestamp = DateTimeOffset.UtcNow, Payload = System.Text.Json.JsonDocument.Parse("{}").RootElement.Clone() };
            await writer.WriteLineAsync("{broken}");
            await writer.WriteLineAsync(ProtocolJson.Serialize(envelope));
            await writer.WriteLineAsync(ProtocolJson.Serialize(envelope));
            envelope.EventId = "event-2"; envelope.Name = "extension.Other";
            await writer.WriteLineAsync(ProtocolJson.Serialize(envelope));
            envelope.Name = "extension.AnyName"; envelope.ConnectionGeneration = "previous-generation";
            await writer.WriteLineAsync(ProtocolJson.Serialize(envelope));
            envelope.ConnectionGeneration = "generation"; envelope.Payload = default;
            await writer.WriteLineAsync("{\"type\":\"event\",\"name\":\"extension.AnyName\",\"payload\":[]}");
        });
        var observed = new ConcurrentQueue<EventEnvelope>();
        var issues = new ConcurrentQueue<EventStreamIssue>();
        try
        {
            var client = new VirexTcpEventClient(new VirexClientOptions { TcpPort = port });
            var subscription = await client.SubscribeAsync(["extension.AnyName"], observed.Enqueue, issues.Enqueue);
            send.TrySetResult();
            await subscription.Completion.WaitAsync(TimeSpan.FromSeconds(3));
            await subscription.CloseAsync();
            Assert.Single(observed);
            Assert.Equal(new[] { "invalid_event_payload", "duplicate_event", "unknown_event_name", "stale_event_generation", "invalid_event_payload" }, issues.Select(issue => issue.Code));
            Assert.Equal("invalid_event_payload", subscription.LastIssue!.Code);
        }
        finally { listener.Stop(); await server; }
    }

    [Fact]
    public async Task ReentrantDisposeAndConcurrentCancellationJoinTheRunningCallback()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Session.ConfigureEvents("synthetic-app", ["custom.Alpha"]);
        using var cancellation = new CancellationTokenSource();
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        VirexEventSubscription? subscription = null;
        var calls = 0;
        subscription = await fixture.Client.TcpEvents.SubscribeAsync(["custom.Alpha"], _ =>
        {
            Interlocked.Increment(ref calls);
            entered.TrySetResult();
            release.Wait(TimeSpan.FromSeconds(3));
            subscription!.Dispose();
        }, cancellationToken: cancellation.Token);
        fixture.Session.EmitEvent("custom.Alpha", "{}");
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(3));
        var cancel = Task.Run(cancellation.Cancel);
        release.Set();
        await cancel.WaitAsync(TimeSpan.FromSeconds(3));
        await subscription.CloseAsync().WaitAsync(TimeSpan.FromSeconds(3));
        fixture.Session.EmitEvent("custom.Alpha", "{}");
        Assert.Equal(1, calls);
    }
    [Fact]
    public async Task TcpAcknowledgesExactCustomNamesBeforeProducingEvents()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Session.ConfigureEvents("synthetic-app", ["custom.Alpha", "custom.Beta"]);
        using var socket = new TcpClient();
        await socket.ConnectAsync("127.0.0.1", fixture.Client.Options.TcpPort);
        using var reader = new StreamReader(socket.GetStream());
        using var writer = new StreamWriter(socket.GetStream()) { AutoFlush = true };
        await reader.ReadLineAsync(); await reader.ReadLineAsync(); // Legacy initial status/product frames.
        await writer.WriteLineAsync(ProtocolJson.Serialize(new EventSubscriptionRequest { RequestId = "sub-1", Names = ["custom.Alpha", "custom.Beta"] }));
        var reply = await reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Contains("eventSubscription", reply);
        fixture.Session.EmitEvent("custom.Alpha", "{\"value\":42}");
        var frame = await reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Contains("custom.Alpha", frame);
    }
    [Fact]
    public async Task AppPublishesItsEventCatalogThroughRest()
    {
        await using var fixture = await Fixture.CreateAsync();
        using var http = new HttpClient { BaseAddress = new Uri(fixture.Prefix) };
        using var response = await http.GetAsync("api/events");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("captureReady", await response.Content.ReadAsStringAsync());
        var openApi = await http.GetStringAsync("openapi/v1.json");
        using var document = System.Text.Json.JsonDocument.Parse(openApi);
        Assert.True(document.RootElement.GetProperty("paths").TryGetProperty("/api/events", out _));
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly RestSimulatorServer _rest;
        private readonly TcpSimulatorServer _tcp;
        public SimulatorSession Session { get; } = new SimulatorSession();
        public string Prefix { get; } = $"http://127.0.0.1:{FreePort()}/";
        public VirexClient Client { get; }
        private Fixture()
        {
            var port = FreePort();
            _rest = new RestSimulatorServer(Session, Prefix);
            _tcp = new TcpSimulatorServer(Session, port);
            Client = new VirexClient(new VirexClientOptions { RestBaseUrl = Prefix, TcpPort = port });
        }
        public static async Task<Fixture> CreateAsync()
        {
            var fixture = new Fixture();
            await fixture._rest.StartAsync();
            await fixture._tcp.StartAsync();
            return fixture;
        }
        public async ValueTask DisposeAsync()
        {
            await Session.ShutdownAsync();
            await _tcp.StopAsync();
            await _rest.StopAsync();
            Client.Dispose();
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
