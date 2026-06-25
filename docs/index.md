# Virex.NET Integration Kit

Virex.NET Integration Kit is the public SDK, simulator, sample, and protocol documentation package for customer systems integrating with Virex.NET-compatible services. It documents only public communication contracts and simulator-verifiable behavior.

The private Virex.NET application is not included in this repository.

## Default Simulator Endpoints

| Interface | Default |
| --- | --- |
| REST | `http://127.0.0.1:5088` |
| TCP | `127.0.0.1:5089` |
| MQTT | `127.0.0.1:1883`, base topic `virex` |
| SDK | `Virex.NET.Client` |

## Product Purpose

Use this kit to build and validate customer-side integrations before connecting to a production-compatible service.

| Area | Purpose |
| --- | --- |
| `Virex.NET.Contracts` | Public DTOs, routes, topic names, TCP/NDJSON formatters, and parsers. |
| `Virex.NET.Client` | C# client wrappers for REST commands and queries, TCP events, and MQTT events. |
| `Virex.NET.Simulator.WPF` | Local simulator exposing REST, TCP, and MQTT endpoints. |
| `samples` | Guided demos for C#, Python, and C++ clients. |
| `docs` | Customer documentation and raw protocol references. |

## Quick Start

First-time users should run the simulator and the C# SDK sample. The sample guides the tester through **Start Servers**, the expected `not_initialized` response before **Initialize**, then the normal cycle after **Initialize**.

For prebuilt simulator downloads, NuGet packages, and sample code links, see [Installation / Download](installation.md).

1. Build and test the kit:

   ```powershell
   dotnet test Virex.NET.Integration.slnx
   ```

2. Start the simulator:

   ```powershell
   dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
   ```

3. In the simulator window, keep the default endpoints and press **Start Servers**.

4. Run the SDK sample in a second terminal:

   ```powershell
   dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj
   ```

5. Follow the sample prompts. It will first demonstrate `HTTP 409 not_initialized`, then ask you to press **Initialize** and continue through WaferInfo, cycle start, and result query.

Expected Event Log examples:

```text
WaferInfo updated from REST: lotId=LOT-RAW-REST-001, waferId=W01, recipeId=RCP-A, slot=1, foupId=FOUP-A, chamberId=CH-1
WaferInfo updated from TCP: lotId=LOT-RAW-TCP-001, waferId=W01, recipeId=RCP-A, slot=1, foupId=FOUP-A, chamberId=CH-1
```

For the full button-by-button workflow, see [Simulator Manual](simulator.md).

## SDK Usage

C# applications should start with `VirexClient`. It centralizes REST, TCP, and MQTT settings and exposes the common integration operations.

```csharp
using Virex.NET.Client;
using Virex.NET.Contracts;

using var client = new VirexClient(new VirexClientOptions
{
    RestBaseUrl = "http://127.0.0.1:5088",
    TcpHost = "127.0.0.1",
    TcpPort = 5089,
    MqttHost = "127.0.0.1",
    MqttPort = 1883,
    MqttTopic = "virex",
    TimeoutMs = 5000,
    TcpFrameTimeoutMs = 5000,
});

var status = await client.GetStatusAsync();

// REST command/query helpers are the default high-level SDK path.
await client.SetWaferInfoAsync(new WaferInfo
{
    LotId = "LOT-001",
    WaferId = "W01",
    RecipeId = "RCP-A",
    Slot = "1",
    FoupId = "FOUP-A",
    ChamberId = "CH-1",
});

await client.InitializeAsync();
await client.StartAsync();

var results = await client.QueryResultsAsync(lotId: "LOT-001");

// TCP is selected explicitly through TcpEvents.
await client.TcpEvents.SendWaferInfoAsync(new WaferInfo
{
    LotId = "LOT-TCP-001",
    WaferId = "W01",
    RecipeId = "RCP-A",
});
await client.TcpEvents.SendStartAsync();

// TCP and MQTT event listeners are started explicitly.
using var eventCts = new CancellationTokenSource();
client.TcpEvents.EventReceived += (_, value) =>
    Console.WriteLine("TCP event: " + value.Type);
client.MqttEvents.EventReceived += (_, value) =>
    Console.WriteLine("MQTT event: " + value.Type);

_ = client.TcpEvents.RunAsync(eventCts.Token);
_ = client.MqttEvents.RunAsync(eventCts.Token);

// MQTT is event-only; it is not used for command/control.
// Call eventCts.Cancel() when your application is done listening.
```

