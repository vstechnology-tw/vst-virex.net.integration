using Virex.NET.Client;
using Virex.NET.Contracts;

var options = new VirexClientOptions
{
    RestBaseUrl = args.Length > 0 ? args[0] : "http://127.0.0.1:5088",
    TcpHost = "127.0.0.1",
    TcpPort = 5089,
    MqttHost = "127.0.0.1",
    MqttPort = 1883,
    MqttTopic = "virex",
};

using var client = new VirexClient(options);

PrintStep("Virex.NET C# SDK ProductInfo Demo");
Console.WriteLine($"REST: {options.RestBaseUrl}");
Prompt("In Simulator, press Start Servers, then press Enter here.");

try
{
    var status = await client.GetStatusAsync();
    PrintStatus(status);

    if (status.State == SystemStates.Uninitialized)
    {
        var initialize = await client.InitializeAsync();
        PrintCommand(initialize);
    }

    var product = new ProductInfo
    {
        WaferID = "WSDK-001",
        LotID = "LOT-SDK-001",
        Recipe = "RCP-A",
        Slot = "1",
        FoupID = "FOUP-A",
        ChamberID = "CH-1",
    };
    PrintCommand(await client.SetProductInfoAsync(product));

    var start = await client.StartAsync("golden-sample", ControlRunModes.Continue);
    PrintCommand(start);

    await Task.Delay(500);
    var results = await client.QueryResultsAsync(waferID: product.WaferID);
    Console.WriteLine($"Result count for {product.WaferID}: {results.Count}");
}
catch (HttpRequestException ex)
{
    Console.WriteLine("Connection failed. In Simulator, press Start Servers and verify the REST endpoint matches this sample.");
    Console.WriteLine(ex.Message);
    return 1;
}
catch (VirexClientException ex)
{
    Console.WriteLine($"HTTP {ex.StatusCode}: {ex.ResponseBody}");
    return 1;
}

return 0;

static void PrintStatus(SystemStatus status) =>
    Console.WriteLine($"Status: state={status.State}");

static void PrintCommand(CommandResponse response) =>
    Console.WriteLine($"{response.Command}: accepted={response.Accepted}, state={response.State}, message={response.Message}");

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
