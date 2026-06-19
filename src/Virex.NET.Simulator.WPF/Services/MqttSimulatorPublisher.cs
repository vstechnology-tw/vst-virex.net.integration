using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using Virex.NET.Contracts;

namespace Virex.NET.Simulator.WPF.Services;

public sealed class MqttSimulatorPublisher
{
    private readonly SimulatorSession _session;
    private readonly string _host;
    private readonly int _port;
    private readonly string _topic;
    private IMqttClient? _client;

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
        _session.WaferInfoChanged += OnWaferInfoChanged;
        _session.ResultCreated += OnResultCreated;
        _session.ErrorChanged += OnErrorChanged;
        _session.WriteLog($"MQTT connected to {_host}:{_port}, topic={_topic}");

        await PublishAsync(MqttTopics.Status, ProtocolJson.Serialize(_session.Status)).ConfigureAwait(false);
        await PublishAsync(MqttTopics.WaferInfo, ProtocolJson.Serialize(_session.WaferInfo)).ConfigureAwait(false);
    }

    public async Task StopAsync()
    {
        _session.StatusChanged -= OnStatusChanged;
        _session.WaferInfoChanged -= OnWaferInfoChanged;
        _session.ResultCreated -= OnResultCreated;
        _session.ErrorChanged -= OnErrorChanged;
        if (_client is not null && _client.IsConnected)
            await _client.DisconnectAsync(new MqttClientDisconnectOptions(), CancellationToken.None).ConfigureAwait(false);
    }

    private void OnStatusChanged(object? sender, StatusDto status) =>
        _ = PublishAsync(MqttTopics.Status, ProtocolJson.Serialize(status));

    private void OnWaferInfoChanged(object? sender, WaferInfo info) =>
        _ = PublishAsync(MqttTopics.WaferInfo, ProtocolJson.Serialize(info));

    private void OnResultCreated(object? sender, ResultSummaryDto result) =>
        _ = PublishAsync(MqttTopics.Result, ProtocolJson.Serialize(result));

    private void OnErrorChanged(object? sender, ErrorStatusDto error) =>
        _ = PublishAsync(MqttTopics.Error, ProtocolJson.Serialize(error));

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
