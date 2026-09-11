using System.Net;
using System.Net.Sockets;
using System.Text;
using Virex.NET.Client;
using Virex.NET.Contracts;

namespace Virex.NET.Contracts.Tests;

public sealed class CommunicationFailureContractTests
{
    [Fact]
    public void RequiredProductStringsCannotBeNull()
    {
        Assert.Throws<System.Text.Json.JsonException>(() => CommandPayloadJson.ReadObject<ProductInfo>("{\"waferID\":null}"));
        Assert.Throws<System.Text.Json.JsonException>(() => CommandPayloadJson.ReadObject<MqttCommandRequest>("{\"productInfo\":{\"lotID\":null}}"));
    }

    [Fact]
    public void ProductInfoCommandUsesCommandTypeAndRetainsLegacyAlias()
    {
        var frame = TcpSocketEventFormatter.FormatProductInfoCommand(new ProductInfo { WaferID = "W" });
        Assert.Contains("\"type\":\"productInfo\"", frame);
        Assert.True(TcpSocketMessageParser.TryParse(frame, out var command, out _));
        Assert.Equal("W", command.ProductInfo!.WaferID);
        Assert.True(TcpSocketMessageParser.TryParse(TcpSocketEventFormatter.FormatProductInfo(new ProductInfo { WaferID = "OLD" }), out var legacy, out _));
        Assert.Equal("productInfo", legacy.Type);
    }

    [Fact]
    public async Task TcpQueryIgnoresAnotherRequestsRejection()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var server = Task.Run(async () =>
        {
            using var peer = await listener.AcceptTcpClientAsync(deadline.Token);
            using var reader = new StreamReader(peer.GetStream());
            using var writer = new StreamWriter(peer.GetStream(), new UTF8Encoding(false)) { AutoFlush = true };
            var request = await reader.ReadLineAsync(deadline.Token);
            var id = CommandPayloadJson.TryReadRequestId(request);
            Assert.False(string.IsNullOrWhiteSpace(id));
            await writer.WriteAsync(TcpSocketEventFormatter.FormatCommandRejected(new CommandResponse
            {
                Command = "QueryResults",
                RequestId = "another-request",
                State = SystemStates.Ready,
                ErrorCode = "query_failed",
                Message = "Another request failed.",
            }));
            await writer.WriteAsync(CommandPayloadJson.WithRequestId(TcpSocketEventFormatter.FormatResults(new ResultList()), id));
        });
        var client = new VirexTcpEventClient(new VirexClientOptions
        {
            TcpHost = "127.0.0.1",
            TcpPort = ((IPEndPoint)listener.LocalEndpoint).Port,
            TimeoutMs = 1000,
        });
        Assert.Equal(0, (await client.QueryResultsAsync(cancellationToken: deadline.Token)).Count);
        await server;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ErrorEventsPreserveActiveErrorFlag(bool hasError)
    {
        var frame = TcpSocketEventFormatter.FormatError(new ErrorInfo { HasError = hasError, State = SystemStates.Ready });
        Assert.Contains("\"hasError\":", frame);
        Assert.True(VirexEventParser.TryParse(frame, out var parsed, out _));
        Assert.Equal(hasError, parsed.Error!.HasError);
    }

    [Fact]
    public void InvalidRunModeReachesTheCommandGuardWithoutBecomingDefaultMode()
    {
        Assert.True(TcpSocketMessageParser.TryParse("{\"type\":\"start\",\"runMode\":\"invalid-mode\"}", out var message, out _));
        Assert.False(ControlRunModes.TryNormalize(message.RunMode, out _));
    }

    [Fact]
    public void UnknownCommandCannotBecomeAProductInfoUpdate()
    {
        Assert.False(TcpSocketMessageParser.TryParse("{\"type\":\"unknown\",\"lotID\":\"DO-NOT-APPLY\"}", out _, out _));
    }

    [Fact]
    public async Task TcpQuerySurfacesRejectionBeforeCallerCancellation()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var server = Task.Run(async () =>
        {
            using var peer = await listener.AcceptTcpClientAsync(deadline.Token);
            using var reader = new StreamReader(peer.GetStream());
            using var writer = new StreamWriter(peer.GetStream(), new UTF8Encoding(false)) { AutoFlush = true };
            var request = await reader.ReadLineAsync(deadline.Token);
            Assert.NotNull(request);
            await writer.WriteAsync(TcpSocketEventFormatter.FormatCommandRejected(new CommandResponse
            {
                Command = "QueryResults",
                State = SystemStates.Ready,
                ErrorCode = "query_failed",
                Message = "Query failed.",
                RequestId = CommandPayloadJson.TryReadRequestId(request),
            }));
            try { await Task.Delay(1500, deadline.Token); } catch (OperationCanceledException) { }
        });
        var client = new VirexTcpEventClient(new VirexClientOptions
        {
            TcpHost = "127.0.0.1",
            TcpPort = ((IPEndPoint)listener.LocalEndpoint).Port,
            TimeoutMs = 500,
        });
        try
        {
            var error = await Record.ExceptionAsync(() => client.QueryResultsAsync(cancellationToken: deadline.Token));
            Assert.NotNull(error);
            Assert.Equal("VirexCommandException", error.GetType().Name);
        }
        finally { deadline.Cancel(); await server; }
    }
}
