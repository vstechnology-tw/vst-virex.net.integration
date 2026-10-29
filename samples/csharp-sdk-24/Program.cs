using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text.Json;
using Virex.NET.Client;
using Virex.NET.Contracts;
using Virex.NET.Samples;

if (args.Length is < 3 or > 4 || !Uri.TryCreate(args[0], UriKind.Absolute, out var uri)
    || uri.Scheme is not ("http" or "https") || string.IsNullOrWhiteSpace(args[1])
    || !int.TryParse(args[2], out var tcpPort) || tcpPort is < 1 or > 65535)
{
    Console.Error.WriteLine("Usage: CSharpSdk24Sample <REST URL> <TCP host> <TCP port> [report.json]");
    return 2;
}

using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
var token = cancellation.Token;
using var client = new VirexClient(new VirexClientOptions
{
    RestBaseUrl = uri.AbsoluteUri, TcpHost = args[1], TcpPort = tcpPort,
});
var checks = new List<string>();
VirexEventSubscription? subscription = null;
try
{
    var softwareVersion = await client.GetSoftwareVersionAsync(token);
    Require(!string.IsNullOrWhiteSpace(softwareVersion.Version), "software version available");
    Require((await client.GetStatusAsync(token)).State == SystemStates.Uninitialized, "initial status");
    var events = await client.GetEventCatalogAsync(token);
    var sources = await client.GetDataSourcesAsync(token);
    var alarms = await client.GetAlarmsAsync(token);
    Require(events.AppId == sources.AppId && events.InstanceId == sources.InstanceId
        && events.AppId == alarms.AppId && events.InstanceId == alarms.InstanceId, "catalog identity");
    var workspace = events.WorkspaceScope;
    Require(workspace?.IsValid == true, "catalog Workspace identity");
    // These are the Simulator example's semantic bindings, not an SDK whitelist.
    var names = new[] { "captureReady", "captureCompleted", "resultCreated" };
    Require(names.All(name => events.Names.Contains(name, StringComparer.Ordinal)), "example event bindings");
    Require(sources.Sources.Any(s => s.Name == "result.findings" && s.Kind == DataSourceKinds.Result), "example result binding");
    var ready = NewArrival(); var completed = NewArrival(); var result = NewArrival();
    var request = new SystemStartRequest
    {
        RequestId = Guid.NewGuid().ToString("N"), RunMode = ControlRunModes.SingleRun,
        InspectionMode = InspectionModes.CaptureAndInspect,
    };
    subscription = await client.TcpEvents.SubscribeAsync(names.Concat(alarms.Sources.Select(s => s.Name)), value =>
    {
        Console.WriteLine($"event {value.Name}: job={value.JobId}, capture={value.CaptureId}, result={value.ResultId}");
        if (value.Name == "captureReady") ready.TrySetResult(value);
        if (value.Name == "captureCompleted") completed.TrySetResult(value);
        if (value.Name == "resultCreated") result.TrySetResult(value);
    }, issue => Console.Error.WriteLine($"stream issue: {issue.Code}: {issue.Message}"),
        new EventSubscriptionScope { AppId = events.AppId, InstanceId = events.InstanceId,
            WorkspaceScope = workspace, RequestId = request.RequestId }, token);
    Require(workspace!.SameAs(subscription.WorkspaceScope), "subscription Workspace identity");
    Require((await client.InitializeAsync(token)).State == SystemStates.Ready, "explicit initialization");
    Require((await client.GetStatusAsync(token)).State == SystemStates.Ready, "ready status");
    var product = new ProductInfo
    {
        LotID = "sample-lot", WaferID = "sample-wafer", Recipe = "sample-recipe",
        Slot = "sample-slot", FoupID = "sample-foup", ChamberID = "sample-chamber",
        AdditionalFields = new Dictionary<string, string> { ["sample-field"] = "sample-value" },
    };
    Require((await client.SetProductInfoAsync(product, token)).Accepted, "product info accepted");
    Require(SameProduct(product, await client.GetProductInfoAsync(token)), "product info read back");
    var accepted = await client.StartAsync(request, token);
    Require(accepted.Accepted && accepted.RequestId == request.RequestId, "REST acceptance correlation");
    Require(accepted.AppId == events.AppId && accepted.InstanceId == events.InstanceId
        && workspace.SameAs(accepted.WorkspaceScope), "accepted work identity");
    // REST acceptance does not authorize scanning. Wait for the actual producer events.
    var readyEvent = await ready.Task.WaitAsync(token);
    var captureEvent = await completed.Task.WaitAsync(token);
    var resultEvent = await result.Task.WaitAsync(token);
    foreach (var value in new[] { readyEvent, captureEvent, resultEvent })
        Require(value.AppId == events.AppId && value.InstanceId == events.InstanceId
            && workspace.SameAs(value.WorkspaceScope) && value.ConnectionGeneration == subscription.ConnectionGeneration
            && value.RequestId == request.RequestId && value.JobId == accepted.JobId
            && value.CaptureId == accepted.CaptureId, "event work correlation: " + value.Name);
    var findings = await CommittedResultReader.ReadAsync(client, accepted, resultEvent, "result.findings", token);
    Require(findings.Value.ValueKind == JsonValueKind.Array, "explicit committed result");
    var next = await client.StartAsync(new SystemStartRequest
    {
        RequestId = Guid.NewGuid().ToString("N"), RunMode = ControlRunModes.Continue, InspectionMode = InspectionModes.CaptureOnly,
    }, token);
    Require(next.Accepted && next.JobId != accepted.JobId, "next work acceptance");
    Require((await client.GetStatusAsync(token)).State == SystemStates.Running, "running status");
    try
    {
        await client.SetProductInfoAsync(new ProductInfo { LotID = "rejected-update" }, token);
        throw new InvalidOperationException("ProductInfo update succeeded while running.");
    }
    catch (VirexClientException ex) when (ex.StatusCode == 409)
    {
        checks.Add("running product info update rejected");
    }
    Require(SameProduct(product, await client.GetProductInfoAsync(token)), "rejected update preserves product info");
    try { await client.GetCurrentResultAsync(token); throw new InvalidOperationException("Previous result remained current."); }
    catch (VirexQueryException ex) when (ex.ErrorCode == QueryErrorCodes.NoCurrentResult) { checks.Add("current result invalidated"); }
    var historical = await CommittedResultReader.ReadAsync(client, accepted, resultEvent, "result.findings", token);
    Require(historical.ResultId == findings.ResultId, "delayed event retains explicit history");
    var live = await client.ReadDataSourceAsync("system.state", cancellationToken: token);
    Require(live.Kind == DataSourceKinds.Live && live.State == SystemStates.Running && live.ResultId is null, "live state during next work");
    await client.StopAsync(token);
    Require((await client.GetStatusAsync(token)).State == SystemStates.Ready, "status after stop");
    Require((await client.ResetAsync(token)).State == SystemStates.Uninitialized, "reset completes cleanup to Uninitialized");
    Require((await client.InitializeAsync(token)).State == SystemStates.Ready, "explicit initialization after reset");
    Require((await client.DeinitializeAsync(token)).State == SystemStates.Uninitialized, "final deinitialization");
    await subscription.CloseAsync();
    checks.Add("subscription close joined");
    var report = new
    {
        success = true, checks, targetFramework = Assembly.GetExecutingAssembly().GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName,
        actualFramework = RuntimeInformation.FrameworkDescription, environmentVersion = Environment.Version.ToString(),
        restEndpoint = uri.AbsoluteUri, tcpHost = args[1], tcpPort, softwareVersion.Version,
        events.AppId, events.InstanceId, events.WorkspaceScope,
        accepted, resultId = findings.ResultId, next,
        loaded = new[] { Describe(typeof(VirexClient).Assembly), Describe(typeof(CommandResponse).Assembly), Describe(Assembly.GetExecutingAssembly()) },
        scope = "Synthetic SDK to REST/TCP Simulator only; real App, equipment and release acceptance are separate.",
    };
    var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
    Console.WriteLine(json);
    if (args.Length == 4) await File.WriteAllTextAsync(args[3], json, token);
    return 0;
}
catch (Exception ex) when (ex is VirexClientException or VirexQueryException or OperationCanceledException or HttpRequestException or InvalidOperationException)
{
    Console.Error.WriteLine(ex.ToString());
    return 1;
}
finally
{
    if (subscription is not null) await subscription.CloseAsync();
}

void Require(bool value, string check)
{
    if (!value) throw new InvalidOperationException("Failed: " + check);
    checks.Add(check);
}
static TaskCompletionSource<EventEnvelope> NewArrival() => new(TaskCreationOptions.RunContinuationsAsynchronously);
static bool SameProduct(ProductInfo expected, ProductInfo actual) =>
    expected.LotID == actual.LotID && expected.WaferID == actual.WaferID && expected.Recipe == actual.Recipe
    && expected.Slot == actual.Slot && expected.FoupID == actual.FoupID && expected.ChamberID == actual.ChamberID
    && expected.AdditionalFields?.Count == actual.AdditionalFields?.Count
    && (expected.AdditionalFields is null || expected.AdditionalFields.All(pair =>
        actual.AdditionalFields is not null && actual.AdditionalFields.TryGetValue(pair.Key, out var value) && value == pair.Value));
static object Describe(Assembly assembly) => new
{
    name = assembly.GetName().Name, path = assembly.Location,
    version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
    sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assembly.Location))),
};
