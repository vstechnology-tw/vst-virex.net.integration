using Virex.NET.Client;
using Virex.NET.Contracts;

var options = new VirexClientOptions
{
    RestBaseUrl = args.Length > 0 ? args[0] : "http://127.0.0.1:5088",
    TcpHost = "127.0.0.1",
    TcpPort = 5089,
    MqttHost = "127.0.0.1",
    MqttPort = 1883,
    MqttTopic = "Virex.NET",
};

using var client = new VirexClient(options);
var lotId = "LOT-SDK-001";

PrintStep("Virex.NET C# SDK Guided Demo");
Console.WriteLine("This sample shows that the simulator UI controls the server state.");
Console.WriteLine($"REST: {options.RestBaseUrl}");
Console.WriteLine($"TCP : {options.TcpHost}:{options.TcpPort}");
Console.WriteLine($"MQTT: {options.MqttHost}:{options.MqttPort}, topic={options.MqttTopic}");
Prompt("In Simulator, press Start Servers. Leave Initialize unpressed for the first check, then press Enter here.");

try
{
    PrintStep("Step 1 - Read current status");
    var status = await client.GetStatusAsync();
    PrintStatus(status);

    if (!status.Initialized)
    {
        PrintStep("Step 2 - Expected negative check");
        Console.WriteLine("The simulator is not initialized yet. Calling Start should return not_initialized.");
        await ExpectNotInitializedAsync(client);

        Prompt("In Simulator, press Initialize. Confirm Status shows initialized=True, processState=ready, then press Enter here.");
        status = await client.GetStatusAsync();
        PrintStatus(status);
    }
    else
    {
        Console.WriteLine("Simulator is already initialized, so the negative not_initialized check is skipped.");
    }

    PrintStep("Step 3 - Update WaferInfo through the SDK");
    await client.SetWaferInfoAsync(new WaferInfo
    {
        LotId = lotId,
        WaferId = "W01",
        RecipeId = "RCP-A",
        Slot = "1",
        FoupId = "FOUP-A",
        ChamberId = "CH-1",
    });
    Console.WriteLine("Expected Simulator Event Log:");
    Console.WriteLine("WaferInfo updated from REST: lotId=LOT-SDK-001, waferId=W01, recipeId=RCP-A, slot=1, foupId=FOUP-A, chamberId=CH-1");

    PrintStep("Step 4 - Start a normal inspection cycle");
    Console.WriteLine("Expected Simulator Status: capturing -> inspecting -> saving -> ready.");
    var start = await client.StartAsync();
    Console.WriteLine($"Start returned: initialized={start.Initialized}, processState={start.ProcessState}, message={start.Message}");

    PrintStep("Step 5 - Query result summary by lotId");
    var results = await client.QueryResultsAsync(lotId: lotId);
    Console.WriteLine($"Result count for {lotId}: {results.Count}");
    Console.WriteLine("If the count is 0, press Start Cycle or Emit Fake Result in Simulator and verify the WaferInfo lotId.");
}
catch (HttpRequestException ex)
{
    Console.WriteLine("Connection failed. In Simulator, press Start Servers and verify the REST endpoint matches this sample.");
    Console.WriteLine(ex.Message);
    return 1;
}
catch (VirexClientException ex)
{
    Console.WriteLine("The simulator returned an error response:");
    Console.WriteLine($"HTTP {ex.StatusCode}: {ex.ResponseBody}");
    return 1;
}

return 0;

static async Task ExpectNotInitializedAsync(VirexClient client)
{
    try
    {
        await client.StartAsync();
        Console.WriteLine("Start succeeded because the simulator was already initialized.");
    }
    catch (VirexClientException ex) when (ex.StatusCode == 409 && ex.ResponseBody.Contains("not_initialized", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Expected response: HTTP 409 not_initialized.");
    }
}

static void PrintStatus(StatusDto status) =>
    Console.WriteLine($"Status: initialized={status.Initialized}, processState={status.ProcessState}, recipe={status.Recipe}");

static void PrintStep(string title)
{
    Console.WriteLine();
    Console.WriteLine("== " + title + " ==");
}

static void Prompt(string message)
{
    Console.WriteLine();
    Console.WriteLine("Action required in Simulator:");
    Console.WriteLine(message);
    Console.ReadLine();
}
