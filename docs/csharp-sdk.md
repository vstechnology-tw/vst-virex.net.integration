# C# SDK Guide

`Virex.NET.Client` provides typed wrappers for the public RESTful API, TCP, and MQTT integration APIs.

## Install

```powershell
dotnet add package Virex.NET.Client
```

If you only need public data models and protocol helpers:

```powershell
dotnet add package Virex.NET.Contracts
```

## Create Client

```csharp
using Virex.NET.Client;

using var client = new VirexClient(new VirexClientOptions
{
    RestBaseUrl = "http://127.0.0.1:5088",
    TcpHost = "127.0.0.1",
    TcpPort = 5089,
    MqttHost = "127.0.0.1",
    MqttPort = 1883,
    MqttTopic = "virex",
    TimeoutMs = 5000,
    TcpFrameTimeoutMs = 5000,
});
```

## RESTful API Command/Query Flow

```csharp
using Virex.NET.Contracts;

var status = await client.GetStatusAsync();

var initialize = await client.InitializeAsync();
if (!initialize.Accepted)
    throw new InvalidOperationException(initialize.Message);

await client.SetProductInfoAsync(new ProductInfo
{
    WaferID = "W01",
    LotID = "LOT-001",
    Recipe = "RCP-A",
    Slot = "1",
    FoupID = "FOUP-A",
    ChamberID = "CH-1",
});

var start = await client.StartAsync("golden-sample", ControlRunModes.Continue);
Console.WriteLine(start.State); // Running

var results = await client.QueryResultsAsync(lotID: "LOT-001");
```

## TCP events

```csharp
client.TcpEvents.EventReceived += (_, value) =>
{
    Console.WriteLine($"TCP {value.Type}");
};

using var cts = new CancellationTokenSource();
var tcpTask = client.TcpEvents.RunAsync(cts.Token);

await client.TcpEvents.SendProductInfoAsync(new ProductInfo
{
    WaferID = "W01",
    LotID = "LOT-TCP-001",
    Recipe = "RCP-A",
});

await client.TcpEvents.SendStartAsync("tcp-check", ControlRunModes.Continue);
```

TCP also supports RESTful API equivalent query frames:

```csharp
var tcpStatus = await client.TcpEvents.GetStatusAsync();
var tcpError = await client.TcpEvents.GetErrorAsync();
var tcpProductInfo = await client.TcpEvents.GetProductInfoAsync();
var tcpResults = await client.TcpEvents.QueryResultsAsync(lotID: "LOT-001");
```

## MQTT commands and events

MQTT command calls publish to `virex/commands/...` and wait for a correlated response on `virex/responses/{correlationId}`:

```csharp
var mqttStatus = await client.MqttCommands.GetStatusAsync();
var mqttInitialize = await client.MqttCommands.InitializeAsync();
var mqttStart = await client.MqttCommands.StartAsync("mqtt-check", ControlRunModes.Continue);
var mqttResults = await client.MqttCommands.QueryResultsAsync(lotID: "LOT-001");
```

```csharp
client.MqttEvents.EventReceived += (_, value) =>
{
    Console.WriteLine($"MQTT {value.Type}");
};

using var cts = new CancellationTokenSource();
await client.MqttEvents.RunAsync(cts.Token);
```

## Error handling

RESTful API transport failures and non-success HTTP responses throw `VirexClientException`. Protocol-level command rejections are represented by `CommandResponse`:

```csharp
var response = await client.StartAsync();
if (!response.Accepted && response.ErrorCode == CommandErrorCodes.InvalidState)
{
    Console.WriteLine($"Start rejected in state {response.State}");
}
```

`invalid_state` is normal command validation behavior, not a transport failure.
