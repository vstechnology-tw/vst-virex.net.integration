# 傳送內容 / Payload Reference

本頁說明 Virex.NET public integration surfaces 實際傳送的 JSON content。內容涵蓋 customer-visible DTOs、route/topic mappings 與 simulator verification steps，不描述 private inspection logic 或 runtime internals。

## JSON 規則

所有 REST、TCP、MQTT payloads 使用相同 public JSON naming rules：

| Rule | Behavior |
| --- | --- |
| Property names | 序列化為 `camelCase`。 |
| Null values | 序列化 JSON 時省略。 |
| Incoming property names | 讀取時大小寫不敏感。 |
| Text encoding | UTF-8 JSON。 |

範例：

```json
{
  "initialized": true,
  "processState": "ready",
  "recipe": "RCP-A"
}
```

`processState` 可為：

| Value | Meaning |
| --- | --- |
| `ready` | Simulator idle，可接受下一個 action。 |
| `capturing` | 正在模擬 capture step。 |
| `inspecting` | 正在模擬 inspection step。 |
| `saving` | 正在模擬 save step。 |

Command transitions 與 rejected states 請看 [狀態機](state-machine.md)。

## Direction Matrix

| Payload | REST | TCP | MQTT |
| --- | --- | --- | --- |
| Status | `GET /api/status` outbound response | `type: "status"` outbound event | `virex/status` outbound event |
| Control status | Control POST routes outbound response | 不使用 | 不使用 |
| Error status | `GET /api/error` outbound response | `type: "error"` outbound event | `virex/error` outbound event |
| WaferInfo | `POST /api/wafer-info` inbound request；`GET /api/wafer-info` outbound response | `type: "waferInfo"` inbound command；`type: "waferInfo"` outbound event | `virex/wafer-info` outbound event |
| Result summary | 出現在 `GET /api/results` 回應的 `items[]` 裡 | `type: "result"` outbound event | `virex/result` outbound event |
| REST Result 查詢回應 | `GET /api/results` 回傳的外層 wrapper | 不使用 | 不使用 |
| Start command | `POST /api/control/start` control route | `type: "start"` inbound command | 不使用 |
| Stop command | `POST /api/control/stop` control route | `type: "stop"` inbound command | 不使用 |

MQTT 只做 outbound。MQTT payload 不需要 `type` property，因為 topic 已經識別 event。

## WaferInfo

Direction:

| Transport | Direction |
| --- | --- |
| REST | `POST /api/wafer-info` inbound；`GET /api/wafer-info` outbound。 |
| TCP | Inbound command 與 outbound event。 |
| MQTT | Outbound event only。 |

When it is sent:

| Scenario | Behavior |
| --- | --- |
| Host updates wafer context | Start 或 query simulated cycle 前，透過 REST 或 TCP 送 WaferInfo。 |
| Simulator publishes wafer context | WaferInfo 變更時，TCP 與 MQTT clients 會收到 event。 |

JSON example:

```json
{
  "lotId": "LOT-001",
  "waferId": "W01",
  "recipeId": "RCP-A",
  "slot": "1",
  "foupId": "FOUP-A",
  "chamberId": "CH-1"
}
```

Field table:

| Field | Type | Description |
| --- | --- | --- |
| `lotId` | string | 用於 result filtering 與 event correlation 的 public lot identifier。 |
| `waferId` | string | Public wafer identifier。 |
| `recipeId` | string | 與 simulated wafer context 相關的 public recipe identifier。 |
| `slot` | string | Slot identifier。 |
| `foupId` | string | FOUP identifier。 |
| `chamberId` | string | Chamber identifier。 |

Simulator/sample verification:

1. 啟動 simulator 並按 **Start Servers**。
2. 透過 C# SDK、raw REST sample、raw TCP sample 送 WaferInfo，或在 simulator 編輯欄位後按 **Apply WaferInfo**。
3. 確認 Event Log 同一行列出所有 public fields，例如：

```text
WaferInfo updated from REST: lotId=LOT-RAW-REST-001, waferId=W01, recipeId=RCP-A, slot=1, foupId=FOUP-A, chamberId=CH-1
```

## Status

Direction:

| Transport | Direction |
| --- | --- |
| REST | `GET /api/status` outbound response。 |
| TCP | `type: "status"` outbound event。 |
| MQTT | `virex/status` outbound event。 |

When it is sent:

| Scenario | Behavior |
| --- | --- |
| Client reads current simulator state | REST 回傳 current status。 |
| Simulator state changes | TCP 與 MQTT clients 會收到 status events。 |

