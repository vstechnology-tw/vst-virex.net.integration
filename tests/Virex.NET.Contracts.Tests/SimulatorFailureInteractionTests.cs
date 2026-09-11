using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using Virex.NET.Contracts;
using Virex.NET.Simulator.Core;
using Virex.NET.Simulator.WPF.Services;

namespace Virex.NET.Contracts.Tests;

public sealed class SimulatorFailureInteractionTests
{
    [Theory]
    [InlineData("{")]
    [InlineData("null")]
    [InlineData("[]")]
    public async Task MalformedStartReturnsFailureWithoutStarting(string payload)
    {
        var session = new SimulatorSession();
        await session.InitializeAsync();
        using var port = new TcpListener(IPAddress.Loopback, 0);
        port.Start();
        var endpoint = "http://127.0.0.1:" + ((IPEndPoint)port.LocalEndpoint).Port + "/";
        port.Stop();
        var server = new RestSimulatorServer(session, endpoint);
        await server.StartAsync();
        try
        {
            var rejections = new List<CommandResponse>();
            session.CommandRejected += (_, response) => rejections.Add(response);
            using var client = new HttpClient { BaseAddress = new Uri(endpoint) };
            using var response = await client.PostAsync(RestRoutes.ApiSystemStart, new StringContent(payload, Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(SimulatorState.Ready, session.State);
            Assert.Equal(CommandErrorCodes.InvalidPayload, Assert.Single(rejections).ErrorCode);
        }
        finally { await server.StopAsync(); }
    }

    [Fact]
    public async Task ProductInfoCompletionEventIsReadyAndConcurrentStartIsRejected()
    {
        var session = new SimulatorSession();
        await session.InitializeAsync();
        SimulatorState? completionState = null;
        session.ProductInfoChanged += (_, _) => completionState = session.State;
        var update = session.SetProductInfoAsync(new ProductInfo { WaferID = "NEW" });
        Assert.Equal(SimulatorState.UpdatingProductInfo, session.State);
        var start = await session.StartAsync(new SystemStartRequest());
        Assert.False(start.Accepted);
        Assert.True((await update).Accepted);
        Assert.Equal(SimulatorState.Ready, completionState);
    }
}
