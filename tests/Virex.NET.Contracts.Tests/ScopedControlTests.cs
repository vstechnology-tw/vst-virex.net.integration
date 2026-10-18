using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Virex.NET.Client;
using Virex.NET.Contracts;
using Virex.NET.Simulator.Core;
using Virex.NET.Simulator.WPF.Services;

namespace Virex.NET.Contracts.Tests;

public sealed class ScopedControlTests
{
    private static readonly string[] CommandNames = ["Initialize", "Start", "Stop", "Reset", "Deinitialize", "SetOperationMode", "SetProductInfo"];
    public static IEnumerable<object[]> Commands => CommandNames.Select(c => new object[] { c });
    private static ControlTarget Target(SimulatorSession session) => new() { AppId = session.AppId, InstanceId = session.InstanceId, WorkspaceScope = session.CurrentWorkspaceScope };
    private static ScopedControlRequest Request(ControlTarget target, string command)
    {
        object? payload = command switch
        {
            "Start" => new SystemStartRequest { RunMode = ControlRunModes.Continue },
            "Stop" => new SystemStopRequest { Reason = "scoped stop" },
            "SetOperationMode" => new SetOperationModeRequest { Mode = OperationModes.Remote },
            "SetProductInfo" => new ProductInfo { WaferID = "SCOPED" },
            _ => null,
        };
        using var document = JsonDocument.Parse(ProtocolJson.Serialize(payload));
        return new() { ExpectedTarget = target, Command = command, Payload = payload is null ? null : document.RootElement.Clone() };
    }

    [Theory, MemberData(nameof(Commands))]
    public async Task StaleActivationAfterAtoBtoARejectsEveryCommandWithoutMutatingB(string command)
    {
        var session = new SimulatorSession();
        var original = Target(session);
        var product = ProtocolJson.Serialize(session.ProductInfo);
        var changes = 0;
        session.StatusChanged += (_, _) => changes++;
        session.ProductInfoChanged += (_, _) => changes++;
        session.OperationModeChanged += (_, _) => changes++;
        session.ResultCreated += (_, _) => changes++;
        await session.SelectWorkspaceForTestsAsync(Guid.NewGuid(), Guid.NewGuid());
        var b = Target(session);
        var rejectedB = await session.ExecuteScopedAsync(Request(original, command));
        Assert.Equal(CommandErrorCodes.TargetMismatch, rejectedB.ErrorCode);
        Assert.Equal(b.WorkspaceScope!.ActivationId, session.CurrentWorkspaceScope.ActivationId);
        Assert.Equal(SimulatorState.Uninitialized, session.State);
        Assert.Equal(product, ProtocolJson.Serialize(session.ProductInfo));
        Assert.Equal(OperationModes.Local, session.OperationMode.Mode);
        Assert.Empty(session.Results);
        Assert.Equal(0, changes);
        await session.SelectWorkspaceForTestsAsync(original.WorkspaceScope!.WorkspaceId, original.WorkspaceScope.RegistrationId);
        Assert.Equal(CommandErrorCodes.TargetMismatch, (await session.ExecuteScopedAsync(Request(original, command))).ErrorCode);
        Assert.Equal(0, changes);
    }

    [Theory, InlineData("app"), InlineData("instance"), InlineData("workspace"), InlineData("registration"), InlineData("activation")]
    public async Task EveryTargetComponentIsRequiredForAdmission(string component)
    {
        var session = new SimulatorSession();
        var target = Target(session);
        switch (component)
        {
            case "app": target.AppId = "other"; break;
            case "instance": target.InstanceId = Guid.NewGuid().ToString("N"); break;
            case "workspace": target.WorkspaceScope!.WorkspaceId = Guid.NewGuid(); break;
            case "registration": target.WorkspaceScope!.RegistrationId = Guid.NewGuid(); break;
            case "activation": target.WorkspaceScope!.ActivationId = Guid.NewGuid(); break;
        }
        Assert.Equal(CommandErrorCodes.TargetMismatch, (await session.ExecuteScopedAsync(Request(target, "Initialize"))).ErrorCode);
        Assert.Equal(SimulatorState.Uninitialized, session.State);
    }

    [Fact]
    public async Task TargetIsValidatedAfterWaitingForTheSameWorkspaceSwitchGate()
    {
        var session = new SimulatorSession();
        var a = Target(session);
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        session.WorkspaceScopeChanged += (_, _) => { entered.Set(); Assert.True(release.Wait(TimeSpan.FromSeconds(10))); };
        var switching = Task.Run(() => session.SelectWorkspaceForTestsAsync(Guid.NewGuid(), Guid.NewGuid()));
        Assert.True(entered.Wait(TimeSpan.FromSeconds(10)));
        var pending = session.ExecuteScopedAsync(Request(a, "Initialize"));
        Assert.False(pending.IsCompleted);
        release.Set();
        await switching;
        Assert.Equal(CommandErrorCodes.TargetMismatch, (await pending).ErrorCode);
        Assert.Equal(SimulatorState.Uninitialized, session.State);
    }

