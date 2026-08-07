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

When operator recovery is required, the response remains in the public `Deinitializing` state and carries a structured action:

```json
{
  "accepted": false,
  "state": "Deinitializing",
  "command": "Stop",
  "errorCode": "requires_deinitialize",
  "recoveryAction": "Deinitialize",
  "recoveryStartedAt": "2026-08-07T10:00:00.000+00:00",
  "recoverySource": "Acquisition",
  "recoveryPhase": "Deinitializing",
  "recoveryDetails": "Camera acquisition failed.",
  "message": "Deinitialize is required before another command can be accepted."
}
```

## Field

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `accepted` | boolean | Yes | Whether the command is accepted. |
| `state` | string | Yes | Current state after command processing. |
| `command` | string | Yes | The public command name. |
| `errorCode` | string | No | Omitted for accepted commands. `invalid_state` means the command is invalid in the current state. |
| `recoveryAction` | string | No | Operator recovery action required by the current state. The public protocol currently defines `Deinitialize`; application restart remains a UI-only last resort. |
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
