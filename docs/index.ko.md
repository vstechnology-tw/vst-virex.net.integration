# Virex.NET Integration Kit

Virex.NET Integration Kit는 Virex.NET 호환 서비스와 연동하는 고객 측 시스템을 위한 공개 SDK, 시뮬레이터, 샘플, 프로토콜 문서 패키지입니다. 이 저장소는 공개 통신 계약과 시뮬레이터로 검증할 수 있는 동작만 설명합니다.

비공개 Virex.NET 애플리케이션은 이 저장소에 포함되어 있지 않습니다.

## 기본 시뮬레이터 엔드포인트

| 인터페이스 | 기본값 |
| --- | --- |
| REST | `http://127.0.0.1:5088` |
| TCP | `127.0.0.1:5089` |
| MQTT | `127.0.0.1:1883`, 기본 토픽 `virex` |
| SDK | `Virex.NET.Client` |

## 제품 목적

프로덕션 호환 서비스에 연결하기 전에 고객 측 연동을 구축하고 검증할 때 이 키트를 사용합니다.

| 영역 | 목적 |
| --- | --- |
| `Virex.NET.Contracts` | 공개 DTO, 라우트, 토픽 이름, TCP/NDJSON 포매터와 파서. |
| `Virex.NET.Client` | REST 명령과 조회, TCP 이벤트, MQTT 이벤트를 위한 C# 클라이언트 래퍼. |
| `Virex.NET.Simulator.WPF` | REST, TCP, MQTT 엔드포인트를 제공하는 로컬 시뮬레이터. |
| `samples` | C#, Python, C++ 클라이언트를 위한 안내형 데모. |
| `docs` | 고객 문서와 원시 프로토콜 참조. |

## 빠른 시작

처음 사용하는 경우 시뮬레이터와 C# SDK 샘플을 실행하십시오. 샘플은 **Start Servers**, **Initialize** 전에 예상되는 `not_initialized` 응답, 그리고 **Initialize** 후의 정상 사이클을 순서대로 안내합니다.

미리 빌드된 시뮬레이터 다운로드, NuGet 패키지, 샘플 코드 링크는 [설치 / 다운로드](installation.md)를 참조하십시오.

1. 키트를 빌드하고 테스트합니다.

   ```powershell
   dotnet test Virex.NET.Integration.slnx
   ```

2. 시뮬레이터를 시작합니다.

   ```powershell
   dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
   ```

3. 시뮬레이터 창에서 기본 엔드포인트를 유지하고 **Start Servers** 를 누릅니다.

4. 두 번째 터미널에서 SDK 샘플을 실행합니다.

   ```powershell
   dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj
   ```

5. 샘플 안내를 따릅니다. 먼저 `HTTP 409 not_initialized` 를 확인한 뒤 **Initialize** 를 누르고, WaferInfo, 사이클 시작, 결과 조회로 진행합니다.

예상 Event Log 예:

```text
WaferInfo updated from REST: lotId=LOT-RAW-REST-001, waferId=W01, recipeId=RCP-A, slot=1, foupId=FOUP-A, chamberId=CH-1
WaferInfo updated from TCP: lotId=LOT-RAW-TCP-001, waferId=W01, recipeId=RCP-A, slot=1, foupId=FOUP-A, chamberId=CH-1
```

버튼별 전체 절차는 [시뮬레이터 설명서](simulator.md)를 참조하십시오.

## SDK 사용

C# 애플리케이션은 `VirexClient` 에서 시작하는 것이 좋습니다. `VirexClient` 는 REST, TCP, MQTT 설정을 한곳에서 관리하고 일반적인 연동 작업을 제공합니다.

```csharp
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
    TimeoutMs = 5000,
    TcpFrameTimeoutMs = 5000,
});

var status = await client.GetStatusAsync();

// REST 명령/조회 헬퍼는 SDK 의 기본 고수준 경로입니다.
await client.SetWaferInfoAsync(new WaferInfo
{
    LotId = "LOT-001",
    WaferId = "W01",
    RecipeId = "RCP-A",
    Slot = "1",
    FoupId = "FOUP-A",
    ChamberId = "CH-1",
});

await client.InitializeAsync();
await client.StartAsync("golden-sample", ControlRunModes.Continue);

var results = await client.QueryResultsAsync(lotId: "LOT-001");

// TCP 는 TcpEvents 를 통해 명시적으로 선택합니다.
await client.TcpEvents.SendWaferInfoAsync(new WaferInfo
{
    LotId = "LOT-TCP-001",
    WaferId = "W01",
    RecipeId = "RCP-A",
});
await client.TcpEvents.SendStartAsync("golden-sample", ControlRunModes.Continue);

// TCP 와 MQTT 이벤트 리스너는 명시적으로 시작합니다.
using var eventCts = new CancellationTokenSource();
client.TcpEvents.EventReceived += (_, value) =>
    Console.WriteLine("TCP event: " + value.Type);
client.MqttEvents.EventReceived += (_, value) =>
    Console.WriteLine("MQTT event: " + value.Type);

_ = client.TcpEvents.RunAsync(eventCts.Token);
_ = client.MqttEvents.RunAsync(eventCts.Token);

// MQTT 는 이벤트 전용이며 명령/제어에는 사용하지 않습니다.
// 애플리케이션의 수신을 종료할 때 eventCts.Cancel() 을 호출합니다.
```

주요 SDK 메서드:

- `GetStatusAsync`
- `SetWaferInfoAsync`
- `InitializeAsync`
- `TerminateAsync`
- `StartAsync`
- `StopAsync`
- `QueryResultsAsync`