    [Fact]
    public async Task AdmittedCommandKeepsItsActivationWhileWorkspaceSwitchWaits()
    {
        var session = new SimulatorSession();
        var target = Target(session);
        var admitted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        session.StatusChanged += (_, status) => { if (status.State == SystemStates.Initializing) admitted.TrySetResult(); };
        var initialize = session.ExecuteScopedAsync(Request(target, "Initialize"));
        await admitted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var switching = session.SelectWorkspaceForTestsAsync(Guid.NewGuid(), Guid.NewGuid());
        Assert.False(switching.IsCompleted);
        Assert.True((await initialize).Accepted);
        await Assert.ThrowsAsync<InvalidOperationException>(() => switching);
        Assert.True(target.WorkspaceScope!.SameAs(session.CurrentWorkspaceScope));
        await session.DeinitializeAsync();
    }

    [Fact]
    public async Task ScopedSdkAndRealReceiverSupportEveryCommandAndKeepLegacyControl()
    {
        var session = new SimulatorSession(resultRootDirectory: null, operationManagementEnabled: true);
        var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
        var url = "http://127.0.0.1:" + port + "/";
        var server = new RestSimulatorServer(session, url);
        await server.StartAsync();
        try
        {
            using var http = new HttpClient { BaseAddress = new Uri(url) };
            var client = new VirexRestClient(http);
            var target = Target(session);
            var mutations = 0;
            session.StatusChanged += (_, _) => mutations++;
            session.ProductInfoChanged += (_, _) => mutations++;
            session.OperationModeChanged += (_, _) => mutations++;
            foreach (var invalid in new[]
            {
                "{}", "null", "{\"command\":\"Initialize\"}",
                ProtocolJson.Serialize(Request(target, "Initialize")).Replace("Initialize", "Unknown"),
                ProtocolJson.Serialize(Request(target, "Start")).Replace("requestId", "unrecognized"),
                ProtocolJson.Serialize(Request(target, "Initialize")).TrimEnd('}') + ",\"payload\":{\"mode\":\"remote\"}}",
            })
            {
                using var denied = await http.PostAsync(RestRoutes.ApiScopedControl, new StringContent(invalid, Encoding.UTF8, "application/json"));
                Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
                Assert.Equal(CommandErrorCodes.InvalidPayload, ProtocolJson.Deserialize<CommandResponse>(await denied.Content.ReadAsStringAsync())!.ErrorCode);
            }
            Assert.Equal(0, mutations);
            Assert.Equal(SimulatorState.Uninitialized, session.State);
            var stale = target.Snapshot(); stale.InstanceId = "old-process";
            var mismatch = await Assert.ThrowsAsync<VirexClientException>(() => client.SetOperationModeScopedAsync(stale, new() { Mode = OperationModes.Remote }));
            Assert.Equal(409, mismatch.StatusCode);
            Assert.Equal(0, mutations);
            using (var openApi = JsonDocument.Parse(await http.GetStringAsync(RestRoutes.OpenApiJson)))
            {
                Assert.True(openApi.RootElement.GetProperty("paths").TryGetProperty(RestRoutes.ApiScopedControl, out _));
                Assert.True(openApi.RootElement.GetProperty("components").GetProperty("schemas").TryGetProperty("ControlTarget", out _));
            }
            Assert.True((await client.SetOperationModeScopedAsync(target, new() { Mode = OperationModes.Remote })).Accepted);
            Assert.True((await client.InitializeScopedAsync(target)).Accepted);
            Assert.True((await client.SetProductInfoScopedAsync(target, new() { WaferID = "SCOPED" })).Accepted);
            Assert.Equal("SCOPED", session.ProductInfo.WaferID);
            var start = await client.StartScopedAsync(target, new() { RequestId = "actual-scoped-request", RunMode = ControlRunModes.Continue });
            Assert.True(start.Accepted); Assert.Equal("actual-scoped-request", start.RequestId); Assert.False(string.IsNullOrWhiteSpace(start.JobId));
            Assert.True((await client.StopScopedAsync(target, new())).Accepted);
            Assert.True((await client.DeinitializeScopedAsync(target)).Accepted);
            Assert.True((await client.InitializeScopedAsync(target)).Accepted);
            Assert.True((await client.ResetScopedAsync(target)).Accepted);
            Assert.Equal(SimulatorState.Uninitialized, session.State);
            Assert.True((await client.InitializeAsync()).Accepted);
            Assert.True((await client.DeinitializeAsync()).Accepted);
        }
        finally { await server.StopAsync(); }
    }

