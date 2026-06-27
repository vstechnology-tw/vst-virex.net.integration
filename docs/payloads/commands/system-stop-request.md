# SystemStopRequest

`SystemStopRequest` is the optional request body for stopping a run.

## JSON

```json
{
  "reason": "operator-request"
}
```

## Field

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `reason` | string | No | Stop reason or related label. |

## Use location

| Interface | Usage |
| --- | --- |
| REST | `POST /api/system/stop` request content. |
| TCP | Incoming `type: "stop"` command frame. |
