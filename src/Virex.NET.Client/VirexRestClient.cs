using System.Text;
using Virex.NET.Contracts;

namespace Virex.NET.Client;

public sealed class VirexRestClient
{
    private readonly HttpClient _http;

    public VirexRestClient(HttpClient http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    public Task<SystemStatus> GetStatusAsync(CancellationToken cancellationToken = default) =>
        GetAsync<SystemStatus>(RestRoutes.ApiStatus, cancellationToken);

    public Task<ErrorInfo> GetErrorAsync(CancellationToken cancellationToken = default) =>
        GetAsync<ErrorInfo>(RestRoutes.ApiError, cancellationToken);

    public Task<ProductInfo> GetProductInfoAsync(CancellationToken cancellationToken = default) =>
        GetAsync<ProductInfo>(RestRoutes.ApiProductInfo, cancellationToken);

    public Task<CommandResponse> SetProductInfoAsync(ProductInfo info, CancellationToken cancellationToken = default) =>
        PostAsync(RestRoutes.ApiProductInfo, info, cancellationToken);

    public Task<CommandResponse> InitializeAsync(CancellationToken cancellationToken = default) =>
        PostAsync(RestRoutes.ApiSystemInitialize, cancellationToken);

    public Task<CommandResponse> DeinitializeAsync(CancellationToken cancellationToken = default) =>
        PostAsync(RestRoutes.ApiSystemDeinitialize, cancellationToken);

    public Task<CommandResponse> StartAsync(CancellationToken cancellationToken = default) =>
        PostAsync(RestRoutes.ApiSystemStart, cancellationToken);

    public Task<CommandResponse> StartAsync(string? condition, CancellationToken cancellationToken = default) =>
        StartAsync(new SystemStartRequest { Condition = condition }, cancellationToken);

    public Task<CommandResponse> StartAsync(string? condition, string? runMode, CancellationToken cancellationToken = default) =>
        StartAsync(new SystemStartRequest { Condition = condition, RunMode = runMode }, cancellationToken);

    public Task<CommandResponse> StartAsync(SystemStartRequest request, CancellationToken cancellationToken = default) =>
        PostAsync(RestRoutes.ApiSystemStart, request, cancellationToken);

    public Task<CommandResponse> StopAsync(CancellationToken cancellationToken = default) =>
        PostAsync(RestRoutes.ApiSystemStop, cancellationToken);

    public Task<CommandResponse> StopAsync(string? reason, CancellationToken cancellationToken = default) =>
        StopAsync(new SystemStopRequest { Reason = reason }, cancellationToken);

    public Task<CommandResponse> StopAsync(SystemStopRequest request, CancellationToken cancellationToken = default) =>
        PostAsync(RestRoutes.ApiSystemStop, request, cancellationToken);

    public Task<ResultList> QueryResultsAsync(
        string? lotID = null,
        string? waferID = null,
        string? recipe = null,
        CancellationToken cancellationToken = default)
    {
        var query = QueryString(lotID, waferID, recipe);
        return GetAsync<ResultList>(RestRoutes.ApiResults + query, cancellationToken);
    }

    private async Task<CommandResponse> PostAsync(string route, CancellationToken cancellationToken)
    {
        var response = await _http.PostAsync(route.TrimStart('/'), null, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response).ConfigureAwait(false);
        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return ProtocolJson.Deserialize<CommandResponse>(json) ?? new CommandResponse();
    }

    private async Task<CommandResponse> PostAsync<T>(string route, T payload, CancellationToken cancellationToken)
    {
        var response = await _http.PostAsync(route.TrimStart('/'), JsonContent(payload), cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response).ConfigureAwait(false);
        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return ProtocolJson.Deserialize<CommandResponse>(json) ?? new CommandResponse();
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

    private static string QueryString(string? lotID, string? waferID, string? recipe)
    {
        var parts = new List<string>();
        Add(parts, "waferID", waferID);
        Add(parts, "lotID", lotID);
        Add(parts, "recipe", recipe);
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
