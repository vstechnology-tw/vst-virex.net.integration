# Protocol 版本規則

Public packages 與 protocol changes 使用 semantic versioning。

Examples:

```text
1.0.0 Initial REST/MQTT/TCP integration contract.
1.1.0 Backward-compatible field or endpoint added.
2.0.0 Breaking payload, route, topic, or behavior change.
```

Rules:

- Minor version 不可移除或改名 public JSON fields。
- Minor version 可以加入 additive fields。
- Breaking changes 需要 major version。
- Simulator、SDK、docs、contract tests 必須保持同一個 version。
- 任何 public DTO、route、topic 或 event-shape change 都要同步更新 [傳送內容 / Payloads](payloads.md)。
