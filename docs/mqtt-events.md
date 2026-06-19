# MQTT Events

MQTT is outbound-only from the Virex.NET-compatible service.

Default base topic:

```text
Virex.NET
```

Published topics:

```text
Virex.NET/status
Virex.NET/wafer-info
Virex.NET/result
Virex.NET/error
```

Payloads use the same JSON shapes as REST and TCP socket events, except MQTT payloads do not require a `type` field because the child topic identifies the event.

MQTT is not used for command/control.
