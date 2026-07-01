using System.Net.Http.Json;
using Virex.NET.Contracts;

var baseUrl = args.Length > 0 ? args[0].TrimEnd('/') + "/" : "http://127.0.0.1:5088/";
using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };

var productInfo = new ProductInfo
{
    WaferID = "WCS-REST-210-001",
    LotID = "LOT-CS-REST-210",
    Recipe = "RCP-DEMO",
    Slot = "1",
    FoupID = "FOUP-DEMO",
    ChamberID = "CH-1",
};

PrintStep("Virex.NET C# Raw RESTful API 13-Step Demo");
Console.WriteLine($"RESTful API base URL: {baseUrl}");
Prompt("In Simulator, press Start Servers, then press Enter here.");

try
{
    PrintStep("Step 1 - Query status");
    PrintStatus(await GetJsonAsync<SystemStatus>(RestRoutes.ApiStatus));

    PrintStep("Step 2 - Query error");
    PrintError(await GetJsonAsync<ErrorInfo>(RestRoutes.ApiError));

    PrintStep("Step 3 - Query ProductInfo");
    PrintProductInfo(await GetJsonAsync<ProductInfo>(RestRoutes.ApiProductInfo));

    PrintStep("Step 4 - Initialize");
    PrintResponse(await PostAsync(RestRoutes.ApiSystemInitialize));

    PrintStep("Step 5 - Confirm Ready");
    PrintStatus(await GetJsonAsync<SystemStatus>(RestRoutes.ApiStatus));

    PrintStep("Step 6 - Set ProductInfo");
    PrintResponse(await PostJsonAsync(RestRoutes.ApiProductInfo, productInfo));

    PrintStep("Step 7 - Confirm ProductInfo");
    PrintProductInfo(await GetJsonAsync<ProductInfo>(RestRoutes.ApiProductInfo));

    PrintStep("Step 8 - Start run");
    PrintResponse(await PostJsonAsync(RestRoutes.ApiSystemStart, new SystemStartRequest { Condition = "golden-sample", RunMode = ControlRunModes.Continue }));

    PrintStep("Step 9 - Observe run events");
    Console.WriteLine("RESTful API has no event stream; waiting briefly, then polling status/results.");
    await Task.Delay(1200);
    PrintStatus(await GetJsonAsync<SystemStatus>(RestRoutes.ApiStatus));

    PrintStep("Step 10 - Stop run");
    PrintResponse(await PostJsonAsync(RestRoutes.ApiSystemStop, new SystemStopRequest { Reason = "operator-request" }));

    PrintStep("Step 11 - Query results");
    var results = await GetJsonAsync<ResultList>(
        RestRoutes.ApiResults + "?lotID=" + Uri.EscapeDataString(productInfo.LotID) + "&waferID=" + Uri.EscapeDataString(productInfo.WaferID));
    Console.WriteLine($"Result count for {productInfo.LotID}/{productInfo.WaferID}: {results.Count}");

    PrintStep("Step 12 - Deinitialize");
    PrintResponse(await PostAsync(RestRoutes.ApiSystemDeinitialize));

    PrintStep("Step 13 - Confirm Uninitialized");
    PrintStatus(await GetJsonAsync<SystemStatus>(RestRoutes.ApiStatus));
}
catch (HttpRequestException ex)
{
    Console.WriteLine("Connection failed. In Simulator, press Start Servers and verify the RESTful API endpoint matches this sample.");
    Console.WriteLine(ex.Message);
    return 1;
}

return 0;

async Task<T> GetJsonAsync<T>(string route) =>
    await http.GetFromJsonAsync<T>(route.TrimStart('/'), ProtocolJson.Options) ?? throw new InvalidOperationException("Empty response.");

async Task<string> PostAsync(string route)
{
    var response = await http.PostAsync(route.TrimStart('/'), null);
    return await ReadResponseAsync(response);
}

async Task<string> PostJsonAsync<T>(string route, T payload)
{
    var response = await http.PostAsJsonAsync(route.TrimStart('/'), payload, ProtocolJson.Options);
    return await ReadResponseAsync(response);
}

static async Task<string> ReadResponseAsync(HttpResponseMessage response)
{
    var body = await response.Content.ReadAsStringAsync();
    if (!response.IsSuccessStatusCode)
        Console.WriteLine($"HTTP {(int)response.StatusCode}: {body}");
    else
        Console.WriteLine(body);
    response.EnsureSuccessStatusCode();
    return body;
}

static void PrintStatus(SystemStatus status) =>
    Console.WriteLine($"Status: state={status.State}");

static void PrintError(ErrorInfo error) =>
    Console.WriteLine($"Error: hasError={error.HasError}, state={error.State}, message={error.Message}");

static void PrintProductInfo(ProductInfo info) =>
    Console.WriteLine($"ProductInfo: lotID={info.LotID}, waferID={info.WaferID}, recipe={info.Recipe}, slot={info.Slot}, foupID={info.FoupID}, chamberID={info.ChamberID}");

static void PrintResponse(string _)
{
}

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
