using System.Text;
using System.Text.Json;
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

    public async Task<RecipeList> GetRecipesAsync(CancellationToken cancellationToken = default)
    {
        var value = await GetQueryAsync<RecipeList>(RestRoutes.ApiRecipes, ["items", "count"], cancellationToken).ConfigureAwait(false);
        if (value.Items is null || value.Count != value.Items.Length || value.Items.Any(x => x is null || !ValidRecipe(x.Recipe, x.Revision)))
            throw new InvalidOperationException("Invalid recipe list response.");
        return value;
    }

    public async Task<RecipeInfo> GetCurrentRecipeAsync(CancellationToken cancellationToken = default)
    {
        var value = await GetQueryAsync<RecipeInfo>(RestRoutes.ApiCurrentRecipe, ["recipe", "revision"], cancellationToken).ConfigureAwait(false);
        if (!ValidRecipe(value.Recipe, value.Revision))
            throw new InvalidOperationException("Invalid current recipe response.");
        return value;
    }

    public async Task<RecipeParameters> GetCurrentRecipeParametersAsync(CancellationToken cancellationToken = default)
    {
        return await GetQueryAsync<RecipeParameters>(RestRoutes.ApiCurrentRecipeParameters, ["recipe", "revision", "groups"], cancellationToken,
            QueryPayloadJson.ReadRecipeParameters).ConfigureAwait(false);
    }

    public async Task<ResultDetail> GetResultDetailAsync(string resultId, CancellationToken cancellationToken = default)
    {
        var value = await GetQueryAsync<ResultDetail>(RestRoutes.ResultDetail(resultId), ["schemaVersion", "resultId", "summary", "findings"], cancellationToken,
            QueryPayloadJson.ReadResultDetail).ConfigureAwait(false);
        if (value.SchemaVersion != ResultDetail.CurrentSchemaVersion || value.ResultId != resultId || value.Summary is null || value.Summary.ResultId != resultId || value.Findings is null)
            throw new InvalidOperationException("Invalid or mismatched result detail response.");
        return value;
    }

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
        using var response = await _http.GetAsync(route.TrimStart('/'), cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response).ConfigureAwait(false);
        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return ProtocolJson.Deserialize<T>(json) ?? throw new InvalidOperationException("Empty response.");
    }

    private async Task<T> GetQueryAsync<T>(string route, string[] required, CancellationToken cancellationToken, Func<JsonElement, T>? reader = null)
    {
        using var response = await _http.GetAsync(route.TrimStart('/'), cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response).ConfigureAwait(false);
        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object || required.Any(name =>
            !document.RootElement.EnumerateObject().Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))))
            throw new InvalidOperationException("Incomplete query response.");
        return reader is null ? ProtocolJson.Deserialize<T>(json) ?? throw new InvalidOperationException("Empty query response.")
            : reader(document.RootElement);
    }

    private static bool ValidRecipe(string recipe, string revision) =>
        !string.IsNullOrWhiteSpace(recipe) && !string.IsNullOrWhiteSpace(revision);

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
