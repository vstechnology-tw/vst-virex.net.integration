# Simulator Manual

Virex.NET Integration Simulator는 고객 측 통합 개발을 위한 로컬 테스트 도구입니다. REST, TCP, MQTT 엔드포인트를 제공하고 WaferInfo, 상태 전환, 결과 요약, 오류 이벤트를 시뮬레이션할 수 있습니다.

Repository root에서 시작합니다.

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
```

## Purpose

| Purpose | What to verify |
| --- | --- |
| Validate the SDK | `VirexClient` 가 상태를 읽고 WaferInfo를 제출하며 사이클을 시작하고 결과 요약을 조회할 수 있는지 확인합니다. |
| Validate raw protocols | C#이 아닌 시스템이 REST payload, TCP/NDJSON frame, MQTT topic을 테스트할 수 있게 합니다. |
| Simulate events | Production-compatible service에 연결하기 전에 `status`, `wafer-info`, `result`, `error` 이벤트를 로컬에서 보냅니다. |

## App UI

아래 스크린샷은 가이드 샘플에서 사용하는 시뮬레이터 창입니다.

<figure>
  <div style="position: relative; width: 100%; max-width: 1008px; aspect-ratio: 1008 / 658;">
    <img src="assets/simulator-main-window.png" alt="Virex.NET Integration Simulator main window" style="display: block; width: 100%; height: 100%; object-fit: contain;">
    <span aria-label="Area 1" style="position: absolute; left: 3%; top: 12%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">1</span>
    <span aria-label="Area 2" style="position: absolute; left: 42%; top: 12%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">2</span>
    <span aria-label="Area 3" style="position: absolute; left: 82%; top: 12%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">3</span>
    <span aria-label="Area 4" style="position: absolute; left: 4%; top: 58%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">4</span>
    <span aria-label="Area 5" style="position: absolute; left: 89%; top: 20%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">5</span>
    <span aria-label="Area 6" style="position: absolute; left: 60%; top: 32%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">6</span>
    <span aria-label="Area 7" style="position: absolute; left: 89%; top: 38%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">7</span>
    <span aria-label="Area 8" style="position: absolute; left: 89%; top: 47%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">8</span>
  </div>
  <figcaption>번호 마커는 아래 Area 표에 대응합니다.</figcaption>
</figure>

| Area | Name | Purpose |
| --- | --- | --- |
| 1 | Connection Settings | REST prefix, TCP port, MQTT host/port/topic, result prefix. 테스트 전에 확인합니다. |
| 2 | WaferInfo | Lot ID, Wafer ID, Recipe ID, Slot, FOUP ID, Chamber ID. |
| 3 | State | 현재 `initialized`, `processState`, `recipe` 및 주요 작업 버튼. |
| 4 | Event Log | 서비스 시작, WaferInfo 업데이트, 사이클 이벤트, 결과, 오류 및 기타 실행 기록. |
| 5 | **Start Servers** | 가장 먼저 누르는 버튼입니다. REST/TCP/MQTT 서비스는 이 단계 후 사용할 수 있습니다. |
| 6 | **Apply WaferInfo** | WaferInfo fields를 편집한 후 현재 테스트 wafer context를 적용합니다. |
| 7 | **Start Cycle** | 전체 사이클, 상태 전환, 결과 요약을 시뮬레이션합니다. |
| 8 | **Emit Fake Result** / **Emit Error** | 클라이언트 측 처리 테스트를 위해 결과 또는 오류 이벤트를 수동으로 보냅니다. |

## Standard Operating Procedure

1. Simulator app을 시작합니다.

   ```powershell
   dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
   ```

2. 연결 설정을 확인합니다.

   처음에는 기본값을 유지하십시오.

   | Setting | Default |
   | --- | --- |
   | REST | `http://127.0.0.1:5088/` |
   | TCP | `5089` |
   | MQTT broker | `127.0.0.1:1883` |
   | MQTT topic | `virex` |

