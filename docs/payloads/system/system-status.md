# SystemStatus

`SystemStatus` reports the current public lifecycle state.

## JSON

```json
{
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
| `state` | string | Yes | Current lifecycle state. |
| `errorCode` | string | No | Stable machine-readable recovery or failure code. |
| `recoveryAction` | string | No | Operator recovery action required by the state; recovery normally uses `Deinitialize`. Omitted when no action is required. |
| `recoveryStartedAt` | string (date-time) | No | UTC timestamp at which the current recovery attempt began. |
| `recoverySource` | string | No | Subsystem that reported the failure, such as `Acquisition`. |
| `recoveryPhase` | string | No | Recovery phase currently in progress, such as `Deinitializing`. |
| `recoveryDetails` | string | No | Human-readable recovery context. |

## State Values

```text
Uninitialized
Initializing
Ready
UpdatingProductInfo
Running
Deinitializing
```

## Use location

| Interface | Usage |
| --- | --- |
| RESTful API | `GET /api/status` response. |
| TCP | `statusChanged`, `runStarted`, `runCompleted` events. |
| MQTT | `virex/statusChanged`, `virex/runStarted`, `virex/runCompleted`. |

`status` is the resource or event category. `state` is the lifecycle value reported by this status payload.
