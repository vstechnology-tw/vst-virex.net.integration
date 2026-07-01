using System.Text;
using MQTTnet;
using MQTTnet.Client;
using Virex.NET.Contracts;

namespace Virex.NET.Client;

public sealed class VirexMqttCommandClient
{
    private readonly VirexClientOptions _options;

    public VirexMqttCommandClient(VirexClientOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<SystemStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(MqttTopics.CommandStatusGet, new MqttCommandRequest(), cancellationToken).ConfigureAwait(false);
        return response.Status ?? new SystemStatus();
    }

    public async Task<ErrorInfo> GetErrorAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(MqttTopics.CommandErrorGet, new MqttCommandRequest(), cancellationToken).ConfigureAwait(false);
        return response.Error ?? new ErrorInfo();
    }

    public async Task<ProductInfo> GetProductInfoAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(MqttTopics.CommandProductInfoGet, new MqttCommandRequest(), cancellationToken).ConfigureAwait(false);
        return response.ProductInfo ?? new ProductInfo();
    }

    public Task<CommandResponse> SetProductInfoAsync(ProductInfo info, CancellationToken cancellationToken = default) =>
        SendCommandAsync(MqttTopics.CommandProductInfoSet, new MqttCommandRequest { ProductInfo = info }, cancellationToken);

    public Task<CommandResponse> InitializeAsync(CancellationToken cancellationToken = default) =>
        SendCommandAsync(MqttTopics.CommandSystemInitialize, new MqttCommandRequest(), cancellationToken);

    public Task<CommandResponse> DeinitializeAsync(CancellationToken cancellationToken = default) =>
        SendCommandAsync(MqttTopics.CommandSystemDeinitialize, new MqttCommandRequest(), cancellationToken);

    public Task<CommandResponse> StartAsync(string? condition = null, string? runMode = null, CancellationToken cancellationToken = default) =>
        SendCommandAsync(MqttTopics.CommandSystemStart, new MqttCommandRequest { Condition = condition, RunMode = runMode }, cancellationToken);

    public Task<CommandResponse> StopAsync(string? reason = null, CancellationToken cancellationToken = default) =>
        SendCommandAsync(MqttTopics.CommandSystemStop, new MqttCommandRequest { Reason = reason }, cancellationToken);

    public async Task<ResultList> QueryResultsAsync(
        string? lotID = null,
        string? waferID = null,
        string? recipe = null,
        CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(MqttTopics.CommandResultsQuery, new MqttCommandRequest { LotID = lotID, WaferID = waferID, Recipe = recipe }, cancellationToken).ConfigureAwait(false);
        return response.Results ?? new ResultList();
    }

    private async Task<CommandResponse> SendCommandAsync(string commandTopic, MqttCommandRequest request, CancellationToken cancellationToken)
    {
        var response = await SendAsync(commandTopic, request, cancellationToken).ConfigureAwait(false);
        return response.CommandResponse ?? new CommandResponse
        {
            Accepted = response.Accepted,
            ErrorCode = response.ErrorCode,
            Message = response.Message ?? string.Empty,
        };
    }

    private async Task<MqttCommandResponse> SendAsync(string commandTopic, MqttCommandRequest request, CancellationToken cancellationToken)
    {
        var factory = new MqttFactory();
        using var client = factory.CreateMqttClient();
        var correlationId = string.IsNullOrWhiteSpace(request.CorrelationId)
            ? Guid.NewGuid().ToString("N")
            : request.CorrelationId;
        request.CorrelationId = correlationId;

        var responseTopic = MqttTopics.ResponseTopic(_options.MqttTopic, correlationId);
        var received = new TaskCompletionSource<MqttCommandResponse>();
        client.ApplicationMessageReceivedAsync += e =>
        {
            if (e.ApplicationMessage.Topic == responseTopic)
            {
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment.ToArray());
                var response = ProtocolJson.Deserialize<MqttCommandResponse>(payload);
                if (response is not null)
                    received.TrySetResult(response);
            }

            return Task.CompletedTask;
        };

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(_options.MqttHost, _options.MqttPort)
            .WithCleanSession()
            .Build();
        await client.ConnectAsync(options, cancellationToken).ConfigureAwait(false);

        var subscribeOptions = factory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(f => f.WithTopic(responseTopic))
            .Build();
        await client.SubscribeAsync(subscribeOptions, cancellationToken).ConfigureAwait(false);

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(MqttTopics.Combine(_options.MqttTopic, commandTopic))
            .WithPayload(Encoding.UTF8.GetBytes(ProtocolJson.Serialize(request)))
            .Build();
        await client.PublishAsync(message, cancellationToken).ConfigureAwait(false);

        var timeoutMs = _options.TimeoutMs <= 0 ? 5000 : _options.TimeoutMs;
        var completed = await Task.WhenAny(received.Task, Task.Delay(timeoutMs, cancellationToken)).ConfigureAwait(false);
        if (completed != received.Task)
            throw new TimeoutException($"MQTT command response was not received within {timeoutMs} ms.");

        var result = await received.Task.ConfigureAwait(false);
        if (client.IsConnected)
            await client.DisconnectAsync(new MqttClientDisconnectOptions(), CancellationToken.None).ConfigureAwait(false);
        return result;
    }
}
