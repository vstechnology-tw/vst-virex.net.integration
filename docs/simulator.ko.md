# Simulator Manual

Virex.NET Integration Simulator는 customer-side integration development를 위한 local test tool입니다. REST, TCP, MQTT endpoints를 제공하고 WaferInfo, state transitions, result summaries, error events를 simulate할 수 있습니다.

Repository root에서 시작합니다.

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
```

## Purpose

| Purpose | What to verify |
| --- | --- |
| Validate the SDK | `VirexClient` 가 status를 읽고 WaferInfo를 제출하며 cycle을 시작하고 result summaries를 query할 수 있는지 확인합니다. |
| Validate raw protocols | Non-C# systems가 REST payloads, TCP/NDJSON frames, MQTT topics를 test할 수 있게 합니다. |
| Simulate events | Production-compatible service에 연결하기 전에 status, wafer-info, result, error events를 local에서 emit합니다. |

## App UI

아래 screenshot은 guided samples에서 사용하는 simulator window입니다.

<figure>
  <div style="position: relative; display: inline-block; max-width: 100%;">
    <img src="assets/simulator-main-window.png" alt="Virex.NET Integration Simulator main window" style="display: block; width: 100%; height: auto;">
    <span aria-label="Area 1" style="position: absolute; left: 3%; top: 12%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">1</span>
    <span aria-label="Area 2" style="position: absolute; left: 42%; top: 12%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">2</span>
    <span aria-label="Area 3" style="position: absolute; left: 82%; top: 12%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">3</span>
    <span aria-label="Area 4" style="position: absolute; left: 4%; top: 58%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">4</span>
    <span aria-label="Area 5" style="position: absolute; left: 89%; top: 20%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">5</span>
    <span aria-label="Area 6" style="position: absolute; left: 60%; top: 32%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">6</span>
    <span aria-label="Area 7" style="position: absolute; left: 89%; top: 38%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">7</span>
    <span aria-label="Area 8" style="position: absolute; left: 89%; top: 47%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">8</span>
  </div>
  <figcaption>번호 marker는 아래 Area table에 대응합니다.</figcaption>
</figure>

| Area | Name | Purpose |
| --- | --- | --- |
| 1 | Connection Settings | REST prefix, TCP port, MQTT host/port/topic, result prefix. Testing 전에 확인합니다. |
| 2 | WaferInfo | Lot ID, Wafer ID, Recipe ID, Slot, FOUP ID, Chamber ID. |
| 3 | State | Current `initialized`, `processState`, `recipe`, main operation buttons. |
| 4 | Event Log | Server start, WaferInfo updates, cycle events, results, errors, 기타 activity. |
| 5 | **Start Servers** | 가장 먼저 누르는 button입니다. REST/TCP/MQTT services는 이 단계 후 사용할 수 있습니다. |
| 6 | **Apply WaferInfo** | WaferInfo fields를 편집한 후 현재 test wafer context를 적용합니다. |
| 7 | **Start Cycle** | Full cycle, state transitions, result summary를 simulate합니다. |
| 8 | **Emit Fake Result** / **Emit Error** | Client-side handling tests를 위해 result 또는 error events를 수동 emit합니다. |

## Standard Operating Procedure

1. Simulator app을 시작합니다.

   ```powershell
   dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
   ```

2. Connection settings를 확인합니다.

   처음에는 defaults를 유지하십시오.

   | Setting | Default |
   | --- | --- |
   | REST | `http://127.0.0.1:5088/` |
   | TCP | `5089` |
   | MQTT broker | `127.0.0.1:1883` |
   | MQTT topic | `virex` |

3. **Start Servers** 를 누릅니다.

   Startup이 성공하면 Event Log에 REST listening, TCP listening, MQTT started/connected records가 표시됩니다. SDK 및 sample clients는 이 단계 후에만 연결할 수 있습니다.

   REST verification pages는 이 단계 직후 사용할 수 있습니다.

   ```text
   Scalar:       http://127.0.0.1:5088/scalar
   OpenAPI JSON: http://127.0.0.1:5088/openapi/v1.json
   ```

   Scalar를 사용하면 browser에서 status, wafer-info, control, results endpoints를 호출할 수 있습니다.

4. 먼저 `not_initialized` 를 검증합니다.

   **Initialize** 를 누르기 전에 SDK 또는 REST sample을 실행합니다. Sample이 start를 호출하면 expected return은 `HTTP 409 not_initialized` 입니다. 이는 sample이 UI state를 올바르게 반영하고 있음을 의미합니다.

5. WaferInfo를 입력하고 적용합니다.

   Lot ID, Wafer ID, Recipe ID, Slot, FOUP ID, Chamber ID를 입력하고 **Apply WaferInfo** 를 누르거나 sample이 WaferInfo를 보내도록 합니다. Event Log는 모든 fields를 한 줄로 표시해야 합니다.

   ```text
   WaferInfo updated from REST: lotId=LOT-RAW-REST-001, waferId=W01, recipeId=RCP-A, slot=1, foupId=FOUP-A, chamberId=CH-1
   ```

