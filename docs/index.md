# Virex.NET Integration Suite

The Virex.NET Integration Kit provides the public contracts, simulator, samples, and documentation required to integrate external systems with Virex.NET compatible services. This repository contains only externally visible integration contracts and does not contain private production implementation details.

The simulator and the production Virex.NET endpoint should implement the same `Virex.NET.Contracts`. If an external vendor completes integration against `Virex.NET.Simulator.WPF`, switching to the production endpoint should only require changing endpoint settings.

## Communication interfaces

Virex.NET exposes the same business integration surface through three communication interfaces. Choose one interface as the primary integration path for your system, then implement the same command, event, and payload behavior described in the linked protocol page.

| Interface | Default simulator endpoint | Use when | Reference |
| --- | --- | --- | --- |
| RESTful API | `http://127.0.0.1:5088` | Your system prefers request/response HTTP calls and can poll for state or results. | [RESTful API](rest-api.md) |
| TCP Socket | `127.0.0.1:5089` | Your system needs bidirectional NDJSON over a persistent socket. | [TCP Socket Protocol](tcp-socket.md) |
| MQTT | `127.0.0.1:1883`, root topic `virex` | Your system already uses broker-based command, response, and event messaging. | [MQTT Protocol](mqtt-events.md) |

## Shared business functions

RESTful API, TCP Socket, and MQTT expose the same public business functions. The transport names differ, but the lifecycle rules, state restrictions, command responses, event payloads, and result payloads are aligned. Customers should select one communication interface and implement the full flow there instead of mixing transports unless there is a clear operational reason.

### Commands and queries

| Business function | RESTful API | TCP Socket | MQTT |
| --- | --- | --- | --- |
| Query status | `GET /api/status` | `{"type":"status"}` | `virex/commands/status/get` |
| Query error | `GET /api/error` | `{"type":"error"}` | `virex/commands/error/get` |
| Query ProductInfo | `GET /api/product-info` | `{"type":"getProductInfo"}` | `virex/commands/product-info/get` |
| Initialize | `POST /api/system/initialize` | `{"type":"initialize"}` | `virex/commands/system/initialize` |
| Set ProductInfo | `POST /api/product-info` | `{"type":"productInfo", ...}` | `virex/commands/product-info/set` |
| Start run | `POST /api/system/start` | `{"type":"start", ...}` | `virex/commands/system/start` |
| Stop run | `POST /api/system/stop` | `{"type":"stop", ...}` | `virex/commands/system/stop` |
| Query results | `GET /api/results` | `{"type":"results", ...}` | `virex/commands/results/query` |
| Deinitialize | `POST /api/system/deinitialize` | `{"type":"deinitialize"}` | `virex/commands/system/deinitialize` |

### Events

| Event | Meaning |
| --- | --- |
| `statusChanged` | Public lifecycle state changed. |
| `productInfoChanged` | ProductInfo update completed. |
| `runStarted` | A run entered `Running`. |
| `runCompleted` | A run returned to `Ready`. |
| `resultCreated` | A public result summary was created. |
| `errorChanged` | Public error information changed. |
| `commandRejected` | A command was rejected by state rules or validation. |

RESTful API represents commands as HTTP routes and does not provide an event stream; clients can observe state and results by polling. TCP Socket and MQTT provide the same events as pushed messages.

## Basic packet formats

All three communication interfaces use UTF-8 JSON payloads with the same public data models. Start with these references:

| Format area | Reference |
| --- | --- |
| JSON model rules and shared payloads | [Payload Reference](payloads.md) |
| RESTful API routes, HTTP bodies, and response codes | [RESTful API](rest-api.md) |
| TCP NDJSON frame format and frame types | [TCP Socket Protocol](tcp-socket.md) |
| MQTT topics, command request envelopes, and correlated responses | [MQTT Protocol](mqtt-events.md) |

## Project purpose

| Area | Purpose |
| --- | --- |
| `Virex.NET.Contracts` | Public data models, RESTful API routes, MQTT topic names, and TCP/NDJSON formatting and parsing tools. |
| `Virex.NET.Client` | C# SDK wrapper for RESTful API commands/queries, TCP socket communication, and MQTT command/event communication. |
| `Virex.NET.Simulator.Core` | Simulator-specific state machine and session behavior used by the local simulator. |
| `Virex.NET.Simulator.WPF` | A local simulator that provides public RESTful API/TCP/MQTT contracts. |
| `samples` | C#, Python, C++ integration examples. |
| `docs` | Public protocol and simulator documentation. |

## Quick start

1. Build and test:

   ```powershell
   dotnet test Virex.NET.Integration.slnx
   ```

2. Start the simulator:

   ```powershell
   dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
   ```

3. Press **Start Servers** in the simulator to start the RESTful API/TCP/MQTT services.

4. Open another terminal to execute the SDK example:

   ```powershell
   dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj
   ```

The examples follow the same 13-step demo flow: query status, query error, query ProductInfo, initialize, confirm Ready, set ProductInfo, confirm ProductInfo, start run, observe run activity, stop run, query results, deinitialize, and confirm Uninitialized.

## Main public data models

| Data Model | Usage |
| --- | --- |
| [ProductInfo](payloads/product/product-info.md) | Product information associated with a run and its result. |
| [SystemStatus](payloads/system/system-status.md) | Current lifecycle state. |
| [CommandResponse](payloads/commands/command-response.md) | Acceptance or rejection result for a command. |
| [ResultSummary](payloads/results/result-summary.md) | Public summary of a completed run. |
| [ErrorInfo](payloads/system/error-info.md) | Currently active error information. |

These are JSON data models. Vendors can use the C# types in `Virex.NET.Contracts` or define equivalent models in their own language.

## Reference documents

- [Install/Download](installation.md)
- [Simulator Guide](simulator.md)
- [System State Machine](state-machine.md)
- [Payload Reference](payloads.md)
- [RESTful API](rest-api.md)
- [TCP Socket Protocol](tcp-socket.md)
- [MQTT Protocol](mqtt-events.md)
- [Samples](samples.md)
