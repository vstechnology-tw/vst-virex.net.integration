using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Virex.NET.Client;
using Virex.NET.Contracts;
using Virex.NET.Simulator.Core;
using Virex.NET.Simulator.WPF.Services;

internal static class Program
{
    private static async Task<int> Main()
    {
        var root = Path.Combine(Path.GetTempPath(), "virex-smoke-" + Guid.NewGuid().ToString("N"));
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var prefix = "http://127.0.0.1:" + port + "/";
        var session = new SimulatorSession(root);
        var server = new RestSimulatorServer(session, prefix);
        await server.StartAsync();
        try
        {
            using var http = new HttpClient { BaseAddress = new Uri(prefix) };
            var client = new VirexRestClient(http);
            Require((await client.GetRecipesAsync()).Count >= 2, "Recipe catalog");
            try
            {
                await client.GetCurrentRecipeAsync();
                throw new InvalidOperationException("No-current query falsely succeeded.");
            }
            catch (VirexClientException error)
            {
                Require(error.StatusCode == 409, "No current recipe");
            }
            Require((await client.InitializeAsync()).Accepted, "Legacy Initialize");
            var current = await client.GetCurrentRecipeAsync();
            var parameters = await client.GetCurrentRecipeParametersAsync();
            Require(current.Recipe == parameters.Recipe && current.Revision == parameters.Revision, "Loaded snapshot consistency");
            Require(parameters.Groups[0].Parameters[0].Value.GetInt32() == 1000, "Parameter value");
            Require((await client.StartAsync()).Accepted, "Legacy Start");
            Require((await session.RunCompletedAsync()).Accepted, "Result commit");
            var list = await client.QueryResultsAsync();
            Require(list.Count == 1, "Legacy result list");
            var detail = await client.GetResultDetailAsync(list.Items[0].ResultId);
            Require(detail.ResultId == list.Items[0].ResultId && detail.SchemaVersion == 1 && detail.Findings.Length == 0, "Exact zero-Findings detail");
            File.Delete(list.Items[0].ResultPath);
            try
            {
                await client.GetResultDetailAsync(detail.ResultId);
                throw new InvalidOperationException("Deleted-result query falsely succeeded.");
            }
            catch (VirexClientException error)
            {
                Require(error.StatusCode == 410, "Deleted result");
            }
            Require((await client.GetStatusAsync()).State == SystemStates.Ready && !(await client.GetErrorAsync()).HasError, "Read-only errors");
            Require((await client.DeinitializeAsync()).Accepted, "Legacy Deinitialize");
            Console.WriteLine("PASS: runtime=" + Environment.Version + "; four REST queries, errors and legacy flow.");
            return 0;
        }
        finally
        {
            if (session.State == SimulatorState.Running)
                await session.StopAsync();
            await server.StopAsync();
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    private static void Require(bool condition, string check)
    {
        if (!condition)
            throw new InvalidOperationException("FAIL: " + check);
    }
}
