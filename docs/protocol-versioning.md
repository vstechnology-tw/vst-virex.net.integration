# Protocol Versioning

Use semantic versioning for public packages and protocol changes.

Examples:

```text
1.0.0 Initial REST/MQTT/TCP integration contract.
1.1.0 Backward-compatible field or endpoint added.
2.0.0 Breaking payload, route, topic, or behavior change.
```

Rules:

- Do not remove or rename public JSON fields in a minor version.
- Additive fields are allowed in a minor version.
- Breaking changes require a major version.
- Keep simulator, SDK, docs, and contract tests on the same version.
