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

## Field

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `accepted` | boolean | Yes | Whether the command is accepted. |
| `state` | string | Yes | Current state after command processing. |
| `command` | string | Yes | The public command name. |
| `errorCode` | string | No | Omitted for accepted commands. `invalid_state` means the command is invalid in the current state. |
| `message` | string | Yes | Response message. |

## Use location

| Interface | Usage |
| --- | --- |
| REST | System command routes and `POST /api/product-info` response body. |
| TCP | `commandRejected` event when a command is rejected. |
| MQTT | `virex/commandRejected`. |
