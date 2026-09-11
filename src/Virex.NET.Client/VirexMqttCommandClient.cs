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
        ThrowIfRejected(response);
        return response.Status ?? throw new InvalidDataException("MQTT response omitted status.");
    }

    public async Task<ErrorInfo> GetErrorAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(MqttTopics.CommandErrorGet, new MqttCommandRequest(), cancellationToken).ConfigureAwait(false);
        ThrowIfRejected(response);
        return response.Error ?? throw new InvalidDataException("MQTT response omitted error information.");
    }

    public async Task<ProductInfo> GetProductInfoAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(MqttTopics.CommandProductInfoGet, new MqttCommandRequest(), cancellationToken).ConfigureAwait(false);
        ThrowIfRejected(response);
        return response.ProductInfo ?? throw new InvalidDataException("MQTT response omitted ProductInfo.");
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
        ThrowIfRejected(response);
        return response.Results ?? throw new InvalidDataException("MQTT response omitted results.");
    }

    private async Task<CommandResponse> SendCommandAsync(string commandTopic, MqttCommandRequest request, CancellationToken cancellationToken)
    {
        var response = await SendAsync(commandTopic, request, cancellationToken).ConfigureAwait(false);
        if (response.CommandResponse is not null) return response.CommandResponse;
        ThrowIfRejected(response);
        throw new InvalidDataException("MQTT response omitted the command response.");
    }

    private static void ThrowIfRejected(MqttCommandResponse response)
    {
        if (!response.Accepted)
            throw new VirexCommandException(response);
    }

    private async Task<MqttCommandResponse> SendAsync(string commandTopic, MqttCommandRequest request, CancellationToken cancellationToken)
    {
        var timeoutMs = _options.TimeoutMs <= 0 ? 5000 : _options.TimeoutMs;
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeoutMs);
        try
        {
            var factory = new MqttFactory();
            using var client = factory.CreateMqttClient();
            var correlationId = string.IsNullOrWhiteSpace(request.CorrelationId)
                ? Guid.NewGuid().ToString("N")
                : request.CorrelationId;
            request.CorrelationId = correlationId;

            var responseTopic = MqttTopics.ResponseTopic(_options.MqttTopic, correlationId);
            var received = new TaskCompletionSource<MqttCommandResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            client.ApplicationMessageReceivedAsync += e =>
            {
                if (e.ApplicationMessage.Topic == responseTopic)
                {
                    try
                    {
                        var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment.ToArray());
                        var response = CommandPayloadJson.ReadObject<MqttCommandResponse>(payload)
                            ?? throw new InvalidDataException("Empty MQTT response.");
                        received.TrySetResult(response);
                    }
                    catch (Exception ex) { received.TrySetException(new InvalidDataException("Invalid MQTT response.", ex)); }
                }

                return Task.CompletedTask;
            };

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(_options.MqttHost, _options.MqttPort)
                .WithCleanSession()
                .Build();
            await client.ConnectAsync(options, deadline.Token).ConfigureAwait(false);

            var subscribeOptions = factory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(f => f.WithTopic(responseTopic))
                .Build();
            await client.SubscribeAsync(subscribeOptions, deadline.Token).ConfigureAwait(false);

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(MqttTopics.Combine(_options.MqttTopic, commandTopic))
                .WithPayload(Encoding.UTF8.GetBytes(ProtocolJson.Serialize(request)))
                .Build();
            await client.PublishAsync(message, deadline.Token).ConfigureAwait(false);

            var completed = await Task.WhenAny(received.Task, Task.Delay(Timeout.Infinite, deadline.Token)).ConfigureAwait(false);
            if (completed != received.Task)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw new TimeoutException($"MQTT command response was not received within {timeoutMs} ms.");
            }

            var result = await received.Task.ConfigureAwait(false);
            if (client.IsConnected)
            {
                try { await client.DisconnectAsync(new MqttClientDisconnectOptions(), deadline.Token).ConfigureAwait(false); }
                catch { /* A completed response must not become a failure during connection cleanup. */ }
            }
            return result;
        }
        catch (Exception ex) when (deadline.IsCancellationRequested && ex is not VirexCommandException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new TimeoutException($"MQTT command response was not received within {timeoutMs} ms.", ex);
        }
    }
}
