# MQTT Events

MQTT는 Virex.NET-compatible service에서 나가는 outbound-only event에 사용됩니다.

**Start Servers** 를 클릭하면 simulator가 embedded MQTT broker를 시작합니다. Local clients는 default broker `127.0.0.1:1883` 를 subscribe할 수 있습니다. simulator testing에는 외부 broker가 필요하지 않습니다.

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

Payload는 REST 및 TCP socket events와 동일한 JSON shape를 사용합니다. 단, MQTT payload는 child topic이 event를 식별하므로 `type` field가 필요하지 않습니다.

MQTT는 command/control에 사용하지 않습니다.

Field-level payload details 및 simulator verification steps는 [Transmitted Content / Payloads](payloads.md)를 참조하십시오.
