using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
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
            Assert.Contains("ControlStartRequest", openApi);
            Assert.Contains("runMode", openApi);
            Assert.Contains("ControlStopRequest", openApi);
            Assert.Contains("reason", openApi);
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

    [Fact]
    public async Task ControlStartAcceptsOptionalConditionPayload()
    {
        var session = new SimulatorSession();
        var messages = new List<string>();
        session.Log += (_, message) => messages.Add(message);
        var prefix = "http://127.0.0.1:" + GetFreeTcpPort() + "/";
        var server = new RestSimulatorServer(session, prefix);
        await server.StartAsync();

        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(prefix) };
            var initialize = await client.PostAsync(RestRoutes.ApiControlInitialize.TrimStart('/'), null);
            initialize.EnsureSuccessStatusCode();

            var start = await client.PostAsJsonAsync(
                RestRoutes.ApiControlStart.TrimStart('/'),
                new ControlStartRequest { Condition = "golden-sample", RunMode = ControlRunModes.SingleRun },
                ProtocolJson.Options);

            Assert.Equal(HttpStatusCode.OK, start.StatusCode);
            Assert.Contains("Start condition: golden-sample", messages);
            Assert.Contains("Start run mode: single", messages);
        }
        finally
        {
            await server.StopAsync();
        }
    }

    [Fact]
    public async Task ControlStopAcceptsOptionalReasonPayloadAndLegacyEmptyBody()
    {
        var session = new SimulatorSession();
        var messages = new List<string>();
        session.Log += (_, message) => messages.Add(message);
        var prefix = "http://127.0.0.1:" + GetFreeTcpPort() + "/";
        var server = new RestSimulatorServer(session, prefix);
        await server.StartAsync();

        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(prefix) };
            var initialize = await client.PostAsync(RestRoutes.ApiControlInitialize.TrimStart('/'), null);
            initialize.EnsureSuccessStatusCode();

            var firstStart = client.PostAsync(RestRoutes.ApiControlStart.TrimStart('/'), null);
            await Task.Delay(100);
            var stop = await client.PostAsJsonAsync(
                RestRoutes.ApiControlStop.TrimStart('/'),
                new ControlStopRequest { Reason = "operator-request" },
                ProtocolJson.Options);
            await firstStart;

            Assert.Equal(HttpStatusCode.OK, stop.StatusCode);
            Assert.Contains("Stopped. reason=operator-request", messages);

            var secondStart = await client.PostAsync(RestRoutes.ApiControlStart.TrimStart('/'), null);
            Assert.Equal(HttpStatusCode.OK, secondStart.StatusCode);
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
