# Simulator Manual

Virex.NET Integration Simulator は customer-side integration development 用の local test tool です。REST、TCP、MQTT endpoints を公開し、WaferInfo、state transitions、result summaries、error events を simulate できます。

Repository root から起動します。

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
```

## Purpose

| Purpose | What to verify |
| --- | --- |
| Validate the SDK | `VirexClient` が status を読み、WaferInfo を送信し、cycle を開始し、result summaries を query できることを確認します。 |
| Validate raw protocols | Non-C# systems が REST payloads、TCP/NDJSON frames、MQTT topics を test できるようにします。 |
| Simulate events | Production-compatible service に接続する前に、status、wafer-info、result、error events を local で emit します。 |

## App UI

次の screenshot は guided samples で使用する simulator window です。

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
  <figcaption>番号マーカーは下の Area table に対応します。</figcaption>
</figure>

| Area | Name | Purpose |
| --- | --- | --- |
| 1 | Connection Settings | REST prefix、TCP port、MQTT host/port/topic、result prefix。Testing 前に確認します。 |
| 2 | WaferInfo | Lot ID、Wafer ID、Recipe ID、Slot、FOUP ID、Chamber ID。 |
| 3 | State | Current `initialized`、`processState`、`recipe`、main operation buttons。 |
| 4 | Event Log | Server start、WaferInfo updates、cycle events、results、errors、その他 activity。 |
| 5 | **Start Servers** | 最初に押す button。REST/TCP/MQTT services はこの後に利用可能です。 |
| 6 | **Apply WaferInfo** | WaferInfo fields 編集後、現在の test wafer context を適用します。 |
| 7 | **Start Cycle** | Full cycle、state transitions、result summary を simulate します。 |
| 8 | **Emit Fake Result** / **Emit Error** | Client-side handling tests 用に result または error events を手動 emit します。 |

## Standard Operating Procedure

1. Simulator app を起動します。

   ```powershell
   dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
   ```

2. Connection settings を確認します。

   初回は defaults を使用してください。

   | Setting | Default |
   | --- | --- |
   | REST | `http://127.0.0.1:5088/` |
   | TCP | `5089` |
   | MQTT broker | `127.0.0.1:1883` |
   | MQTT topic | `virex` |

3. **Start Servers** を押します。

   Startup に成功すると、Event Log に REST listening、TCP listening、MQTT started/connected records が表示されます。SDK と sample clients はこの後に接続できます。

   REST verification pages はこの時点で利用できます。

   ```text
   Scalar:       http://127.0.0.1:5088/scalar
   OpenAPI JSON: http://127.0.0.1:5088/openapi/v1.json
   ```

   Scalar を使うと browser から status、wafer-info、control、results endpoints を呼び出せます。

4. まず `not_initialized` を検証します。

   **Initialize** を押す前に SDK または REST sample を実行します。Sample が start を呼ぶと、expected return は `HTTP 409 not_initialized` です。これは sample が UI state を正しく反映していることを示します。

5. WaferInfo を入力して適用します。

   Lot ID、Wafer ID、Recipe ID、Slot、FOUP ID、Chamber ID を入力し、**Apply WaferInfo** を押します。または sample から WaferInfo を送信します。Event Log は全 fields を 1 行で表示する必要があります。

   ```text
   WaferInfo updated from REST: lotId=LOT-RAW-REST-001, waferId=W01, recipeId=RCP-A, slot=1, foupId=FOUP-A, chamberId=CH-1
   ```

6. **Initialize** と **Start Cycle** を実行します。

   **Initialize** を押して initialized state にします。その後 **Start Cycle** を押すか、sample を続行します。Simulator は `capturing`、`inspecting`、`saving` を経由して `ready` に戻ります。

7. Test events を emit します。

   **Emit Fake Result** で result handling を test します。**Emit Error** で error handling を test します。MQTT sample 実行中は、これらの buttons が `virex/result` と `virex/error` を生成します。

8. Test を終了します。

   **Stop Servers** を押すか、simulator window を閉じます。

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

1. Default connection settings を維持します。
2. **Start Servers** を押します。
3. `samples/csharp-sdk` を実行します。
4. Event Log に WaferInfo、start、result activity が表示されることを確認します。

### Scenario B: Manual WaferInfo Test

1. Test wafer context を入力します。
2. **Apply WaferInfo** を押します。
3. **Initialize** を押します。
4. **Start Cycle** を押し、result summary を待ちます。

### Scenario C: Result Event Handling

1. **Start Servers** を押します。
2. TCP または MQTT event listener を起動します。
3. **Emit Fake Result** を押します。
4. Client が result summary を受信し、Event Log に event が記録されることを確認します。

### Scenario D: Error Handling

1. **Start Servers** を押します。
2. TCP または MQTT event listener を起動します。
3. **Emit Error** を押します。
4. Client が error message を表示または log することを確認します。

## Client Validation

| Client path | Command | Validation |
| --- | --- | --- |
| SDK sample | `dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj` | Canonical guided demo: 最初に `not_initialized` を確認し、**Initialize** を押して cycle と result query を完了します。 |
| Raw REST sample | `dotnet run --project samples\csharp-raw-rest\CSharpRawRestSample.csproj` | REST status、WaferInfo、start、result query を示します。 |
| Raw TCP sample | `dotnet run --project samples\csharp-raw-tcp\CSharpRawTcpSample.csproj` | Initial frames と WaferInfo update event を確認します。 |
| Raw MQTT sample | `dotnet run --project samples\csharp-raw-mqtt\CSharpRawMqttSample.csproj` | UI actions から status、wafer-info、result、error topics を確認します。 |

## Troubleshooting

| Symptom | Resolution |
| --- | --- |
| **Start Servers** fails | REST prefix または TCP port が使用中か、MQTT broker が起動できるかを確認します。 |
| SDK cannot connect | **Start Servers** が押されていること、SDK `RestBaseUrl`、TCP host/port、MQTT host/port/topic が UI と一致することを確認します。 |
| Seeing `not_initialized` | **Initialize** がまだ押されていない場合は expected です。**Initialize** を押し、Status を確認して sample を続行します。 |
| No events received | まず Event Log に emitted events があるか確認し、client が TCP または MQTT 経由で subscribed していることを確認します。 |
| No result returned | 先に **Start Cycle** または **Emit Fake Result** を実行し、query filters が current WaferInfo と一致することを確認します。 |
