using System.Text;
using Mediator;
using MQTTnet;
using MQTTnet.Client;
using Virex.NET.Contracts;
using Virex.NET.Simulator.Core;

namespace Virex.NET.Simulator.WPF.Services;

public sealed class MqttSimulatorPublisher :
    INotificationHandler<StatusChangedEvent>,
    INotificationHandler<ProductInfoChangedEvent>,
    INotificationHandler<ResultCreatedEvent>,
    INotificationHandler<CommandRejectedEvent>,
    INotificationHandler<InitializationCompletedEvent>,
    INotificationHandler<ProductInfoUpdateCompletedEvent>,
    INotificationHandler<RunCompletedEvent>,
    INotificationHandler<DeinitializationCompletedEvent>
{
    private readonly SimulatorSession _session;
    private readonly string _host;
    private readonly int _port;
    private readonly string _topic;
    private IMqttClient? _client;
    private bool _runActive;

    public MqttSimulatorPublisher(SimulatorSession session, string host, int port, string topic)
    {
        _session = session;
        _host = string.IsNullOrWhiteSpace(host) ? "127.0.0.1" : host.Trim();
        _port = port;
        _topic = string.IsNullOrWhiteSpace(topic) ? MqttTopics.DefaultBaseTopic : topic.Trim();
    }

    public async Task StartAsync()
    {
        var factory = new MqttFactory();
        _client = factory.CreateMqttClient();
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(_host, _port)
            .WithCleanSession()
            .Build();

        await _client.ConnectAsync(options, CancellationToken.None).ConfigureAwait(false);
        _client.ApplicationMessageReceivedAsync += HandleCommandAsync;
        var subscribeOptions = factory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(f => f.WithTopic(MqttTopics.Combine(_topic, MqttTopics.Commands + "/#")))
            .Build();
        await _client.SubscribeAsync(subscribeOptions, CancellationToken.None).ConfigureAwait(false);
        _session.StatusChanged += OnStatusChanged;
        _session.ProductInfoChanged += OnProductInfoChanged;
        _session.ResultCreated += OnResultCreated;
        _session.ErrorChanged += OnErrorChanged;
        _session.CommandRejected += OnCommandRejected;
        _session.WriteLog($"MQTT connected to {_host}:{_port}, topic={_topic}");

        await PublishAsync(MqttTopics.StatusChanged, ProtocolJson.Serialize(_session.Status)).ConfigureAwait(false);
        await PublishAsync(MqttTopics.ProductInfoChanged, ProtocolJson.Serialize(_session.ProductInfo)).ConfigureAwait(false);
    }

    public async Task StopAsync()
    {
        _session.StatusChanged -= OnStatusChanged;
        _session.ProductInfoChanged -= OnProductInfoChanged;
        _session.ResultCreated -= OnResultCreated;
        _session.ErrorChanged -= OnErrorChanged;
        _session.CommandRejected -= OnCommandRejected;
        if (_client is not null && _client.IsConnected)
        {
            _client.ApplicationMessageReceivedAsync -= HandleCommandAsync;
            await _client.DisconnectAsync(new MqttClientDisconnectOptions(), CancellationToken.None).ConfigureAwait(false);
        }
    }

    public ValueTask Handle(StatusChangedEvent notification, CancellationToken cancellationToken) =>
        new ValueTask(PublishAsync(MqttTopics.StatusChanged, ProtocolJson.Serialize(notification.Status)));

    public ValueTask Handle(ProductInfoChangedEvent notification, CancellationToken cancellationToken) =>
        new ValueTask(PublishAsync(MqttTopics.ProductInfoChanged, ProtocolJson.Serialize(notification.ProductInfo)));

    public ValueTask Handle(ResultCreatedEvent notification, CancellationToken cancellationToken) =>
        new ValueTask(PublishAsync(MqttTopics.ResultCreated, ProtocolJson.Serialize(notification.Result)));

    public ValueTask Handle(CommandRejectedEvent notification, CancellationToken cancellationToken) =>
        new ValueTask(PublishAsync(MqttTopics.CommandRejected, ProtocolJson.Serialize(notification.Response)));

    public ValueTask Handle(InitializationCompletedEvent notification, CancellationToken cancellationToken) =>
        default;

    public ValueTask Handle(ProductInfoUpdateCompletedEvent notification, CancellationToken cancellationToken) =>
        default;

    public ValueTask Handle(RunCompletedEvent notification, CancellationToken cancellationToken) =>
        default;

    public ValueTask Handle(DeinitializationCompletedEvent notification, CancellationToken cancellationToken) =>
        default;

    private void OnStatusChanged(object? sender, SystemStatus status)
    {
        _ = PublishAsync(MqttTopics.StatusChanged, ProtocolJson.Serialize(status));

        if (string.Equals(status.State, SystemStates.Running, StringComparison.OrdinalIgnoreCase))
        {
            _runActive = true;
            _ = PublishAsync(MqttTopics.RunStarted, ProtocolJson.Serialize(status));
        }

        if (_runActive && string.Equals(status.State, SystemStates.Ready, StringComparison.OrdinalIgnoreCase))
        {
            _runActive = false;
            _ = PublishAsync(MqttTopics.RunCompleted, ProtocolJson.Serialize(status));
        }
    }

    private void OnProductInfoChanged(object? sender, ProductInfo info) =>
        _ = PublishAsync(MqttTopics.ProductInfoChanged, ProtocolJson.Serialize(info));

    private void OnResultCreated(object? sender, ResultSummary result) =>
        _ = PublishAsync(MqttTopics.ResultCreated, ProtocolJson.Serialize(result));

    private void OnErrorChanged(object? sender, ErrorInfo error) =>
        _ = PublishAsync(MqttTopics.ErrorChanged, ProtocolJson.Serialize(error));

    private void OnCommandRejected(object? sender, CommandResponse response) =>
        _ = PublishAsync(MqttTopics.CommandRejected, ProtocolJson.Serialize(response));

    private async Task HandleCommandAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        var topic = e.ApplicationMessage.Topic;
        var childTopic = ChildTopic(topic);
        if (!childTopic.StartsWith(MqttTopics.Commands + "/", StringComparison.OrdinalIgnoreCase))
            return;

        var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment.ToArray());
        var request = string.IsNullOrWhiteSpace(payload)
            ? new MqttCommandRequest()
            : ProtocolJson.Deserialize<MqttCommandRequest>(payload) ?? new MqttCommandRequest();
        if (string.IsNullOrWhiteSpace(request.CorrelationId))
            request.CorrelationId = Guid.NewGuid().ToString("N");

        var response = await ExecuteCommandAsync(childTopic, payload, request).ConfigureAwait(false);
        await PublishResponseAsync(request.CorrelationId, response).ConfigureAwait(false);
    }

    private async Task<MqttCommandResponse> ExecuteCommandAsync(string childTopic, string payload, MqttCommandRequest request)
    {
        var response = new MqttCommandResponse
        {
            CorrelationId = request.CorrelationId,
            Topic = childTopic,
        };

        if (childTopic == MqttTopics.CommandStatusGet)
            response.Status = _session.Status;
        else if (childTopic == MqttTopics.CommandErrorGet)
            response.Error = _session.Error;
        else if (childTopic == MqttTopics.CommandProductInfoGet)
            response.ProductInfo = _session.ProductInfo;
        else if (childTopic == MqttTopics.CommandProductInfoSet)
            response.CommandResponse = await _session.SetProductInfoAsync(ReadProductInfo(payload, request)).ConfigureAwait(false);
        else if (childTopic == MqttTopics.CommandSystemInitialize)
            response.CommandResponse = await _session.InitializeAsync().ConfigureAwait(false);
        else if (childTopic == MqttTopics.CommandSystemDeinitialize)
            response.CommandResponse = await _session.DeinitializeAsync().ConfigureAwait(false);
        else if (childTopic == MqttTopics.CommandSystemStart)
            response.CommandResponse = await _session.StartAsync(new SystemStartRequest
            {
                Condition = request.Condition,
                RunMode = request.RunMode,
            }).ConfigureAwait(false);
        else if (childTopic == MqttTopics.CommandSystemStop)
            response.CommandResponse = await _session.StopAsync(new SystemStopRequest { Reason = request.Reason }).ConfigureAwait(false);
        else if (childTopic == MqttTopics.CommandResultsQuery)
        {
            var items = _session.QueryResults(request.LotID, request.WaferID, request.Recipe);
            response.Results = new ResultList { Items = items, Count = items.Length };
        }
        else
        {
            response.Accepted = false;
            response.ErrorCode = "unknown_topic";
            response.Message = "Unknown MQTT command topic.";
        }

        if (response.CommandResponse is not null)
            response.Accepted = response.CommandResponse.Accepted;

        return response;
    }

    private static ProductInfo ReadProductInfo(string payload, MqttCommandRequest request)
    {
        if (request.ProductInfo is not null)
            return request.ProductInfo;

        return ProductInfoJsonParser.TryParse(payload, out var info, out _)
            ? info
            : new ProductInfo();
    }

    private string ChildTopic(string topic)
    {
        var root = string.IsNullOrWhiteSpace(_topic) ? MqttTopics.DefaultBaseTopic : _topic.Trim();
        var prefix = root.TrimEnd('/') + "/";
        return topic.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? topic.Substring(prefix.Length)
            : topic;
    }

    private async Task PublishAsync(string childTopic, string payload)
    {
        if (_client is null || !_client.IsConnected)
            return;

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(MqttTopics.Combine(_topic, childTopic))
            .WithPayload(Encoding.UTF8.GetBytes(payload))
            .Build();

        await _client.PublishAsync(message, CancellationToken.None).ConfigureAwait(false);
    }

    private Task PublishResponseAsync(string correlationId, MqttCommandResponse response) =>
        PublishRawAsync(MqttTopics.ResponseTopic(_topic, correlationId), ProtocolJson.Serialize(response));

    private async Task PublishRawAsync(string topic, string payload)
    {
        if (_client is null || !_client.IsConnected)
            return;

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(Encoding.UTF8.GetBytes(payload))
            .Build();

        await _client.PublishAsync(message, CancellationToken.None).ConfigureAwait(false);
    }
}
