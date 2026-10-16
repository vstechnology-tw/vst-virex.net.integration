using System.Net;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Virex.NET.Client;
using Virex.NET.Contracts;

namespace Virex.NET.Contracts.Tests;

public sealed class AlarmTests
{
    [Fact]
    public async Task DeinitializeJoiningResetHasItsOwnResponseAndTheSameCleanupFailure()
    {
        await using var fixture = await PublicTransportFixture.CreateAsync(false);
        await fixture.Client.InitializeAsync();
        fixture.Session.ConfigureDeinitializeFailures(1);
        var reset = fixture.Client.ResetAsync();
        await WaitForState(fixture.Client, SystemStates.Deinitializing);
        var deinitialize = fixture.Client.DeinitializeAsync();
        var a = await Assert.ThrowsAsync<VirexClientException>(() => reset);
        var b = await Assert.ThrowsAsync<VirexClientException>(() => deinitialize);
        Assert.Equal("Reset", ProtocolJson.Deserialize<CommandResponse>(a.ResponseBody)!.Command);
        Assert.Equal("Deinitialize", ProtocolJson.Deserialize<CommandResponse>(b.ResponseBody)!.Command);
        Assert.Equal("cleanup_failed", ProtocolJson.Deserialize<CommandResponse>(b.ResponseBody)!.ErrorCode);
        Assert.Equal(SystemStates.Faulted, (await fixture.Client.GetStatusAsync()).State);
        await fixture.Client.ResetAsync();
    }