JSON example:

```json
{
  "initialized": true,
  "processState": "ready",
  "recipe": "RCP-A"
}
```

Field table:

| Field | Type | Description |
| --- | --- | --- |
| `initialized` | boolean | Simulated system 是否已 initialized。 |
| `processState` | string | Current public process state：`ready`、`capturing`、`inspecting`、`saving`。 |
| `recipe` | string | Status 回報的 current public recipe value。 |

Simulator/sample verification:

1. 啟動 simulator 並按 **Start Servers**。
2. 執行 C# SDK 或 raw REST sample，確認它讀取 `GET /api/status`。
3. 按 **Initialize** 或 **Start Cycle**，確認 TCP 或 MQTT samples 印出更新後的 status event。

## Control Status

Direction:

| Transport | Direction |
| --- | --- |
| REST | Control POST routes 的 outbound response。 |
| TCP | 不使用。 |
| MQTT | 不使用。 |

When it is sent:

| Scenario | Behavior |
| --- | --- |
| Client posts a control action | REST 回傳 action 後的 state 與 public message。 |

JSON example:

```json
{
  "initialized": true,
  "processState": "ready",
  "recipe": "RCP-A",
  "message": "Initialized."
}
```

Field table:

| Field | Type | Description |
| --- | --- | --- |
| `initialized` | boolean | Control action 後 simulated system 是否 initialized。 |
| `processState` | string | Control action 後的 public process state。 |
| `recipe` | string | Current public recipe value。 |
| `message` | string | Control action 的 public response message。 |

Simulator/sample verification:

1. 啟動 simulator 並按 **Start Servers**。
2. 從 REST-capable sample 送 `POST /api/control/initialize`、`/terminate`、`/start` 或 `/stop`。
3. 確認 sample 印出的 JSON response 有 `initialized`、`processState`、`recipe`、`message`。

## Error Status

Direction:

| Transport | Direction |
| --- | --- |
| REST | `GET /api/error` outbound response。 |
| TCP | `type: "error"` outbound event。 |
| MQTT | `virex/error` outbound event。 |

When it is sent:

| Scenario | Behavior |
| --- | --- |
| Client reads current error state | REST 回傳 current error status。 |
| Simulator error changes | TCP 與 MQTT clients 會收到 error event。 |

JSON example:

```json
{
  "hasError": true,
  "message": "Simulated error.",
  "initialized": true,
  "processState": "ready",
  "recipe": "RCP-A"
}
```

Field table:

| Field | Type | Description |
| --- | --- | --- |
| `hasError` | boolean | 目前是否有 active error。 |
| `message` | string | Public error message。為 null 時省略。 |
| `initialized` | boolean | Error status 當下的 initialization state。 |
| `processState` | string | Error status 當下的 public process state。 |
| `recipe` | string | Current public recipe value。 |

Simulator/sample verification:

1. 啟動 simulator 並按 **Start Servers**。
2. 執行 TCP 或 MQTT sample 並保持連線。
3. 在 simulator 按 **Emit Error**。
4. 確認 sample 印出包含 `hasError`、`message`、`initialized`、`processState`、`recipe` 的 error payload。

## Result Summary

`ResultSummaryDto` 是單筆 result summary item。REST 會把它放在 `ResultListDto.items[]` 裡，TCP 與 MQTT 則直接送出單筆 Result Summary。

Direction:

| Transport | Direction |
| --- | --- |
| REST | 出現在 `GET /api/results` 回應的 `items[]` 裡，每一筆都是一個 Result Summary。 |
| TCP | `type: "result"` outbound event。 |
| MQTT | `virex/result` outbound event。 |

When it is sent:

| Scenario | Behavior |
| --- | --- |
| Simulator creates a result | TCP 與 MQTT clients 會收到 result summary event。 |
| Client queries historical simulated results | REST 回傳 `ResultListDto`，其中 `items[]` 每一筆都是 `ResultSummaryDto`。 |

JSON example:

```json
{
  "resultId": "RID-1",
  "timestamp": "2026-06-20T15:30:12+08:00",
  "lotId": "LOT-001",
  "waferId": "W01",
  "recipeId": "RCP-A",
  "slot": "1",
  "foupId": "FOUP-A",
  "chamberId": "CH-1",
  "overallResult": "OK",
  "defectCount": 0,
  "imageRelativePath": "20260620/LOT-001/20260620_153012_W01.tiff",
  "resultRelativePath": "20260620/LOT-001/20260620_153012_W01.json",
  "imagePath": "/data/virex-results/20260620/LOT-001/20260620_153012_W01.tiff",
  "previewImagePath": "/data/virex-results/20260620/LOT-001/20260620_153012_W01.jpg",
  "resultPath": "/data/virex-results/20260620/LOT-001/20260620_153012_W01.json"
}
```

