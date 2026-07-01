using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;
using Virex.NET.Client;
using Virex.NET.Contracts;
using Virex.NET.Simulator.Core;
using Virex.NET.Simulator.WPF.Services;

namespace Virex.NET.Contracts.Tests;

public sealed class EmbeddedMqttBrokerTests
{
    [Fact]
    public async Task MqttSimulatorPublisherPublishesStateOnlyStatusEvent()
    {
        var port = GetFreeTcpPort();
        await using var broker = new EmbeddedMqttBroker(port);
        await broker.StartAsync();

        var factory = new MqttFactory();
        using var client = factory.CreateMqttClient();
        var receivedStatus = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.ApplicationMessageReceivedAsync += e =>
        {
            if (e.ApplicationMessage.Topic == MqttTopics.Combine(MqttTopics.DefaultBaseTopic, MqttTopics.StatusChanged))
            {
                receivedStatus.TrySetResult(Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment.ToArray()));
            }

            return Task.CompletedTask;
        };

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("127.0.0.1", port)
            .WithCleanSession()
            .Build();

        await client.ConnectAsync(options, CancellationToken.None);
        var subscribeOptions = factory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(f => f.WithTopic(MqttTopics.Combine(MqttTopics.DefaultBaseTopic, "#")))
            .Build();
        await client.SubscribeAsync(subscribeOptions, CancellationToken.None);

        var session = new SimulatorSession();
        var publisher = new MqttSimulatorPublisher(session, "127.0.0.1", port, MqttTopics.DefaultBaseTopic);
        await publisher.StartAsync();

        var payload = await receivedStatus.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Contains("\"state\":\"Uninitialized\"", payload);

        await publisher.StopAsync();
        await client.DisconnectAsync(new MqttClientDisconnectOptions(), CancellationToken.None);
    }

    [Fact]
    public async Task MqttSimulatorPublisherHandlesRestEquivalentCommandsWithCorrelatedResponses()
    {
        var port = GetFreeTcpPort();
        await using var broker = new EmbeddedMqttBroker(port);
        await broker.StartAsync();

        var session = new SimulatorSession();
        var publisher = new MqttSimulatorPublisher(session, "127.0.0.1", port, MqttTopics.DefaultBaseTopic);
        await publisher.StartAsync();

        var factory = new MqttFactory();
        using var client = factory.CreateMqttClient();
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.ApplicationMessageReceivedAsync += e =>
        {
            if (e.ApplicationMessage.Topic == MqttTopics.ResponseTopic(MqttTopics.DefaultBaseTopic, "status-1"))
            {
                received.TrySetResult(Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment.ToArray()));
            }

            return Task.CompletedTask;
        };

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("127.0.0.1", port)
            .WithCleanSession()
            .Build();

        await client.ConnectAsync(options, CancellationToken.None);
        var subscribeOptions = factory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(f => f.WithTopic(MqttTopics.ResponseTopic(MqttTopics.DefaultBaseTopic, "status-1")))
            .Build();
        await client.SubscribeAsync(subscribeOptions, CancellationToken.None);
        await PublishAsync(client, MqttTopics.Combine(MqttTopics.DefaultBaseTopic, MqttTopics.CommandStatusGet), """{"correlationId":"status-1"}""");

        var payload = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
        using var doc = JsonDocument.Parse(payload);
        Assert.Equal("status-1", doc.RootElement.GetProperty("correlationId").GetString());
        Assert.Equal(SystemStates.Uninitialized, doc.RootElement.GetProperty("status").GetProperty("state").GetString());

        await publisher.StopAsync();
        await client.DisconnectAsync(new MqttClientDisconnectOptions(), CancellationToken.None);
    }

    [Fact]
    public async Task MqttCommandClientGetsStatusThroughCommandTopic()
    {
        var port = GetFreeTcpPort();
        await using var broker = new EmbeddedMqttBroker(port);
        await broker.StartAsync();

        var session = new SimulatorSession();
        var publisher = new MqttSimulatorPublisher(session, "127.0.0.1", port, MqttTopics.DefaultBaseTopic);
        await publisher.StartAsync();

        var client = new VirexMqttCommandClient(new VirexClientOptions
        {
            MqttHost = "127.0.0.1",
            MqttPort = port,
            MqttTopic = MqttTopics.DefaultBaseTopic,
            TimeoutMs = 5000,
        });

        var status = await client.GetStatusAsync();

        Assert.Equal(SystemStates.Uninitialized, status.State);

        await publisher.StopAsync();
    }

    private static Task PublishAsync(IMqttClient client, string topic, string payload)
    {
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(Encoding.UTF8.GetBytes(payload))
            .Build();
        return client.PublishAsync(message, CancellationToken.None);
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
