using Virex.NET.Contracts;

namespace Virex.NET.Client;

public sealed class VirexClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;

    public VirexClient(VirexClientOptions options)
        : this(options, new HttpClient(), ownsHttpClient: true)
    {
    }

    public VirexClient(VirexClientOptions options, HttpClient httpClient)
        : this(options, httpClient, ownsHttpClient: false)
    {
    }

    private VirexClient(VirexClientOptions options, HttpClient httpClient, bool ownsHttpClient)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _ownsHttpClient = ownsHttpClient;
        _httpClient.BaseAddress = new Uri(EnsureTrailingSlash(options.RestBaseUrl), UriKind.Absolute);
        _httpClient.Timeout = TimeSpan.FromMilliseconds(options.TimeoutMs <= 0 ? 5000 : options.TimeoutMs);
        Rest = new VirexRestClient(_httpClient);
        TcpEvents = new VirexTcpEventClient(options);
        MqttEvents = new VirexMqttEventSubscriber(options);
    }

    public VirexClientOptions Options { get; }

    public VirexRestClient Rest { get; }

    public VirexTcpEventClient TcpEvents { get; }

    public VirexMqttEventSubscriber MqttEvents { get; }

    public Task<SystemStatus> GetStatusAsync(CancellationToken cancellationToken = default) =>
        Rest.GetStatusAsync(cancellationToken);

    public Task<ProductInfo> GetProductInfoAsync(CancellationToken cancellationToken = default) =>
        Rest.GetProductInfoAsync(cancellationToken);

    public Task<CommandResponse> SetProductInfoAsync(ProductInfo info, CancellationToken cancellationToken = default) =>
        Rest.SetProductInfoAsync(info, cancellationToken);

    public Task<CommandResponse> InitializeAsync(CancellationToken cancellationToken = default) =>
        Rest.InitializeAsync(cancellationToken);

    public Task<CommandResponse> DeinitializeAsync(CancellationToken cancellationToken = default) =>
        Rest.DeinitializeAsync(cancellationToken);

    public Task<CommandResponse> StartAsync(CancellationToken cancellationToken = default) =>
        Rest.StartAsync(cancellationToken);

    public Task<CommandResponse> StartAsync(string? condition, CancellationToken cancellationToken = default) =>
        Rest.StartAsync(condition, cancellationToken);

    public Task<CommandResponse> StartAsync(string? condition, string? runMode, CancellationToken cancellationToken = default) =>
        Rest.StartAsync(condition, runMode, cancellationToken);

    public Task<CommandResponse> StartAsync(SystemStartRequest request, CancellationToken cancellationToken = default) =>
        Rest.StartAsync(request, cancellationToken);

    public Task<CommandResponse> StopAsync(CancellationToken cancellationToken = default) =>
        Rest.StopAsync(cancellationToken);

    public Task<CommandResponse> StopAsync(string? reason, CancellationToken cancellationToken = default) =>
        Rest.StopAsync(reason, cancellationToken);

    public Task<CommandResponse> StopAsync(SystemStopRequest request, CancellationToken cancellationToken = default) =>
        Rest.StopAsync(request, cancellationToken);

    public Task<ResultList> QueryResultsAsync(
        string? lotID = null,
        string? waferID = null,
        string? recipe = null,
        CancellationToken cancellationToken = default) =>
        Rest.QueryResultsAsync(lotID, waferID, recipe, cancellationToken);

    public void Dispose()
    {
        if (_ownsHttpClient)
            _httpClient.Dispose();
    }

    private static string EnsureTrailingSlash(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? "http://127.0.0.1:5088/"
            : value.Trim().TrimEnd('/') + "/";
}
