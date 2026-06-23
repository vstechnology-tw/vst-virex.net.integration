# Payload Reference

이 페이지는 public Virex.NET integration surfaces에서 전송되는 JSON content를 설명합니다. Customer-visible DTOs, route/topic mappings, simulator verification steps를 다룹니다. Private inspection logic 또는 runtime internals는 설명하지 않습니다.

## JSON Rules

모든 REST, TCP, MQTT payloads는 동일한 public JSON naming rules를 사용합니다.

| Rule | Behavior |
| --- | --- |
| Property names | `camelCase` 로 serialized 됩니다. |
| Null values | Serialized JSON에서 omit됩니다. |
| Incoming property names | Case-insensitive로 읽습니다. |
| Text encoding | UTF-8 JSON. |

Example:

```json
{
  "initialized": true,
  "processState": "ready",
  "recipe": "RCP-A"
}
```

`processState` 는 다음 중 하나입니다.

| Value | Meaning |
| --- | --- |
| `ready` | Simulator가 idle이며 다음 action을 받을 준비가 된 상태입니다. |
| `capturing` | Simulated capture step이 active입니다. |
| `inspecting` | Simulated inspection step이 active입니다. |
| `saving` | Simulated save step이 active입니다. |

Command transitions 및 rejected states는 [State Machine](state-machine.md)를 참조하십시오.

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

MQTT는 outbound-only입니다. Topic이 event를 식별하므로 MQTT payloads에는 `type` property가 필요하지 않습니다.

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
| Host updates wafer context | Simulated cycle을 시작하거나 query하기 전에 REST 또는 TCP로 WaferInfo를 보냅니다. |
| Simulator publishes wafer context | WaferInfo가 변경되면 TCP 및 MQTT clients가 event를 수신합니다. |

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
| `lotId` | string | Result filtering 및 event correlation에 사용하는 public lot identifier. |
| `waferId` | string | Public wafer identifier. |
| `recipeId` | string | Simulated wafer context와 연결된 public recipe identifier. |
| `slot` | string | Slot identifier. |
| `foupId` | string | FOUP identifier. |
| `chamberId` | string | Chamber identifier. |

Simulator/sample verification:

1. Simulator를 시작하고 **Start Servers** 를 누릅니다.
2. C# SDK, raw REST sample, raw TCP sample로 WaferInfo를 보내거나 simulator fields를 편집한 뒤 **Apply WaferInfo** 를 누릅니다.
3. Simulator Event Log가 모든 public fields를 한 줄에 출력하는지 확인합니다.

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
| Client reads current simulator state | REST는 current status를 반환합니다. |
| Simulator state changes | TCP 및 MQTT clients는 status events를 수신합니다. |

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
| `initialized` | boolean | Simulated system이 initialized 되었는지 여부. |
| `processState` | string | Current public process state: `ready`, `capturing`, `inspecting`, or `saving`. |
| `recipe` | string | Status가 보고하는 current public recipe value. |

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
| Client posts a control action | REST는 action 후 state와 public message를 반환합니다. |

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
| `initialized` | boolean | Control action 후 simulated system이 initialized 되었는지 여부. |
| `processState` | string | Control action 후 public process state. |
| `recipe` | string | Current public recipe value. |
| `message` | string | Control action의 public response message. |

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
| Client reads current error state | REST는 current error status를 반환합니다. |
| Simulator error changes | TCP 및 MQTT clients는 error event를 수신합니다. |

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
| `hasError` | boolean | Error가 현재 active인지 여부. |
| `message` | string | Public error message. Null이면 omit됩니다. |
| `initialized` | boolean | Error status 시점의 initialization state. |
| `processState` | string | Error status 시점의 public process state. |
| `recipe` | string | Current public recipe value. |

## Result Summary

`ResultSummaryDto` 는 단일 result summary item입니다. REST는 이를 `ResultListDto.items[]` 에 포함하고, TCP 및 MQTT는 직접 publish합니다.

Direction:

| Transport | Direction |
| --- | --- |
| REST | Item inside `GET /api/results` response `items[]`. |
| TCP | Outbound event with `type: "result"`. |
| MQTT | Outbound event on `virex/result`. |

When it is sent:

| Scenario | Behavior |
| --- | --- |
| Simulator creates a result | TCP 및 MQTT clients는 result summary event를 수신합니다. |
| Client queries historical simulated results | REST는 `ResultListDto` 를 반환하며 각 `items[]` entry는 하나의 `ResultSummaryDto` 입니다. |

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
| `lotId` | string | Active WaferInfo에서 copy된 lot identifier. |
| `waferId` | string | Active WaferInfo에서 copy된 wafer identifier. |
| `recipeId` | string | Result와 연결된 recipe identifier. |
| `slot` | string | Active WaferInfo에서 copy된 slot identifier. |
| `foupId` | string | Active WaferInfo에서 copy된 FOUP identifier. |
| `chamberId` | string | Active WaferInfo에서 copy된 chamber identifier. |
| `overallResult` | string | Public summary result value. |
| `defectCount` | number | Summary 내 모든 defect categories의 total count. |
| `imageRelativePath` | string | Associated image artifact의 relative path string. |
| `resultRelativePath` | string | Associated result artifact의 relative path string. |
| `imagePath` | string | Simulator Result prefix 적용 후 public image path. |
| `previewImagePath` | string | Simulator Result prefix 적용 후 public preview image path. |
| `resultPath` | string | Simulator Result prefix 적용 후 public result path. |

Result summaries는 summary-only입니다. Defect lists, die lists, crop lists, image binaries, private inspection internals는 포함하지 않습니다.

## REST Result Query Response

`ResultListDto` 는 result queries용 REST response wrapper입니다. 각 `items[]` entry는 하나의 `ResultSummaryDto` 입니다.

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
| Client queries result summaries | REST는 matching summary items와 count를 포함한 list wrapper를 반환합니다. |

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
| `items` | array | `ResultSummaryDto` objects 배열. |
| `count` | number | 반환된 result summary items 수. |

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

TCP messages는 JSON objects입니다. TCP outbound events에는 `type` property가 포함됩니다. TCP inbound commands는 다음 shapes를 사용합니다.

| Inbound command | Direction | JSON example | Notes |
| --- | --- | --- | --- |
| WaferInfo | Inbound to simulator | `{"type":"waferInfo","lotId":"LOT-001","waferId":"W01","recipeId":"RCP-A","slot":"1","foupId":"FOUP-A","chamberId":"CH-1"}` | Legacy WaferInfo frames는 `type` 을 omit할 수 있으며 parser는 WaferInfo JSON parser로 fallback합니다. |
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

MQTT is outbound-only. 모든 simulator events를 관찰하려면 base topic wildcard를 subscribe합니다.

```text
virex/#
```

| Topic | Direction | Payload content | Notes |
| --- | --- | --- | --- |
| `virex/status` | Outbound from simulator | `StatusDto` | Payload does not require `type`. |
| `virex/wafer-info` | Outbound from simulator | `WaferInfo` | Payload does not require `type`. |
| `virex/result` | Outbound from simulator | `ResultSummaryDto` | Payload does not require `type`. |
| `virex/error` | Outbound from simulator | `ErrorStatusDto` | Payload does not require `type`. |
