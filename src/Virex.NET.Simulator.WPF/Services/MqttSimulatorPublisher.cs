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
            await _client.DisconnectAsync(new MqttClientDisconnectOptions(), CancellationToken.None).ConfigureAwait(false);
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
}
