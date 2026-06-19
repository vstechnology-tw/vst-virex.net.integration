using System.Text;
using MQTTnet;
using MQTTnet.Client;
using Virex.NET.Contracts;

var host = args.Length > 0 ? args[0] : "127.0.0.1";
var port = args.Length > 1 ? int.Parse(args[1]) : 1883;
var baseTopic = args.Length > 2 ? args[2] : MqttTopics.DefaultBaseTopic;
var duration = args.Length > 3 ? TimeSpan.FromSeconds(int.Parse(args[3])) : TimeSpan.FromSeconds(30);

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

await client.ConnectAsync(options, timeout.Token);
var subscribeOptions = factory.CreateSubscribeOptionsBuilder()
    .WithTopicFilter(f => f.WithTopic(MqttTopics.Combine(baseTopic, "#")))
    .Build();
await client.SubscribeAsync(subscribeOptions, timeout.Token);

Console.WriteLine($"Subscribed to {baseTopic}/# for {duration.TotalSeconds:0} seconds.");
Console.WriteLine("Trigger simulator events with Apply WaferInfo, Start Cycle, Emit Fake Result, or Emit Error.");

try
{
    await Task.Delay(Timeout.InfiniteTimeSpan, timeout.Token);
}
catch (OperationCanceledException)
{
}

if (client.IsConnected)
    await client.DisconnectAsync(new MqttClientDisconnectOptions(), CancellationToken.None);

