# Version

Use semantic versioning for public package and protocol changes.

| Version | Meaning |
| --- | --- |
| `1.0.0` | Initial public integration contract. |
| `1.1.0` | Additive fields, endpoints, topics, or events. |
| `2.0.0` | Breaking data content, route, topic, or behavior changes. |

## Version rules

- Minor versions do not remove or rename public JSON fields.
- Minor versions may add additive fields.
- Breaking changes require a major version.
- The simulator, SDK, documentation, and contract tests must be synchronized.
- [Payload Reference](payloads.md) must be aligned with the public payload models, routes, topics, and event structures.
