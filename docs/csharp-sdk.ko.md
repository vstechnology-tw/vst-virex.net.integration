# C# SDK 안내

`Virex.NET.Client`는 공개 RESTful API, TCP 및 MQTT 통합 API에 대한 타입이 지정된 래퍼를 제공합니다.

## 설치

```powershell
dotnet add package Virex.NET.Client
```

공개 데이터 모델과 프로토콜 도우미만 필요한 경우:

```powershell
dotnet add package Virex.NET.Contracts
```

## 클라이언트 생성

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

## RESTful API 명령/쿼리 흐름

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

## TCP 이벤트

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

## MQTT 이벤트

```csharp
client.MqttEvents.EventReceived += (_, value) =>
{
    Console.WriteLine($"MQTT {value.Type}");
};

using var cts = new CancellationTokenSource();
await client.MqttEvents.RunAsync(cts.Token);
```

MQTT는 이벤트에만 사용됩니다. 명령에는 RESTful API 또는 TCP를 사용하십시오.

## 오류 처리

RESTful API 전송 방식 오류 및 성공하지 않은 HTTP 응답으로 인해 `VirexClientException`가 발생합니다. 프로토콜 수준 명령 거부는 `CommandResponse`로 표시됩니다.

```csharp
var response = await client.StartAsync();
if (!response.Accepted && response.ErrorCode == CommandErrorCodes.InvalidState)
{
    Console.WriteLine($"Start rejected in state {response.State}");
}
```

`invalid_state`는 전송 오류가 아닌 일반적인 명령 유효성 검사 동작입니다.
