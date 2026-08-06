# ErrorInfo

`ErrorInfo` describes the error information currently in effect.

It is not a lifecycle state. `hasError=false` means there are currently no active errors.

## JSON

```json
{
  "hasError": true,
  "message": "Recipe load failed.",
  "state": "Ready"
}
```

## Field

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `hasError` | boolean | Yes | Whether an active error currently exists. |
| `message` | string | No | Error message. Omitted if there is no message. |
| `state` | string | Yes | Lifecycle state when the error information is reported. |
| `recoveryAction` | string | No | Operator recovery action required by the error; omitted when no action is required. |

## Use location

| Interface | Usage |
| --- | --- |
| TCP | `errorChanged` event. |
| MQTT | `virex/errorChanged`. |