3. **Start Servers** 를 누릅니다.

   시작에 성공하면 Event Log에 REST listening, TCP listening, MQTT started/connected 기록이 표시됩니다. SDK 및 샘플 클라이언트는 이 단계 후에만 연결할 수 있습니다.

   REST 검증 페이지는 이 단계 직후 사용할 수 있습니다.

   ```text
   Scalar:       http://127.0.0.1:5088/scalar
   OpenAPI JSON: http://127.0.0.1:5088/openapi/v1.json
   ```

   Scalar를 사용하면 브라우저에서 status, wafer-info, control, results endpoint를 호출할 수 있습니다.

4. 먼저 `not_initialized` 를 검증합니다.

   **Initialize** 를 누르기 전에 SDK 또는 REST 샘플을 실행합니다. 샘플이 start를 호출하면 예상 반환값은 `HTTP 409 not_initialized` 입니다. 이는 샘플이 UI 상태를 올바르게 반영하고 있음을 의미합니다.

5. WaferInfo를 입력하고 적용합니다.

   Lot ID, Wafer ID, Recipe ID, Slot, FOUP ID, Chamber ID를 입력하고 **Apply WaferInfo** 를 누르거나 샘플이 WaferInfo를 보내도록 합니다. Event Log는 모든 필드를 한 줄로 표시해야 합니다.

   ```text
   WaferInfo updated from REST: lotId=LOT-RAW-REST-001, waferId=W01, recipeId=RCP-A, slot=1, foupId=FOUP-A, chamberId=CH-1
   ```

6. **Initialize** 와 **Start Cycle** 을 실행합니다.

   **Initialize** 를 눌러 initialized 상태로 설정합니다. 그 다음 **Start Cycle** 을 누르거나 샘플을 계속합니다. Simulator는 `capturing`, `inspecting`, `saving` 을 거쳐 `ready` 로 돌아갑니다.

7. 테스트 이벤트를 보냅니다.

   **Emit Fake Result** 로 결과 처리를 테스트합니다. **Emit Error** 로 오류 처리를 테스트합니다. MQTT 샘플 실행 중에는 이 버튼이 `virex/result` 및 `virex/error` 를 생성해야 합니다.

8. 테스트를 종료합니다.

   **Stop Servers** 를 누르거나 시뮬레이터 창을 닫습니다.

## 시나리오별 테스트 흐름

| 시나리오 | 조건 | UI SOP | 예상 결과 |
| --- | --- | --- | --- |
| 통신 서비스 시작 | Simulator app이 열려 있고 서비스가 아직 시작되지 않았습니다. | REST/TCP/MQTT 설정을 확인한 뒤 **Start Servers** 를 누릅니다. | Event Log에 REST listening, TCP listening, MQTT connected/started가 표시되고 샘플이 연결됩니다. |
| `not_initialized` | **Start Servers** 는 눌렀지만 **Initialize** 는 아직 누르지 않았습니다. | **Initialize** 를 누르지 말고 C# SDK 또는 REST 샘플의 start 단계를 실행합니다. | 콘솔에 HTTP `409` / `not_initialized` 가 표시됩니다. 이는 연결 실패가 아니라 예상된 상태 동작입니다. |
| 정상 initialize 및 cycle | **Start Servers** 를 눌렀고 상태가 `ready` 입니다. | **Initialize** 를 누르고 Status가 `initialized=True` 인지 확인한 뒤 샘플의 start를 계속합니다. | Status가 `capturing`, `inspecting`, `saving`, `ready` 순서로 표시되고 Event Log에 결과 전송이 기록됩니다. |
| WaferInfo 업데이트 확인 | **Start Servers** 를 눌렀습니다. | UI에서 **Apply WaferInfo** 를 누르거나 SDK/REST/TCP 샘플이 WaferInfo를 보내도록 합니다. | Event Log에 `lotId`, `waferId`, `recipeId`, `slot`, `foupId`, `chamberId` 가 한 줄로 표시됩니다. |
| MQTT 이벤트 관찰 | MQTT 샘플이 `virex/#` 를 구독 중입니다. | **Apply WaferInfo**, **Initialize**, **Start Cycle**, **Emit Fake Result**, **Emit Error** 를 누릅니다. | 콘솔에 `wafer-info`, `status`, `result`, `error` topic이 표시됩니다. |
| 결과 조회 | **Start Cycle** 이 완료되었거나 **Emit Fake Result** 를 눌렀습니다. | 현재 WaferInfo의 `lotId` 로 SDK/REST 샘플에서 조회합니다. | 콘솔에 result count가 표시됩니다. 0이면 query filter가 WaferInfo와 일치하는지 확인합니다. |

