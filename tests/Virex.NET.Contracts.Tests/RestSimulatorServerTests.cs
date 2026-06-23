using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using Virex.NET.Contracts;
using Virex.NET.Simulator.WPF.Services;

namespace Virex.NET.Contracts.Tests;

public sealed class RestSimulatorServerTests
{
    [Fact]
    public async Task OpenApiAndScalarRoutesAreAvailable()
    {
        var session = new SimulatorSession();
        var prefix = "http://127.0.0.1:" + GetFreeTcpPort() + "/";
        var server = new RestSimulatorServer(session, prefix);
        await server.StartAsync();

        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(prefix) };

            var openApi = await client.GetStringAsync(RestRoutes.OpenApiJson);
            var scalar = await client.GetStringAsync(RestRoutes.Scalar);

            using var document = JsonDocument.Parse(openApi);
            var paths = document.RootElement.GetProperty("paths");
            Assert.True(paths.TryGetProperty(RestRoutes.ApiWaferInfo, out _));
            Assert.True(paths.TryGetProperty(RestRoutes.ApiStatus, out _));
            Assert.True(paths.TryGetProperty(RestRoutes.ApiError, out _));
            Assert.True(paths.TryGetProperty(RestRoutes.ApiControlInitialize, out _));
            Assert.True(paths.TryGetProperty(RestRoutes.ApiControlTerminate, out _));
            Assert.True(paths.TryGetProperty(RestRoutes.ApiResults, out var resultsPath));
            Assert.Contains("recipeId", openApi);
            Assert.Contains("previewImagePath", openApi);
            Assert.DoesNotContain("results/latest", openApi, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("deinitialize", openApi, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("socket-info", openApi, StringComparison.OrdinalIgnoreCase);
            Assert.True(resultsPath.GetProperty("get").TryGetProperty("parameters", out _));

            Assert.Contains("@scalar/api-reference", scalar);
            Assert.Contains("data-url=\"/openapi/v1.json\"", scalar);
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
            Assert.Contains("Only lotId, waferId, and recipeId", body);
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
