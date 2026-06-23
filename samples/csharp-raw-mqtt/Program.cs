using System.Text;
using MQTTnet;
using MQTTnet.Client;
using Virex.NET.Contracts;

var host = args.Length > 0 ? args[0] : "127.0.0.1";
var port = args.Length > 1 ? int.Parse(args[1]) : 1883;
var baseTopic = args.Length > 2 ? args[2] : MqttTopics.DefaultBaseTopic;
var duration = args.Length > 3 ? TimeSpan.FromSeconds(int.Parse(args[3])) : TimeSpan.FromSeconds(30);

PrintStep("Virex.NET C# Raw MQTT Guided Demo");
Console.WriteLine("This sample subscribes to simulator MQTT events and lets you trigger each event from the UI.");
Console.WriteLine($"MQTT endpoint: {host}:{port}");
Console.WriteLine($"Topic filter: {MqttTopics.Combine(baseTopic, "#")}");
Prompt("In Simulator, press Start Servers, then press Enter here.");

var factory = new MqttFactory();
using var client = factory.CreateMqttClient();
using var timeout = new CancellationTokenSource(duration);

client.ApplicationMessageReceivedAsync += e =>
{
    var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment.ToArray());
    Console.WriteLine($"{e.ApplicationMessage.Topic}: {payload}");
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

PrintStep("Step 1 - Trigger events from Simulator");
Console.WriteLine($"Subscribed to {baseTopic}/# for {duration.TotalSeconds:0} seconds.");
Console.WriteLine("Expected UI actions and topics:");
Console.WriteLine("- Press Apply WaferInfo: expect virex/wafer-info.");
Console.WriteLine("- Press Initialize: expect virex/status with initialized=true.");
Console.WriteLine("- Press Start Cycle: expect virex/status transitions.");
Console.WriteLine("- Press Emit Fake Result: expect virex/result.");
Console.WriteLine("- Press Emit Error: expect virex/error.");

try
{
    await Task.Delay(Timeout.InfiniteTimeSpan, timeout.Token);
}
catch (OperationCanceledException)
{
}

if (client.IsConnected)
    await client.DisconnectAsync(new MqttClientDisconnectOptions(), CancellationToken.None);

return 0;

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
