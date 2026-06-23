# Startup Order And State Transitions

This page uses a simplified state diagram to explain the startup order a client should follow when integrating with the simulator, and the public state changes caused by external commands. The diagram keeps only the states a customer needs for integration decisions; it does not enumerate every internal possibility.

The simulator reports state with two public values. Clients do not need internal implementation details; they only need these fields to decide whether the next command is valid.

| Field | Meaning |
| --- | --- |
| `initialized` | Whether the simulated service has been initialized. |
| `processState` | Current process state. `ready` means the next cycle can be accepted; `capturing`, `inspecting`, and `saving` are cycle progress updates. |

`Start Servers` controls whether REST, TCP, and MQTT endpoints are listening. It does not change `initialized` or `processState`.

## State Diagram

![Virex.NET simulator state transition diagram](assets/state-machine-flow.svg)

In the diagram, **external command** means a control command sent by a client, the SDK, REST, or TCP. `capturing`, `inspecting`, and `saving` are progress states advanced internally by the simulator during a cycle. Clients usually wait for status/result events, or send **Stop** if they need to cancel.

## Client Rules

| Rule | Client-side check |
| --- | --- |
| **Start Servers** is the communication prerequisite. | Connect only after REST/TCP/MQTT endpoints are listening. |
| **Initialize** is the cycle prerequisite. | If `initialized=false`, start returns `409 not_initialized`. |
| **Start Cycle** should be sent only when initialized and ready. | Start is accepted when `initialized=true` and `processState=ready`. |
| Active cycle states are progress updates. | `capturing`, `inspecting`, and `saving` mean a cycle is running; wait for status/result events or send stop. |
| The simulator returns to ready after the result event. | After the result summary, `processState=ready` and the next cycle can begin. |

## Commands And Transitions

| Command or UI action | Required state | Result |
| --- | --- | --- |
| **Initialize** / `POST /api/control/initialize` | `initialized=false`, `processState=ready` | Sets `initialized=true`, keeps `processState=ready`. |
| **Terminate** / `POST /api/control/terminate` | `processState=ready` | Sets `initialized=false`, keeps `processState=ready`. |
| **Start Cycle** / `POST /api/control/start` / TCP `{"type":"start"}` | `initialized=true`, `processState=ready` | Transitions through `capturing`, `inspecting`, `saving`, then returns to `ready` after result emission. |
| **Stop** / `POST /api/control/stop` / TCP `{"type":"stop"}` | Active process state: `capturing`, `inspecting`, or `saving` | Cancels the current cycle and returns to `ready`. |
| **Apply WaferInfo** / WaferInfo REST or TCP update | Any process state | Updates wafer context and emits wafer-info events. It does not change `processState`. |
| **Emit Fake Result** | Any process state | Emits a single result summary event. It does not change `processState`. |
| **Emit Error** | Any process state | Emits an error event. It does not change `processState`. |

## Common Rejected Commands

| Condition | Command | Response |
| --- | --- | --- |
| `initialized=false` | Start cycle | HTTP `409` / `not_initialized`; state remains `initialized=false`, `processState=ready`. |
| `processState` is `capturing`, `inspecting`, or `saving` | Start cycle | HTTP `409` / `process_active`; current cycle continues. |
| `initialized=false` | Stop | HTTP `409` / `not_initialized`; state remains unchanged. |
| `initialized=true`, `processState=ready` | Stop | HTTP `409` / `not_running`; state remains unchanged. |
| `processState` is not `ready` | Terminate | HTTP `409`; state remains unchanged. |

## Event Visibility

State changes are visible through:

- REST `GET /api/status`
- TCP `status` events
- MQTT `virex/status` events
- SDK `GetStatusAsync`

Result, wafer-info, and error events are separate event types. They may occur while the simulator is `ready`, but they are not additional `processState` values.
