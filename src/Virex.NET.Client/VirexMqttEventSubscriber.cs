using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using Virex.NET.Contracts;

namespace Virex.NET.Client;

public sealed class VirexMqttEventSubscriber
{
    private readonly VirexClientOptions _options;

    public VirexMqttEventSubscriber(VirexClientOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public event EventHandler<VirexEvent>? EventReceived;

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var factory = new MqttFactory();
        using var client = factory.CreateMqttClient();
        client.ApplicationMessageReceivedAsync += e =>
        {
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment.ToArray());
            if (TryAttachTypeFromTopic(e.ApplicationMessage.Topic, payload, out var json) &&
                VirexEventParser.TryParse(json, out var value, out _))
            {
                EventReceived?.Invoke(this, value);
            }

            return Task.CompletedTask;
        };

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(_options.MqttHost, _options.MqttPort)
            .WithCleanSession()
            .Build();

        await client.ConnectAsync(options, cancellationToken).ConfigureAwait(false);
        var subscribeOptions = factory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(f => f.WithTopic(MqttTopics.Combine(_options.MqttTopic, "#")))
            .Build();
        await client.SubscribeAsync(subscribeOptions, cancellationToken).ConfigureAwait(false);

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            if (client.IsConnected)
                await client.DisconnectAsync(new MqttClientDisconnectOptions(), CancellationToken.None).ConfigureAwait(false);
        }
    }

    private static bool TryAttachTypeFromTopic(string topic, string payload, out string json)
    {
        json = payload;
        if (payload.IndexOf("\"type\"", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        var child = topic.TrimEnd('/').Substring(topic.TrimEnd('/').LastIndexOf('/') + 1);
        var type = child switch
        {
            "status" => "status",
            "wafer-info" => "waferInfo",
            "result" => "result",
            "error" => "error",
            _ => string.Empty,
        };

        if (string.IsNullOrWhiteSpace(type))
            return false;

        json = "{\"type\":\"" + type + "\"," + payload.Trim().TrimStart('{');
        return true;
    }
}
