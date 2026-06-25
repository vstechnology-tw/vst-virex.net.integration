using System.Net;
using Virex.NET.Client;
using Virex.NET.Contracts;

namespace Virex.NET.Contracts.Tests;

public sealed class VirexRestClientTests
{
    [Fact]
    public async Task StartAsyncSendsConditionAndRunModePayload()
    {
        var handler = new RecordingHandler("""{"initialized":true,"processState":"ready","recipe":"Default","message":"Cycle completed."}""");
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:5088/") };
        var client = new VirexRestClient(http);

        await client.StartAsync("golden-sample", ControlRunModes.Continue);

        Assert.Equal("/api/control/start", handler.RequestUri?.AbsolutePath);
        Assert.Equal("""{"condition":"golden-sample","runMode":"continue"}""", handler.RequestBody);
    }

    [Fact]
    public async Task StopAsyncSendsReasonPayload()
    {
        var handler = new RecordingHandler("""{"initialized":true,"processState":"ready","recipe":"Default","message":"Stopped."}""");
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:5088/") };
        var client = new VirexRestClient(http);

        await client.StopAsync("operator-request");

        Assert.Equal("/api/control/stop", handler.RequestUri?.AbsolutePath);
        Assert.Equal("""{"reason":"operator-request"}""", handler.RequestBody);
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
