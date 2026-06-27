# Virex.NET Integration Kit

This repository contains the public Virex.NET integration contract, C# client SDK, local simulator, samples, and documentation. It does not include private Virex.NET production internals.

The simulator and production-compatible services are expected to expose the same public contract. A vendor integration that works with `Virex.NET.Simulator.WPF` should be able to target a production Virex.NET endpoint by changing endpoint settings.

## Projects

| Project | Purpose |
| --- | --- |
| `Virex.NET.Contracts` | Public payload schemas as C# models, REST routes, MQTT topic names, and TCP/NDJSON helpers. |
| `Virex.NET.Client` | C# SDK wrappers for REST, TCP, and MQTT integration. |
| `Virex.NET.Simulator.Core` | Simulator-specific state/session behavior. |
| `Virex.NET.Simulator.WPF` | Local Windows simulator exposing REST, TCP, and MQTT endpoints. |
| `samples` | C#, Python, and C++ integration examples. |
| `docs` | Public protocol and simulator documentation. |

## Quick Start

```powershell
dotnet test Virex.NET.Integration.slnx
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
```

In the simulator, press **Start Servers**, then run:

```powershell
dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj
```

Default simulator endpoints:

| Interface | Default |
| --- | --- |
| REST | `http://127.0.0.1:5088` |
| TCP | `127.0.0.1:5089` |
| MQTT | `127.0.0.1:1883`, base topic `virex` |

## Current Public REST Surface

```text
GET  /api/status
GET  /api/product-info
POST /api/product-info
POST /api/system/initialize
POST /api/system/deinitialize
POST /api/system/start
POST /api/system/stop
GET  /api/results
```

## Current Public Event Names

```text
statusChanged
productInfoChanged
runStarted
runCompleted
resultCreated
errorChanged
commandRejected
```

## Documentation

Start here:

- [Documentation Index](docs/index.md)
- [REST API](docs/rest-api.md)
- [System State Machine](docs/state-machine.md)
- [Payload Reference](docs/payloads.md)
- [TCP Socket Protocol](docs/tcp-socket.md)
- [MQTT Events](docs/mqtt-events.md)
- [Samples](docs/samples.md)

## Verification

Before claiming changes are complete, run:

```powershell
dotnet test Virex.NET.Integration.slnx
```
