# C# SDK ガイド

`Virex.NET.Client` は、公開 RESTful API、TCP、および MQTT 統合 API の型付きラッパーを提供します。

## インストール

```powershell
dotnet add package Virex.NET.Client
```

公開 データモデルとプロトコル ヘルパーのみが必要な場合:

```powershell
dotnet add package Virex.NET.Contracts
```

## クライアントの作成

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

## RESTful API コマンド/クエリの流れ

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

## TCP イベント

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

## MQTT イベント

```csharp
client.MqttEvents.EventReceived += (_, value) =>
{
    Console.WriteLine($"MQTT {value.Type}");
};

using var cts = new CancellationTokenSource();
await client.MqttEvents.RunAsync(cts.Token);
```

MQTT はイベントにのみ使用されます。コマンドにはRESTまたはTCPを使用してください。

## エラー処理

RESTful API 通信の失敗と成功しない HTTP 応答は `VirexClientException` をスローします。プロトコルレベルのコマンド拒否は、`CommandResponse` で表されます。

```csharp
var response = await client.StartAsync();
if (!response.Accepted && response.ErrorCode == CommandErrorCodes.InvalidState)
{
    Console.WriteLine($"Start rejected in state {response.State}");
}
```

`invalid_state` は通常のコマンド検証動作であり、トランスポートの失敗ではありません。