Field table:

| Field | Type | Description |
| --- | --- | --- |
| `resultId` | string | Public result identifier。 |
| `timestamp` | string | Result timestamp string。 |
| `lotId` | string | 從 active WaferInfo 複製的 lot identifier。 |
| `waferId` | string | 從 active WaferInfo 複製的 wafer identifier。 |
| `recipeId` | string | Result 關聯的 recipe identifier。 |
| `slot` | string | 從 active WaferInfo 複製的 slot identifier。 |
| `foupId` | string | 從 active WaferInfo 複製的 FOUP identifier。 |
| `chamberId` | string | 從 active WaferInfo 複製的 chamber identifier。 |
| `overallResult` | string | Public summary result value。 |
| `defectCount` | number | Summary 中所有瑕疵類別的總瑕疵數。 |
| `imageRelativePath` | string | Associated image artifact 的 relative path string。 |
| `resultRelativePath` | string | Associated result artifact 的 relative path string。 |
| `imagePath` | string | 套用 simulator Result prefix 後的 public image path。 |
| `previewImagePath` | string | 套用 simulator Result prefix 後的 public preview image path。 |
| `resultPath` | string | 套用 simulator Result prefix 後的 public result path。 |

Result summaries 只提供 summary。它們不包含 defect lists、die lists、crop lists、image binaries 或 private inspection internals。

Simulator/sample verification:

1. 啟動 simulator 並按 **Start Servers**。
2. Apply WaferInfo，按 **Initialize**，再按 **Start Cycle** 或 **Emit Fake Result**。
3. 確認 TCP 或 MQTT sample 印出 `result` event，或用 REST-capable sample 呼叫 `GET /api/results`。
4. 確認回傳 summary 包含 WaferInfo identifiers 與 count fields，但不包含 defect lists、die lists、crop lists、image binaries 或 private inspection internals。

## REST Result 查詢回應

`ResultListDto` 是 REST result query 的 response wrapper；其中 `items[]` 的每一筆都是一個 `ResultSummaryDto`。

Direction:

| Transport | Direction |
| --- | --- |
| REST | `GET /api/results` 回傳的外層 wrapper，包含 `items[]` 與 `count`。 |
| TCP | 不使用。 |
| MQTT | 不使用。 |

此 wrapper 只用於 REST。TCP 與 MQTT 會直接送出單筆 Result Summary，不使用 Result List。

When it is sent:

| Scenario | Behavior |
| --- | --- |
| Client queries result summaries | REST 回傳 list wrapper，包含 matching summary items 與 count。 |

`GET /api/results` 支援以下 optional filters：

- `lotId`
- `waferId`
- `recipeId`

提供多個 filters 時，會以 AND 合併。

Query examples：

```text
GET /api/results
GET /api/results?lotId=LOT-001
GET /api/results?lotId=LOT-001&waferId=W01
GET /api/results?lotId=LOT-001&waferId=W01&recipeId=RCP-A
```

JSON example:

```json
{
  "items": [
    {
      "resultId": "RID-1",
      "timestamp": "2026-06-20T15:30:12+08:00",
      "lotId": "LOT-001",
      "waferId": "W01",
      "recipeId": "RCP-A",
      "slot": "1",
      "foupId": "FOUP-A",
      "chamberId": "CH-1",
      "overallResult": "OK",
      "defectCount": 0,
      "imageRelativePath": "20260620/LOT-001/20260620_153012_W01.tiff",
      "resultRelativePath": "20260620/LOT-001/20260620_153012_W01.json",
      "imagePath": "/data/virex-results/20260620/LOT-001/20260620_153012_W01.tiff",
      "previewImagePath": "/data/virex-results/20260620/LOT-001/20260620_153012_W01.jpg",
      "resultPath": "/data/virex-results/20260620/LOT-001/20260620_153012_W01.json"
    }
  ],
  "count": 1
}
```

Field table:

| Field | Type | Description |
| --- | --- | --- |
| `items` | array | `ResultSummaryDto` objects array。 |
| `count` | number | 回傳的 result summary item 數量。 |

Simulator/sample verification:

