# ErrorInfo

`ErrorInfo` describes the error information currently in effect.

It is not a lifecycle state. `hasError=false` means there are currently no active errors.

## JSON

```json
{
  "hasError": true,
  "message": "Recipe load failed.",
  "state": "Deinitializing",
  "errorCode": "requires_deinitialize",
  "recoveryAction": "Deinitialize",
  "recoveryStartedAt": "2026-08-07T10:00:00.000+00:00",
  "recoverySource": "Acquisition",
  "recoveryPhase": "Deinitializing",
  "recoveryDetails": "Camera acquisition failed."
}
```

## Field

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `hasError` | boolean | Yes | Whether an active error currently exists. |
| `message` | string | No | Error message. Omitted if there is no message. |
| `state` | string | Yes | Lifecycle state when the error information is reported. |
| `errorCode` | string | No | Machine-readable error classification, such as `requires_deinitialize`. |
| `recoveryAction` | string | No | Operator recovery action required by the error; omitted when no action is required. |
| `recoveryStartedAt` | string (date-time) | No | UTC timestamp at which the current recovery attempt began. |
| `recoverySource` | string | No | Subsystem that reported the failure. |
| `recoveryPhase` | string | No | Recovery phase currently in progress. |
| `recoveryDetails` | string | No | Human-readable recovery context. |

## Use location

| Interface | Usage |
| --- | --- |
| TCP | `errorChanged` event. |
| MQTT | `virex/errorChanged`. |