    [Fact]
    public async Task ShutdownJoinsAdmittedResetAndPropagatesItsCleanupFailure()
    {
        await using var fixture = await PublicTransportFixture.CreateAsync(false);
        await fixture.Client.InitializeAsync();
        fixture.Session.ConfigureDeinitializeFailures(1, "synthetic reset cleanup failure");
        var reset = fixture.Client.ResetAsync();
        await WaitForState(fixture.Client, SystemStates.Deinitializing);
        var shutdown = fixture.Session.ShutdownAsync();
        await Assert.ThrowsAsync<VirexClientException>(() => reset);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => shutdown);
        Assert.Equal("synthetic reset cleanup failure", error.Message);
        Assert.Equal(SystemStates.Faulted, (await fixture.Client.GetStatusAsync()).State);
        Assert.False(await fixture.Session.RaiseAlarmAsync("absent", "late callback"));
    }

    [Fact]
    public async Task GenericEventInjectionCannotBypassAnAlarmProducerOrItsFaultLatch()
    {
        await using var fixture = await PublicTransportFixture.CreateAsync(false);
        await fixture.Session.ConfigureAlarmSourceAsync(new AlarmSourceDescriptor { Name = "custom.fault", Source = "synthetic", Category = "fault" });
        foreach (var source in (await fixture.Client.GetAlarmsAsync()).Sources)
            Assert.Throws<ArgumentException>(() => fixture.Session.EmitEvent(source.Name, "{}"));
        Assert.Equal(SystemStates.Uninitialized, (await fixture.Client.GetStatusAsync()).State);
        Assert.False((await fixture.Client.GetErrorAsync()).HasError);
    }

    [Fact]
    public async Task ResetJoiningAnExistingDeinitializeSharesItsFailureWithoutRetryingCleanup()
    {
        await using var fixture = await PublicTransportFixture.CreateAsync(false);
        await fixture.Client.InitializeAsync();
        fixture.Session.ConfigureDeinitializeFailures(1);
        var deinitialize = fixture.Client.DeinitializeAsync();
        await WaitForState(fixture.Client, SystemStates.Deinitializing);
        var reset = fixture.Client.ResetAsync();
        await Assert.ThrowsAsync<VirexClientException>(() => deinitialize);
        var failure = await Assert.ThrowsAsync<VirexClientException>(() => reset);
        var response = ProtocolJson.Deserialize<CommandResponse>(failure.ResponseBody)!;
        Assert.Equal("Reset", response.Command);
        Assert.Equal("cleanup_failed", response.ErrorCode);
        Assert.Equal(SystemStates.Faulted, response.State);
        Assert.Equal(SystemStates.Uninitialized, (await fixture.Client.ResetAsync()).State);
    }

    [Fact]
    public async Task DuplicateCatalogPropertiesCannotReplaceTheActualProducerListWithEmptySuccess()
    {
        const string json = "{\"appId\":\"A\",\"instanceId\":\"I\",\"timestamp\":\"2026-10-09T00:00:00Z\",\"sources\":[{\"name\":\"actual\",\"source\":\"module\",\"category\":\"fault\"}],\"sources\":[]}";
        using var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var server = Task.Run(async () =>
        {
            using var socket = await listener.AcceptTcpClientAsync();
            using var reader = new StreamReader(socket.GetStream(), Encoding.UTF8, leaveOpen: true);
            while (!string.IsNullOrEmpty(await reader.ReadLineAsync())) { }
            var bytes = Encoding.UTF8.GetBytes(json);
            await socket.GetStream().WriteAsync(Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: " + bytes.Length + "\r\nConnection: close\r\n\r\n"));
            await socket.GetStream().WriteAsync(bytes);
        });
        try
        {
            using var client = new VirexClient(new VirexClientOptions { RestBaseUrl = "http://127.0.0.1:" + port + "/" });
            var error = await Assert.ThrowsAsync<VirexQueryException>(() => client.GetAlarmsAsync());
            Assert.Equal("api_failed", error.ErrorCode);
        }
        finally { listener.Stop(); await server.WaitAsync(TimeSpan.FromSeconds(3)); }
    }

    [Theory]
    [InlineData(ControlRunModes.SingleRun)]
    [InlineData(ControlRunModes.Continue)]
    public async Task ActualPersistenceFailurePublishesItsSourceAndLatchesWithoutCommittingAResult(string mode)
    {
        var invalidRoot = Path.GetTempFileName();
        try
        {
            await using var fixture = await PublicTransportFixture.CreateAsync(false, invalidRoot);
            await fixture.Client.InitializeAsync();
            var received = new TaskCompletionSource<EventEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var subscription = await fixture.Client.TcpEvents.SubscribeAsync(["simulator.persistenceFailed"], e => received.TrySetResult(e));
            var start = await fixture.Client.StartAsync(new SystemStartRequest { RunMode = mode, RequestId = "persist-fault" });
            var alarm = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(SystemStates.Faulted, (await fixture.Client.GetStatusAsync()).State);
            Assert.Equal(start.RequestId, alarm.RequestId); Assert.Equal(start.JobId, alarm.JobId); Assert.Equal(start.CaptureId, alarm.CaptureId);
            Assert.Equal("artifactPersistence", alarm.Payload.GetProperty("source").GetString());
            Assert.Equal("fault", alarm.Payload.GetProperty("category").GetString());
            Assert.Empty((await fixture.Client.QueryResultsAsync()).Items);
            var current = await Assert.ThrowsAsync<VirexQueryException>(() => fixture.Client.GetCurrentResultAsync());
            Assert.Equal("no_current_result", current.ErrorCode);
            var uncommitted = await Assert.ThrowsAsync<VirexQueryException>(() => fixture.Client.ReadDataSourceAsync("result.findings", start.CaptureId));
            Assert.Equal("result_not_committed", uncommitted.ErrorCode);
            Assert.True((await fixture.Client.GetErrorAsync()).HasError);
            Assert.Equal(SystemStates.Uninitialized, (await fixture.Client.ResetAsync()).State);
        }
        finally { File.Delete(invalidRoot); }
    }

    [Fact]
    public async Task ActualFaultErrorSetAndClearAreExactlyOnceAndClosedSubscriptionGetsNoLateCallback()
    {
        await using var fixture = await PublicTransportFixture.CreateAsync(false);
        fixture.Session.ConfigureEvents("fault-errors", ["test.Barrier"]);
        await fixture.Client.InitializeAsync();
        var errors = new ConcurrentQueue<bool>();
        var occurrences = new ConcurrentQueue<EventEnvelope>();
        var drained = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var subscription = await fixture.Client.TcpEvents.SubscribeAsync(["errorChanged", "simulator.acquisitionFault", "test.Barrier"], e =>
        {
            if (e.Name == "test.Barrier") drained.TrySetResult();
            else if (e.Name == "errorChanged") errors.Enqueue(e.Payload.GetProperty("hasError").GetBoolean());
            else occurrences.Enqueue(e);
        });
        var start = await fixture.Client.StartAsync(new SystemStartRequest { RunMode = ControlRunModes.Continue, InspectionMode = InspectionModes.CaptureOnly });
        await fixture.Session.SimulateAcquisitionFaultAsync("source fault");
        await fixture.Client.ResetAsync();
        fixture.Session.EmitEvent("test.Barrier", "{}"); await drained.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Collection(errors, value => Assert.True(value), value => Assert.False(value));
        var alarm = Assert.Single(occurrences);
        Assert.Equal(start.JobId, alarm.JobId); Assert.Equal(start.CaptureId, alarm.CaptureId);
        await subscription.CloseAsync();
        await fixture.Client.InitializeAsync();
        await fixture.Session.SimulateAcquisitionFaultAsync("new generation occurrence");
        Assert.Single(occurrences);
        Assert.Collection(errors, value => Assert.True(value), value => Assert.False(value));
        await fixture.Client.ResetAsync();
    }
    [Fact]
    public async Task ExistingAcquisitionFaultIsCataloguedAndRequiresResetBeforeInitialize()
    {
        await using var fixture = await PublicTransportFixture.CreateAsync(false);
        await fixture.Client.InitializeAsync();
        await fixture.Client.StartAsync(new SystemStartRequest { RunMode = ControlRunModes.Continue });
        await fixture.Session.SimulateAcquisitionFaultAsync("synthetic acquisition fault");
        Assert.Equal(SystemStates.Faulted, (await fixture.Client.GetStatusAsync()).State);
        Assert.Contains((await fixture.Client.GetAlarmsAsync()).Sources, x => x.Name == "simulator.acquisitionFault" && x.Category == "fault");
        await Assert.ThrowsAsync<VirexClientException>(() => fixture.Client.InitializeAsync());
        Assert.Equal(SystemStates.Uninitialized, (await fixture.Client.ResetAsync()).State);
        Assert.Equal(SystemStates.Ready, (await fixture.Client.InitializeAsync()).State);
    }
    [Theory]
    [InlineData("defect")]
    [InlineData("criticalDefect")]
    public async Task DefectCategoriesNotifyCommittedResultWithoutLatchingOrUsingSupersededWork(string category)
    {
        await using var fixture = await PublicTransportFixture.CreateAsync(false);
        var name = "inspection." + category + "/extra";
        var descriptor = new AlarmSourceDescriptor { Name = name, Source = "synthetic-inspection", Category = category };
        await fixture.Session.ConfigureAlarmSourceAsync(descriptor);
        descriptor.Name = "caller-mutated";
        var source = Assert.Single((await fixture.Client.GetAlarmsAsync()).Sources, x => x.Name == name);
        Assert.Equal(category, source.Category);
        await fixture.Client.InitializeAsync();
        await fixture.Session.SetOperationContextForTestsAsync(OperationContexts.Local);
        var start = await fixture.Client.StartAsync(new SystemStartRequest { RequestId = "defect-work", RunMode = ControlRunModes.Continue });
        Assert.Equal(OperationContexts.Local, start.OperationContext);
        await fixture.Session.RunCompletedAsync();
        await fixture.Session.SetOperationContextForTestsAsync(OperationContexts.Remote);
        var received = new TaskCompletionSource<EventEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = await fixture.Client.TcpEvents.SubscribeAsync([name], e => received.TrySetResult(e));
        var before = DateTimeOffset.UtcNow;
        Assert.True(await fixture.Session.RaiseAlarmAsync(name, "explicit synthetic finding", expectedJobId: start.JobId, resultId: start.CaptureId));
        var evt = await received.Task.WaitAsync(TimeSpan.FromSeconds(3));
        var alarm = ProtocolJson.Deserialize<AlarmNotification>(evt.Payload.GetRawText())!;
        Assert.Equal(category, alarm.Category); Assert.Equal("synthetic-inspection", alarm.Source);
        Assert.Equal(start.AppId, alarm.AppId); Assert.Equal(start.InstanceId, alarm.InstanceId);
        Assert.Equal(start.RequestId, alarm.RequestId); Assert.Equal(start.JobId, alarm.JobId);
        Assert.Equal(start.CaptureId, alarm.CaptureId); Assert.Equal(start.CaptureId, alarm.ResultId);
        Assert.Equal(alarm.ResultId, evt.ResultId); Assert.InRange(alarm.Timestamp, before, DateTimeOffset.UtcNow);
        Assert.Equal(OperationContexts.Local, evt.OperationContext);
        Assert.Equal(SystemStates.Ready, alarm.State);
        Assert.False((await fixture.Client.GetErrorAsync()).HasError);
        Assert.Equal(SystemStates.Ready, (await fixture.Client.GetStatusAsync()).State);
        var next = await fixture.Client.StartAsync(new SystemStartRequest { RunMode = ControlRunModes.Continue, InspectionMode = InspectionModes.CaptureOnly });
        Assert.Equal(OperationContexts.Remote, next.OperationContext);
        Assert.False(await fixture.Session.RaiseAlarmAsync(name, "late finding", expectedJobId: start.JobId, resultId: start.CaptureId));
        Assert.False(await fixture.Session.RaiseAlarmAsync(name, "uncommitted", resultId: next.CaptureId));
        Assert.False(await fixture.Session.RaiseAlarmAsync(name, "unknown result", resultId: "absent"));
        Assert.True(await fixture.Session.RaiseAlarmAsync(name, "current work notification", expectedJobId: next.JobId));
        Assert.Equal(SystemStates.Running, (await fixture.Client.GetStatusAsync()).State);
        await fixture.Client.StopAsync();
    }

    [Fact]
    public async Task ConcurrentResetJoinsOneCleanupAndRepeatedSuccessHasNoNewCleanup()
    {
        await using var fixture = await PublicTransportFixture.CreateAsync(false);
        fixture.Session.ConfigureEvents("reset-app", ["test.Barrier"]);
        await fixture.Client.InitializeAsync();
        fixture.Session.ConfigureDeinitializeFailures(1);
        var alarms = new ConcurrentQueue<EventEnvelope>();
        var drained = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = await fixture.Client.TcpEvents.SubscribeAsync(["simulator.cleanupFailed", "test.Barrier"], e =>
        {
            if (e.Name == "test.Barrier") drained.TrySetResult(); else alarms.Enqueue(e);
        });
        var first = fixture.Client.ResetAsync();
        await WaitForState(fixture.Client, SystemStates.Deinitializing);
        var second = fixture.Client.ResetAsync();
        var a = await Assert.ThrowsAsync<VirexClientException>(() => first);
        var b = await Assert.ThrowsAsync<VirexClientException>(() => second);
        Assert.Equal("cleanup_failed", ProtocolJson.Deserialize<CommandResponse>(a.ResponseBody)!.ErrorCode);
        Assert.Equal("cleanup_failed", ProtocolJson.Deserialize<CommandResponse>(b.ResponseBody)!.ErrorCode);
        Assert.Equal(SystemStates.Faulted, (await fixture.Client.GetStatusAsync()).State);
        fixture.Session.EmitEvent("test.Barrier", "{}"); await drained.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Single(alarms);
        Assert.Equal(SystemStates.Uninitialized, (await fixture.Client.ResetAsync()).State);
        fixture.Session.ConfigureDeinitializeFailures(1);
        Assert.Equal(SystemStates.Uninitialized, (await fixture.Client.ResetAsync()).State);
        await fixture.Client.InitializeAsync();
        await Assert.ThrowsAsync<VirexClientException>(() => fixture.Client.ResetAsync()); // Idempotent reset did not consume a cleanup attempt.
    }

    [Fact]
    public async Task CancelledResetWaitDoesNotAbandonAdmittedCleanupOrAllowPrematureInitialize()
    {
        await using var fixture = await PublicTransportFixture.CreateAsync(false);
        await fixture.Client.InitializeAsync();
        using var preCancelled = new CancellationTokenSource(); preCancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.Client.ResetAsync(preCancelled.Token));
        Assert.Equal(SystemStates.Ready, (await fixture.Client.GetStatusAsync()).State);
        using var cancel = new CancellationTokenSource();
        var first = fixture.Client.ResetAsync(cancel.Token);
        await WaitForState(fixture.Client, SystemStates.Deinitializing);
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        await Assert.ThrowsAsync<VirexClientException>(() => fixture.Client.StartAsync());
        var joined = await fixture.Client.ResetAsync();
        Assert.Equal(SystemStates.Uninitialized, joined.State);
        Assert.Equal(SystemStates.Uninitialized, (await fixture.Client.GetStatusAsync()).State);
        Assert.Equal(SystemStates.Ready, (await fixture.Client.InitializeAsync()).State);
    }

    [Fact]
    public async Task LocalRejectsExternalResetWithoutClearingFaultOrRecipeAndQueriesRemainAvailable()
    {
        await using var fixture = await PublicTransportFixture.CreateAsync(true);
        await fixture.Session.ConfigureAlarmSourceAsync(new AlarmSourceDescriptor { Name = "module.fault", Source = "synthetic", Category = "fault" });
        await fixture.Client.SetOperationModeAsync(OperationModes.Remote);
        await fixture.Client.InitializeAsync();
        var recipe = await fixture.Client.GetCurrentRecipeAsync();
        await fixture.Client.SetOperationModeAsync(OperationModes.Local);
        Assert.True(await fixture.Session.RaiseAlarmAsync("module.fault", "local synthetic hardware fault"));
        var denied = await Assert.ThrowsAsync<VirexClientException>(() => fixture.Client.ResetAsync());
        Assert.Equal("operation_not_allowed", ProtocolJson.Deserialize<CommandResponse>(denied.ResponseBody)!.ErrorCode);
        Assert.Equal(SystemStates.Faulted, (await fixture.Client.GetStatusAsync()).State);
        Assert.True((await fixture.Client.GetErrorAsync()).HasError);
        Assert.Equal(recipe.Revision, (await fixture.Client.GetCurrentRecipeAsync()).Revision);
        Assert.Contains((await fixture.Client.GetAlarmsAsync()).Sources, x => x.Name == "module.fault");
        await fixture.Client.SetOperationModeAsync(OperationModes.Remote);
        Assert.Equal(SystemStates.Uninitialized, (await fixture.Client.ResetAsync()).State);
        Assert.Equal(OperationModes.Remote, (await fixture.Client.GetOperationModeAsync()).Mode);
    }

    private static async Task WaitForState(VirexClient client, string state)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        while ((await client.GetStatusAsync(timeout.Token)).State != state)
            await Task.Delay(10, timeout.Token);
    }
    [Fact]
    public async Task CustomFaultStopsAndLatchesOnlyItsAppWithCompleteTerminalIdentity()
    {
        await using var fixture = await PublicTransportFixture.CreateAsync(false);
        await using var other = await PublicTransportFixture.CreateAsync(false);
        const string name = " factory.Fault / 自訂? ";
        fixture.Session.ConfigureEvents("App-A", []);
        other.Session.ConfigureEvents("App-B", []);
        await fixture.Session.ConfigureAlarmSourceAsync(new AlarmSourceDescriptor { Name = name, Source = "synthetic-module", Category = "fault" });
        Assert.Contains((await fixture.Client.GetAlarmsAsync()).Sources, source => source.Name == name);
        Assert.Contains(name, (await fixture.Client.GetEventCatalogAsync()).Names);
        await fixture.Client.InitializeAsync(); await other.Client.InitializeAsync();
        var start = await fixture.Client.StartAsync(new SystemStartRequest { RequestId = "fault-job", RunMode = ControlRunModes.Continue });
        var otherStart = await other.Client.StartAsync(new SystemStartRequest { RunMode = ControlRunModes.Continue });
        var received = new TaskCompletionSource<EventEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = await fixture.Client.TcpEvents.SubscribeAsync([name], e => received.TrySetResult(e));
        Assert.True(await fixture.Session.RaiseAlarmAsync(name, "synthetic module fault", expectedJobId: start.JobId));
        Assert.Equal("Faulted", (await fixture.Client.GetStatusAsync()).State);
        var alarm = await received.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal(start.AppId, alarm.AppId); Assert.Equal(start.InstanceId, alarm.InstanceId);
        Assert.Equal(start.RequestId, alarm.RequestId); Assert.Equal(start.JobId, alarm.JobId); Assert.Equal(start.CaptureId, alarm.CaptureId);
        Assert.Equal("synthetic-module", alarm.Payload.GetProperty("source").GetString());
        Assert.Equal("Faulted", alarm.Payload.GetProperty("state").GetString());
        Assert.Null(alarm.ResultId);
        Assert.Equal(SystemStates.Running, (await other.Client.GetStatusAsync()).State);
        await Assert.ThrowsAsync<VirexClientException>(() => fixture.Client.StartAsync());
        await Assert.ThrowsAsync<VirexClientException>(() => fixture.Client.InitializeAsync());
        Assert.Equal(SystemStates.Uninitialized, (await fixture.Client.ResetAsync()).State);
        Assert.False(await fixture.Session.RaiseAlarmAsync(name, "late old fault", expectedJobId: start.JobId));
        await fixture.Client.InitializeAsync();
        var next = await fixture.Client.StartAsync(new SystemStartRequest { RunMode = ControlRunModes.Continue, InspectionMode = InspectionModes.CaptureOnly });
        Assert.NotEqual(start.JobId, next.JobId);
        await fixture.Client.StopAsync(); await other.Client.StopAsync();
        Assert.NotNull(otherStart.JobId);
    }
    [Fact]
    public async Task CleanupFailureRemainsFaultedAndPublishesItsCataloguedAlarm()
    {
        await using var fixture = await PublicTransportFixture.CreateAsync(false);
        await fixture.Client.InitializeAsync();
        fixture.Session.ConfigureDeinitializeFailures(1, "synthetic cleanup failure");
        var received = new TaskCompletionSource<EventEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = await fixture.Client.TcpEvents.SubscribeAsync(["simulator.cleanupFailed"], e => received.TrySetResult(e));
        var failure = await Assert.ThrowsAsync<VirexClientException>(() => fixture.Client.ResetAsync());
        Assert.Equal(409, failure.StatusCode);
        Assert.Equal("Faulted", (await fixture.Client.GetStatusAsync()).State);
        var alarm = await received.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal("fault", alarm.Payload.GetProperty("category").GetString());
        Assert.Equal("Faulted", alarm.Payload.GetProperty("state").GetString());
        Assert.True((await fixture.Client.GetErrorAsync()).HasError);
        await Assert.ThrowsAsync<VirexClientException>(() => fixture.Client.InitializeAsync());
        Assert.Equal(SystemStates.Uninitialized, (await fixture.Client.ResetAsync()).State);
        Assert.False((await fixture.Client.GetErrorAsync()).HasError);
        Assert.Equal(SystemStates.Ready, (await fixture.Client.InitializeAsync()).State);
    }
    [Fact]
    public async Task ResetStopsRunningWorkAndRequiresExplicitInitialization()
    {
        await using var fixture = await PublicTransportFixture.CreateAsync(false);
        await fixture.Client.InitializeAsync();
        await fixture.Client.StartAsync(new SystemStartRequest { RequestId = "reset-work", RunMode = ControlRunModes.Continue });
        using var reset = await fixture.Http.PostAsync("api/system/reset", null);
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
        var response = ProtocolJson.Deserialize<CommandResponse>(await reset.Content.ReadAsStringAsync())!;
        Assert.True(response.Accepted);
        Assert.Equal("Reset", response.Command);
        Assert.Equal(SystemStates.Uninitialized, response.State);
        Assert.Equal(SystemStates.Uninitialized, (await fixture.Client.GetStatusAsync()).State);
        await Assert.ThrowsAsync<VirexClientException>(() => fixture.Client.StartAsync());
        Assert.Equal(SystemStates.Ready, (await fixture.Client.InitializeAsync()).State);
        await Task.Delay(1200);
        Assert.Equal(SystemStates.Ready, (await fixture.Client.GetStatusAsync()).State);
    }
    [Fact]
    public async Task CatalogListsActualSyntheticAlarmProducersThroughRest()
    {
        await using var fixture = await PublicTransportFixture.CreateAsync(false);
        using var response = await fixture.Http.GetAsync("api/alarms");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("simulator", document.RootElement.GetProperty("appId").GetString());
        var source = Assert.Single(document.RootElement.GetProperty("sources").EnumerateArray(), x => x.GetProperty("name").GetString() == "simulator.cleanupFailed");
        Assert.Equal("simulator.cleanupFailed", source.GetProperty("name").GetString());
        Assert.Equal("cleanup", source.GetProperty("source").GetString());
        Assert.Equal("fault", source.GetProperty("category").GetString());
    }
}
