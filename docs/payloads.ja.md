# Payload Reference

このページは public Virex.NET integration surfaces で送受信される JSON content を説明します。Customer-visible DTOs、route/topic mappings、simulator verification steps を対象とします。Private inspection logic や runtime internals は説明しません。

## JSON Rules

すべての REST、TCP、MQTT payloads は同じ public JSON naming rules を使用します。

| Rule | Behavior |
| --- | --- |
| Property names | `camelCase` で serialized されます。 |
| Null values | Serialized JSON から omit されます。 |
| Incoming property names | Case-insensitive に読み取られます。 |
| Text encoding | UTF-8 JSON。 |

Example:

```json
{
  "initialized": true,
  "processState": "ready",
  "recipe": "RCP-A"
}
```

`processState` は次のいずれかです。

| Value | Meaning |
| --- | --- |
| `ready` | Simulator は idle で次の action を受け付け可能です。 |
| `capturing` | Simulated capture step が active です。 |
| `inspecting` | Simulated inspection step が active です。 |
| `saving` | Simulated save step が active です。 |

Command transitions と rejected states は [State Machine](state-machine.md) を参照してください。

## Direction Matrix

| Payload | REST | TCP | MQTT |
| --- | --- | --- | --- |
| Status | Outbound response from `GET /api/status` | Outbound event with `type: "status"` | Outbound event on `virex/status` |
| Control status | Outbound response from control POST routes | Not used | Not used |
| Error status | Outbound response from `GET /api/error` | Outbound event with `type: "error"` | Outbound event on `virex/error` |
| WaferInfo | Inbound request to `POST /api/wafer-info`; outbound response from `GET /api/wafer-info` | Inbound command with `type: "waferInfo"`; outbound event with `type: "waferInfo"` | Outbound event on `virex/wafer-info` |
| Result summary | Item inside `GET /api/results` response `items[]` | Outbound event with `type: "result"` | Outbound event on `virex/result` |
| REST result query response | Response wrapper returned by `GET /api/results` | Not used | Not used |
| Start command | Control route `POST /api/control/start` | Inbound command with `type: "start"` | Not used |
| Stop command | Control route `POST /api/control/stop` | Inbound command with `type: "stop"` | Not used |

MQTT は outbound-only です。MQTT payloads は topic が event を識別するため、`type` property は不要です。

## WaferInfo

Direction:

| Transport | Direction |
| --- | --- |
| REST | Inbound on `POST /api/wafer-info`; outbound on `GET /api/wafer-info`. |
| TCP | Inbound command and outbound event. |
| MQTT | Outbound event only. |

When it is sent:

| Scenario | Behavior |
| --- | --- |
| Host updates wafer context | Simulated cycle の開始または query 前に REST または TCP で WaferInfo を送信します。 |
| Simulator publishes wafer context | WaferInfo が変わると TCP と MQTT clients は event を受信します。 |

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
| `lotId` | string | Result filtering と event correlation に使用する public lot identifier。 |
| `waferId` | string | Public wafer identifier。 |
| `recipeId` | string | Simulated wafer context に関連する public recipe identifier。 |
| `slot` | string | Slot identifier。 |
| `foupId` | string | FOUP identifier。 |
| `chamberId` | string | Chamber identifier。 |

Simulator/sample verification:

1. Simulator を起動し、**Start Servers** を押します。
2. C# SDK、raw REST sample、raw TCP sample から WaferInfo を送信するか、simulator fields を編集して **Apply WaferInfo** を押します。
3. Simulator Event Log がすべての public fields を 1 行で出力することを確認します。

```text
WaferInfo updated from REST: lotId=LOT-RAW-REST-001, waferId=W01, recipeId=RCP-A, slot=1, foupId=FOUP-A, chamberId=CH-1
```

## Status

Direction:

| Transport | Direction |
| --- | --- |
| REST | Outbound response from `GET /api/status`. |
| TCP | Outbound event with `type: "status"`. |
| MQTT | Outbound event on `virex/status`. |

When it is sent:

| Scenario | Behavior |
| --- | --- |
| Client reads current simulator state | REST は current status を返します。 |
| Simulator state changes | TCP と MQTT clients は status events を受信します。 |

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
| `initialized` | boolean | Simulated system が initialized されているかどうか。 |
| `processState` | string | Current public process state: `ready`, `capturing`, `inspecting`, or `saving`. |
| `recipe` | string | Status が報告する current public recipe value。 |

## Control Status

Direction:

| Transport | Direction |
| --- | --- |
| REST | Outbound response from control POST routes. |
| TCP | Not used. |
| MQTT | Not used. |

When it is sent:

| Scenario | Behavior |
| --- | --- |
| Client posts a control action | REST は action 後の state と public message を返します。 |

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
| `initialized` | boolean | Control action 後に simulated system が initialized されているかどうか。 |
| `processState` | string | Control action 後の public process state。 |
| `recipe` | string | Current public recipe value。 |
| `message` | string | Control action の public response message。 |

## Error Status

Direction:

| Transport | Direction |
| --- | --- |
| REST | Outbound response from `GET /api/error`. |
| TCP | Outbound event with `type: "error"`. |
| MQTT | Outbound event on `virex/error`. |

When it is sent:

| Scenario | Behavior |
| --- | --- |
| Client reads current error state | REST は current error status を返します。 |
| Simulator error changes | TCP と MQTT clients は error event を受信します。 |

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
| `hasError` | boolean | Error が現在 active かどうか。 |
| `message` | string | Public error message。Null の場合は omit されます。 |
| `initialized` | boolean | Error status 時点の initialization state。 |
| `processState` | string | Error status 時点の public process state。 |
| `recipe` | string | Current public recipe value。 |

