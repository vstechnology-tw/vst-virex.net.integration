using System.Collections.Concurrent;
using System.Text;
using MQTTnet;
using MQTTnet.Client;
using Virex.NET.Contracts;

var host = args.Length > 0 ? args[0] : "127.0.0.1";
var port = args.Length > 1 ? int.Parse(args[1]) : 1883;
var baseTopic = args.Length > 2 ? args[2] : MqttTopics.DefaultBaseTopic;
var product = new ProductInfo
{
    WaferID = "WCS-MQTT-210-001",
    LotID = "LOT-CS-MQTT-210",
    Recipe = "RCP-DEMO",
    Slot = "1",
    FoupID = "FOUP-DEMO",
    ChamberID = "CH-1",
};

PrintStep("Virex.NET C# Raw MQTT 13-Step Demo");
Console.WriteLine($"MQTT endpoint: {host}:{port}");
Console.WriteLine($"Topic filter: {MqttTopics.Combine(baseTopic, "#")}");
Prompt("In Simulator, press Start Servers, then press Enter here.");

var factory = new MqttFactory();
using var client = factory.CreateMqttClient();
using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var responseWaiters = new ConcurrentDictionary<string, TaskCompletionSource<string>>();
var eventWaiter = new TaskCompletionSource<string>();

client.ApplicationMessageReceivedAsync += e =>
{
    var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment.ToArray());
    var line = $"{e.ApplicationMessage.Topic}: {payload}";
    Console.WriteLine(line);

    if (responseWaiters.TryRemove(e.ApplicationMessage.Topic, out var response))
        response.TrySetResult(payload);
    else if (!e.ApplicationMessage.Topic.Contains("/responses/", StringComparison.OrdinalIgnoreCase))
        eventWaiter.TrySetResult(line);

    return Task.CompletedTask;
};

var options = new MqttClientOptionsBuilder()
    .WithTcpServer(host, port)
    .WithCleanSession()
    .Build();

try
{
    await client.ConnectAsync(options, timeout.Token);
}
catch (Exception ex)
{
    Console.WriteLine("Connection failed. In Simulator, press Start Servers and verify the MQTT host, port, and topic.");
    Console.WriteLine(ex.Message);
    return 1;
}

var subscribeOptions = factory.CreateSubscribeOptionsBuilder()
    .WithTopicFilter(f => f.WithTopic(MqttTopics.Combine(baseTopic, "#")))
    .Build();
await client.SubscribeAsync(subscribeOptions, timeout.Token);

PrintStep("Step 1 - Query status");
await PublishCommandAsync(MqttTopics.CommandStatusGet, new MqttCommandRequest { CorrelationId = "csharp-raw-mqtt-status-1" });

PrintStep("Step 2 - Query error");
await PublishCommandAsync(MqttTopics.CommandErrorGet, new MqttCommandRequest { CorrelationId = "csharp-raw-mqtt-error-2" });

PrintStep("Step 3 - Query ProductInfo");
await PublishCommandAsync(MqttTopics.CommandProductInfoGet, new MqttCommandRequest { CorrelationId = "csharp-raw-mqtt-product-get-3" });

PrintStep("Step 4 - Initialize");
await PublishCommandAsync(MqttTopics.CommandSystemInitialize, new MqttCommandRequest { CorrelationId = "csharp-raw-mqtt-initialize-4" });

PrintStep("Step 5 - Confirm Ready");
await PublishCommandAsync(MqttTopics.CommandStatusGet, new MqttCommandRequest { CorrelationId = "csharp-raw-mqtt-ready-5" });

PrintStep("Step 6 - Set ProductInfo");
await PublishCommandAsync(MqttTopics.CommandProductInfoSet, new MqttCommandRequest { CorrelationId = "csharp-raw-mqtt-product-set-6", ProductInfo = product });

PrintStep("Step 7 - Confirm ProductInfo");
await PublishCommandAsync(MqttTopics.CommandProductInfoGet, new MqttCommandRequest { CorrelationId = "csharp-raw-mqtt-product-confirm-7" });

PrintStep("Step 8 - Start run");
eventWaiter = new TaskCompletionSource<string>();
await PublishCommandAsync(MqttTopics.CommandSystemStart, new MqttCommandRequest { CorrelationId = "csharp-raw-mqtt-start-8", Condition = "golden-sample", RunMode = ControlRunModes.Continue });

PrintStep("Step 9 - Observe run events");
Console.WriteLine(await WaitForEventAsync());

PrintStep("Step 10 - Stop run");
await PublishCommandAsync(MqttTopics.CommandSystemStop, new MqttCommandRequest { CorrelationId = "csharp-raw-mqtt-stop-10", Reason = "operator-request" });

PrintStep("Step 11 - Query results");
await PublishCommandAsync(MqttTopics.CommandResultsQuery, new MqttCommandRequest { CorrelationId = "csharp-raw-mqtt-results-11", LotID = product.LotID, WaferID = product.WaferID });

PrintStep("Step 12 - Deinitialize");
await PublishCommandAsync(MqttTopics.CommandSystemDeinitialize, new MqttCommandRequest { CorrelationId = "csharp-raw-mqtt-deinitialize-12" });

PrintStep("Step 13 - Confirm Uninitialized");
await PublishCommandAsync(MqttTopics.CommandStatusGet, new MqttCommandRequest { CorrelationId = "csharp-raw-mqtt-uninitialized-13" });

if (client.IsConnected)
    await client.DisconnectAsync(new MqttClientDisconnectOptions(), CancellationToken.None);

return 0;

async Task PublishCommandAsync(string commandTopic, MqttCommandRequest request)
{
    var responseTopic = MqttTopics.ResponseTopic(baseTopic, request.CorrelationId);
    var waiter = new TaskCompletionSource<string>();
    responseWaiters[responseTopic] = waiter;

    var message = new MqttApplicationMessageBuilder()
        .WithTopic(MqttTopics.Combine(baseTopic, commandTopic))
        .WithPayload(Encoding.UTF8.GetBytes(ProtocolJson.Serialize(request)))
        .Build();
    await client.PublishAsync(message, timeout.Token);
    Console.WriteLine($"Published {MqttTopics.Combine(baseTopic, commandTopic)}.");

    var completed = await Task.WhenAny(waiter.Task, Task.Delay(TimeSpan.FromSeconds(5), timeout.Token));
    if (completed != waiter.Task)
        throw new TimeoutException($"Response not received on {responseTopic}.");
}

async Task<string> WaitForEventAsync()
{
    var completed = await Task.WhenAny(eventWaiter.Task, Task.Delay(TimeSpan.FromSeconds(2), timeout.Token));
    return completed == eventWaiter.Task
        ? await eventWaiter.Task
        : "No event observed within 2 seconds.";
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
