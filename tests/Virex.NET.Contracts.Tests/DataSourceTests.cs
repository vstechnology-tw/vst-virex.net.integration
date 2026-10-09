using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Virex.NET.Client;
using Virex.NET.Contracts;

namespace Virex.NET.Contracts.Tests;

public sealed class DataSourceTests
{
    [Theory]
    [InlineData("integer", "0")]
    [InlineData("number", "0.25")]
    [InlineData("boolean", "false")]
    [InlineData("string", "\"\"")]
    [InlineData("array", "[]")]
    [InlineData("object", "{}")]
    public async Task DeclaredJsonTypesPreserveRealZeroFalseAndEmptyValues(string type, string json)
    {
        await using var fixture = await PublicTransportFixture.CreateAsync(false);
        using (var document = JsonDocument.Parse(json))
            await fixture.Session.ConfigureLiveDataSourceAsync("synthetic.typed", type, document.RootElement);
        var value = await fixture.Client.ReadDataSourceAsync("synthetic.typed");
        Assert.Equal(type, value.Type);
        Assert.Equal(json, value.Value.GetRawText());
        Assert.Equal(type, Assert.Single((await fixture.Client.GetDataSourcesAsync()).Sources, s => s.Name == value.Name).Type);
    }

    [Theory]
    [InlineData("name=%20")]
    [InlineData("name=system.state&name=result.findings")]
    [InlineData("name=system.state&extra=1")]
    [InlineData("name=system.state&resultId=")]
    public async Task InvalidQueryIsReadOnlyAndDoesNotBecomeAnotherSourceSelection(string query)
    {
        await using var fixture = await PublicTransportFixture.CreateAsync(false);
        using var response = await fixture.Http.GetAsync("api/data-sources/value?" + query);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("invalid_query", ProtocolJson.Deserialize<QueryError>(await response.Content.ReadAsStringAsync())!.ErrorCode);
        Assert.Equal(SystemStates.Uninitialized, (await fixture.Client.GetStatusAsync()).State);
        Assert.False((await fixture.Client.GetErrorAsync()).HasError);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancelledGenerationCannotPublishOrRestoreItsResultAfterNextJob(bool fault)
    {
        await using var fixture = await PublicTransportFixture.CreateAsync(false);
        await fixture.Client.InitializeAsync();
        var events = new System.Collections.Concurrent.ConcurrentQueue<EventEnvelope>();
        using var subscription = await fixture.Client.TcpEvents.SubscribeAsync(["resultCreated"], events.Enqueue);
        var old = await fixture.Client.StartAsync(new SystemStartRequest { RequestId = "old", RunMode = ControlRunModes.Continue });
        if (fault) { await fixture.Session.SimulateAcquisitionFaultAsync("synthetic source fault"); await fixture.Client.ResetAsync(); await fixture.Client.InitializeAsync(); }
        else await fixture.Client.StopAsync();
        await Failure(() => fixture.Client.GetCurrentResultAsync(), "no_current_result", 409);
        var next = await fixture.Client.StartAsync(new SystemStartRequest { RequestId = "next", RunMode = ControlRunModes.Continue });
        await fixture.Session.RunCompletedAsync();
        await Task.Delay(1200); // Both cancelled and completed timer deadlines have elapsed.
        var current = await fixture.Client.GetCurrentResultAsync();
        Assert.Equal(next.JobId, current.JobId);
        Assert.Equal(next.CaptureId, current.CaptureId);
        Assert.NotEqual(old.JobId, current.JobId);
        var evt = Assert.Single(events);
        Assert.Equal(current.ResultId, evt.ResultId);
        Assert.Equal(next.JobId, evt.JobId);
        await Failure(() => fixture.Client.ReadDataSourceAsync("result.findings", old.CaptureId), "result_not_committed", 409);
        Assert.Equal(current.ResultId, (await fixture.Client.GetCurrentResultAsync()).ResultId);
    }

    [Fact]
    public async Task AppsAndRestartInstancesKeepIndependentCurrentResultsForTheSameProduct()
    {
        await using var a = await PublicTransportFixture.CreateAsync(false);
        await using var b = await PublicTransportFixture.CreateAsync(false);
        a.Session.ConfigureEvents("App-A", []); b.Session.ConfigureEvents("App-B", []);
        await a.Client.InitializeAsync(); await b.Client.InitializeAsync();
        var startA = await a.Client.StartAsync(new SystemStartRequest { RunMode = ControlRunModes.Continue });
        var startB = await b.Client.StartAsync(new SystemStartRequest { RunMode = ControlRunModes.Continue });
        await a.Session.RunCompletedAsync();
        await Failure(() => b.Client.GetCurrentResultAsync(), "no_current_result", 409);
        await b.Session.RunCompletedAsync();
        var valueA = await a.Client.ReadDataSourceAsync("result.detail", startA.CaptureId);
        var valueB = await b.Client.ReadDataSourceAsync("result.detail", startB.CaptureId);
        Assert.Equal("App-A", valueA.AppId); Assert.Equal("App-B", valueB.AppId);
        Assert.NotEqual(valueA.InstanceId, valueB.InstanceId);
        Assert.NotEqual(valueA.JobId, valueB.JobId);
        Assert.Equal(valueA.Value.GetProperty("summary").GetProperty("lotID").GetString(), valueB.Value.GetProperty("summary").GetProperty("lotID").GetString());
        await a.Client.StartAsync(new SystemStartRequest { RunMode = ControlRunModes.Continue });
        await a.Client.StopAsync();
        Assert.Equal(valueB.ResultId, (await b.Client.GetCurrentResultAsync()).ResultId);
        await using var restarted = await PublicTransportFixture.CreateAsync(false);
        restarted.Session.ConfigureEvents("App-A", []);
        Assert.NotEqual(valueA.InstanceId, (await restarted.Client.GetDataSourcesAsync()).InstanceId);
        await Failure(() => restarted.Client.GetCurrentResultAsync(), "no_current_result", 409);
        await Failure(() => restarted.Client.ReadDataSourceAsync("result.findings", valueA.ResultId), "result_not_found", 404);
    }

    [Fact]
    public async Task RejectedPreparationAndPreCancelledRequestPreserveCommittedCurrentResult()
    {
        await using var fixture = await PublicTransportFixture.CreateAsync(false);
        await fixture.Client.InitializeAsync();
        await fixture.Client.StartAsync(new SystemStartRequest { RunMode = ControlRunModes.Continue });
        await fixture.Session.RunCompletedAsync();
        var current = await fixture.Client.GetCurrentResultAsync();
        await fixture.Session.ConfigureCaptureSimulationAsync(["synthetic"], failPreparation: true);
        await Assert.ThrowsAsync<VirexClientException>(() => fixture.Client.StartAsync());
        Assert.Equal(current.ResultId, (await fixture.Client.GetCurrentResultAsync()).ResultId);
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.Client.StartAsync(cancel.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.Client.ReadDataSourceAsync("system.state", cancellationToken: cancel.Token));
        Assert.Equal(current.ResultId, (await fixture.Client.GetCurrentResultAsync()).ResultId);
    }

    [Fact]
    public async Task CustomResultSourceUsesExistingPublicDetailAndRequiresExactResultId()
    {
        await using var fixture = await PublicTransportFixture.CreateAsync(false);
        await fixture.Session.ConfigureResultDataSourceAsync("inspection.Output / 自訂", "detail");
        await fixture.Client.InitializeAsync();
        var start = await fixture.Client.StartAsync(new SystemStartRequest { RequestId = "detail-job", RunMode = ControlRunModes.Continue });
        await fixture.Session.RunCompletedAsync();
        var value = await fixture.Client.ReadDataSourceAsync("inspection.Output / 自訂", start.CaptureId);
        Assert.Equal("object", value.Type);
        Assert.Equal(start.CaptureId, value.Value.GetProperty("resultId").GetString());
        Assert.Equal(start.JobId, value.Value.GetProperty("summary").GetProperty("jobId").GetString());
        Assert.Empty(value.Value.GetProperty("findings").EnumerateArray());
        var source = Assert.Single((await fixture.Client.GetDataSourcesAsync()).Sources, s => s.Name == value.Name);
        Assert.Equal("result", source.Kind);
        Assert.Equal("available", source.Availability);
        await Failure(() => fixture.Client.ReadDataSourceAsync(value.Name), "invalid_query", 400);
    }

    [Fact]
    public async Task CommittedArtifactCannotSubstituteAnotherJobOrTurnReadFailureIntoSuccess()
    {
        await using var fixture = await PublicTransportFixture.CreateAsync(false);
        await fixture.Client.InitializeAsync();
        var start = await fixture.Client.StartAsync(new SystemStartRequest { RunMode = ControlRunModes.Continue });
        await fixture.Session.RunCompletedAsync();
        var saved = Assert.Single((await fixture.Client.QueryResultsAsync()).Items);
        var original = await File.ReadAllTextAsync(saved.ResultPath);
        var artifact = JsonNode.Parse(original)!;
        artifact["detail"]!["summary"]!["jobId"] = "another-job";
        await File.WriteAllTextAsync(saved.ResultPath, artifact.ToJsonString());
        await Failure(() => fixture.Client.ReadDataSourceAsync("result.findings", saved.ResultId), "query_failed", 503);
        await File.WriteAllTextAsync(saved.ResultPath, "{}");
        await Failure(() => fixture.Client.ReadDataSourceAsync("result.findings", saved.ResultId), "query_failed", 503);
        await File.WriteAllTextAsync(saved.ResultPath, original);
        Assert.Equal(start.JobId, (await fixture.Client.ReadDataSourceAsync("result.findings", saved.ResultId)).JobId);
        File.Delete(saved.ResultPath);
        await Failure(() => fixture.Client.ReadDataSourceAsync("result.findings", saved.ResultId), "result_deleted", 410);
        Assert.Equal(SystemStates.Ready, (await fixture.Client.GetStatusAsync()).State);
        Assert.False((await fixture.Client.GetErrorAsync()).HasError);
    }

    [Fact]
    public async Task CurrentReferenceIsExplicitAndNeverFallsBackAfterAcceptedCaptureOnlyWork()
    {
        await using var fixture = await PublicTransportFixture.CreateAsync(false);
        await Failure(() => fixture.Client.GetCurrentResultAsync(), "no_current_result", 409);
        await fixture.Client.InitializeAsync();
        var start = await fixture.Client.StartAsync(new SystemStartRequest { RequestId = "current-A", RunMode = ControlRunModes.Continue });
        await fixture.Session.RunCompletedAsync();
        var current = await fixture.Client.GetCurrentResultAsync();
        Assert.Equal(start.CaptureId, current.ResultId);
        Assert.Equal(start.JobId, current.JobId);
        await fixture.Client.StartAsync(new SystemStartRequest { RunMode = ControlRunModes.Continue, InspectionMode = InspectionModes.CaptureOnly });
        await fixture.Session.RunCompletedAsync();
        await Failure(() => fixture.Client.GetCurrentResultAsync(), "no_current_result", 409);
    }

    [Fact]
    public async Task QueryFailuresAreTypedAndNeverReturnZeroOrPreviousValue()
    {
        await using var fixture = await PublicTransportFixture.CreateAsync(false);
        await fixture.Session.ConfigureLiveDataSourceAsync("synthetic.missing", "number", null);
        await fixture.Session.ConfigureLiveDataSourceAsync("synthetic.offline", "number", null, available: false);
        await Failure(() => fixture.Client.ReadDataSourceAsync("unknown"), "unknown_source", 404);
        await Failure(() => fixture.Client.ReadDataSourceAsync("synthetic.missing"), "source_value_missing", 409);
        await Failure(() => fixture.Client.ReadDataSourceAsync("synthetic.offline"), "source_unavailable", 409);
        await Failure(() => fixture.Client.ReadDataSourceAsync("result.findings"), "invalid_query", 400);
        await Failure(() => fixture.Client.ReadDataSourceAsync("result.findings", "absent"), "result_not_found", 404);
        await fixture.Client.InitializeAsync();
        var start = await fixture.Client.StartAsync(new SystemStartRequest { RunMode = ControlRunModes.Continue });
        await Failure(() => fixture.Client.ReadDataSourceAsync("result.findings", start.CaptureId), "result_not_committed", 409);
        await fixture.Client.StopAsync();
        Assert.Equal(SystemStates.Ready, (await fixture.Client.ReadDataSourceAsync("system.state")).Value.GetString());
    }

    private static async Task Failure(Func<Task> query, string code, int status)
    {
        var error = await Assert.ThrowsAsync<VirexQueryException>(query);
        Assert.Equal(code, error.ErrorCode);
        Assert.Equal(status, error.StatusCode);
    }

    [Fact]
    public async Task CustomExactSourceNameHasOwnedTypedJsonAndNeedsNoSdkWhitelist()
    {
        await using var fixture = await PublicTransportFixture.CreateAsync(false);
        const string name = " factory.extra / Δ? ";
        using (var document = JsonDocument.Parse("{\"measurement\":12.5,\"samples\":[1,true]}"))
            await fixture.Session.ConfigureLiveDataSourceAsync(name, "object", document.RootElement);
        var catalog = await fixture.Client.GetDataSourcesAsync();
        var source = Assert.Single(catalog.Sources, s => s.Name == name);
        Assert.Equal("object", source.Type);
        Assert.Equal("live", source.Kind);
        var value = await fixture.Client.ReadDataSourceAsync(name);
        Assert.Equal(name, value.Name);
        Assert.Equal(12.5, value.Value.GetProperty("measurement").GetDouble());
        Assert.True(value.Value.GetProperty("samples")[1].GetBoolean());
        // Reading again remains valid after the configuration JsonDocument was disposed.
        Assert.Equal(value.Value.GetRawText(), (await fixture.Client.ReadDataSourceAsync(name)).Value.GetRawText());
    }

    [Fact]
    public async Task AcceptedJobInvalidatesCurrentResultWhileHistoryAndLiveRemainReadable()
    {
        await using var fixture = await PublicTransportFixture.CreateAsync(false);
        await fixture.Client.InitializeAsync();
        var first = await fixture.Client.StartAsync(new SystemStartRequest { RunMode = ControlRunModes.Continue });
        await fixture.Session.RunCompletedAsync();
        Assert.Equal("available", (await fixture.Client.GetDataSourcesAsync()).Sources.Single(s => s.Name == "result.findings").Availability);
        await Assert.ThrowsAsync<VirexClientException>(() => fixture.Client.StartAsync(new SystemStartRequest { InspectionMode = "invalid" }));
        Assert.Equal("available", (await fixture.Client.GetDataSourcesAsync()).Sources.Single(s => s.Name == "result.findings").Availability);
        var second = await fixture.Client.StartAsync(new SystemStartRequest { RunMode = ControlRunModes.Continue, InspectionMode = InspectionModes.CaptureOnly });
        Assert.NotEqual(first.JobId, second.JobId);
        Assert.Equal("no_current_result", (await fixture.Client.GetDataSourcesAsync()).Sources.Single(s => s.Name == "result.findings").Availability);
        Assert.Equal(SystemStates.Running, (await fixture.Client.ReadDataSourceAsync("system.state")).Value.GetString());
        Assert.Equal(first.JobId, (await fixture.Client.ReadDataSourceAsync("result.findings", first.CaptureId)).JobId);
        await fixture.Client.StopAsync();
        await Task.Delay(1200); // Old timer deadlines must not restore the preceding result.
        Assert.Equal("no_current_result", (await fixture.Client.GetDataSourcesAsync()).Sources.Single(s => s.Name == "result.findings").Availability);
    }

    [Fact]
    public async Task ExplicitCommittedResultMatchesRestCommandAndTcpEventIdentity()
    {
        await using var fixture = await PublicTransportFixture.CreateAsync(false);
        fixture.Session.ConfigureEvents("synthetic-app-A", []);
        await fixture.Client.InitializeAsync();
        var received = new TaskCompletionSource<EventEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);
        var subscription = await fixture.Client.TcpEvents.SubscribeAsync(["resultCreated"], e => { received.TrySetResult(e); });
        using (subscription)
        {
            var start = await fixture.Client.StartAsync(new SystemStartRequest { RequestId = "read-request", RunMode = ControlRunModes.Continue });
            await fixture.Session.RunCompletedAsync();
            var evt = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var value = await fixture.Client.ReadDataSourceAsync("result.findings", evt.ResultId);
            Assert.Equal("result", value.Kind);
            Assert.Equal(JsonValueKind.Array, value.Value.ValueKind);
            Assert.Empty(value.Value.EnumerateArray()); // Genuine zero findings is a valid committed result.
            Assert.Equal(evt.ResultId, value.ResultId);
            Assert.Equal(start.JobId, value.JobId);
            Assert.Equal(start.CaptureId, value.CaptureId);
            Assert.Equal(start.RequestId, value.RequestId);
            Assert.Equal(start.AppId, value.AppId);
            Assert.Equal(start.InstanceId, value.InstanceId);
            Assert.Equal(evt.AppId, value.AppId);
            Assert.Equal(evt.InstanceId, value.InstanceId);
            Assert.Equal(evt.JobId, value.JobId);
            Assert.Equal(evt.CaptureId, value.CaptureId);
        }
    }

