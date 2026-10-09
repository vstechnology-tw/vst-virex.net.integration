using System.Net;
using Virex.NET.Client;

namespace Virex.NET.Contracts.Tests;

public sealed class DataSourceClientTests
{
    [Theory]
    [InlineData("catalog", "{\"sources\":null}")]
    [InlineData("catalog", "{\"sources\":[{}]}")]
    [InlineData("catalog", "{\"sources\":[{\"name\":\"A\",\"kind\":\"live\",\"type\":\"string\",\"availability\":\"no_current_result\"}]}")]
    [InlineData("current", "{\"jobId\":\"J\",\"captureId\":\"C\"}")]
    [InlineData("current", "{\"jobId\":\"J\",\"captureId\":\"C\",\"resultId\":null}")]
    public async Task MissingCatalogOrReferenceDataCannotBecomeDefaultSuccess(string query, string fields)
    {
        using var http = Http("{" + Prefix + fields[1..]);
        var client = new VirexRestClient(http);
        var error = await Assert.ThrowsAsync<VirexQueryException>(() => query == "catalog" ? (Task)client.GetDataSourcesAsync() : client.GetCurrentResultAsync());
        Assert.Equal("api_failed", error.ErrorCode);
    }

    [Fact]
    public async Task ResultValueMustMatchTheExplicitRequestedIdentifier()
    {
        using var http = Http("{" + Prefix + "\"name\":\"custom\",\"state\":\"Ready\",\"kind\":\"result\",\"type\":\"array\",\"value\":[],\"jobId\":\"J\",\"captureId\":\"C\",\"resultId\":\"other\"}");
        var error = await Assert.ThrowsAsync<VirexQueryException>(() => new VirexRestClient(http).ReadDataSourceAsync("custom", "requested"));
        Assert.Equal("api_failed", error.ErrorCode);
    }

    [Fact]
    public async Task ApiTimeoutIsTypedWhileCallerCancellationRemainsCancellation()
    {
        using var http = new HttpClient(new TimeoutHandler()) { BaseAddress = new Uri("http://localhost/") };
        var error = await Assert.ThrowsAsync<VirexQueryException>(() => new VirexRestClient(http).ReadDataSourceAsync("custom"));
        Assert.Equal("api_failed", error.ErrorCode);
        Assert.Equal(0, error.StatusCode);
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new VirexRestClient(http).ReadDataSourceAsync("custom", cancellationToken: cancel.Token));
    }

    private const string Prefix = "\"appId\":\"A\",\"instanceId\":\"I\",\"timestamp\":\"2026-10-09T00:00:00Z\",";

    [Theory]
    [InlineData("\"kind\":\"live\",\"type\":\"integer\",\"value\":1.5")]
    [InlineData("\"kind\":\"live\",\"type\":\"number\",\"value\":\"0\"")]
    [InlineData("\"kind\":\"live\",\"type\":\"number\",\"value\":null")]
    [InlineData("\"kind\":\"live\",\"type\":\"object\",\"value\":[]")]
    [InlineData("\"kind\":\"unknown\",\"type\":\"integer\",\"value\":0")]
    [InlineData("\"kind\":\"result\",\"type\":\"integer\",\"value\":0")]
    [InlineData("\"kind\":\"live\",\"type\":\"integer\",\"value\":0,\"resultId\":\"old\"")]
    [InlineData("\"kind\":\"live\",\"type\":\"integer\",\"value\":0,\"name\":\"other\"")]
    public async Task InconsistentSuccessPayloadCannotBecomeAValidValue(string fields)
    {
        using var http = Http("{" + Prefix + "\"name\":\"custom\",\"state\":\"Ready\"," + fields + "}");
        var error = await Assert.ThrowsAsync<VirexQueryException>(() => new VirexRestClient(http).ReadDataSourceAsync("custom"));
        Assert.Equal("api_failed", error.ErrorCode);
    }

    private static HttpClient Http(string body) => new(new Handler(body)) { BaseAddress = new Uri("http://localhost/") };

    private sealed class Handler(string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
    }

    private sealed class TimeoutHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new TaskCanceledException("Synthetic API timeout.");
        }
    }
}
