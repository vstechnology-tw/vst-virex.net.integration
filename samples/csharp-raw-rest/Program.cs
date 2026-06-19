using System.Net.Http.Json;
using Virex.NET.Contracts;

var baseUrl = args.Length > 0 ? args[0].TrimEnd('/') + "/" : "http://127.0.0.1:5088/";
using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };

var status = await http.GetFromJsonAsync<StatusDto>(RestRoutes.ApiStatus.TrimStart('/'));
Console.WriteLine($"Status: initialized={status?.Initialized}, processState={status?.ProcessState}, recipe={status?.Recipe}");

var waferInfo = new WaferInfo
{
    LotId = "LOT-RAW-REST-001",
    WaferId = "W01",
    RecipeId = "RCP-A",
    Slot = "1",
    FoupId = "FOUP-A",
    ChamberId = "CH-1",
};

var response = await http.PostAsJsonAsync(RestRoutes.ApiWaferInfo.TrimStart('/'), waferInfo, ProtocolJson.Options);
response.EnsureSuccessStatusCode();
Console.WriteLine("WaferInfo updated through raw REST.");
