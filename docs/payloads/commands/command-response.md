# CommandResponse

`CommandResponse` reports whether a command was accepted and the state after command processing.

## JSON on acceptance

```json
{
  "accepted": true,
  "state": "Ready",
  "command": "Initialize",
  "message": "Initialize accepted."
}
```

## JSON on rejection

```json
{
  "accepted": false,
  "state": "Running",
  "command": "SetProductInfo",
  "errorCode": "invalid_state",
  "message": "SetProductInfo is not valid while state is Running."
}
```

Classified faults remain in public `Faulted` with `recoveryAction: "Reset"`. Successful Reset returns `Uninitialized`; see [Alarm recovery](../../alarms.md). For example, a rejected Start while an acquisition fault is latched returns:

```json
{
  "accepted": false,
  "state": "Faulted",
  "command": "Start",
  "errorCode": "invalid_state",
  "recoveryAction": "Reset",
  "recoveryStartedAt": "2026-08-07T10:00:00.000+00:00",
  "recoverySource": "Acquisition",
  "recoveryPhase": "Faulted",
  "recoveryDetails": "Synthetic acquisition fault.",
  "message": "Command is not valid in the current state."
}
```

## Field

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `accepted` | boolean | Yes | Whether the command is accepted. |
| `state` | string | Yes | Current state after command processing. |
| `command` | string | Yes | The public command name. |
| `requestId` | string | No | Echoes a TCP query request ID on a correlated rejection. |
| `errorCode` | string | No | Omitted for accepted commands. `invalid_state` means the command is invalid in the current state. |
| `recoveryAction` | string | No | `Reset` for latched `Faulted`, or `Deinitialize` during cleanup. Successful cleanup still requires explicit Initialize. |
| `recoveryStartedAt` | string (date-time) | No | UTC timestamp at which the current recovery attempt began. |
| `recoverySource` | string | No | Subsystem that reported the failure. |
| `recoveryPhase` | string | No | Recovery phase currently in progress. |
| `recoveryDetails` | string | No | Human-readable recovery context. |
| `message` | string | Yes | Response message. |

## Use location

| Interface | Usage |
| --- | --- |
| RESTful API | System command routes and `POST /api/product-info` response body. |
| TCP | `commandRejected` event when a command is rejected. |
| MQTT | `virex/commandRejected`. |
