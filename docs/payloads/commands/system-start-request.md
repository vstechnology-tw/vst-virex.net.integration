# SystemStartRequest

`SystemStartRequest` is the optional request body for starting a run.

## JSON

```json
{
  "condition": "golden-sample",
  "runMode": "continue"
}
```

## Field

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `condition` | string | No | Start condition or related label. |
| `runMode` | string | No | Run mode. When omitted or blank, `continue` is assumed. |

## Use location

| Interface | Usage |
| --- | --- |
| REST | `POST /api/system/start` request content. |
| TCP | Incoming `type: "start"` command frame. |

See [ControlRunModes](control-run-modes.md) for allowed `runMode` values.
