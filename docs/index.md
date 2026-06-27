# Virex.NET Integration Suite

The Virex.NET Integration Kit provides the public contracts, C# SDK, simulator, samples, and documentation required to integrate external systems with Virex.NET compatible services. This repository does not contain private implementation details of the production Virex.NET product.

The simulator and the production Virex.NET endpoint should implement the same `Virex.NET.Contracts`. If an external vendor completes integration against `Virex.NET.Simulator.WPF`, switching to the production endpoint should only require changing REST/TCP/MQTT endpoint settings.

## Project purpose

| Area | Purpose |
| --- | --- |
| `Virex.NET.Contracts` | Public data models, REST routes, MQTT topic names, and TCP/NDJSON formatting and parsing tools. |
| `Virex.NET.Client` | C# SDK wrapper for REST commands/queries, TCP events, and MQTT events. |
| `Virex.NET.Simulator.Core` | Simulator-specific state machine and session behavior used by the local simulator. |
| `Virex.NET.Simulator.WPF` | A local simulator that provides public REST/TCP/MQTT contracts. |
| `samples` | C#, Python, C++ integration examples. |
| `docs` | Public protocol and simulator documentation. |

## Default Simulator Endpoints

| Interface | Default |
| --- | --- |
| REST | `http://127.0.0.1:5088` |
| TCP | `127.0.0.1:5089` |
| MQTT | `127.0.0.1:1883`, root topic `virex` |

## Quick start

1. Build and test:

   ```powershell
   dotnet test Virex.NET.Integration.sln
   ```

2. Start the simulator:

   ```powershell
   dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
   ```

3. Press **Start Servers** in the simulator to start the REST/TCP/MQTT service.

4. Open another terminal to execute the SDK example:

   ```powershell
   dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj
   ```

The example initializes the system, updates ProductInfo, starts a run, waits for the run to complete, and queries results.

## Main SDK methods

- `GetStatusAsync`
- `GetProductInfoAsync`
- `SetProductInfoAsync`
- `InitializeAsync`
- `DeinitializeAsync`
- `StartAsync`
- `StopAsync`
- `QueryResultsAsync`

## Main Public Data Models

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
- [REST API](rest-api.md)
- [TCP Socket Protocol](tcp-socket.md)
- [MQTT Events](mqtt-events.md)
- [Samples](samples.md)
