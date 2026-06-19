using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Virex.NET.Contracts;

namespace Virex.NET.Client;

public sealed class VirexRestClient
{
    private readonly HttpClient _http;

    public VirexRestClient(HttpClient http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    public Task<StatusDto> GetStatusAsync(CancellationToken cancellationToken = default) =>
        GetAsync<StatusDto>(RestRoutes.ApiStatus, cancellationToken);

    public Task<ErrorStatusDto> GetErrorAsync(CancellationToken cancellationToken = default) =>
        GetAsync<ErrorStatusDto>(RestRoutes.ApiError, cancellationToken);

    public Task<WaferInfo> GetWaferInfoAsync(CancellationToken cancellationToken = default) =>
        GetAsync<WaferInfo>(RestRoutes.ApiWaferInfo, cancellationToken);

    public async Task SetWaferInfoAsync(WaferInfo info, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsync(
            RestRoutes.ApiWaferInfo.TrimStart('/'),
            JsonContent(info),
            cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response).ConfigureAwait(false);
    }

    public Task<ControlStatusDto> InitializeAsync(CancellationToken cancellationToken = default) =>
        PostControlAsync(RestRoutes.ApiControlInitialize, cancellationToken);

    public Task<ControlStatusDto> TerminateAsync(CancellationToken cancellationToken = default) =>
        PostControlAsync(RestRoutes.ApiControlTerminate, cancellationToken);

    public Task<ControlStatusDto> StartAsync(CancellationToken cancellationToken = default) =>
        PostControlAsync(RestRoutes.ApiControlStart, cancellationToken);

    public Task<ControlStatusDto> StopAsync(CancellationToken cancellationToken = default) =>
        PostControlAsync(RestRoutes.ApiControlStop, cancellationToken);

    public Task<ResultListDto> QueryResultsAsync(
        string? lotId = null,
        string? waferId = null,
        string? recipeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = QueryString(lotId, waferId, recipeId);
        return GetAsync<ResultListDto>(RestRoutes.ApiResults + query, cancellationToken);
    }

    private async Task<ControlStatusDto> PostControlAsync(string route, CancellationToken cancellationToken)
    {
        var response = await _http.PostAsync(route.TrimStart('/'), null, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response).ConfigureAwait(false);
        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return ProtocolJson.Deserialize<ControlStatusDto>(json) ?? new ControlStatusDto();
    }

    private async Task<T> GetAsync<T>(string route, CancellationToken cancellationToken)
    {
        var response = await _http.GetAsync(route.TrimStart('/'), cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response).ConfigureAwait(false);
        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return ProtocolJson.Deserialize<T>(json) ?? throw new InvalidOperationException("Empty response.");
    }

    private static StringContent JsonContent<T>(T value) =>
        new StringContent(ProtocolJson.Serialize(value), Encoding.UTF8, "application/json");

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        throw new VirexClientException((int)response.StatusCode, body);
    }

    private static string QueryString(string? lotId, string? waferId, string? recipeId)
    {
        var parts = new List<string>();
        Add(parts, "lotId", lotId);
        Add(parts, "waferId", waferId);
        Add(parts, "recipeId", recipeId);
        return parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
    }

    private static void Add(List<string> parts, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            var trimmed = value!.Trim();
            parts.Add(Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(trimmed));
        }
    }
}

public sealed class VirexClientException : Exception
{
    public VirexClientException(int statusCode, string responseBody)
        : base($"Virex.NET request failed with HTTP {statusCode}: {responseBody}")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    public int StatusCode { get; }

    public string ResponseBody { get; }
}
