# MQTT Events

MQTT 是 Virex.NET-compatible service 對外發布的 outbound-only event channel。

按下 **Start Servers** 後，simulator 會啟動 embedded MQTT broker。本機 clients 可以訂閱 default broker `127.0.0.1:1883`；simulator testing 不需要外部 broker。

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

Payloads 使用與 REST/TCP socket events 相同的 JSON shapes；但 MQTT payload 不需要 `type` field，因為 child topic 已識別 event。

MQTT 不用於 command/control。

Field-level payload details 與 simulator verification steps 請看 [傳送內容 / Payloads](payloads.md)。
