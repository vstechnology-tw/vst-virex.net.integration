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
    [Fact]
    public async Task MqttPublishesImageGrabbedWithoutPathAndCorrelatesResult()
    {
        var root = Path.Combine(Path.GetTempPath(), "virex-mqtt-image-grabbed-" + Guid.NewGuid().ToString("N"));
        var port = GetFreeTcpPort();

        await using var broker = new EmbeddedMqttBroker(port);
        await broker.StartAsync();

        var factory = new MqttFactory();
        using var client = factory.CreateMqttClient();
        var imageReceived = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var resultReceived = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var imageDomainLogged = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var resultDomainLogged = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var imageMqttLogged = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var resultMqttLogged = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.ApplicationMessageReceivedAsync += e =>
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment.ToArray());
            if (topic == MqttTopics.Combine(MqttTopics.DefaultBaseTopic, MqttTopics.ImageGrabbed))
                imageReceived.TrySetResult(payload);
            if (topic == MqttTopics.Combine(MqttTopics.DefaultBaseTopic, MqttTopics.ResultCreated))
                resultReceived.TrySetResult(payload);

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

        var session = new SimulatorSession(root);
        var publisher = new MqttSimulatorPublisher(session, "127.0.0.1", port, MqttTopics.DefaultBaseTopic);
        session.Log += (_, message) =>
        {
            if (message.StartsWith("Event: imageGrabbed", StringComparison.Ordinal))
                imageDomainLogged.TrySetResult(true);
            if (message.StartsWith("Event: resultCreated", StringComparison.Ordinal))
                resultDomainLogged.TrySetResult(true);
            if (message.Contains("MQTT event published: topic=virex/imageGrabbed", StringComparison.Ordinal))
                imageMqttLogged.TrySetResult(true);
            if (message.Contains("MQTT event published: topic=virex/resultCreated", StringComparison.Ordinal))
                resultMqttLogged.TrySetResult(true);
        };
        await publisher.StartAsync();

        try
        {
            await session.InitializeAsync();
            await session.StartAsync(new SystemStartRequest { RunMode = ControlRunModes.SingleRun });
            await session.RunCompletedAsync();

            await Task.WhenAll(
                imageDomainLogged.Task,
                resultDomainLogged.Task,
                imageMqttLogged.Task,
                resultMqttLogged.Task).WaitAsync(TimeSpan.FromSeconds(5));

            var imagePayload = await imageReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var resultPayload = await resultReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
            using var imageDocument = JsonDocument.Parse(imagePayload);
            using var resultDocument = JsonDocument.Parse(resultPayload);

            Assert.False(imageDocument.RootElement.TryGetProperty("imagePath", out _));
            Assert.False(imageDocument.RootElement.TryGetProperty("resultPath", out _));

            var captureId = imageDocument.RootElement.GetProperty("captureId").GetString();
            Assert.Equal(captureId, resultDocument.RootElement.GetProperty("captureId").GetString());
        }
        finally
        {
            await publisher.StopAsync();
            await client.DisconnectAsync(new MqttClientDisconnectOptions(), CancellationToken.None);
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}
