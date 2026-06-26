# Payload Reference

This page documents the JSON content transmitted by the public Virex.NET integration surfaces. It describes customer-visible DTOs, route/topic mappings, and simulator verification steps. It does not describe private inspection logic or runtime internals.

## JSON Rules

All REST, TCP, and MQTT payloads use the same public JSON naming rules:

| Rule | Behavior |
| --- | --- |
| Property names | Serialized as `camelCase`. |
| Null values | Omitted from serialized JSON. |
| Incoming property names | Read case-insensitively. |
| Text encoding | UTF-8 JSON. |

Example:

```json
{
  "initialized": true,
  "processState": "ready",
  "recipe": "RCP-A"
}
```

`processState` is one of:

| Value | Meaning |
| --- | --- |
| `ready` | The simulator is idle and ready for the next action. |
| `capturing` | A simulated capture step is active. |
| `inspecting` | A simulated inspection step is active. |
| `saving` | A simulated save step is active. |

For command transitions and rejected states, see [State Machine](state-machine.md).

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

MQTT is outbound-only. MQTT payloads do not need a `type` property because the topic identifies the event.

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
| Host updates wafer context | Send WaferInfo by REST or TCP before starting or querying a simulated cycle. |
| Simulator publishes wafer context | TCP and MQTT clients receive an event when WaferInfo changes. |

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
| `lotId` | string | Public lot identifier used for result filtering and event correlation. |
| `waferId` | string | Public wafer identifier. |
| `recipeId` | string | Public recipe identifier associated with the simulated wafer context. |
| `slot` | string | Slot identifier. |
| `foupId` | string | FOUP identifier. |
| `chamberId` | string | Chamber identifier. |

Simulator/sample verification:

1. Start the simulator and click **Start Servers**.
2. Send WaferInfo through the C# SDK, raw REST sample, or raw TCP sample, or edit the simulator fields and click **Apply WaferInfo**.
3. Confirm the simulator Event Log prints all public fields in one line, for example:

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
| Client reads current simulator state | REST returns the current status. |
| Simulator state changes | TCP and MQTT clients receive status events. |

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
| `initialized` | boolean | Whether the simulated system has been initialized. |
| `processState` | string | Current public process state: `ready`, `capturing`, `inspecting`, or `saving`. |
| `recipe` | string | Current public recipe value reported by status. |

Simulator/sample verification:

1. Start the simulator and click **Start Servers**.
2. Run the C# SDK or raw REST sample and confirm it reads `GET /api/status`.
3. Click **Initialize** or **Start Cycle** and confirm TCP or MQTT samples print a status event with the updated `initialized` or `processState` value.

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
| Client posts a control action | REST returns the state after the action plus a public message. |

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
| `initialized` | boolean | Whether the simulated system is initialized after the control action. |
| `processState` | string | Public process state after the control action. |
| `recipe` | string | Current public recipe value. |
| `message` | string | Public response message for the control action. |

Simulator/sample verification:

1. Start the simulator and click **Start Servers**.
2. Send `POST /api/control/initialize`, `POST /api/control/terminate`, `POST /api/control/start`, or `POST /api/control/stop` from a REST-capable sample.
3. Confirm the sample prints a JSON response with `initialized`, `processState`, `recipe`, and `message`.

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
| Client reads current error state | REST returns the current error status. |
| Simulator error changes | TCP and MQTT clients receive an error event. |

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
| `hasError` | boolean | Whether an error is currently active. |
| `message` | string | Public error message. Omitted when null. |
| `initialized` | boolean | Initialization state at the time of the error status. |
| `processState` | string | Public process state at the time of the error status. |
| `recipe` | string | Current public recipe value. |

Simulator/sample verification:

1. Start the simulator and click **Start Servers**.
2. Run a TCP or MQTT sample and keep it connected.
3. Click **Emit Error** in the simulator.
4. Confirm the sample prints an error payload with `hasError`, `message`, `initialized`, `processState`, and `recipe`.

## Result Summary

`ResultSummaryDto` is a single result summary item. REST embeds it in `ResultListDto.items[]`, while TCP and MQTT publish it directly.

Direction:

| Transport | Direction |
| --- | --- |
| REST | Item inside `GET /api/results` response `items[]`. |
| TCP | Outbound event with `type: "result"`. |
| MQTT | Outbound event on `virex/result`. |

When it is sent:

| Scenario | Behavior |
| --- | --- |
| Simulator creates a result | TCP and MQTT clients receive a result summary event. |
| Client queries historical simulated results | REST returns a `ResultListDto`; each `items[]` entry is one `ResultSummaryDto`. |

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
| `lotId` | string | Lot identifier copied from the active WaferInfo. |
| `waferId` | string | Wafer identifier copied from the active WaferInfo. |
| `recipeId` | string | Recipe identifier associated with the result. |
| `slot` | string | Slot identifier copied from the active WaferInfo. |
| `foupId` | string | FOUP identifier copied from the active WaferInfo. |
| `chamberId` | string | Chamber identifier copied from the active WaferInfo. |
| `overallResult` | string | Public summary result value. |
| `defectCount` | number | Total count of all defect categories in the summary. |
| `imageRelativePath` | string | Relative path string for an associated image artifact. |
| `resultRelativePath` | string | Relative path string for an associated result artifact. |
| `imagePath` | string | Public image path after applying the simulator Result prefix. |
| `previewImagePath` | string | Public preview image path after applying the simulator Result prefix. |
| `resultPath` | string | Public result path after applying the simulator Result prefix. |

