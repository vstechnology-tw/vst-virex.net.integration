# Protocol Versioning

Public packages と protocol changes には semantic versioning を使用します。

Examples:

```text
1.0.0 Initial REST/MQTT/TCP integration contract.
1.1.0 Backward-compatible field or endpoint added.
2.0.0 Breaking payload, route, topic, or behavior change.
```

Rules:

- Minor version で public JSON fields を削除または rename しないでください。
- Minor version では additive fields を追加できます。
- Breaking changes には major version が必要です。
- Simulator、SDK、docs、contract tests は同じ version に揃えてください。
- Public DTO、route、topic、event-shape が変わる場合は、[Transmitted Content / Payloads](payloads.md) も同期してください。