    [Theory, MemberData(nameof(Commands))]
    public async Task StaleTargetCannotChangeAnAvailableNewStation(string command)
    {
        var session = new SimulatorSession();
        var a = Target(session);
        await session.SelectWorkspaceForTestsAsync(Guid.NewGuid(), Guid.NewGuid());
        await session.InitializeAsync();
        if (command is "Stop" or "Reset")
            Assert.True((await session.StartAsync(new SystemStartRequest { RunMode = ControlRunModes.Continue })).Accepted);
        var state = session.State;
        var product = ProtocolJson.Serialize(session.ProductInfo);
        var mode = session.OperationMode.Mode;
        try
        {
            var rejected = await session.ExecuteScopedAsync(Request(a, command));
            Assert.Equal(CommandErrorCodes.TargetMismatch, rejected.ErrorCode);
            Assert.Equal(state, session.State);
            Assert.Equal(product, ProtocolJson.Serialize(session.ProductInfo));
            Assert.Equal(mode, session.OperationMode.Mode);
        }
        finally { await session.ShutdownAsync(); }
    }

    [Fact]
    public async Task ScopedResetRetainsCleanupOwnershipAfterCallerCancellation()
    {
        var session = new SimulatorSession();
        var target = Target(session);
        await session.InitializeAsync();
        using var cancel = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        session.StatusChanged += (_, status) => { if (status.State == SystemStates.Deinitializing) entered.TrySetResult(); };
        var reset = session.ExecuteScopedAsync(Request(target, "Reset"), cancel.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reset);
        var cleanup = await session.ExecuteScopedAsync(Request(target, "Reset"));
        Assert.True(cleanup.Accepted);
        Assert.Equal("Reset", cleanup.Command);
        Assert.Equal(SimulatorState.Uninitialized, session.State);
        await session.SelectWorkspaceForTestsAsync(Guid.NewGuid(), Guid.NewGuid());
        Assert.Equal(CommandErrorCodes.TargetMismatch, (await session.ExecuteScopedAsync(Request(target, "Reset"))).ErrorCode);
    }

    [Theory, InlineData(404), InlineData(405)]
    public async Task UnsupportedServerNeverReceivesLegacyMutation(int status)
    {
        var hits = new List<string>();
        using var http = new HttpClient(new Handler(async (request, token) =>
        {
            hits.Add(request.RequestUri!.AbsolutePath);
            _ = await request.Content!.ReadAsStringAsync(token);
            return new((HttpStatusCode)status) { Content = new StringContent("unsupported") };
        })) { BaseAddress = new("http://localhost/") };
        var client = new VirexRestClient(http); var target = Target(new SimulatorSession());
        foreach (var command in Commands)
            await Assert.ThrowsAsync<VirexClientException>(() => Send(client, target, (string)command[0]));
        Assert.Equal(7, hits.Count); Assert.All(hits, hit => Assert.Equal(RestRoutes.ApiScopedControl, hit));
    }