Result summaries are intentionally summary-only. They do not include defect lists, die lists, crop lists, image binaries, or private inspection internals.

Simulator/sample verification:

1. Start the simulator and click **Start Servers**.
2. Apply WaferInfo, click **Initialize**, then click **Start Cycle** or **Emit Fake Result**.
3. Confirm a TCP or MQTT sample prints a `result` event, or use a REST-capable sample to call `GET /api/results`.
4. Confirm the returned summary includes the WaferInfo identifiers and count fields, but does not include defect lists, die lists, crop lists, image binaries, or private inspection internals.

## REST Result Query Response

`ResultListDto` is the REST response wrapper for result queries. Each `items[]` entry is one `ResultSummaryDto`.

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
| Client queries result summaries | REST returns a list wrapper with matching summary items and a count. |

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
| `items` | array | Array of `ResultSummaryDto` objects. |
| `count` | number | Number of result summary items returned. |

Simulator/sample verification:

1. Create a simulated result with matching WaferInfo.
2. Call `GET /api/results` from a REST-capable sample.
3. Confirm `count` matches the number of returned `items`.

## REST Mapping

| Method and route | Direction | Payload content | Notes |
| --- | --- | --- | --- |
| `GET /api/status` | Outbound response | `StatusDto` | Reads current public state. |
| `GET /api/error` | Outbound response | `ErrorStatusDto` | Reads current public error state. |
| `GET /api/wafer-info` | Outbound response | `WaferInfo` | Reads current public wafer context. |
| `POST /api/wafer-info` | Inbound request | `WaferInfo` | Updates current public wafer context. |
| `POST /api/control/initialize` | Outbound response | `ControlStatusDto` | Initializes the simulator state. |
| `POST /api/control/terminate` | Outbound response | `ControlStatusDto` | Terminates the simulator state. |
| `POST /api/control/start` | Optional inbound `ControlStartRequest`; outbound response `ControlStatusDto` | `condition` is optional. `runMode` is optional and defaults to `continue`; supported values are `continue` and `single`. |
| `POST /api/control/stop` | Optional inbound `ControlStopRequest`; outbound response `ControlStatusDto` | `reason` is optional and may be omitted or blank. |
| `GET /api/results` | Outbound response | `ResultListDto` | Optional filters: `lotId`, `waferId`, `recipeId`. Multiple filters are combined with AND. |

REST verification:

1. Start the simulator and click **Start Servers**.
2. Run the C# SDK, C# raw REST, or C++ raw REST sample.
3. Confirm the sample reads status, posts WaferInfo, runs a control action, and queries result summaries using the public payloads above.

## TCP Mapping

TCP messages are JSON objects. TCP outbound events include a `type` property. TCP inbound commands use the following shapes:

| Inbound command | Direction | JSON example | Notes |
| --- | --- | --- | --- |
| WaferInfo | Inbound to simulator | `{"type":"waferInfo","lotId":"LOT-001","waferId":"W01","recipeId":"RCP-A","slot":"1","foupId":"FOUP-A","chamberId":"CH-1"}` | Legacy WaferInfo frames may omit `type`; the parser falls back to the WaferInfo JSON parser. |
| Start | Inbound to simulator | `{"type":"start","condition":"golden-sample","runMode":"continue"}` | `condition` is optional. `runMode` defaults to `continue`; legacy `{"type":"start"}` remains valid. |
| Stop | Inbound to simulator | `{"type":"stop","reason":"operator-request"}` | `reason` is optional; legacy `{"type":"stop"}` remains valid. |

TCP outbound events:

| Event type | Direction | Payload content |
| --- | --- | --- |
| `status` | Outbound from simulator | `StatusDto` plus `type`. |
| `waferInfo` | Outbound from simulator | `WaferInfo` plus `type`. |
| `result` | Outbound from simulator | `ResultSummaryDto` plus `type`. |
| `error` | Outbound from simulator | `ErrorStatusDto` plus `type`. |

TCP verification:

1. Start the simulator and click **Start Servers**.
2. Run a raw TCP sample.
3. Confirm it prints the initial status and WaferInfo frames.
4. Let the sample send a WaferInfo frame and confirm the simulator Event Log shows the TCP WaferInfo update.

## MQTT Mapping

MQTT is outbound-only. Subscribe to the base topic wildcard to observe all simulator events:

```text
virex/#
```

| Topic | Direction | Payload content | Notes |
| --- | --- | --- | --- |
| `virex/status` | Outbound from simulator | `StatusDto` | Payload does not require `type`. |
| `virex/wafer-info` | Outbound from simulator | `WaferInfo` | Payload does not require `type`. |
| `virex/result` | Outbound from simulator | `ResultSummaryDto` | Payload does not require `type`. |
| `virex/error` | Outbound from simulator | `ErrorStatusDto` | Payload does not require `type`. |

MQTT verification:

1. Start the simulator and click **Start Servers**.
2. Run a raw MQTT sample and keep it subscribed.
3. Click **Apply WaferInfo**, **Initialize**, **Start Cycle**, **Emit Fake Result**, and **Emit Error**.
4. Confirm the sample prints the matching topic and JSON payload for `wafer-info`, `status`, `result`, and `error`.
