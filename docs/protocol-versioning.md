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

## Published release notes

These entries summarize the releases published for customers. Each heading links to the corresponding GitHub Release.

### [v2.1.1](https://github.com/vstechnology-tw/vst-virex.net.integration/releases/tag/v2.1.1) — 2026-08-17

- Adds the public `imageGrabbed` event across REST-compatible event models, TCP, MQTT, the C# SDK, and the simulator.
- Adds stable `captureId` correlation between `imageGrabbed` and the later `resultCreated` event.
- The simulator provides test image, preview, and result files through the paths in `resultCreated`.
- Updates simulator diagnostics, transport examples, and customer documentation.

### [v2.1.0](https://github.com/vstechnology-tw/vst-virex.net.integration/releases/tag/v2.1.0) — 2026-07-01

- Completes command and query parity across RESTful API, TCP Socket, and MQTT.
- Adds MQTT command/response support for status, error, ProductInfo, lifecycle commands, and result queries.
- Adds TCP query frames for status, error, ProductInfo, and results.
- Aligns the C#, Python, and C++ samples to the same 13-step integration flow.
- Publishes Contracts and Client packages plus simulator downloads for .NET Framework 4.8, .NET 8, and .NET 10.

### [v2.0.3.1](https://github.com/vstechnology-tw/vst-virex.net.integration/releases/tag/v2.0.3.1) — 2026-06-30

- Simulator-only release.
- Documents and implements TCP initialize and deinitialize commands.
- Changes simulator result image output and public documentation from `.tiff` to `.bmp`.
- Updates installation documentation and OpenAPI metadata.

### [v2.0.3](https://github.com/vstechnology-tw/vst-virex.net.integration/releases/tag/v2.0.3) — 2026-06-28

- Republishes packages and simulator downloads from the latest main commit.
- Includes the ProductInfo contract updates in the published packages and simulator.

### [v2.0.2](https://github.com/vstechnology-tw/vst-virex.net.integration/releases/tag/v2.0.2) — 2026-06-27

- Replaces the public `WaferInfo` contract with `ProductInfo`.
- Adds the ProductInfo-based lifecycle behavior to the simulator.
- Updates RESTful API, TCP, MQTT, samples, and multilingual documentation.

### [v2.0.1](https://github.com/vstechnology-tw/vst-virex.net.integration/releases/tag/v2.0.1) — 2026-06-23

- Publishes the initial 2.0 public integration contract and simulator distributions.
- Migrates documentation to MkDocs Material with English, Traditional Chinese, Japanese, and Korean versions.
- Clarifies Result Summary versus REST result-query payloads.
- Updates result paths and payload fields, MQTT defaults, OpenAPI/Scalar validation, and TCP framing behavior.

## Historical tags without a GitHub Release page

`v1.0.0` and `v1.0.1` exist as repository tags, but GitHub has no published Release pages or release-note records for them. The first published GitHub Release is `v2.0.1`.