Main SDK methods:

- `GetStatusAsync`
- `SetWaferInfoAsync`
- `InitializeAsync`
- `TerminateAsync`
- `StartAsync`
- `StopAsync`
- `QueryResultsAsync`

Non-success REST responses throw `VirexClientException` with the HTTP status code and response body. In guided samples, `HTTP 409 not_initialized` before **Initialize** is expected simulator state, not a connection failure.

`TcpFrameTimeoutMs` protects TCP/NDJSON reads after partial frame data has arrived. A long idle gap between complete frames is allowed, but once the client receives the first byte of a frame, the rest of that frame must arrive and end with `\n` within the configured timeout.

## Interface Selection

| Interface | Best fit | Direction | Typical use |
| --- | --- | --- | --- |
| REST | Commands and queries | Client to service | Read status, send WaferInfo, control cycles, and query results. |
| TCP / NDJSON | Direct socket integration | Bidirectional | Send command frames and receive status, wafer-info, result, and error events. |
| MQTT | Event subscription | Outbound events only | Subscribe to status, wafer-info, result, and error through a broker. MQTT is not used for command/control. |

For exact JSON bodies, fields, topics, and frames, see [Transmitted Content / Payloads](payloads.md).

## Typical Use Cases

Each use case is verified by both simulator UI state and sample output. Confirm the condition first, press the named UI button, then compare console output with Event Log.

| Use case | Required state | UI action | Sample | Expected output |
| --- | --- | --- | --- | --- |
| Start communication services | Simulator is open | Press **Start Servers** | Any sample | Event Log shows REST/TCP/MQTT listening; samples can connect. |
| `not_initialized` | **Start Servers** pressed, **Initialize** not pressed | Do not press **Initialize** yet | C# SDK, raw REST | Start returns HTTP `409` / `not_initialized`. |
| Normal cycle | `processState=ready` | Press **Initialize**, then continue the sample | C# SDK, raw REST | State transitions through `capturing`, `inspecting`, `saving`, and `ready`; a result is created. |
| WaferInfo update verification | **Start Servers** pressed | Press **Apply WaferInfo** or send from sample | SDK, REST, TCP | Event Log lists `lotId`, `waferId`, `recipeId`, `slot`, `foupId`, and `chamberId` in one line. |
| MQTT event observation | MQTT sample subscribed to `virex/#` | Press **Apply WaferInfo**, **Initialize**, **Start Cycle**, **Emit Fake Result**, **Emit Error** | Raw MQTT | Console shows `wafer-info`, `status`, `result`, and `error` topics. |
| Result query | Cycle completed or **Emit Fake Result** pressed | Run result query from SDK/REST sample | SDK, REST | Console prints result count filtered by matching wafer context. |

## Main Data Contracts

| Contract | Public fields |
| --- | --- |
| WaferInfo | `lotId`, `waferId`, `recipeId`, `slot`, `foupId`, `chamberId` |
| Status | `initialized`, `processState`, `recipe` |
| Result summary | `resultId`, `timestamp`, wafer context, `overallResult`, total `defectCount`, result/image paths |
| Error | `hasError`, `message`, `initialized`, `processState`, `recipe` |

Result responses and result events provide summaries only. They do not include defect lists, die lists, crop lists, image binaries, or private inspection details.

## Troubleshooting

| Symptom | Check |
| --- | --- |
| REST request fails | Confirm the simulator is running, `RestBaseUrl` is correct, firewall access is allowed, and inspect `VirexClientException` response body. |
| TCP events are missing | Confirm host/port, ensure each NDJSON frame ends with a newline, and keep the event loop running. |
| MQTT events are missing | Confirm broker connectivity, port `1883`, base topic `virex`, and subscription topic tree. |
| Result query is empty | Confirm the simulator has emitted a result and that `lotId`, `waferId`, and `recipeId` filters match the submitted WaferInfo. |

## References

- [Installation / Download](installation.md)
- [Simulator Manual](simulator.md)
- [State Machine](state-machine.md)
- [Transmitted Content / Payloads](payloads.md)
- [Samples](samples.md)
- [REST API](rest-api.md)
- [TCP Socket Protocol](tcp-socket.md)
- [MQTT Events](mqtt-events.md)
- [Protocol Versioning](protocol-versioning.md)
