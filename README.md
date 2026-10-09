# Virex.NET Integration Kit

[![Virex.NET.Contracts](https://img.shields.io/nuget/v/Virex.NET.Contracts?label=Virex.NET.Contracts)](https://www.nuget.org/packages/Virex.NET.Contracts)
[![Virex.NET.Client](https://img.shields.io/nuget/v/Virex.NET.Client?label=Virex.NET.Client)](https://www.nuget.org/packages/Virex.NET.Client)

This repository contains the public Virex.NET integration contract, C# client SDK, local simulator, samples, and documentation. It does not include private Virex.NET production internals.

The current source targets an unpublished 2.4.0 candidate: REST commands/queries, exact-name TCP event subscriptions, live/explicit result sources, and two-stage Alarm recovery. See [candidate acceptance](docs/candidate-acceptance.md) and the [ordinary NuGet consumer sample](samples/csharp-sdk-24/README.md). Published package badges describe the public feed, not this local candidate. Simulator acceptance requires separate real App and equipment acceptance.

## Projects

| Project | Purpose |
| --- | --- |
| `Virex.NET.Contracts` | Public payload schemas as C# models, RESTful API routes, MQTT topic names, and TCP/NDJSON helpers. |
| `Virex.NET.Client` | C# SDK wrappers for RESTful API, TCP, and MQTT integration. |
| `Virex.NET.Simulator.WPF` | Local Windows simulator exposing RESTful API, TCP, and MQTT endpoints. |
| `samples` | C#, Python, and C++ integration examples. |
| `docs` | Public protocol and simulator documentation. |

## Quick Start

```powershell
dotnet test Virex.NET.Integration.slnx
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj --framework net10.0-windows
```

In the simulator, press **Start Servers**, then run:

```powershell
dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj
```

Default simulator endpoints:

| Interface | Default |
| --- | --- |
| RESTful API | `http://127.0.0.1:5088` |
| TCP | `127.0.0.1:5089` |
| MQTT | `127.0.0.1:1883`, base topic `virex` |

## Current Public RESTful API Surface

```text
GET  /api/status
GET  /api/events
GET  /api/alarms
GET  /api/data-sources
GET  /api/data-sources/value?name={name}&resultId={explicitResultId}
GET  /api/data-sources/current-result
GET  /api/error
GET  /api/product-info
POST /api/product-info
GET  /api/operation-mode
POST /api/operation-mode
GET  /api/recipes
GET  /api/recipes/current
GET  /api/recipes/current/parameters
POST /api/system/initialize
POST /api/system/deinitialize
POST /api/system/reset
POST /api/system/start
POST /api/system/stop
GET  /api/results
GET  /api/results/{resultId}
```

## Built-in Event Name Examples

Discover the actual producer names with `GET /api/events`. Subscribe to multiple exact case-sensitive names with TCP/NDJSON. This list is not an SDK whitelist. Alarm event names are discovered with `GET /api/alarms`.

```text
statusChanged
productInfoChanged
operationModeChanged
captureReady
captureCompleted
imageGrabbed
runStarted
runCompleted
resultCreated
errorChanged
commandRejected
```

## Documentation

Start here:

- [Documentation Index](docs/index.md)
- [2.4.0 Candidate Acceptance](docs/candidate-acceptance.md)
- [Exact-name Event Subscriptions](docs/event-subscriptions.md)
- [Alarm Sources and Two-stage Recovery](docs/alarms.md)
- [RESTful API](docs/rest-api.md)
- [System State Machine](docs/state-machine.md)
- [Command Completion and Errors (2.2.3)](docs/communication-errors.md)
- [Recipe and Result Queries](docs/read-only-queries.md)
- [Live and Result Data Sources](docs/data-sources.md)
- [Per-start Inspection Mode](docs/inspection-mode.md)
- [Local and Remote Operation Mode](docs/operation-mode.md)
- [Capture Lifecycle](docs/capture-lifecycle.md)
- [Payload Reference](docs/payloads.md)
- [TCP Socket Protocol](docs/tcp-socket.md)
- [MQTT Protocol](docs/mqtt-events.md)
- [Samples](docs/samples.md)

## Verification

Before claiming changes are complete, run:

```powershell
dotnet test Virex.NET.Integration.slnx
```
