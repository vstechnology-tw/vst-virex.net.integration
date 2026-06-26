using System;
using System.Net.Http;
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

    public Task<StatusDto> GetStatusAsync(CancellationToken cancellationToken = default) =>
        Rest.GetStatusAsync(cancellationToken);

    public Task SetWaferInfoAsync(WaferInfo info, CancellationToken cancellationToken = default) =>
        Rest.SetWaferInfoAsync(info, cancellationToken);

    public Task<ControlStatusDto> InitializeAsync(CancellationToken cancellationToken = default) =>
        Rest.InitializeAsync(cancellationToken);

    public Task<ControlStatusDto> TerminateAsync(CancellationToken cancellationToken = default) =>
        Rest.TerminateAsync(cancellationToken);

    public Task<ControlStatusDto> StartAsync(CancellationToken cancellationToken = default) =>
        Rest.StartAsync(cancellationToken);

    public Task<ControlStatusDto> StartAsync(string? condition, CancellationToken cancellationToken = default) =>
        Rest.StartAsync(condition, cancellationToken);

    public Task<ControlStatusDto> StartAsync(string? condition, string? runMode, CancellationToken cancellationToken = default) =>
        Rest.StartAsync(condition, runMode, cancellationToken);

    public Task<ControlStatusDto> StartAsync(ControlStartRequest request, CancellationToken cancellationToken = default) =>
        Rest.StartAsync(request, cancellationToken);

    public Task<ControlStatusDto> StopAsync(CancellationToken cancellationToken = default) =>
        Rest.StopAsync(cancellationToken);

    public Task<ControlStatusDto> StopAsync(string? reason, CancellationToken cancellationToken = default) =>
        Rest.StopAsync(reason, cancellationToken);

    public Task<ControlStatusDto> StopAsync(ControlStopRequest request, CancellationToken cancellationToken = default) =>
        Rest.StopAsync(request, cancellationToken);

    public Task<ResultListDto> QueryResultsAsync(
        string? lotId = null,
        string? waferId = null,
        string? recipeId = null,
        CancellationToken cancellationToken = default) =>
        Rest.QueryResultsAsync(lotId, waferId, recipeId, cancellationToken);

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
