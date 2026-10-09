using System.Net;
using Virex.NET.Client;
using Virex.NET.Contracts;

namespace Virex.NET.Contracts.Tests;

public sealed class VirexRestClientTests
{
    [Fact]
    public async Task StartAsyncSendsSystemRouteAndPayload()
    {
        var handler = new RecordingHandler("""{"accepted":true,"state":"Running","command":"Start","message":"Started."}""");
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:5088/") };
        var client = new VirexRestClient(http);

        await client.StartAsync("golden-sample", ControlRunModes.Continue);

        Assert.Equal(RestRoutes.ApiSystemStart, handler.RequestUri?.AbsolutePath);
        using var body = System.Text.Json.JsonDocument.Parse(handler.RequestBody);
        Assert.Equal("golden-sample", body.RootElement.GetProperty("condition").GetString());
        Assert.Equal("continue", body.RootElement.GetProperty("runMode").GetString());
        Assert.True(Guid.TryParseExact(body.RootElement.GetProperty("requestId").GetString(), "N", out _));
    }

    [Fact]
    public async Task SetProductInfoSendsProductInfoRoute()
    {
        var handler = new RecordingHandler("""{"accepted":true,"state":"Ready","command":"SetProductInfo","message":"ProductInfo updated."}""");
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:5088/") };
        var client = new VirexRestClient(http);

        await client.SetProductInfoAsync(new ProductInfo { WaferID = "W01" });

        Assert.Equal(RestRoutes.ApiProductInfo, handler.RequestUri?.AbsolutePath);
        Assert.Contains("\"waferID\":\"W01\"", handler.RequestBody);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly string _responseBody;

        public RecordingHandler(string responseBody)
        {
            _responseBody = responseBody;
        }

        public Uri? RequestUri { get; private set; }

        public string RequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            RequestBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseBody),
            };
        }
    }
}
