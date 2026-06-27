using System.Net;
using MQTTnet;
using MQTTnet.Server;

namespace Virex.NET.Simulator.WPF.Services;

public sealed class EmbeddedMqttBroker : IAsyncDisposable
{
    private readonly IPAddress _bindAddress;
    private readonly int _port;
    private MqttServer? _server;

    public EmbeddedMqttBroker(int port)
        : this(IPAddress.Loopback, port)
    {
    }

    public EmbeddedMqttBroker(string host, int port)
        : this(ParseBindAddress(host), port)
    {
    }

    private EmbeddedMqttBroker(IPAddress bindAddress, int port)
    {
        if (port <= 0 || port > 65535)
            throw new ArgumentOutOfRangeException(nameof(port), "MQTT broker port must be between 1 and 65535.");

        _bindAddress = bindAddress;
        _port = port;
    }

    public bool IsStarted => _server?.IsStarted == true;

    public async Task StartAsync()
    {
        if (IsStarted)
            return;

        var factory = new MqttFactory();
        var options = new MqttServerOptionsBuilder()
            .WithDefaultEndpoint()
            .WithDefaultEndpointBoundIPAddress(_bindAddress)
            .WithDefaultEndpointPort(_port)
            .Build();

        _server = factory.CreateMqttServer(options);
        await _server.StartAsync().ConfigureAwait(false);
    }

    public async Task StopAsync()
    {
        if (_server is null)
            return;

        if (_server.IsStarted)
        {
            var options = new MqttServerStopOptionsBuilder().Build();
            await _server.StopAsync(options).ConfigureAwait(false);
        }

        _server = null;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }

    private static IPAddress ParseBindAddress(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return IPAddress.Loopback;

        var trimmed = host.Trim();
        if (trimmed.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            return IPAddress.Loopback;

        if (IPAddress.TryParse(trimmed, out var address))
            return address;

        return IPAddress.Loopback;
    }
}
