# Virex.NET Integration Kit

Virex.NET Integration Kit는 Virex.NET-compatible services와 통합하는 customer systems를 위한 public SDK, simulator, sample, protocol documentation package입니다. Public communication contracts와 simulator에서 검증 가능한 behavior만 문서화합니다.

Private Virex.NET application은 이 repository에 포함되지 않습니다.

## Default Simulator Endpoints

| Interface | Default |
| --- | --- |
| REST | `http://127.0.0.1:5088` |
| TCP | `127.0.0.1:5089` |
| MQTT | `127.0.0.1:1883`, base topic `virex` |
| SDK | `Virex.NET.Client` |

## Product Purpose

Production-compatible service에 연결하기 전에 customer-side integrations를 구축하고 검증하는 데 이 kit를 사용합니다.

| Area | Purpose |
| --- | --- |
| `Virex.NET.Contracts` | Public DTOs, routes, topic names, TCP/NDJSON formatters, parsers. |
| `Virex.NET.Client` | REST commands/queries, TCP events, MQTT events용 C# client wrappers. |
| `Virex.NET.Simulator.WPF` | REST, TCP, MQTT endpoints를 제공하는 local simulator. |
| `samples` | C#, Python, C++ clients용 guided demos. |
| `docs` | Customer documentation 및 raw protocol references. |

## Quick Start

처음 사용하는 경우 simulator와 C# SDK sample을 실행하십시오. Sample은 **Start Servers**, **Initialize** 전 예상되는 `not_initialized` response, 그리고 **Initialize** 후 normal cycle을 안내합니다.

1. Kit를 build/test합니다.

   ```powershell
   dotnet test Virex.NET.Integration.slnx
   ```

2. Simulator를 시작합니다.

   ```powershell
   dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
   ```

3. Simulator window에서 default endpoints를 유지하고 **Start Servers** 를 누릅니다.

4. 두 번째 terminal에서 SDK sample을 실행합니다.

   ```powershell
   dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj
   ```

5. Sample prompts를 따릅니다. 먼저 `HTTP 409 not_initialized` 를 보여준 뒤 **Initialize** 를 누르도록 안내하고, WaferInfo, cycle start, result query로 진행합니다.

Expected Event Log examples:

```text
WaferInfo updated from REST: lotId=LOT-RAW-REST-001, waferId=W01, recipeId=RCP-A, slot=1, foupId=FOUP-A, chamberId=CH-1
WaferInfo updated from TCP: lotId=LOT-RAW-TCP-001, waferId=W01, recipeId=RCP-A, slot=1, foupId=FOUP-A, chamberId=CH-1
```

Button-by-button workflow는 [Simulator Manual](simulator.md)을 참조하십시오.

## SDK Usage

C# applications는 `VirexClient` 로 시작하는 것이 좋습니다. REST, TCP, MQTT settings를 중앙에서 관리하고 common integration operations를 제공합니다.

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
await client.StartAsync();

var results = await client.QueryResultsAsync(lotId: "LOT-001");
```

Main SDK methods:

- `GetStatusAsync`
- `SetWaferInfoAsync`
- `InitializeAsync`
- `TerminateAsync`
- `StartAsync`
- `StopAsync`
- `QueryResultsAsync`

Non-success REST responses는 HTTP status code와 response body를 포함한 `VirexClientException` 을 throw합니다. Guided samples에서 **Initialize** 전 `HTTP 409 not_initialized` 는 expected simulator state이며 connection failure가 아닙니다.

`TcpFrameTimeoutMs` 는 partial frame data가 도착한 뒤 TCP/NDJSON read를 보호합니다. Complete frames 사이의 긴 idle gap은 허용되지만, client가 frame의 첫 byte를 받은 뒤에는 해당 frame의 나머지와 `\n` 이 configured timeout 안에 도착해야 합니다.

## Interface Selection

| Interface | Best fit | Direction | Typical use |
| --- | --- | --- | --- |
| REST | Commands and queries | Client to service | Read status, send WaferInfo, control cycles, query results. |
| TCP / NDJSON | Direct socket integration | Bidirectional | Command frames를 보내고 status, wafer-info, result, error events를 수신합니다. |
| MQTT | Event subscription | Outbound events only | Broker를 통해 status, wafer-info, result, error를 subscribe합니다. MQTT는 command/control에 사용하지 않습니다. |

정확한 JSON bodies, fields, topics, frames는 [Transmitted Content / Payloads](payloads.md)를 참조하십시오.

## Typical Use Cases

각 use case는 simulator UI state와 sample output으로 모두 검증됩니다. 먼저 condition을 확인하고 지정된 UI button을 누른 뒤 console output과 Event Log를 비교하십시오.

| Use case | Required state | UI action | Sample | Expected output |
| --- | --- | --- | --- | --- |
| Start communication services | Simulator is open | Press **Start Servers** | Any sample | Event Log shows REST/TCP/MQTT listening; samples can connect. |
| `not_initialized` | **Start Servers** pressed, **Initialize** not pressed | Do not press **Initialize** yet | C# SDK, raw REST | Start returns HTTP `409` / `not_initialized`. |
| Normal cycle | `processState=ready` | Press **Initialize**, then continue the sample | C# SDK, raw REST | State transitions through `capturing`, `inspecting`, `saving`, and `ready`; a result is created. |
| WaferInfo update verification | **Start Servers** pressed | Press **Apply WaferInfo** or send from sample | SDK, REST, TCP | Event Log lists `lotId`, `waferId`, `recipeId`, `slot`, `foupId`, and `chamberId` in one line. |
| MQTT event observation | MQTT sample subscribed to `virex/#` | Press **Apply WaferInfo**, **Initialize**, **Start Cycle**, **Emit Fake Result**, **Emit Error** | Raw MQTT | Console shows `wafer-info`, `status`, `result`, and `error` topics. |
| Result query | Cycle completed or **Emit Fake Result** pressed | Run result query from SDK/REST sample | SDK, REST | Console prints result count filtered by matching wafer context. |

## Main Data Contracts

| Contract | Public fields |
| --- | --- |
| WaferInfo | `lotId`, `waferId`, `recipeId`, `slot`, `foupId`, `chamberId` |
| Status | `initialized`, `processState`, `recipe` |
| Result summary | `resultId`, `timestamp`, wafer context, `overallResult`, total `defectCount`, result/image paths |
| Error | `hasError`, `message`, `initialized`, `processState`, `recipe` |

Result responses 및 result events는 summaries만 제공합니다. Defect lists, die lists, crop lists, image binaries, private inspection details는 포함하지 않습니다.

## Troubleshooting

| Symptom | Check |
| --- | --- |
| REST request fails | Simulator가 running인지, `RestBaseUrl` 이 올바른지, firewall access가 허용되는지 확인하고 `VirexClientException` response body를 확인하십시오. |
| TCP events are missing | Host/port를 확인하고 각 NDJSON frame이 newline으로 끝나는지, event loop가 계속 실행 중인지 확인하십시오. |
| MQTT events are missing | Broker connectivity, port `1883`, base topic `virex`, subscription topic tree를 확인하십시오. |
| Result query is empty | Simulator가 result를 emit했고 `lotId`, `waferId`, `recipeId` filters가 submitted WaferInfo와 일치하는지 확인하십시오. |

## References

- [Simulator Manual](simulator.md)
- [State Machine](state-machine.md)
- [Transmitted Content / Payloads](payloads.md)
- [Samples](samples.md)
- [REST API](rest-api.md)
- [TCP Socket Protocol](tcp-socket.md)
- [MQTT Events](mqtt-events.md)
- [Protocol Versioning](protocol-versioning.md)
