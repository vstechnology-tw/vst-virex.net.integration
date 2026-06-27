using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using Virex.NET.Contracts;
using Virex.NET.Simulator.Core;
using Virex.NET.Simulator.WPF.Services;

namespace Virex.NET.Contracts.Tests;

public sealed class RestSimulatorServerTests
{
    [Fact]
    public async Task OpenApiExposesProductInfoAndSystemRoutes()
    {
        var session = new SimulatorSession();
        var prefix = "http://127.0.0.1:" + GetFreeTcpPort() + "/";
        var server = new RestSimulatorServer(session, prefix);
        await server.StartAsync();

        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(prefix) };

            var openApi = await client.GetStringAsync(RestRoutes.OpenApiJson);
            using var document = JsonDocument.Parse(openApi);
            var paths = document.RootElement.GetProperty("paths");

            Assert.True(paths.TryGetProperty(RestRoutes.ApiProductInfo, out _));
            Assert.True(paths.TryGetProperty(RestRoutes.ApiSystemInitialize, out _));
            Assert.True(paths.TryGetProperty(RestRoutes.ApiSystemDeinitialize, out _));
            Assert.True(paths.TryGetProperty(RestRoutes.ApiSystemStart, out _));
            Assert.True(paths.TryGetProperty(RestRoutes.ApiSystemStop, out _));
        }
        finally
        {
            await server.StopAsync();
        }
    }

    [Fact]
    public async Task ProductInfoRequiresReadyState()
    {
        var session = new SimulatorSession();
        var prefix = "http://127.0.0.1:" + GetFreeTcpPort() + "/";
        var server = new RestSimulatorServer(session, prefix);
        await server.StartAsync();

        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(prefix) };

            var rejected = await client.PostAsJsonAsync(
                RestRoutes.ApiProductInfo.TrimStart('/'),
                new ProductInfo { WaferID = "W1" },
                ProtocolJson.Options);

            Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);

            var initialized = await client.PostAsync(RestRoutes.ApiSystemInitialize.TrimStart('/'), null);
            initialized.EnsureSuccessStatusCode();

            var accepted = await client.PostAsJsonAsync(
                RestRoutes.ApiProductInfo.TrimStart('/'),
                new ProductInfo { WaferID = "W1", LotID = "LOT-1", Recipe = "RCP-A", Slot = "1" },
                ProtocolJson.Options);

            Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
            var body = await accepted.Content.ReadFromJsonAsync<CommandResponse>(ProtocolJson.Options);
            Assert.True(body?.Accepted);
            Assert.Equal(SystemStates.Ready, body?.State);
        }
        finally
        {
            await server.StopAsync();
        }
    }

    [Fact]
    public async Task SystemStartReturnsRunning()
    {
        var session = new SimulatorSession();
        var prefix = "http://127.0.0.1:" + GetFreeTcpPort() + "/";
        var server = new RestSimulatorServer(session, prefix);
        await server.StartAsync();

        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(prefix) };
            (await client.PostAsync(RestRoutes.ApiSystemInitialize.TrimStart('/'), null)).EnsureSuccessStatusCode();

            var start = await client.PostAsJsonAsync(
                RestRoutes.ApiSystemStart.TrimStart('/'),
                new SystemStartRequest { Condition = "golden-sample", RunMode = ControlRunModes.SingleRun },
                ProtocolJson.Options);

            var response = await start.Content.ReadFromJsonAsync<CommandResponse>(ProtocolJson.Options);

            Assert.Equal(HttpStatusCode.OK, start.StatusCode);
            Assert.True(response?.Accepted);
            Assert.Equal(SystemStates.Running, response?.State);
        }
        finally
        {
            await server.StopAsync();
        }
    }

    [Fact]
    public async Task ResultsQueryRejectsUnsupportedQueryKeys()
    {
        var session = new SimulatorSession();
        var prefix = "http://127.0.0.1:" + GetFreeTcpPort() + "/";
        var server = new RestSimulatorServer(session, prefix);
        await server.StartAsync();

        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(prefix) };

            var response = await client.GetAsync(RestRoutes.ApiResults + "?slot=9");
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("Only waferID, lotID, and recipe", body);
        }
        finally
        {
            await server.StopAsync();
        }
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
