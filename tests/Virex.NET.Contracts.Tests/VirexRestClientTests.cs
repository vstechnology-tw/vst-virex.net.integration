using System.Net;
using System.Text.Json;
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

    [Fact]
    public async Task ScopedStartOmitsRequireRemoteByDefaultAndWhenFalse()
    {
        var handler = new RecordingHandler("""{"accepted":true,"state":"Running","command":"Start","message":"Started."}""");
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:5088/") };
        var client = new VirexRestClient(http);

        await client.StartScopedAsync(Target(), new SystemStartRequest { RequestId = "old-overload", Condition = "old" });
        Assert.DoesNotContain("requireRemote", handler.RequestBody);

        await client.StartScopedAsync(Target(), new SystemStartRequest { RequestId = "explicit-false", Condition = "false" }, false);
        Assert.DoesNotContain("requireRemote", handler.RequestBody);
        Assert.Equal(ScopedControlCommands.Start, ScopedControlJson.ReadRequest(handler.RequestBody).Command);
    }

    [Fact]
    public async Task ScopedStartRequireRemoteSendsTrueAndSnapshotsBeforeCallerMutation()
    {
        var handler = new RecordingHandler("""{"accepted":true,"state":"Running","command":"Start","message":"Started."}""");
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:5088/") };
        var client = new VirexRestClient(http);
        var target = Target();
        var request = new SystemStartRequest { RequestId = "required", Condition = "before" };

        var call = client.StartScopedAsync(target, request, true);
        target.InstanceId = "mutated";
        request.Condition = "after";
        await call;

        var parsed = ScopedControlJson.ReadRequest(handler.RequestBody);
        Assert.True(parsed.RequireRemote);
        Assert.Equal("instance", parsed.ExpectedTarget!.InstanceId);
        Assert.Equal("before", ScopedControlJson.ReadPayload<SystemStartRequest>(parsed).Condition);
    }

    private static ControlTarget Target() => new()
    {
        AppId = "app",
        InstanceId = "instance",
        WorkspaceScope = new WorkspaceScopeIdentity
        {
            WorkspaceId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            RegistrationId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            ActivationId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
        },
    };

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