## Guided Validation Scenarios

### Scenario A: First SDK Connection Check

1. 기본 연결 설정을 유지합니다.
2. **Start Servers** 를 누릅니다.
3. `samples/csharp-sdk` 를 실행합니다.
4. Event Log에 WaferInfo, start, result 실행 기록이 표시되는지 확인합니다.

### Scenario B: 수동 WaferInfo 테스트

1. 테스트용 wafer context를 입력합니다.
2. **Apply WaferInfo** 를 누릅니다.
3. **Initialize** 를 누릅니다.
4. **Start Cycle** 을 누르고 결과 요약을 기다립니다.

### Scenario C: 결과 이벤트 처리

1. **Start Servers** 를 누릅니다.
2. TCP 또는 MQTT 이벤트 리스너를 시작합니다.
3. **Emit Fake Result** 를 누릅니다.
4. 클라이언트가 결과 요약을 수신하고 Event Log가 이벤트를 기록하는지 확인합니다.

### Scenario D: 오류 처리

1. **Start Servers** 를 누릅니다.
2. TCP 또는 MQTT 이벤트 리스너를 시작합니다.
3. **Emit Error** 를 누릅니다.
4. 클라이언트가 오류 메시지를 표시하거나 기록하는지 확인합니다.

## 클라이언트 검증

| 클라이언트 | 명령 | 검증 내용 |
| --- | --- | --- |
| SDK 샘플 | `dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj` | 표준 가이드 데모입니다. 먼저 `not_initialized` 를 확인하고 **Initialize** 를 눌러 cycle과 result query를 완료합니다. |
| Raw REST 샘플 | `dotnet run --project samples\csharp-raw-rest\CSharpRawRestSample.csproj` | REST status, WaferInfo, start, result query를 보여줍니다. |
| Raw TCP 샘플 | `dotnet run --project samples\csharp-raw-tcp\CSharpRawTcpSample.csproj` | 초기 frame 및 WaferInfo update event를 확인합니다. |
| Raw MQTT 샘플 | `dotnet run --project samples\csharp-raw-mqtt\CSharpRawMqttSample.csproj` | UI 작업에서 `status`, `wafer-info`, `result`, `error` topic을 확인합니다. |

## Troubleshooting

| 증상 | 해결 방법 |
| --- | --- |
| **Start Servers** 실패 | REST prefix 또는 TCP port가 이미 사용 중인지, MQTT broker가 시작될 수 있는지 확인합니다. |
| SDK가 연결되지 않음 | **Start Servers** 를 눌렀는지, SDK `RestBaseUrl`, TCP host/port, MQTT host/port/topic이 UI와 일치하는지 확인합니다. |
| `not_initialized` 표시 | **Initialize** 를 아직 누르지 않았다면 예상된 결과입니다. **Initialize** 를 누르고 Status를 확인한 뒤 샘플을 계속합니다. |
| 이벤트를 받지 못함 | 먼저 Event Log에 전송된 이벤트가 있는지 확인한 뒤 클라이언트가 TCP 또는 MQTT를 통해 구독 중인지 확인합니다. |
| 결과가 반환되지 않음 | 먼저 **Start Cycle** 또는 **Emit Fake Result** 를 실행하고 query filter가 현재 WaferInfo와 일치하는지 확인합니다. |