    [Fact]
    public async Task ConcurrentRequestsOwnTargetSnapshotsAndCancellationDoesNotFallback()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var bodies = new List<string>();
        using var http = new HttpClient(new Handler(async (request, token) =>
        {
            var body = await request.Content!.ReadAsStringAsync(token);
            lock (bodies) bodies.Add(body);
            entered.TrySetResult();
            await Task.Delay(Timeout.Infinite, token);
            return new(HttpStatusCode.OK);
        })) { BaseAddress = new("http://localhost/") };
        http.DefaultRequestHeaders.Add("Test-Fixed", "unchanged");
        var client = new VirexRestClient(http); var target = Target(new SimulatorSession());
        var original = target.Snapshot();
        using var cancel = new CancellationTokenSource();
        var request = client.InitializeScopedAsync(target, cancel.Token);
        await entered.Task;
        target.WorkspaceScope!.ActivationId = Guid.NewGuid(); target.InstanceId = "mutated";
        var second = client.ResetScopedAsync(target, cancel.Token);
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);
        Assert.Equal(2, bodies.Count);
        Assert.True(original.SameAs(ScopedControlJson.ReadRequest(bodies[0]).ExpectedTarget));
        Assert.True(target.SameAs(ScopedControlJson.ReadRequest(bodies[1]).ExpectedTarget));
        Assert.Single(http.DefaultRequestHeaders); Assert.Equal("unchanged", http.DefaultRequestHeaders.GetValues("Test-Fixed").Single());
    }

    [Theory, InlineData("missing"), InlineData("invalid"), InlineData("unknown"), InlineData("mismatch"), InlineData("duplicate"), InlineData("source")]
    public async Task MalformedEnvelopeHasNoReceiverSideEffects(string kind)
    {
        var session = new SimulatorSession(); var request = Request(Target(session), "Initialize");
        var json = ProtocolJson.Serialize(request);
        json = kind switch
        {
            "missing" => "{\"command\":\"Initialize\"}",
            "invalid" => json.Replace(request.ExpectedTarget!.WorkspaceScope!.ActivationId.ToString(), Guid.Empty.ToString()),
            "unknown" => json.Replace("Initialize", "Unknown"),
            "mismatch" => json.TrimEnd('}') + ",\"payload\":{\"mode\":\"remote\"}}",
            "duplicate" => json.TrimEnd('}') + ",\"command\":\"Start\"}",
            _ => json.TrimEnd('}') + ",\"source\":\"Local\"}",
        };
        Assert.Throws<JsonException>(() => ScopedControlJson.ReadRequest(json));
        Assert.Equal(SimulatorState.Uninitialized, session.State);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => session.ExecuteScopedAsync(request, new CancellationToken(true)));
        Assert.Equal(SimulatorState.Uninitialized, session.State);
    }

    [Fact]
    public void RequireRemoteEnvelopeIsStartOnlyStrictAndOmittedWhenFalse()
    {
        var session = new SimulatorSession();
        var target = Target(session);
        var optional = Request(target, "Start");
        var optionalJson = ProtocolJson.Serialize(optional);
        Assert.DoesNotContain("requireRemote", optionalJson);
        Assert.False(ScopedControlJson.ReadRequest(optionalJson).RequireRemote);

        optional.RequireRemote = true;
        var requiredJson = ProtocolJson.Serialize(optional);
        Assert.Contains("\"requireRemote\":true", requiredJson);
        Assert.True(ScopedControlJson.ReadRequest(requiredJson).RequireRemote);

        var nonStart = Request(target, "Initialize");
        nonStart.RequireRemote = true;
        Assert.Throws<JsonException>(() => ScopedControlJson.ReadRequest(ProtocolJson.Serialize(nonStart)));

        var initializeJson = ProtocolJson.Serialize(Request(target, "Initialize"));
        Assert.Throws<JsonException>(() => ScopedControlJson.ReadRequest(initializeJson.TrimEnd('}') + ",\"requireRemote\":false}"));
        Assert.Throws<JsonException>(() => ScopedControlJson.ReadRequest(initializeJson.TrimEnd('}') + ",\"requireRemote\":\"true\"}"));
        Assert.Throws<JsonException>(() => ScopedControlJson.ReadRequest(initializeJson.TrimEnd('}') + ",\"requireRemote\":true,\"requireRemote\":false}"));
    }

    [Fact]
    public async Task ScopedStartRequiresRemoteOnlyWhenRequestedAndCannotDowngrade()
    {
        var session = new SimulatorSession();
        try
        {
            var target = Target(session);
            Assert.True((await session.ExecuteScopedAsync(Request(target, "Initialize"))).Accepted);

            var required = Request(target, "Start");
            required.RequireRemote = true;
            var local = await session.ExecuteScopedAsync(required);
            Assert.False(local.Accepted);
            Assert.Equal(CommandErrorCodes.OperationNotAllowed, local.ErrorCode);
            Assert.Equal(SimulatorState.Ready, session.State);

            // False/absent retains the simulator's existing optional-mode semantics.
            Assert.True((await session.ExecuteScopedAsync(Request(target, "Start"))).Accepted);
            Assert.True((await session.ExecuteScopedAsync(Request(target, "Stop"))).Accepted);

            Assert.True((await session.SetOperationModeAsync(new SetOperationModeRequest { Mode = OperationModes.Remote })).Accepted);
            var remote = Request(target, "Start");
            remote.RequireRemote = true;
            var accepted = await session.ExecuteScopedAsync(remote);
            Assert.True(accepted.Accepted);
            Assert.Equal(OperationModes.Remote, session.OperationMode.Mode);
        }
        finally { await session.ShutdownAsync(); }
    }

    private static Task<CommandResponse> Send(VirexRestClient client, ControlTarget target, string command) => command switch
    {
        "Initialize" => client.InitializeScopedAsync(target), "Deinitialize" => client.DeinitializeScopedAsync(target), "Reset" => client.ResetScopedAsync(target),
        "Start" => client.StartScopedAsync(target, new()), "Stop" => client.StopScopedAsync(target, new()),
        "SetOperationMode" => client.SetOperationModeScopedAsync(target, new() { Mode = OperationModes.Remote }),
        _ => client.SetProductInfoScopedAsync(target, new()),
    };
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request, cancellationToken);
    }
}
