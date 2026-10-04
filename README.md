# Virex.NET Integration Kit

[![Virex.NET.Contracts](https://img.shields.io/nuget/v/Virex.NET.Contracts?label=Virex.NET.Contracts)](https://www.nuget.org/packages/Virex.NET.Contracts)
[![Virex.NET.Client](https://img.shields.io/nuget/v/Virex.NET.Client?label=Virex.NET.Client)](https://www.nuget.org/packages/Virex.NET.Client)

This repository contains the public Virex.NET integration contract, C# client SDK, local simulator, samples, and documentation. It does not include private Virex.NET production internals.

The simulator and production-compatible services are expected to expose the same public contract. A vendor integration that works with `Virex.NET.Simulator.WPF` should be able to target a production Virex.NET endpoint by changing endpoint settings.

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
POST /api/system/start
POST /api/system/stop
GET  /api/results
GET  /api/results/{resultId}
```

## Current Public Event Names

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
- [RESTful API](docs/rest-api.md)
- [System State Machine](docs/state-machine.md)
- [Command Completion and Errors (2.2.3)](docs/communication-errors.md)
- [Recipe and Result Queries](docs/read-only-queries.md)
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
