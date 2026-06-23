# MQTT Events

MQTT is outbound-only from the Virex.NET-compatible service.

The simulator starts an embedded MQTT broker when **Start Servers** is clicked. Local clients can subscribe to the default broker at `127.0.0.1:1883`; no external broker is required for simulator testing.

Default base topic:

```text
virex
```

Published topics:

```text
virex/status
virex/wafer-info
virex/result
virex/error
```

Payloads use the same JSON shapes as REST and TCP socket events, except MQTT payloads do not require a `type` field because the child topic identifies the event.

MQTT is not used for command/control.

For field-level payload details and simulator verification steps, see [Transmitted Content / Payloads](payloads.md).