## Result Summary

`ResultSummaryDto` は単一の result summary item です。REST は `ResultListDto.items[]` に埋め込み、TCP と MQTT は直接 publish します。

Direction:

| Transport | Direction |
| --- | --- |
| REST | Item inside `GET /api/results` response `items[]`. |
| TCP | Outbound event with `type: "result"`. |
| MQTT | Outbound event on `virex/result`. |

When it is sent:

| Scenario | Behavior |
| --- | --- |
| Simulator creates a result | TCP と MQTT clients は result summary event を受信します。 |
| Client queries historical simulated results | REST は `ResultListDto` を返し、各 `items[]` entry は 1 つの `ResultSummaryDto` です。 |

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
| `resultId` | string | Public result identifier. |
| `timestamp` | string | Result timestamp string. |
| `lotId` | string | Active WaferInfo から copy された lot identifier。 |
| `waferId` | string | Active WaferInfo から copy された wafer identifier。 |
| `recipeId` | string | Result に関連する recipe identifier。 |
| `slot` | string | Active WaferInfo から copy された slot identifier。 |
| `foupId` | string | Active WaferInfo から copy された FOUP identifier。 |
| `chamberId` | string | Active WaferInfo から copy された chamber identifier。 |
| `overallResult` | string | Public summary result value. |
| `defectCount` | number | Summary 内のすべての defect categories の total count。 |
| `imageRelativePath` | string | Associated image artifact の relative path string。 |
| `resultRelativePath` | string | Associated result artifact の relative path string。 |
| `imagePath` | string | Simulator Result prefix 適用後の public image path。 |
| `previewImagePath` | string | Simulator Result prefix 適用後の public preview image path。 |
| `resultPath` | string | Simulator Result prefix 適用後の public result path。 |

Result summaries は summary-only です。Defect lists、die lists、crop lists、image binaries、private inspection internals は含みません。

## REST Result Query Response

`ResultListDto` は result queries 用の REST response wrapper です。各 `items[]` entry は 1 つの `ResultSummaryDto` です。

Direction:

| Transport | Direction |
| --- | --- |
| REST | Response wrapper returned by `GET /api/results`. |
| TCP | Not used. |
| MQTT | Not used. |

This wrapper is REST-only. TCP and MQTT publish a single Result Summary directly and do not use Result List.

When it is sent:

| Scenario | Behavior |
| --- | --- |
| Client queries result summaries | REST は matching summary items と count を含む list wrapper を返します。 |

`GET /api/results` supports optional filters:

- `lotId`
- `waferId`
- `recipeId`

When multiple filters are supplied, they are combined with AND.

Query examples:

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
| `items` | array | `ResultSummaryDto` objects の array。 |
| `count` | number | Returned result summary items の数。 |

## REST Mapping

| Method and route | Direction | Payload content | Notes |
| --- | --- | --- | --- |
| `GET /api/status` | Outbound response | `StatusDto` | Reads current public state. |
| `GET /api/error` | Outbound response | `ErrorStatusDto` | Reads current public error state. |
| `GET /api/wafer-info` | Outbound response | `WaferInfo` | Reads current public wafer context. |
| `POST /api/wafer-info` | Inbound request | `WaferInfo` | Updates current public wafer context. |
| `POST /api/control/initialize` | Outbound response | `ControlStatusDto` | Initializes the simulator state. |
| `POST /api/control/terminate` | Outbound response | `ControlStatusDto` | Terminates the simulator state. |
| `POST /api/control/start` | Outbound response | `ControlStatusDto` | Starts a simulated cycle. |
| `POST /api/control/stop` | Outbound response | `ControlStatusDto` | Stops a simulated cycle. |
| `GET /api/results` | Outbound response | `ResultListDto` | Optional filters: `lotId`, `waferId`, `recipeId`. Multiple filters are combined with AND. |

## TCP Mapping

TCP messages are JSON objects. TCP outbound events include a `type` property. TCP inbound commands use the following shapes:

| Inbound command | Direction | JSON example | Notes |
| --- | --- | --- | --- |
| WaferInfo | Inbound to simulator | `{"type":"waferInfo","lotId":"LOT-001","waferId":"W01","recipeId":"RCP-A","slot":"1","foupId":"FOUP-A","chamberId":"CH-1"}` | Legacy WaferInfo frames may omit `type`; parser は WaferInfo JSON parser に fallback します。 |
| Start | Inbound to simulator | `{"type":"start"}` | Starts a simulated cycle. |
| Stop | Inbound to simulator | `{"type":"stop"}` | Stops a simulated cycle. |

TCP outbound events:

| Event type | Direction | Payload content |
| --- | --- | --- |
| `status` | Outbound from simulator | `StatusDto` plus `type`. |
| `waferInfo` | Outbound from simulator | `WaferInfo` plus `type`. |
| `result` | Outbound from simulator | `ResultSummaryDto` plus `type`. |
| `error` | Outbound from simulator | `ErrorStatusDto` plus `type`. |

## MQTT Mapping

MQTT is outbound-only. すべての simulator events を観察するには base topic wildcard を subscribe します。

```text
virex/#
```

| Topic | Direction | Payload content | Notes |
| --- | --- | --- | --- |
| `virex/status` | Outbound from simulator | `StatusDto` | Payload does not require `type`. |
| `virex/wafer-info` | Outbound from simulator | `WaferInfo` | Payload does not require `type`. |
| `virex/result` | Outbound from simulator | `ResultSummaryDto` | Payload does not require `type`. |
| `virex/error` | Outbound from simulator | `ErrorStatusDto` | Payload does not require `type`. |
