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

var product = new ProductInfo
{
    WaferID = "WSDK-210-001",
    LotID = "LOT-SDK-210",
    Recipe = "RCP-DEMO",
    Slot = "1",
    FoupID = "FOUP-DEMO",
    ChamberID = "CH-1",
};

using var client = new VirexClient(options);

PrintStep("Virex.NET C# SDK 13-Step Demo");
Console.WriteLine($"RESTful API: {options.RestBaseUrl}");
Prompt("In Simulator, press Start Servers, then press Enter here.");

try
{
    PrintStep("Step 1 - Query status");
    PrintStatus(await client.GetStatusAsync());

    PrintStep("Step 2 - Query error");
    PrintError(await client.GetErrorAsync());

    PrintStep("Step 3 - Query ProductInfo");
    PrintProductInfo(await client.GetProductInfoAsync());

    PrintStep("Step 4 - Initialize");
    PrintCommand(await client.InitializeAsync());

    PrintStep("Step 5 - Confirm Ready");
    PrintStatus(await client.GetStatusAsync());

    PrintStep("Step 6 - Set ProductInfo");
    PrintCommand(await client.SetProductInfoAsync(product));

    PrintStep("Step 7 - Confirm ProductInfo");
    PrintProductInfo(await client.GetProductInfoAsync());

    PrintStep("Step 8 - Start run");
    PrintCommand(await client.StartAsync("golden-sample", ControlRunModes.Continue));

    PrintStep("Step 9 - Observe run events");
    Console.WriteLine("RESTful API has no event stream; waiting briefly, then polling status/results.");
    await Task.Delay(1200);
    PrintStatus(await client.GetStatusAsync());

    PrintStep("Step 10 - Stop run");
    PrintCommand(await client.StopAsync("operator-request"));

    PrintStep("Step 11 - Query results");
    var results = await client.QueryResultsAsync(lotID: product.LotID, waferID: product.WaferID);
    Console.WriteLine($"Result count for {product.LotID}/{product.WaferID}: {results.Count}");

    PrintStep("Step 12 - Deinitialize");
    PrintCommand(await client.DeinitializeAsync());

    PrintStep("Step 13 - Confirm Uninitialized");
    PrintStatus(await client.GetStatusAsync());
}
catch (HttpRequestException ex)
{
    Console.WriteLine("Connection failed. In Simulator, press Start Servers and verify the RESTful API endpoint matches this sample.");
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

static void PrintError(ErrorInfo error) =>
    Console.WriteLine($"Error: hasError={error.HasError}, state={error.State}, message={error.Message}");

static void PrintProductInfo(ProductInfo info) =>
    Console.WriteLine($"ProductInfo: lotID={info.LotID}, waferID={info.WaferID}, recipe={info.Recipe}, slot={info.Slot}, foupID={info.FoupID}, chamberID={info.ChamberID}");

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
