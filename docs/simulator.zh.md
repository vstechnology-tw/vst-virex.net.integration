# Simulator 操作手冊

Virex.NET Integration Simulator 是客戶端整合開發時使用的本機測試工具。它提供 REST、TCP、MQTT endpoints，並可模擬 WaferInfo、狀態變化、result summaries 與 error events。

從 repository root 啟動：

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
```

## 用途

| 用途 | 要驗證的內容 |
| --- | --- |
| 驗證 SDK | 確認 `VirexClient` 可以讀 status、送 WaferInfo、start cycle、query result summaries。 |
| 驗證 raw protocols | 讓非 C# 系統測試 REST payloads、TCP/NDJSON frames 與 MQTT topics。 |
| 模擬 events | 在本機先模擬 status、wafer-info、result、error events，讓 client handling logic 先完成。 |

## App UI

下圖是 guided samples 使用的 simulator window。

<figure>
  <div style="position: relative; display: inline-block; max-width: 100%;">
    <img src="../assets/simulator-main-window.png" alt="Virex.NET Integration Simulator main window" style="display: block; width: 100%; height: auto;">
    <span aria-label="Area 1" style="position: absolute; left: 3%; top: 12%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">1</span>
    <span aria-label="Area 2" style="position: absolute; left: 42%; top: 12%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">2</span>
    <span aria-label="Area 3" style="position: absolute; left: 82%; top: 12%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">3</span>
    <span aria-label="Area 4" style="position: absolute; left: 4%; top: 58%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">4</span>
    <span aria-label="Area 5" style="position: absolute; left: 89%; top: 20%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">5</span>
    <span aria-label="Area 6" style="position: absolute; left: 60%; top: 32%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">6</span>
    <span aria-label="Area 7" style="position: absolute; left: 89%; top: 38%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">7</span>
    <span aria-label="Area 8" style="position: absolute; left: 89%; top: 47%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">8</span>
  </div>
  <figcaption>數字標記對應下方 Area 表格。</figcaption>
</figure>

| Area | Name | Purpose |
| --- | --- | --- |
| 1 | Connection Settings | REST prefix、TCP port、MQTT host/port/topic、result prefix。測試前先確認此區。 |
| 2 | WaferInfo | Lot ID、Wafer ID、Recipe ID、Slot、FOUP ID、Chamber ID。 |
| 3 | State | 目前 `initialized`、`processState`、`recipe` 與主要操作按鈕。 |
| 4 | Event Log | Server start、WaferInfo updates、cycle events、results、errors 與其他 activity。 |
| 5 | **Start Servers** | 第一個要按的按鈕。按下後 REST/TCP/MQTT services 才會開始對外服務。 |
| 6 | **Apply WaferInfo** | 修改 WaferInfo fields 後，套用目前 test wafer context。 |
| 7 | **Start Cycle** | 模擬完整 cycle、state transitions 與 result summary。 |
| 8 | **Emit Fake Result** / **Emit Error** | 手動產生 result 或 error events，用於 client-side handling tests。 |

## 標準操作 SOP

1. 啟動 simulator app：

   ```powershell
   dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
   ```

2. 確認 connection settings。

   第一次使用建議保留預設值：

   | Setting | Default |
   | --- | --- |
   | REST | `http://127.0.0.1:5088/` |
   | TCP | `5089` |
   | MQTT broker | `127.0.0.1:1883` |
   | MQTT topic | `virex` |

3. 按 **Start Servers**。

   成功後 Event Log 會顯示 REST listening、TCP listening、MQTT started/connected records。SDK 與 sample clients 必須等此步驟後才能連線。

   此步驟後 REST 驗證頁也會立即可用：

   ```text
   Scalar:       http://127.0.0.1:5088/scalar
   OpenAPI JSON: http://127.0.0.1:5088/openapi/v1.json
   ```

   可用 Scalar 直接在 browser 呼叫 status、wafer-info、control 與 results endpoints。

4. 先驗證 `not_initialized`。

   在按 **Initialize** 前執行 SDK 或 REST sample。當 sample 呼叫 start 時，預期回傳 `HTTP 409 not_initialized`。這表示 sample 正確反映 UI state。

5. 輸入並套用 WaferInfo。

   填入 Lot ID、Wafer ID、Recipe ID、Slot、FOUP ID、Chamber ID，然後按 **Apply WaferInfo**，或讓 sample 送出 WaferInfo。Event Log 應同一行列出所有欄位：

   ```text
   WaferInfo updated from REST: lotId=LOT-RAW-REST-001, waferId=W01, recipeId=RCP-A, slot=1, foupId=FOUP-A, chamberId=CH-1
   ```

6. 執行 **Initialize** 與 **Start Cycle**。

   按 **Initialize** 進入 initialized state。接著按 **Start Cycle** 或讓 sample 繼續。Simulator 會依序進入 `capturing`、`inspecting`、`saving`，再回到 `ready`。

