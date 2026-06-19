using Virex.NET.Client;
using Virex.NET.Contracts;

using var mqttTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
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
client.MqttEvents.EventReceived += (_, e) => Console.WriteLine($"MQTT {e.Type}: {e.RawJson}");

Console.WriteLine("Subscribing to MQTT events...");
var mqttTask = client.MqttEvents.RunAsync(mqttTimeout.Token);
await Task.Delay(500);

Console.WriteLine("Reading status...");
var status = await client.GetStatusAsync();
Console.WriteLine($"{status.Initialized} {status.ProcessState} recipe={status.Recipe}");

Console.WriteLine("Updating wafer info...");
await client.SetWaferInfoAsync(new WaferInfo
{
    LotId = "LOT-SDK-001",
    WaferId = "W01",
    RecipeId = "RCP-A",
    Slot = "1",
    FoupId = "FOUP-A",
    ChamberId = "CH-1",
});

Console.WriteLine("Starting inspection cycle...");
await client.StartAsync();

Console.WriteLine("Querying latest results...");
var results = await client.QueryResultsAsync(lotId: "LOT-SDK-001");
Console.WriteLine($"Result count: {results.Count}");

mqttTimeout.Cancel();
await mqttTask;
