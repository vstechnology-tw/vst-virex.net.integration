# Protocol Versioning

Public packages 및 protocol changes에는 semantic versioning을 사용합니다.

Examples:

```text
1.0.0 Initial REST/MQTT/TCP integration contract.
1.1.0 Backward-compatible field or endpoint added.
2.0.0 Breaking payload, route, topic, or behavior change.
```

Rules:

- Minor version에서 public JSON fields를 삭제하거나 rename하지 마십시오.
- Minor version에서는 additive fields를 추가할 수 있습니다.
- Breaking changes에는 major version이 필요합니다.
- Simulator, SDK, docs, contract tests는 동일한 version으로 유지하십시오.
- Public DTO, route, topic, event-shape가 변경되면 [Transmitted Content / Payloads](payloads.md)도 동기화하십시오.
