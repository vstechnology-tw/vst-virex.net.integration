using System.Net;
using System.Net.Http.Json;
using Virex.NET.Contracts;

var baseUrl = args.Length > 0 ? args[0].TrimEnd('/') + "/" : "http://127.0.0.1:5088/";
using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };
var lotId = "LOT-RAW-REST-001";

PrintStep("Virex.NET C# Raw REST Guided Demo");
Console.WriteLine("This sample uses direct REST calls and shows how simulator UI state affects API returns.");
Console.WriteLine($"REST base URL: {baseUrl}");
Prompt("In Simulator, press Start Servers. Leave Initialize unpressed for the first check, then press Enter here.");

try
{
    PrintStep("Step 1 - Read /api/status");
    var status = await http.GetFromJsonAsync<StatusDto>(RestRoutes.ApiStatus.TrimStart('/'), ProtocolJson.Options);
    PrintStatus(status);

    if (status?.Initialized != true)
    {
        PrintStep("Step 2 - Expected negative check");
        Console.WriteLine("Calling POST /api/control/start before Initialize should return HTTP 409 not_initialized.");
        var negative = await http.PostAsync(RestRoutes.ApiControlStart.TrimStart('/'), null);
        var negativeBody = await negative.Content.ReadAsStringAsync();
        Console.WriteLine($"Start returned HTTP {(int)negative.StatusCode}: {negativeBody}");
        if (negative.StatusCode != HttpStatusCode.Conflict || !negativeBody.Contains("not_initialized", StringComparison.OrdinalIgnoreCase))
            return Fail("Expected HTTP 409 not_initialized. Confirm the simulator was not initialized before this step.");

        Prompt("In Simulator, press Initialize. Confirm Status shows initialized=True, processState=ready, then press Enter here.");
        status = await http.GetFromJsonAsync<StatusDto>(RestRoutes.ApiStatus.TrimStart('/'), ProtocolJson.Options);
        PrintStatus(status);
    }

    PrintStep("Step 3 - POST /api/wafer-info");
    var waferInfo = new WaferInfo
    {
        LotId = lotId,
        WaferId = "W01",
        RecipeId = "RCP-A",
        Slot = "1",
        FoupId = "FOUP-A",
        ChamberId = "CH-1",
    };

    var waferResponse = await http.PostAsJsonAsync(RestRoutes.ApiWaferInfo.TrimStart('/'), waferInfo, ProtocolJson.Options);
    waferResponse.EnsureSuccessStatusCode();
    Console.WriteLine("Expected Simulator Event Log:");
    Console.WriteLine("WaferInfo updated from REST: lotId=LOT-RAW-REST-001, waferId=W01, recipeId=RCP-A, slot=1, foupId=FOUP-A, chamberId=CH-1");

    PrintStep("Step 4 - POST /api/control/start");
    Console.WriteLine("Expected Simulator Status: capturing -> inspecting -> saving -> ready.");
    var start = await http.PostAsync(RestRoutes.ApiControlStart.TrimStart('/'), null);
    var startBody = await start.Content.ReadAsStringAsync();
    Console.WriteLine($"Start returned HTTP {(int)start.StatusCode}: {startBody}");
    start.EnsureSuccessStatusCode();

    PrintStep("Step 5 - GET /api/results by lotId");
    var results = await http.GetFromJsonAsync<ResultListDto>(
        RestRoutes.ApiResults.TrimStart('/') + "?lotId=" + Uri.EscapeDataString(lotId),
        ProtocolJson.Options);
    Console.WriteLine($"Result count for {lotId}: {results?.Count ?? 0}");
}
catch (HttpRequestException ex)
{
    Console.WriteLine("Connection failed. In Simulator, press Start Servers and verify the REST endpoint matches this sample.");
    Console.WriteLine(ex.Message);
    return 1;
}

return 0;

static void PrintStatus(StatusDto? status) =>
    Console.WriteLine($"Status: initialized={status?.Initialized}, processState={status?.ProcessState}, recipe={status?.Recipe}");

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

static int Fail(string message)
{
    Console.WriteLine(message);
    return 1;
}
