using System.Net;
using Virex.NET.Client;
using Virex.NET.Contracts;

namespace Virex.NET.Contracts.Tests;

public sealed class ReadOnlyQueryClientTests
{
    [Theory]
    [InlineData("recipes")]
    [InlineData("current")]
    [InlineData("parameters")]
    [InlineData("result")]
    public async Task OldAppUnsupportedQueriesRemainHttpFailures(string query)
    {
        using var http = new HttpClient(new StubHandler(HttpStatusCode.NotFound, "Not found")) { BaseAddress = new Uri("http://localhost/") };
        var error = await Assert.ThrowsAsync<VirexClientException>(() => Query(new VirexRestClient(http), query));
        Assert.Equal(404, error.StatusCode);
        Assert.Equal("Not found", error.ResponseBody);
    }

    [Theory]
    [InlineData("recipes", "{}")]
    [InlineData("current", "{}")]
    [InlineData("parameters", "{}")]
    [InlineData("result", "{}")]
    [InlineData("recipes", "{\"items\":[],\"count\":1}")]
    [InlineData("current", "{\"recipe\":\"\",\"revision\":\"v1\"}")]
    [InlineData("parameters", "{\"recipe\":\"A\",\"revision\":\"v1\",\"groups\":[{\"key\":\"g\",\"parameters\":[{\"key\":\"p\",\"type\":\"boolean\",\"value\":\"true\"}]}]}")]
    [InlineData("result", "{\"schemaVersion\":1,\"resultId\":\"other\",\"summary\":{\"resultId\":\"other\"},\"findings\":[]}")]
    [InlineData("result", "{\"schemaVersion\":2,\"resultId\":\"ID\",\"summary\":{\"resultId\":\"ID\"},\"findings\":[]}")]
    public async Task IncompleteOrInconsistentSuccessBodiesAreRejected(string query, string body)
    {
        using var http = new HttpClient(new StubHandler(HttpStatusCode.OK, body)) { BaseAddress = new Uri("http://localhost/") };
        await Assert.ThrowsAsync<InvalidOperationException>(() => Query(new VirexRestClient(http), query));
    }

    [Fact]
    public async Task ExactResultIdIsEncodedAsOneSegmentWithoutTrimming()
    {
        const string id = " ID /?%# ";
        var body = ProtocolJson.Serialize(new ResultDetail { SchemaVersion = 1, ResultId = id, Summary = new ResultSummary { ResultId = id } });
        var handler = new StubHandler(HttpStatusCode.OK, body);
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        Assert.Equal(id, (await new VirexRestClient(http).GetResultDetailAsync(id)).ResultId);
        Assert.Equal("/api/results/" + Uri.EscapeDataString(id), handler.RequestUri!.AbsolutePath);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(".")]
    [InlineData("..")]
    public async Task EmptyAndDotIdentifiersDoNotSendRequests(string id)
    {
        var handler = new StubHandler(HttpStatusCode.OK, "{}");
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        await Assert.ThrowsAsync<ArgumentException>(() => new VirexRestClient(http).GetResultDetailAsync(id));
        Assert.Null(handler.RequestUri);
    }

    [Theory]
    [InlineData("recipes")]
    [InlineData("current")]
    [InlineData("parameters")]
    [InlineData("result")]
    public async Task CancellationIsPreserved(string query)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var http = new HttpClient(new StubHandler(HttpStatusCode.OK, "{}")) { BaseAddress = new Uri("http://localhost/") };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Query(new VirexRestClient(http), query, cancellation.Token));
    }

    private static Task Query(VirexRestClient client, string query, CancellationToken token = default) => query switch
    {
        "recipes" => client.GetRecipesAsync(token), "current" => client.GetCurrentRecipeAsync(token),
        "parameters" => client.GetCurrentRecipeParametersAsync(token), _ => client.GetResultDetailAsync("ID", token),
    };

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
        }
    }
}