    [Fact]
    public async Task LiveValueReportsActualStateAndReadTime()
    {
        await using var fixture = await PublicTransportFixture.CreateAsync(false);
        await fixture.Client.InitializeAsync();
        var before = DateTimeOffset.UtcNow;
        var value = await fixture.Client.ReadDataSourceAsync("system.state");
        Assert.Equal(SystemStates.Ready, value.Value.GetString());
        Assert.InRange(value.Timestamp, before, DateTimeOffset.UtcNow);
        Assert.Equal("live", value.Kind);
        Assert.Equal(SystemStates.Ready, value.State);
        Assert.Null(value.ResultId);
    }

    [Fact]
    public async Task CatalogExposesLiveStateAndCurrentResultAvailabilityThroughRest()
    {
        await using var fixture = await PublicTransportFixture.CreateAsync(false);
        using var response = await fixture.Http.GetAsync("api/data-sources");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("simulator", body.RootElement.GetProperty("appId").GetString());
        var sources = body.RootElement.GetProperty("sources").EnumerateArray().ToArray();
        var state = Assert.Single(sources, s => s.GetProperty("name").GetString() == "system.state");
        Assert.Equal("live", state.GetProperty("kind").GetString());
        Assert.Equal("string", state.GetProperty("type").GetString());
        Assert.Equal("available", state.GetProperty("availability").GetString());
        var result = Assert.Single(sources, s => s.GetProperty("name").GetString() == "result.findings");
        Assert.Equal("result", result.GetProperty("kind").GetString());
        Assert.Equal("no_current_result", result.GetProperty("availability").GetString());
    }
}
