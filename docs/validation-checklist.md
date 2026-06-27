# Validation Checklist

Use this checklist to determine whether a vendor integration is ready to move from the simulator to a production-compatible endpoint.

## REST

| Check | Expected Result |
| --- | --- |
| Read state | `GET /api/status` returns `SystemStatus` with `state`. |
| Initialization | `POST /api/system/initialize` is accepted in `Uninitialized` and returns `Ready`. |
| Update ProductInfo | `POST /api/product-info` is accepted in `Ready` and returns `Ready`. |
| Start | `POST /api/system/start` is accepted in `Ready` and returns `Running`. |
| Stop | `POST /api/system/stop` is accepted in `Running` and returns `Ready`. |
| Deinitialize | `POST /api/system/deinitialize` is accepted in `Ready` and returns `Uninitialized`. |
| Invalid command | Invalid commands return `accepted=false`, `errorCode=invalid_state`, and the current `state`. |
| Results | `GET /api/results` returns summaries matching the ProductInfo snapshot fields. |

## TCP

| Check | Expected Result |
| --- | --- |
| Connection | Client can connect to the configured TCP port. |
| Framing | Each frame is one UTF-8 JSON object ending with `\n`. |
| ProductInfo command | `type: "productInfo"` updates ProductInfo in `Ready`. |
| Start/stop commands | `type: "start"` and `type: "stop"` follow the same state rules as REST. |
| Event parsing | Client can handle `statusChanged`, `productInfoChanged`, `runStarted`, `runCompleted`, `resultCreated`, `errorChanged`, and `commandRejected`. |

## MQTT

| Check | Expected Result |
| --- | --- |
| Subscription | Client can subscribe to `virex/#` or the configured root topic. |
| State events | Client receives `statusChanged`, `runStarted`, and `runCompleted`. |
| ProductInfo event | Client receives `productInfoChanged`. |
| Result event | Client receives `resultCreated`. |
| Rejection event | Client receives `commandRejected` when a command is rejected. |

## Portability

Confirm these points before switching to production:

- Endpoint settings are adjustable.
- The integration does not rely on simulator UI labels or fixed delays.
- The integration uses `Virex.NET.Contracts` models or equivalent JSON structures.
- The integration treats MQTT as a pure outgoing channel.
- The integration can handle reconnection and duplicate events.
