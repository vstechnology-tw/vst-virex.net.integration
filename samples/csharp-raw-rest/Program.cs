using System.Net.Http.Json;
using Virex.NET.Contracts;

var baseUrl = args.Length > 0 ? args[0].TrimEnd('/') + "/" : "http://127.0.0.1:5088/";
using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };

PrintStep("Virex.NET C# Raw REST ProductInfo Demo");
Console.WriteLine($"REST base URL: {baseUrl}");
Prompt("In Simulator, press Start Servers, then press Enter here.");

try
{
    var status = await http.GetFromJsonAsync<SystemStatus>(RestRoutes.ApiStatus.TrimStart('/'), ProtocolJson.Options);
    PrintStatus(status);

    if (status?.State == SystemStates.Uninitialized)
    {
        var initialize = await http.PostAsync(RestRoutes.ApiSystemInitialize.TrimStart('/'), null);
        initialize.EnsureSuccessStatusCode();
        Console.WriteLine(await initialize.Content.ReadAsStringAsync());
    }

    var productInfo = new ProductInfo
    {
        WaferID = "WRAW-REST-001",
        LotID = "LOT-RAW-REST-001",
        Recipe = "RCP-A",
        Slot = "1",
        FoupID = "FOUP-A",
        ChamberID = "CH-1",
    };

    var productResponse = await http.PostAsJsonAsync(RestRoutes.ApiProductInfo.TrimStart('/'), productInfo, ProtocolJson.Options);
    productResponse.EnsureSuccessStatusCode();
    Console.WriteLine(await productResponse.Content.ReadAsStringAsync());

    var start = await http.PostAsJsonAsync(
        RestRoutes.ApiSystemStart.TrimStart('/'),
        new SystemStartRequest { Condition = "golden-sample", RunMode = ControlRunModes.Continue },
        ProtocolJson.Options);
    start.EnsureSuccessStatusCode();
    Console.WriteLine(await start.Content.ReadAsStringAsync());

    await Task.Delay(500);
    var results = await http.GetFromJsonAsync<ResultList>(
        RestRoutes.ApiResults.TrimStart('/') + "?waferID=" + Uri.EscapeDataString(productInfo.WaferID),
        ProtocolJson.Options);
    Console.WriteLine($"Result count for {productInfo.WaferID}: {results?.Count ?? 0}");
}
catch (HttpRequestException ex)
{
    Console.WriteLine("Connection failed. In Simulator, press Start Servers and verify the REST endpoint matches this sample.");
    Console.WriteLine(ex.Message);
    return 1;
}

return 0;

static void PrintStatus(SystemStatus? status) =>
    Console.WriteLine($"Status: state={status?.State}");

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