1. 用 matching WaferInfo 建立 simulated result。
2. 從 REST-capable sample 呼叫 `GET /api/results`。
3. 確認 `count` 與回傳的 `items` 數量一致。

## REST Mapping

| Method and route | Direction | Payload content | Notes |
| --- | --- | --- | --- |
| `GET /api/status` | Outbound response | `StatusDto` | 讀取 current public state。 |
| `GET /api/error` | Outbound response | `ErrorStatusDto` | 讀取 current public error state。 |
| `GET /api/wafer-info` | Outbound response | `WaferInfo` | 讀取 current public wafer context。 |
| `POST /api/wafer-info` | Inbound request | `WaferInfo` | 更新 current public wafer context。 |
| `POST /api/control/initialize` | Outbound response | `ControlStatusDto` | 初始化 simulator state。 |
| `POST /api/control/terminate` | Outbound response | `ControlStatusDto` | Terminate simulator state。 |
| `POST /api/control/start` | Optional inbound `ControlStartRequest`；outbound response `ControlStatusDto` | `condition` optional。`runMode` optional 且預設 `continue`；支援 `continue` 與 `single`。 |
| `POST /api/control/stop` | Optional inbound `ControlStopRequest`；outbound response `ControlStatusDto` | `reason` 可省略或空白，維持向下相容。 |
| `GET /api/results` | Outbound response | `ResultListDto` | Optional filters：`lotId`、`waferId`、`recipeId`。多個 filters 以 AND 合併。 |

REST verification:

1. 啟動 simulator 並按 **Start Servers**。
2. 執行 C# SDK、C# raw REST 或 C++ raw REST sample。
3. 確認 sample 讀 status、post WaferInfo、執行 control action、並用上方 public payloads 查詢 result summaries。

## TCP Mapping

TCP messages 是 JSON objects。TCP outbound events 包含 `type` property。TCP inbound commands 使用以下 shapes：

| Inbound command | Direction | JSON example | Notes |
| --- | --- | --- | --- |
| WaferInfo | Inbound to simulator | `{"type":"waferInfo","lotId":"LOT-001","waferId":"W01","recipeId":"RCP-A","slot":"1","foupId":"FOUP-A","chamberId":"CH-1"}` | Legacy WaferInfo frames 可省略 `type`；parser 會 fallback 到 WaferInfo JSON parser。 |
| Start | Inbound to simulator | `{"type":"start","condition":"golden-sample","runMode":"continue"}` | `condition` optional。`runMode` 預設 `continue`；legacy `{"type":"start"}` 仍有效。 |
| Stop | Inbound to simulator | `{"type":"stop","reason":"operator-request"}` | `reason` optional；legacy `{"type":"stop"}` 仍有效。 |

TCP outbound events:

| Event type | Direction | Payload content |
| --- | --- | --- |
| `status` | Outbound from simulator | `StatusDto` plus `type`。 |
| `waferInfo` | Outbound from simulator | `WaferInfo` plus `type`。 |
| `result` | Outbound from simulator | `ResultSummaryDto` plus `type`。 |
| `error` | Outbound from simulator | `ErrorStatusDto` plus `type`。 |

TCP verification:

1. 啟動 simulator 並按 **Start Servers**。
2. 執行 raw TCP sample。
3. 確認它印出 initial status 與 WaferInfo frames。
4. 讓 sample 送 WaferInfo frame，並確認 Event Log 顯示 TCP WaferInfo update。

## MQTT Mapping

MQTT 只做 outbound。訂閱 base topic wildcard 可觀察所有 simulator events：

```text
virex/#
```

| Topic | Direction | Payload content | Notes |
| --- | --- | --- | --- |
| `virex/status` | Outbound from simulator | `StatusDto` | Payload 不需要 `type`。 |
| `virex/wafer-info` | Outbound from simulator | `WaferInfo` | Payload 不需要 `type`。 |
| `virex/result` | Outbound from simulator | `ResultSummaryDto` | Payload 不需要 `type`。 |
| `virex/error` | Outbound from simulator | `ErrorStatusDto` | Payload 不需要 `type`。 |

MQTT verification:

1. 啟動 simulator 並按 **Start Servers**。
2. 執行 raw MQTT sample 並保持 subscribed。
3. 按 **Apply WaferInfo**、**Initialize**、**Start Cycle**、**Emit Fake Result**、**Emit Error**。
4. 確認 sample 印出 `wafer-info`、`status`、`result`、`error` 的 matching topic 與 JSON payload。