성공이 아닌 REST 응답은 HTTP 상태 코드와 응답 본문을 포함한 `VirexClientException` 을 발생시킵니다. 안내형 샘플에서 **Initialize** 전의 `HTTP 409 not_initialized` 는 예상된 시뮬레이터 상태이며 연결 실패가 아닙니다.

`TcpFrameTimeoutMs` 는 TCP/NDJSON 읽기 중 프레임 일부가 도착한 뒤의 대기 시간을 보호합니다. 완전한 프레임 사이의 긴 유휴 시간은 허용되지만, 클라이언트가 프레임의 첫 바이트를 받은 뒤에는 해당 프레임의 나머지 바이트와 종료 개행 문자 `\n` 이 설정된 제한 시간 안에 도착해야 합니다.

## 인터페이스 선택

| 인터페이스 | 적합한 용도 | 방향 | 일반적인 사용 |
| --- | --- | --- | --- |
| REST | 명령과 조회 | 클라이언트에서 서비스로 | 상태 읽기, WaferInfo 전송, 사이클 제어, 결과 조회. |
| TCP / NDJSON | 직접 소켓 연동 | 양방향 | 명령 프레임을 보내고 status, wafer-info, result, error 이벤트를 수신합니다. |
| MQTT | 이벤트 구독 | 송신 이벤트만 | 브로커를 통해 status, wafer-info, result, error 를 구독합니다. MQTT는 명령/제어에 사용하지 않습니다. |

정확한 JSON 본문, 필드, 토픽, 프레임은 [전송 내용 / 페이로드](payloads.md)를 참조하십시오.

## 일반적인 사용 사례

각 사용 사례는 시뮬레이터 UI 상태와 샘플 출력으로 모두 검증합니다. 먼저 조건을 확인하고 지정된 UI 버튼을 누른 다음, 콘솔 출력과 Event Log 를 비교하십시오.

| 사용 사례 | 필요한 상태 | UI 작업 | 샘플 | 예상 출력 |
| --- | --- | --- | --- | --- |
| 통신 서비스 시작 | 시뮬레이터가 열려 있음 | **Start Servers** 를 누름 | 모든 샘플 | Event Log 에 REST/TCP/MQTT 대기 상태가 표시되고 샘플이 연결할 수 있음. |
| `not_initialized` | **Start Servers** 는 눌렀지만 **Initialize** 는 누르지 않음 | 아직 **Initialize** 를 누르지 않음 | C# SDK, raw REST | Start 가 HTTP `409` / `not_initialized` 를 반환함. |
| 정상 사이클 | `processState=ready` | **Initialize** 를 누른 뒤 샘플 계속 진행 | C# SDK, raw REST | 상태가 `capturing`, `inspecting`, `saving`, `ready` 순서로 바뀌고 결과가 생성됨. |
| WaferInfo 업데이트 검증 | **Start Servers** 가 눌려 있음 | UI에서 **Apply WaferInfo** 를 누르거나 샘플에서 전송 | SDK, REST, TCP | Event Log 에 `lotId`, `waferId`, `recipeId`, `slot`, `foupId`, `chamberId` 가 한 줄로 표시됨. |
| MQTT 이벤트 확인 | MQTT 샘플이 `virex/#` 를 구독 중 | **Apply WaferInfo**, **Initialize**, **Start Cycle**, **Emit Fake Result**, **Emit Error** 를 누름 | Raw MQTT | 콘솔에 `wafer-info`, `status`, `result`, `error` 토픽이 표시됨. |
| 결과 조회 | 사이클이 완료되었거나 **Emit Fake Result** 가 눌림 | SDK/REST 샘플에서 결과 조회 실행 | SDK, REST | 일치하는 웨이퍼 컨텍스트로 필터링된 결과 개수가 콘솔에 표시됨. |

## 주요 데이터 계약

| 계약 | 공개 필드 |
| --- | --- |
| WaferInfo | `lotId`, `waferId`, `recipeId`, `slot`, `foupId`, `chamberId` |
| Status | `initialized`, `processState`, `recipe` |
| Result summary | `resultId`, `timestamp`, 웨이퍼 컨텍스트, `overallResult`, 전체 `defectCount`, 결과/이미지 경로 |
| Error | `hasError`, `message`, `initialized`, `processState`, `recipe` |

결과 응답과 결과 이벤트는 요약만 제공합니다. 결함 목록, 다이 목록, 크롭 목록, 이미지 바이너리, 비공개 검사 세부 정보는 포함하지 않습니다.

## 문제 해결

| 증상 | 확인 사항 |
| --- | --- |
| REST 요청 실패 | 시뮬레이터가 실행 중인지, `RestBaseUrl` 이 올바른지, 방화벽 접근이 허용되는지 확인하고 `VirexClientException` 의 응답 본문을 확인합니다. |
| TCP 이벤트가 없음 | 호스트/포트를 확인하고, 각 NDJSON 프레임이 개행 문자로 끝나는지, 이벤트 루프가 계속 실행 중인지 확인합니다. |
| MQTT 이벤트가 없음 | 브로커 연결, 포트 `1883`, 기본 토픽 `virex`, 구독 토픽 트리를 확인합니다. |
| 결과 조회가 비어 있음 | 시뮬레이터가 결과를 발행했는지, `lotId`, `waferId`, `recipeId` 필터가 제출한 WaferInfo 와 일치하는지 확인합니다. |

## 참조

- [설치 / 다운로드](installation.md)
- [시뮬레이터 설명서](simulator.md)
- [상태 전이](state-machine.md)
- [전송 내용 / 페이로드](payloads.md)
- [샘플](samples.md)
- [REST API](rest-api.md)
- [TCP 소켓 프로토콜](tcp-socket.md)
- [MQTT 이벤트](mqtt-events.md)
- [프로토콜 버전 관리](protocol-versioning.md)
