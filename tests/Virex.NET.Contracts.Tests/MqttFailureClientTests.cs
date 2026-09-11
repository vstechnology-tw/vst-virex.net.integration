using System.Net;
using System.Net.Sockets;
using System.Text;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Server;
using Virex.NET.Client;
using Virex.NET.Contracts;

namespace Virex.NET.Contracts.Tests;

public sealed class MqttFailureClientTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedQueriesNeverBecomeEmptySuccess(bool malformedResponse)
    {
        using var reservation = new TcpListener(IPAddress.Loopback, 0);
        reservation.Start();
        var port = ((IPEndPoint)reservation.LocalEndpoint).Port;
        reservation.Stop();
        using var broker = new MqttFactory().CreateMqttServer(new MqttServerOptionsBuilder()
            .WithDefaultEndpoint().WithDefaultEndpointBoundIPAddress(IPAddress.Loopback).WithDefaultEndpointPort(port).Build());
        await broker.StartAsync();
        using var responder = new MqttFactory().CreateMqttClient();
        responder.ApplicationMessageReceivedAsync += async args =>
        {
            var request = ProtocolJson.Deserialize<MqttCommandRequest>(Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment.ToArray()))!;
            var response = malformedResponse ? "{" : ProtocolJson.Serialize(new MqttCommandResponse
            {
                CorrelationId = request.CorrelationId,
                Accepted = false,
                ErrorCode = "query_failed",
                Message = "Query failed.",
            });
            await responder.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic(MqttTopics.ResponseTopic("test", request.CorrelationId)).WithPayload(response).Build());
        };
        await responder.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", port).Build());
        await responder.SubscribeAsync(new MqttFactory().CreateSubscribeOptionsBuilder().WithTopicFilter("test/commands/#").Build());
        var client = new VirexMqttCommandClient(new VirexClientOptions { MqttHost = "127.0.0.1", MqttPort = port, MqttTopic = "test", TimeoutMs = 1000 });
        try
        {
            if (malformedResponse)
                await Assert.ThrowsAsync<InvalidDataException>(() => client.QueryResultsAsync());
            else
            {
                var failure = await Assert.ThrowsAsync<VirexCommandException>(() => client.QueryResultsAsync());
                Assert.Equal("query_failed", failure.ErrorCode);
                Assert.Null(failure.Response); // No fabricated lifecycle state for an older response envelope.
            }
        }
        finally { await responder.DisconnectAsync(); await broker.StopAsync(); }
    }
}
