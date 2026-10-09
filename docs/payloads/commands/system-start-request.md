# SystemStartRequest

`SystemStartRequest` is the optional request body for starting a run.

## JSON

```json
{
  "requestId": "work-request-1",
  "recipeName": "RCP-A",
  "condition": "golden-sample",
  "runMode": "continue",
  "inspectionMode": "captureOnly"
}
```

## Field

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `requestId` | string | No | Correlates acceptance and work events. The SDK creates one before sending. |
| `recipeName` | string | No | Catalog name of the Recipe requested for this work. Omitted or null keeps the current Recipe. This is separate from `condition`. |
| `condition` | string | No | Start condition or related label. |
| `runMode` | string | No | Run mode. When omitted or blank, `continue` is assumed. |
| `inspectionMode` | string | No | Inspection policy, separate from run mode. Null uses the existing Recipe behavior. See [InspectionMode](../../inspection-mode.md). |

## Use location

| Interface | Usage |
| --- | --- |
| RESTful API | `POST /api/system/start` request content. |
| TCP | Incoming `type: "start"` command frame. |

See [ControlRunModes](control-run-modes.md) for allowed `runMode` values.

The REST SDK transmits these options without preparing a Recipe itself. `recipeName` is currently supported by the REST `StartAsync(SystemStartRequest)` method. The existing MQTT/TCP SDK methods do not transmit this field. Acceptance does not establish capture readiness. Use the server's correlated readiness event before starting a scan. The simulator currently retains its existing Recipe behavior; this request-field addition does not simulate Recipe switching.
