using System.Text.Json;
using Virex.NET.Contracts;
using Virex.NET.Simulator.Core;

namespace Virex.NET.Contracts.Tests;

public sealed class AtomicStartProductInfoTests
{
    private static ProductInfo Product(Dictionary<string, string>? fields = null) => new()
    { LotID = "L1", WaferID = "W1", Recipe = "Recipe-A", AdditionalFields = fields };

    [Fact]
    public void OptionalFieldsPreserveLegacyWireAndExplicitEmptyValues()
    {
        var legacy = ProtocolJson.Serialize(new SystemStartRequest());
        Assert.DoesNotContain("productInfo", legacy);
        Assert.DoesNotContain("additionalFields", ProtocolJson.Serialize(new ProductInfo()));
        var info = Product(new() { ["station"] = "", ["orientation"] = "90" });
        var parsed = CommandPayloadJson.ReadObject<SystemStartRequest>(ProtocolJson.Serialize(new SystemStartRequest { ProductInfo = info }))!;
        Assert.Equal("", parsed.ProductInfo!.AdditionalFields!["station"]);
        Assert.False(parsed.ProductInfo.AdditionalFields.ContainsKey("absent"));
        Assert.Equal("90", parsed.ProductInfo.Snapshot().AdditionalFields!["orientation"]);
    }

    [Fact]
    public void SnapshotOwnsDictionaryAndPreventsMutationThroughCast()
    {
        var fields = new Dictionary<string, string> { ["station"] = "A" };
        var source = Product(fields); var owned = source.Snapshot();
        fields["station"] = "B"; source.WaferID = "W2";
        Assert.Equal("A", owned.AdditionalFields!["station"]); Assert.Equal("W1", owned.WaferID);
        Assert.Throws<NotSupportedException>(() => ((IDictionary<string, string>)owned.AdditionalFields)["station"] = "C");
    }

    [Theory]
    [InlineData("{\"productInfo\":{\"waferID\":null}}")]
    [InlineData("{\"productInfo\":{\"additionalFields\":{\"station\":null}}}")]
    [InlineData("{\"productInfo\":{\"additionalFields\":{\"\":\"A\"}}}")]
    [InlineData("{\"productInfo\":{\"additionalFields\":{\"station\":123}}}")]
    public void InvalidNestedProductIsRejectedForRestMqttAndTcp(string json)
    {
        Assert.Throws<JsonException>(() => CommandPayloadJson.ReadObject<SystemStartRequest>(json));
        Assert.Throws<JsonException>(() => CommandPayloadJson.ReadObject<MqttCommandRequest>(json));
        Assert.False(TcpSocketMessageParser.TryParse(json.Insert(1, "\"type\":\"start\","), out _, out _));
    }

    [Fact]
    public void TcpOptionsAndProductFramesRetainCompleteContext()
    {
        var request = new SystemStartRequest { RequestId = "start-one", ProductInfo = Product(new() { ["station"] = "A" }), Condition = "top", RunMode = ControlRunModes.SingleRun };
        Assert.True(TcpSocketMessageParser.TryParse(TcpSocketEventFormatter.FormatStartWithOptionsCommand(request), out var start, out var error), error);
        Assert.Equal("start-one", start.RequestId); Assert.Equal("A", start.ProductInfo!.AdditionalFields!["station"]);
        Assert.True(TcpSocketMessageParser.TryParse(TcpSocketEventFormatter.FormatProductInfoCommand(request.ProductInfo), out var product, out error), error);
        Assert.Equal("A", product.ProductInfo!.AdditionalFields!["station"]);
    }

    [Fact]
    public async Task AcceptedStartOwnsCompleteProductThroughSavedResultAndRejectedStartDoesNotReplaceIt()
    {
        var session = new SimulatorSession(); await session.InitializeAsync();
        var fields = new Dictionary<string, string> { ["station"] = "A", ["optional"] = "" };
        var supplied = Product(fields);
        var accepted = await session.StartAsync(new SystemStartRequest { ProductInfo = supplied, RunMode = ControlRunModes.SingleRun });
        Assert.True(accepted.Accepted);
        supplied.WaferID = "mutated"; fields["station"] = "mutated";
        var rejected = await session.StartAsync(new SystemStartRequest { ProductInfo = new ProductInfo { WaferID = "wrong" } });
        Assert.False(rejected.Accepted); Assert.Equal("W1", session.ProductInfo.WaferID);
        Assert.Equal("A", session.ProductInfo.AdditionalFields!["station"]);
        await session.RunCompletedAsync();
        var result = Assert.Single(session.Results);
        Assert.Equal("W1", result.WaferID); Assert.Equal("A", result.AdditionalFields!["station"]);
        var saved = ProtocolJson.Deserialize<ResultSummary>(await File.ReadAllTextAsync(result.ResultPath))!;
        Assert.Equal("A", saved.AdditionalFields!["station"]); Assert.Equal("", saved.AdditionalFields["optional"]);
        var detail = await session.GetResultDetailAsync(result.ResultId);
        Assert.Equal("A", detail.Summary.AdditionalFields!["station"]);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RestSdkAndScopedSdkRetainAtomicProduct(bool scoped)
    {
        var session = new SimulatorSession();
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start(); var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
        var prefix = "http://127.0.0.1:" + port + "/";
        var server = new Virex.NET.Simulator.WPF.Services.RestSimulatorServer(session, prefix);
        await server.StartAsync();
        try
        {
            using var http = new HttpClient { BaseAddress = new Uri(prefix) };
            var client = new Virex.NET.Client.VirexRestClient(http);
            await client.InitializeAsync();
            var request = new SystemStartRequest { ProductInfo = Product(new() { ["station"] = "A" }) };
            var result = scoped
                ? await client.StartScopedAsync(new ControlTarget { AppId = session.AppId, InstanceId = session.InstanceId, WorkspaceScope = session.CurrentWorkspaceScope }, request)
                : await client.StartAsync(request);
            Assert.True(result.Accepted);
            Assert.Equal("W1", session.ProductInfo.WaferID);
            Assert.Equal("A", session.ProductInfo.AdditionalFields!["station"]);
            await session.RunCompletedAsync();
            Assert.Equal("A", Assert.Single(session.Results).AdditionalFields!["station"]);
        }
        finally { await server.StopAsync(); }
    }

}

