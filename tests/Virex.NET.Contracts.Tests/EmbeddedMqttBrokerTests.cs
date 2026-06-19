using System.Net;
using System.Net.Sockets;
using System.Text;
using MQTTnet;
using MQTTnet.Client;
using Virex.NET.Contracts;
using Virex.NET.Simulator.WPF.Services;

namespace Virex.NET.Contracts.Tests;

public sealed class EmbeddedMqttBrokerTests
{
    [Fact]
    public async Task StartAsyncAllowsMqttClientsToConnect()
    {
        var port = GetFreeTcpPort();
        await using var broker = new EmbeddedMqttBroker(port);

        await broker.StartAsync();

        var factory = new MqttFactory();
        using var client = factory.CreateMqttClient();
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("127.0.0.1", port)
            .WithCleanSession()
            .Build();

        var result = await client.ConnectAsync(options, CancellationToken.None);

        Assert.True(client.IsConnected);
        Assert.Equal(MqttClientConnectResultCode.Success, result.ResultCode);

        await client.DisconnectAsync(new MqttClientDisconnectOptions(), CancellationToken.None);
    }

    [Fact]
    public async Task MqttSimulatorPublisherPublishesThroughEmbeddedBroker()
    {
        var port = GetFreeTcpPort();
        await using var broker = new EmbeddedMqttBroker(port);
        await broker.StartAsync();

        var factory = new MqttFactory();
        using var client = factory.CreateMqttClient();
        var receivedStatus = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.ApplicationMessageReceivedAsync += e =>
        {
            if (e.ApplicationMessage.Topic == MqttTopics.Combine(MqttTopics.DefaultBaseTopic, MqttTopics.Status))
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

        Assert.Contains("\"processState\":\"ready\"", payload);

        await publisher.StopAsync();
        await client.DisconnectAsync(new MqttClientDisconnectOptions(), CancellationToken.None);
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
