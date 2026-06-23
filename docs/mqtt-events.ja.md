# MQTT Events

MQTT は Virex.NET-compatible service からの outbound-only event に使用します。

**Start Servers** をクリックすると、simulator は embedded MQTT broker を起動します。Local clients は default broker `127.0.0.1:1883` を subscribe できます。simulator testing では外部 broker は不要です。

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

Payload は REST と TCP socket events と同じ JSON shape を使用します。ただし MQTT payload は child topic が event を識別するため、`type` field は不要です。

MQTT は command/control には使用しません。

Field-level payload details と simulator verification steps は [Transmitted Content / Payloads](payloads.md) を参照してください。