6. **Initialize** 와 **Start Cycle** 을 실행합니다.

   **Initialize** 를 눌러 initialized state로 설정합니다. 그 다음 **Start Cycle** 을 누르거나 sample을 계속합니다. Simulator는 `capturing`, `inspecting`, `saving` 을 거쳐 `ready` 로 돌아갑니다.

7. Test events를 emit합니다.

   **Emit Fake Result** 로 result handling을 test합니다. **Emit Error** 로 error handling을 test합니다. MQTT sample 실행 중에는 이 buttons가 `virex/result` 및 `virex/error` 를 생성해야 합니다.

8. Test를 종료합니다.

   **Stop Servers** 를 누르거나 simulator window를 닫습니다.

## Test Flows by Scenario

| Scenario | Condition | UI SOP | Expected result |
| --- | --- | --- | --- |
| Start communication services | Simulator app is open and servers are not started. | Confirm REST/TCP/MQTT settings, then press **Start Servers**. | Event Log shows REST listening, TCP listening, and MQTT connected/started; samples can connect. |
| `not_initialized` | **Start Servers** was pressed, but **Initialize** was not pressed. | Do not press **Initialize**. Run the C# SDK or REST sample start step. | Console shows HTTP `409` / `not_initialized`; this is expected state behavior, not a connection failure. |
| Normal initialize and cycle | **Start Servers** was pressed and state is `ready`. | Press **Initialize**, confirm Status shows `initialized=True`, then let the sample continue start. | Status shows `capturing`, `inspecting`, `saving`, `ready`; Event Log shows result emission. |
| WaferInfo update verification | **Start Servers** was pressed. | Press **Apply WaferInfo** in the UI, or let SDK/REST/TCP samples send WaferInfo. | Event Log lists `lotId`, `waferId`, `recipeId`, `slot`, `foupId`, and `chamberId` in one line. |
| MQTT event observation | MQTT sample is subscribed to `virex/#`. | Press **Apply WaferInfo**, **Initialize**, **Start Cycle**, **Emit Fake Result**, and **Emit Error**. | Console shows `wafer-info`, `status`, `result`, and `error` topics. |
| Result query | **Start Cycle** completed or **Emit Fake Result** was pressed. | Use SDK/REST samples to query by the current WaferInfo `lotId`. | Console prints result count; if it is 0, confirm the query filter matches WaferInfo. |

## Guided Validation Scenarios

### Scenario A: First SDK Connection Check

1. Default connection settings를 유지합니다.
2. **Start Servers** 를 누릅니다.
3. `samples/csharp-sdk` 를 실행합니다.
4. Event Log에 WaferInfo, start, result activity가 표시되는지 확인합니다.

### Scenario B: Manual WaferInfo Test

1. Test wafer context를 입력합니다.
2. **Apply WaferInfo** 를 누릅니다.
3. **Initialize** 를 누릅니다.
4. **Start Cycle** 을 누르고 result summary를 기다립니다.

### Scenario C: Result Event Handling

1. **Start Servers** 를 누릅니다.
2. TCP 또는 MQTT event listener를 시작합니다.
3. **Emit Fake Result** 를 누릅니다.
4. Client가 result summary를 수신하고 Event Log가 event를 기록하는지 확인합니다.

### Scenario D: Error Handling

1. **Start Servers** 를 누릅니다.
2. TCP 또는 MQTT event listener를 시작합니다.
3. **Emit Error** 를 누릅니다.
4. Client가 error message를 표시하거나 log하는지 확인합니다.

## Client Validation

| Client path | Command | Validation |
| --- | --- | --- |
| SDK sample | `dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj` | Canonical guided demo: 먼저 `not_initialized` 를 확인하고 **Initialize** 를 눌러 cycle과 result query를 완료합니다. |
| Raw REST sample | `dotnet run --project samples\csharp-raw-rest\CSharpRawRestSample.csproj` | REST status, WaferInfo, start, result query를 보여줍니다. |
| Raw TCP sample | `dotnet run --project samples\csharp-raw-tcp\CSharpRawTcpSample.csproj` | Initial frames 및 WaferInfo update event를 확인합니다. |
| Raw MQTT sample | `dotnet run --project samples\csharp-raw-mqtt\CSharpRawMqttSample.csproj` | UI actions에서 status, wafer-info, result, error topics를 확인합니다. |

## Troubleshooting

| Symptom | Resolution |
| --- | --- |
| **Start Servers** fails | REST prefix 또는 TCP port가 이미 사용 중인지, MQTT broker가 시작될 수 있는지 확인합니다. |
| SDK cannot connect | **Start Servers** 를 눌렀는지, SDK `RestBaseUrl`, TCP host/port, MQTT host/port/topic이 UI와 일치하는지 확인합니다. |
| Seeing `not_initialized` | **Initialize** 를 아직 누르지 않았다면 expected입니다. **Initialize** 를 누르고 Status를 확인한 뒤 sample을 계속합니다. |
| No events received | 먼저 Event Log에 emitted events가 있는지 확인한 뒤 client가 TCP 또는 MQTT를 통해 subscribed 상태인지 확인합니다. |
| No result returned | 먼저 **Start Cycle** 또는 **Emit Fake Result** 를 실행하고 query filters가 current WaferInfo와 일치하는지 확인합니다. |
