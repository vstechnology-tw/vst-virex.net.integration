using System.Text;
using System.Text.Json;
using Virex.NET.Contracts;

namespace Virex.NET.Client;

public sealed partial class VirexRestClient
{
    private readonly HttpClient _http;

    public VirexRestClient(HttpClient http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    public Task<SystemStatus> GetStatusAsync(CancellationToken cancellationToken = default) =>
        GetAsync<SystemStatus>(RestRoutes.ApiStatus, cancellationToken);

    /// <summary>Reads the addressed App's actual software version using REST. Does not initialize or modify it.
    /// Returns an owned snapshot. Concurrent calls are supported. Caller cancellation is preserved;
    /// API, timeout and invalid-response failures throw VirexQueryException. HttpClient remains caller-owned.</summary>
    public Task<SoftwareVersionInfo> GetSoftwareVersionAsync(CancellationToken cancellationToken = default) =>
        GetDataSourceQueryAsync<SoftwareVersionInfo>(RestRoutes.ApiSoftwareVersion, ["version"], cancellationToken, QueryPayloadJson.ReadSoftwareVersion);

    /// <summary>Reads an owned Alarm catalog snapshot of exact names for TCP event subscription.
    /// Query/parse/timeout failures throw VirexQueryException; caller cancellation is preserved.
    /// Concurrent calls are supported. This client borrows HttpClient; the caller owns its disposal.</summary>
    public Task<AlarmCatalog> GetAlarmsAsync(CancellationToken cancellationToken = default) =>
        GetDataSourceQueryAsync<AlarmCatalog>(RestRoutes.ApiAlarms, ["appId", "instanceId", "timestamp", "sources"], cancellationToken, AlarmJson.ReadCatalog);

    /// <summary>
    /// Stops work and completes cleanup, returning Uninitialized on success. Explicit Initialize is still required.
    /// HTTP failures throw VirexClientException; caller cancellation cancels the wait and does not undo server cleanup.
    /// Concurrent calls are supported. This client borrows HttpClient; the caller owns its disposal.
    /// </summary>
    public Task<CommandResponse> ResetAsync(CancellationToken cancellationToken = default) =>
        PostAsync(RestRoutes.ApiSystemReset, cancellationToken);

    /// <summary>Reads exact source names and current-work availability. Throws VirexQueryException on API/response failures.</summary>
    public Task<DataSourceCatalog> GetDataSourcesAsync(CancellationToken cancellationToken = default) =>
        GetDataSourceQueryAsync<DataSourceCatalog>(RestRoutes.ApiDataSources, ["appId", "instanceId", "timestamp", "sources"], cancellationToken, DataSourceJson.ReadCatalog);

    /// <summary>Reads current committed identity only, or no_current_result. Reading a value still requires explicit ResultId.</summary>
    public Task<ResultReference> GetCurrentResultAsync(CancellationToken cancellationToken = default) =>
        GetDataSourceQueryAsync<ResultReference>(RestRoutes.ApiCurrentResult, ["appId", "instanceId", "timestamp", "jobId", "captureId", "resultId"], cancellationToken, DataSourceJson.ReadResultReference);

    /// <summary>
    /// Reads an exact source name without a whitelist. Result sources require resultId; live sources forbid it.
    /// Returns an owned JSON snapshot. Concurrent calls use HttpClient safely; caller cancellation is preserved.
    /// Local blank selectors throw ArgumentException; typed query/API failures throw VirexQueryException.
    /// </summary>
    public Task<DataSourceValue> ReadDataSourceAsync(string name, string? resultId = null, CancellationToken cancellationToken = default) =>
        GetDataSourceQueryAsync<DataSourceValue>(RestRoutes.DataSourceValue(name, resultId), ["appId", "instanceId", "name", "kind", "type", "state", "timestamp", "value"], cancellationToken, root =>
        {
            var value = DataSourceJson.ReadValue(root);
            if (value.Name != name || value.ResultId != resultId || (value.Kind == DataSourceKinds.Result && resultId is null)
                || (value.Kind == DataSourceKinds.Live && resultId is not null))
                throw new JsonException("The source response does not match the exact query identity.");
            return value;
        });

    private async Task<T> GetDataSourceQueryAsync<T>(string route, string[] required, CancellationToken cancellationToken, Func<JsonElement, T>? reader = null)
    {
        try { return await GetQueryAsync<T>(route, required, cancellationToken, reader).ConfigureAwait(false); }
        catch (VirexClientException ex)
        {
            QueryError? error = null;
            try { error = ProtocolJson.Deserialize<QueryError>(ex.ResponseBody); } catch (JsonException) { }
            if (error is null || string.IsNullOrWhiteSpace(error.ErrorCode) || string.IsNullOrWhiteSpace(error.Message))
                error = new QueryError { ErrorCode = QueryErrorCodes.ApiFailed, Message = "The API did not return a typed query failure." };
            throw new VirexQueryException(ex.StatusCode, error, ex.ResponseBody, ex);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException)
        {
            throw new VirexQueryException(0, new QueryError { ErrorCode = QueryErrorCodes.ApiFailed, Message = "The API query or response failed." }, innerException: ex);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new VirexQueryException(0, new QueryError { ErrorCode = QueryErrorCodes.ApiFailed, Message = "The API query timed out or was cancelled by the transport." }, innerException: ex);
        }
    }

    public async Task<EventCatalog> GetEventCatalogAsync(CancellationToken cancellationToken = default)
    {
        var value = await GetQueryAsync<EventCatalog>(RestRoutes.ApiEvents, ["appId", "instanceId", "names"], cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(value.AppId) || string.IsNullOrWhiteSpace(value.InstanceId) || value.Names is null
            || value.Names.Any(string.IsNullOrWhiteSpace) || value.Names.Distinct(StringComparer.Ordinal).Count() != value.Names.Length)
            throw new InvalidDataException("Invalid event catalog response.");
        return value;
    }

    public Task<OperationModeInfo> GetOperationModeAsync(CancellationToken cancellationToken = default) =>
        GetAsync<OperationModeInfo>(RestRoutes.ApiOperationMode, cancellationToken);

    public Task<CommandResponse> SetOperationModeAsync(string mode, CancellationToken cancellationToken = default) =>
        PostAsync(RestRoutes.ApiOperationMode, new SetOperationModeRequest { Mode = mode }, cancellationToken);

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
        StartAsync(new SystemStartRequest(), cancellationToken);

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
        using var response = await _http.PostAsync(route.TrimStart('/'), null, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response).ConfigureAwait(false);
        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return ProtocolJson.Deserialize<CommandResponse>(json) ?? new CommandResponse();
    }

    private async Task<CommandResponse> PostAsync<T>(string route, T payload, CancellationToken cancellationToken)
    {
        using var response = await _http.PostAsync(route.TrimStart('/'), JsonContent(payload), cancellationToken).ConfigureAwait(false);
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
