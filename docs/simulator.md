# Simulator Manual

The Virex.NET Integration Simulator is the local test tool for customer-side integration development. It exposes REST, TCP, and MQTT endpoints and can simulate WaferInfo, state transitions, result summaries, and error events.

Start it from the repository root:

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
```

## Purpose

| Purpose | What to verify |
| --- | --- |
| Validate the SDK | Confirm that `VirexClient` can read status, submit WaferInfo, start a cycle, and query result summaries. |
| Validate raw protocols | Let non-C# systems test REST payloads, TCP/NDJSON frames, and MQTT topics. |
| Simulate events | Emit status, wafer-info, result, and error events locally before connecting to a production-compatible service. |

## App UI

The screenshot below shows the simulator window used by the guided samples.

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
  <figcaption>Numbered markers correspond to the Area table below.</figcaption>
</figure>

| Area | Name | Purpose |
| --- | --- | --- |
| 1 | Connection Settings | REST prefix, TCP port, MQTT host/port/topic, and result prefix. Confirm this area before testing. |
| 2 | WaferInfo | Lot ID, Wafer ID, Recipe ID, Slot, FOUP ID, and Chamber ID. |
| 3 | State | Current `initialized`, `processState`, `recipe`, and main operation buttons. |
| 4 | Event Log | Server start, WaferInfo updates, cycle events, results, errors, and other activity. |
| 5 | **Start Servers** | First button to press. REST/TCP/MQTT services are available only after this step. |
| 6 | **Apply WaferInfo** | Applies the current test wafer context after editing WaferInfo fields. |
| 7 | **Start Cycle** | Simulates a full cycle, state transitions, and a result summary. |
| 8 | **Emit Fake Result** / **Emit Error** | Manually emits result or error events for client-side handling tests. |

## Standard Operating Procedure

1. Start the simulator app:

   ```powershell
   dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
   ```

2. Confirm connection settings.

   For first use, keep the defaults:

   | Setting | Default |
   | --- | --- |
   | REST | `http://127.0.0.1:5088/` |
   | TCP | `5089` |
   | MQTT broker | `127.0.0.1:1883` |
   | MQTT topic | `virex` |

3. Press **Start Servers**.

   Successful startup writes REST listening, TCP listening, and MQTT started/connected records to Event Log. SDK and sample clients can connect only after this step.

   The REST verification pages are available immediately after this step:

   ```text
   Scalar:       http://127.0.0.1:5088/scalar
   OpenAPI JSON: http://127.0.0.1:5088/openapi/v1.json
   ```

   Use Scalar to call status, wafer-info, control, and results endpoints from the browser.

4. Verify `not_initialized` first.

   Before pressing **Initialize**, run the SDK or REST sample. When the sample calls start, the expected return is `HTTP 409 not_initialized`. This means the sample is correctly reflecting UI state.

5. Enter and apply WaferInfo.

   Fill in Lot ID, Wafer ID, Recipe ID, Slot, FOUP ID, and Chamber ID, then press **Apply WaferInfo**, or let a sample send WaferInfo. Event Log should list all fields in one line:

   ```text
   WaferInfo updated from REST: lotId=LOT-RAW-REST-001, waferId=W01, recipeId=RCP-A, slot=1, foupId=FOUP-A, chamberId=CH-1
   ```

6. Run **Initialize** and **Start Cycle**.

   Press **Initialize** to set initialized state. Then press **Start Cycle** or let the sample continue. The simulator transitions through `capturing`, `inspecting`, `saving`, and back to `ready`.

7. Emit test events.

   Use **Emit Fake Result** to test result handling. Use **Emit Error** to test error handling. While an MQTT sample is running, these buttons should produce `virex/result` and `virex/error`.

8. End the test.

   Press **Stop Servers** or close the simulator window.

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

1. Keep the default connection settings.
2. Press **Start Servers**.
3. Run `samples/csharp-sdk`.
4. Confirm Event Log shows WaferInfo, start, and result activity.

### Scenario B: Manual WaferInfo Test

1. Enter test wafer context.
2. Press **Apply WaferInfo**.
3. Press **Initialize**.
4. Press **Start Cycle** and wait for the result summary.

### Scenario C: Result Event Handling

1. Press **Start Servers**.
2. Start your TCP or MQTT event listener.
3. Press **Emit Fake Result**.
4. Confirm the client receives the result summary and Event Log records the event.

### Scenario D: Error Handling

1. Press **Start Servers**.
2. Start your TCP or MQTT event listener.
3. Press **Emit Error**.
4. Confirm the client displays or logs the error message.

## Client Validation

| Client path | Command | Validation |
| --- | --- | --- |
| SDK sample | `dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj` | Canonical guided demo: first observe `not_initialized`, then press **Initialize** to complete cycle and result query. |
| Raw REST sample | `dotnet run --project samples\csharp-raw-rest\CSharpRawRestSample.csproj` | Demonstrates REST status, WaferInfo, start, and result query. |
| Raw TCP sample | `dotnet run --project samples\csharp-raw-tcp\CSharpRawTcpSample.csproj` | Confirms initial frames and the WaferInfo update event. |
| Raw MQTT sample | `dotnet run --project samples\csharp-raw-mqtt\CSharpRawMqttSample.csproj` | Confirms status, wafer-info, result, and error topics from UI actions. |

## Troubleshooting

| Symptom | Resolution |
| --- | --- |
| **Start Servers** fails | Check whether the REST prefix or TCP port is already in use and whether the MQTT broker can start. |
| SDK cannot connect | Confirm **Start Servers** was pressed and SDK `RestBaseUrl`, TCP host/port, and MQTT host/port/topic match the UI. |
| Seeing `not_initialized` | If **Initialize** has not been pressed, this is expected. Press **Initialize**, confirm Status, then continue the sample. |
| No events received | First check Event Log for emitted events, then confirm the client is subscribed through TCP or MQTT. |
| No result returned | Run **Start Cycle** or **Emit Fake Result** first, then confirm query filters match the current WaferInfo. |
