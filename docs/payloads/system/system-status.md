# SystemStatus

`SystemStatus` reports the current public lifecycle state.

## JSON

```json
{
  "state": "Ready"
}
```

## Field

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `state` | string | Yes | Current lifecycle state. |

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
| REST | `GET /api/status` response. |
| TCP | `statusChanged`, `runStarted`, `runCompleted` events. |
| MQTT | `virex/statusChanged`, `virex/runStarted`, `virex/runCompleted`. |

`status` is the resource or event category. `state` is the lifecycle value reported by this status payload.
