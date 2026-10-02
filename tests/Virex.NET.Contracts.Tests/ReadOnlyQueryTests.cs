using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using Virex.NET.Client;
using Virex.NET.Contracts;
using Virex.NET.Simulator.Core;
using Virex.NET.Simulator.WPF.Services;

namespace Virex.NET.Contracts.Tests;

public sealed class ReadOnlyQueryTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "virex-query-" + Guid.NewGuid().ToString("N"));
    private readonly SimulatorSession _session;
    private readonly RestSimulatorServer _server;
    private readonly HttpClient _http;
    private readonly VirexClient _client;

    public ReadOnlyQueryTests()
    {
        _session = new SimulatorSession(_root);
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var prefix = "http://127.0.0.1:" + port + "/";
        _server = new RestSimulatorServer(_session, prefix);
        _http = new HttpClient();
        _client = new VirexClient(new VirexClientOptions { RestBaseUrl = prefix }, _http);
    }

    public Task InitializeAsync() => _server.StartAsync();

    public async Task DisposeAsync()
    {
        if (_session.State == SimulatorState.Running)
            await _session.StopAsync();
        await _server.StopAsync();
        _client.Dispose();
        _http.Dispose();
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public async Task LoadedRecipeAndParametersAreConsistentDetachedAndSideEffectFree()
    {
        var events = 0;
        _session.StatusChanged += (_, _) => events++;
        _session.ErrorChanged += (_, _) => events++;
        _session.CommandRejected += (_, _) => events++;
        var list = await _client.GetRecipesAsync();
        Assert.Contains(list.Items, x => x.Recipe == "RCP-A");
        Assert.Equal(list.Items.Length, list.Count);
        await AssertFailure(() => _client.GetCurrentRecipeAsync(), 409, QueryErrorCodes.NoCurrentRecipe);
        await AssertFailure(() => _client.GetCurrentRecipeParametersAsync(), 409, QueryErrorCodes.NoCurrentRecipe);
        Assert.Equal(0, events);

        await _client.InitializeAsync();
        var loaded = await _client.GetCurrentRecipeAsync();
        // ProductInfo is a requested value, not proof that a recipe was loaded.
        _session.ProductInfo.Recipe = "requested-but-not-loaded";
        Assert.Equal("RCP-A", (await _client.GetCurrentRecipeAsync()).Recipe);
        var parameters = await _client.GetCurrentRecipeParametersAsync();
        Assert.Equal(loaded.Recipe, parameters.Recipe);
        Assert.Equal(loaded.Revision, parameters.Revision);
        Assert.Equal("integer", parameters.Groups[0].Parameters[0].Type);
        Assert.Equal(1000, parameters.Groups[0].Parameters[0].Value.GetInt32());
        var beforeEvents = events;
        var beforeStatus = ProtocolJson.Serialize(_session.Status);
        var beforeError = ProtocolJson.Serialize(_session.Error);
        var localSnapshot = _session.GetCurrentRecipeParameters();
        localSnapshot.Groups[0].Key = "mutated";
        Assert.Equal("simulator", (await _client.GetCurrentRecipeParametersAsync()).Groups[0].Key);
        Assert.Equal(beforeEvents, events);
        Assert.Equal(beforeStatus, ProtocolJson.Serialize(_session.Status));
        Assert.Equal(beforeError, ProtocolJson.Serialize(_session.Error));

        var update = _session.SetProductInfoAsync(new ProductInfo { Recipe = "RCP-DEMO" });
        Assert.Equal(SimulatorState.UpdatingProductInfo, _session.State);
        await AssertFailure(() => _client.GetCurrentRecipeAsync(), 409, QueryErrorCodes.QueryNotReady);
        await AssertFailure(() => _client.GetCurrentRecipeParametersAsync(), 409, QueryErrorCodes.QueryNotReady);
        Assert.True((await update).Accepted);
        Assert.Equal("RCP-DEMO", (await _client.GetCurrentRecipeAsync()).Recipe);
        Assert.Equal("RCP-DEMO", (await _client.GetCurrentRecipeParametersAsync()).Recipe);
        await _client.DeinitializeAsync();
        await AssertFailure(() => _client.GetCurrentRecipeAsync(), 409, QueryErrorCodes.NoCurrentRecipe);
    }

    [Fact]
    public async Task CancelledLoadPreservesPreviousLoadedSnapshot()
    {
        await _client.InitializeAsync();
        using var cancellation = new CancellationTokenSource();
        var update = _session.SetProductInfoAsync(new ProductInfo { Recipe = "CANCELLED" }, cancellation.Token);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => update);
        Assert.Equal("RCP-A", (await _client.GetCurrentRecipeAsync()).Recipe);
        Assert.Equal("RCP-A", (await _client.GetCurrentRecipeParametersAsync()).Recipe);
        Assert.DoesNotContain((await _client.GetRecipesAsync()).Items, x => x.Recipe == "CANCELLED");
    }

    [Fact]
    public async Task SameLotWaferResultsUseExactIdAndZeroFindingsIsValid()
    {
        await _client.InitializeAsync();
        await _client.SetProductInfoAsync(new ProductInfo { Recipe = "RCP-A", LotID = "SAME", WaferID = "SAME" });
        Task<ResultDetail>? uncommitted = null;
        _session.ImageGrabbed += (_, image) => uncommitted = _session.GetResultDetailAsync(image.CaptureId);
        var first = await CompleteResult();
        Assert.NotNull(uncommitted);
        var pendingFailure = await Assert.ThrowsAsync<SimulatorQueryException>(() => uncommitted!);
        Assert.Equal(QueryErrorCodes.ResultNotFound, pendingFailure.ErrorCode);
        var second = await CompleteResult();
        Assert.NotEqual(first.ResultId, second.ResultId);
        var listed = await _client.QueryResultsAsync("SAME", "SAME");
        Assert.Equal(2, listed.Count);
        foreach (var result in new[] { first, second })
        {
            var detail = await _client.GetResultDetailAsync(result.ResultId);
            Assert.Equal(result.ResultId, detail.ResultId);
            Assert.Equal(result.CaptureId, detail.Summary.CaptureId);
            Assert.Equal(result.Timestamp, detail.Summary.Timestamp);
            Assert.Equal(ResultDetail.CurrentSchemaVersion, detail.SchemaVersion);
            Assert.Empty(detail.Findings);
            Assert.Equal("OK", detail.Summary.OverallResult);
            // Existing clients can still deserialize the original artifact summary.
            var legacy = ProtocolJson.Deserialize<ResultSummary>(File.ReadAllText(result.ResultPath));
            Assert.Equal(result.ResultId, legacy!.ResultId);
        }
        await AssertFailure(() => _client.GetResultDetailAsync(first.ResultId.ToLowerInvariant()), 404, QueryErrorCodes.ResultNotFound);
    }

    [Fact]
    public async Task FindingsPreservePublicLabelsScoresAndMillimeterGeometry()
    {
        await _client.InitializeAsync();
        var result = await CompleteResult();
        var artifact = JsonNode.Parse(File.ReadAllText(result.ResultPath))!;
        var detail = await _client.GetResultDetailAsync(result.ResultId);
        detail.Findings = [new ResultFinding
        {
            FindingId = "F-1", Kind = "Defect", Label = "example", Score = 0.95,
            ProductPolygon = [new ProductPoint { Xmm = 1.5, Ymm = 2.5 }], DiagnosticImageIds = ["image-1"],
        }];
        artifact["detail"] = JsonNode.Parse(ProtocolJson.Serialize(detail));
        File.WriteAllText(result.ResultPath, artifact.ToJsonString());
        var finding = Assert.Single((await _client.GetResultDetailAsync(result.ResultId)).Findings);
        Assert.Equal("example", finding.Label);
        Assert.Equal(0.95, finding.Score);
        Assert.Equal(1.5, finding.ProductPolygon[0].Xmm);
        Assert.Equal(2.5, finding.ProductPolygon[0].Ymm);
        Assert.Equal("image-1", Assert.Single(finding.DiagnosticImageIds));
    }

    [Fact]
    public async Task MissingDeletedCorruptAndMismatchedResultsFailWithoutStateChanges()
    {
        await _client.InitializeAsync();
        var result = await CompleteResult();
        var events = 0;
        _session.StatusChanged += (_, _) => events++;
        _session.ErrorChanged += (_, _) => events++;
        _session.CommandRejected += (_, _) => events++;
        var original = File.ReadAllText(result.ResultPath);
        var before = ProtocolJson.Serialize(_session.Status);
        await AssertFailure(() => _client.GetResultDetailAsync("unknown/../?%#"), 404, QueryErrorCodes.ResultNotFound);
        File.WriteAllText(result.ResultPath, "invalid json");
        await AssertFailure(() => _client.GetResultDetailAsync(result.ResultId), 503, QueryErrorCodes.QueryFailed);
        var artifact = JsonNode.Parse(original)!;
        artifact["detail"]!["resultId"] = "different-id";
        File.WriteAllText(result.ResultPath, artifact.ToJsonString());
        await AssertFailure(() => _client.GetResultDetailAsync(result.ResultId), 503, QueryErrorCodes.QueryFailed);
        File.Delete(result.ResultPath);
        await AssertFailure(() => _client.GetResultDetailAsync(result.ResultId), 410, QueryErrorCodes.ResultDeleted);
        Assert.Equal(before, ProtocolJson.Serialize(_session.Status));
        Assert.False(_session.Error.HasError);
        Assert.Equal(0, events);
    }

    [Theory]
    [InlineData("[{}]")]
    [InlineData("[null]")]
    [InlineData("[{\"findingId\":\"F\",\"kind\":\"Defect\",\"label\":\"L\",\"productPolygon\":[{}],\"diagnosticImageIds\":[]}]")]
    [InlineData("[{\"findingId\":\"F\",\"kind\":\"Defect\",\"label\":\"L\",\"productPolygon\":[null],\"diagnosticImageIds\":[]}]")]
    [InlineData("[{\"findingId\":\"F\",\"kind\":\"Defect\",\"label\":\"L\",\"productPolygon\":[],\"diagnosticImageIds\":null}]")]
    public async Task IncompleteCommittedFindingsReturnQueryFailureWithoutStateChanges(string findings)
    {
        await _client.InitializeAsync();
        var result = await CompleteResult();
        var artifact = JsonNode.Parse(File.ReadAllText(result.ResultPath))!;
        artifact["detail"]!["findings"] = JsonNode.Parse(findings);
        File.WriteAllText(result.ResultPath, artifact.ToJsonString());
        var events = 0;
        _session.ErrorChanged += (_, _) => events++;
        _session.CommandRejected += (_, _) => events++;
        await AssertFailure(() => _client.GetResultDetailAsync(result.ResultId), 503, QueryErrorCodes.QueryFailed);
        Assert.Equal(SimulatorState.Ready, _session.State);
        Assert.False(_session.Error.HasError);
        Assert.Equal(0, events);
    }

    [Theory]
    [InlineData("/api/recipes")]
    [InlineData("/api/recipes/current")]
    [InlineData("/api/recipes/current/parameters")]
    [InlineData("/api/results/unknown")]
    public async Task NewQueriesRejectWritesAndFilters(string route)
    {
        using var write = await _http.PostAsync(route, null);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, write.StatusCode);
        Assert.Contains("GET", write.Content.Headers.Allow);
        using var filter = await _http.GetAsync(route + "?lotID=latest");
        Assert.Equal(HttpStatusCode.BadRequest, filter.StatusCode);
        Assert.Equal(SimulatorState.Uninitialized, _session.State);
    }

    [Fact]
    public async Task OpenApiDescribesAllNewQueriesAndRequiredWireFields()
    {
        using var document = JsonDocument.Parse(await _http.GetStringAsync(RestRoutes.OpenApiJson));
        var paths = document.RootElement.GetProperty("paths");
        foreach (var route in new[] { RestRoutes.ApiRecipes, RestRoutes.ApiCurrentRecipe, RestRoutes.ApiCurrentRecipeParameters, RestRoutes.ApiResultDetail })
        {
            var operation = paths.GetProperty(route).GetProperty("get");
            Assert.True(operation.GetProperty("responses").TryGetProperty("503", out _));
            Assert.False(paths.GetProperty(route).TryGetProperty("post", out _));
        }
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        Assert.Contains("findings", schemas.GetProperty("ResultDetail").GetProperty("required").EnumerateArray().Select(x => x.GetString()));
        Assert.True(schemas.GetProperty("ProductPoint").GetProperty("properties").TryGetProperty("xmm", out _));
    }

    private async Task<ResultSummary> CompleteResult()
    {
        Assert.True((await _client.StartAsync()).Accepted);
        Assert.True((await _session.RunCompletedAsync()).Accepted);
        return _session.Results[0];
    }

    private static async Task AssertFailure(Func<Task> action, int status, string code)
    {
        var failure = await Assert.ThrowsAsync<VirexClientException>(action);
        Assert.Equal(status, failure.StatusCode);
        Assert.Equal(code, ProtocolJson.Deserialize<QueryError>(failure.ResponseBody)!.ErrorCode);
    }
}
