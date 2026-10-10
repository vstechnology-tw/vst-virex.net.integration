# C# SDK 안내

2.4.0 개발 API의 `GetSoftwareVersionAsync()`(`GET /api/software-version`)는 REST만 사용하며 대상 App의 실제 버전과 빌드 접미사를 읽습니다. API/응답 형식 오류는 `VirexQueryException`으로 보고하며 호출자 취소를 유지합니다. Simulator는 실행 중인 server 어셈블리 버전을 반환합니다.

현재 2.4.0 소스는 미공개 후보입니다. [후보 검증(영문)](candidate-acceptance.md) 및 `samples/csharp-sdk-24`는 REST, 정확한 이름의 TCP 이벤트, 명시적 ResultId, Alarm 및 두 단계 복구를 사용합니다. 이전 SDK/wire 호환성은 2.4.0 필수 검증이 아닙니다.

2.3.0 버전은 VirexClient와 VirexRestClient에 `GetRecipesAsync`, `GetCurrentRecipeAsync`, `GetCurrentRecipeParametersAsync`, `GetResultDetailAsync`를 추가합니다. [DTO 및 오류 계약(영문)](read-only-queries.md)을 참고하세요.


`Virex.NET.Client`는 공개 RESTful API, TCP 및 MQTT 통합 API에 대한 타입이 지정된 래퍼를 제공합니다.

## 전체 예제

모든 `using` 지시문과 project reference가 포함된 실행 가능한 C# 프로그램은[C# SDK 샘플](samples.ko.md)을 사용하세요. 아래 코드 블록은 완전한 파일에서 추린 작업 예제입니다.

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
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Virex.NET.Contracts;
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
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Virex.NET.Client;
using Virex.NET.Contracts;

using var client = new VirexClient(new VirexClientOptions
{
    RestBaseUrl = "http://127.0.0.1:5088",
    TcpHost = "127.0.0.1",
    TcpPort = 5089,
    MqttHost = "127.0.0.1",
    MqttPort = 1883,
    MqttTopic = "virex",
});
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

## 복구 및 Deinitialize 재시도

2.4.0 분류된 장애는 공개 상태 `Faulted`와 `recoveryAction: "Reset"`을 반환합니다. Reset은 중지와 필요한 정리를 완료하며 성공하면 `Uninitialized`가 됩니다. `cleanup_failed`는 `Faulted`를 유지합니다. 이후 명시적 Initialize만 `Ready`로 돌아가며 이전 작업을 재개하지 않습니다. 실제 state, errorCode와 복구 정보를 확인하십시오. `RequiresDeinitialize`는 공개 상태가 아닙니다.

```csharp
var status = await client.GetStatusAsync();
if (status.RecoveryAction == RecoveryActions.Reset)
{
    var cleanup = await client.ResetAsync();
    if (cleanup.Accepted) await client.InitializeAsync();
    Console.WriteLine($"Recovery: accepted={cleanup.Accepted}, phase={cleanup.RecoveryPhase}, details={cleanup.RecoveryDetails}");
}
```

2.4.0 분류된 장애는 공개 상태 `Faulted`와 `recoveryAction: "Reset"`을 반환합니다. Reset은 중지와 필요한 정리를 완료하며 성공하면 `Uninitialized`가 됩니다. `cleanup_failed`는 `Faulted`를 유지합니다. 이후 명시적 Initialize만 `Ready`로 돌아가며 이전 작업을 재개하지 않습니다. 실제 state, errorCode와 복구 정보를 확인하십시오. `RequiresDeinitialize`는 공개 상태가 아닙니다.

## TCP 이벤트

```csharp
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Virex.NET.Client;
using Virex.NET.Contracts;
using var client = new VirexClient(new VirexClientOptions
{
    RestBaseUrl = "http://127.0.0.1:5088",
    TcpHost = "127.0.0.1",
    TcpPort = 5089,
    MqttHost = "127.0.0.1",
    MqttPort = 1883,
    MqttTopic = "virex",
});
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

`value.Type`이 `imageGrabbed`이면 `value.ImageGrabbed`를 읽습니다. `captureId`는 이후 `resultCreated` 이벤트와 일치하며 artifact 경로는 해당 이벤트에서 제공됩니다.

## MQTT 이벤트

```csharp
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Virex.NET.Client;
using Virex.NET.Contracts;
using var client = new VirexClient(new VirexClientOptions
{
    RestBaseUrl = "http://127.0.0.1:5088",
    TcpHost = "127.0.0.1",
    TcpPort = 5089,
    MqttHost = "127.0.0.1",
    MqttPort = 1883,
    MqttTopic = "virex",
});
client.MqttEvents.EventReceived += (_, value) =>
{
    Console.WriteLine($"MQTT {value.Type}");
};

using var cts = new CancellationTokenSource();
await client.MqttEvents.RunAsync(cts.Token);
```

MQTT는 이벤트에만 사용됩니다. 명령에는 RESTful API 또는 TCP를 사용하십시오.

`value.Type`이 `imageGrabbed`이면 `value.ImageGrabbed`를 읽습니다. `captureId`는 이후 `resultCreated` 이벤트와 일치하며 artifact 경로는 해당 이벤트에서 제공됩니다.

## 오류 처리

RESTful API 전송 방식 오류 및 성공하지 않은 HTTP 응답으로 인해 `VirexClientException`가 발생합니다. 프로토콜 수준 명령 거부는 `CommandResponse`로 표시됩니다.

```csharp
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Virex.NET.Client;
using Virex.NET.Contracts;
using var client = new VirexClient(new VirexClientOptions
{
    RestBaseUrl = "http://127.0.0.1:5088",
    TcpHost = "127.0.0.1",
    TcpPort = 5089,
    MqttHost = "127.0.0.1",
    MqttPort = 1883,
    MqttTopic = "virex",
});
var response = await client.StartAsync();
if (!response.Accepted && response.ErrorCode == CommandErrorCodes.InvalidState)
{
    Console.WriteLine($"Start rejected in state {response.State}");
}
```

`invalid_state`는 전송 오류가 아닌 일반적인 명령 유효성 검사 동작입니다.