7. 產生測試 events。

   使用 **Emit Fake Result** 測試 result handling。使用 **Emit Error** 測試 error handling。MQTT sample 執行中時，這兩個按鈕應產生 `virex/result` 與 `virex/error`。

8. 結束測試。

   按 **Stop Servers** 或關閉 simulator window。

## 依情境測試流程

| Scenario | Condition | UI SOP | Expected result |
| --- | --- | --- | --- |
| 啟動通訊服務 | Simulator app 已開啟，servers 尚未啟動。 | 確認 REST/TCP/MQTT settings，然後按 **Start Servers**。 | Event Log 顯示 REST listening、TCP listening、MQTT connected/started；samples 可連線。 |
| `not_initialized` | 已按 **Start Servers**，但尚未按 **Initialize**。 | 不要按 **Initialize**。執行 C# SDK 或 REST sample 的 start step。 | Console 顯示 HTTP `409` / `not_initialized`；這是預期 state behavior，不是 connection failure。 |
| 正常 initialize 與 cycle | 已按 **Start Servers** 且 state 是 `ready`。 | 按 **Initialize**，確認 Status 顯示 `initialized=True`，再讓 sample 繼續 start。 | Status 顯示 `capturing`、`inspecting`、`saving`、`ready`；Event Log 顯示 result emission。 |
| WaferInfo update verification | 已按 **Start Servers**。 | 在 UI 按 **Apply WaferInfo**，或讓 SDK/REST/TCP samples 送出 WaferInfo。 | Event Log 同一行列出 `lotId`、`waferId`、`recipeId`、`slot`、`foupId`、`chamberId`。 |
| MQTT event observation | MQTT sample 已訂閱 `virex/#`。 | 按 **Apply WaferInfo**、**Initialize**、**Start Cycle**、**Emit Fake Result**、**Emit Error**。 | Console 顯示 `wafer-info`、`status`、`result`、`error` topics。 |
| Result query | 已完成 **Start Cycle** 或已按 **Emit Fake Result**。 | 使用 SDK/REST samples 依目前 WaferInfo `lotId` 查詢。 | Console 印出 result count；如果是 0，確認 query filter 是否符合 WaferInfo。 |

## Guided Validation Scenarios

### Scenario A: 第一次 SDK connection check

1. 保留 default connection settings。
2. 按 **Start Servers**。
3. 執行 `samples/csharp-sdk`。
4. 確認 Event Log 顯示 WaferInfo、start、result activity。

### Scenario B: 手動 WaferInfo 測試

1. 輸入 test wafer context。
2. 按 **Apply WaferInfo**。
3. 按 **Initialize**。
4. 按 **Start Cycle** 並等待 result summary。

### Scenario C: Result event handling

1. 按 **Start Servers**。
2. 啟動 TCP 或 MQTT event listener。
3. 按 **Emit Fake Result**。
4. 確認 client 收到 result summary，Event Log 也有紀錄。

### Scenario D: Error handling

1. 按 **Start Servers**。
2. 啟動 TCP 或 MQTT event listener。
3. 按 **Emit Error**。
4. 確認 client 顯示或記錄 error message。

## Client Validation

| Client path | Command | Validation |
| --- | --- | --- |
| SDK sample | `dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj` | Canonical guided demo：先觀察 `not_initialized`，再按 **Initialize** 完成 cycle 與 result query。 |
| Raw REST sample | `dotnet run --project samples\csharp-raw-rest\CSharpRawRestSample.csproj` | 示範 REST status、WaferInfo、start、result query。 |
| Raw TCP sample | `dotnet run --project samples\csharp-raw-tcp\CSharpRawTcpSample.csproj` | 確認 initial frames 與 WaferInfo update event。 |
| Raw MQTT sample | `dotnet run --project samples\csharp-raw-mqtt\CSharpRawMqttSample.csproj` | 透過 UI actions 確認 status、wafer-info、result、error topics。 |

## 疑難排解

| Symptom | Resolution |
| --- | --- |
| **Start Servers** fails | 檢查 REST prefix 或 TCP port 是否已被使用，以及 MQTT broker 是否能啟動。 |
| SDK cannot connect | 確認已按 **Start Servers**，且 SDK `RestBaseUrl`、TCP host/port、MQTT host/port/topic 與 UI 一致。 |
| Seeing `not_initialized` | 如果尚未按 **Initialize**，這是預期結果。按 **Initialize**、確認 Status 後再繼續 sample。 |
| No events received | 先看 Event Log 是否有 emitted events，再確認 client 是否透過 TCP 或 MQTT 訂閱。 |
| No result returned | 先執行 **Start Cycle** 或 **Emit Fake Result**，再確認 query filters 與目前 WaferInfo 一致。 |
