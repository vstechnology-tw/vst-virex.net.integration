# Virex.NET Integration Kit

Public integration kit for customers who need to connect to Virex.NET-compatible systems.

This repository contains:

- `Virex.NET.Contracts`: public DTOs, routes, topic names, TCP/NDJSON formatters, and parsers.
- `Virex.NET.Client`: C# SDK for REST, TCP socket events, and MQTT events.
- `Virex.NET.Simulator.WPF`: WPF simulator that behaves like the Virex.NET communication surface.
- `samples`: small client examples using the SDK or raw REST/TCP calls.
- `docs`: protocol documentation for non-C# clients.

The private Virex.NET application is not included. This repository only contains public communication contracts and integration tooling.

## Build

```powershell
dotnet build Virex.NET.Integration.slnx
dotnet test Virex.NET.Integration.slnx
```

The simulator multi-targets `.NET Framework 4.8`, `.NET 8 Windows`, and `.NET 10 Windows`. The Contracts and Client packages target `netstandard2.0`.

## Run Simulator

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
```

Default endpoints:

```text
REST: http://127.0.0.1:5088
TCP:  5089
MQTT: 127.0.0.1:1883, base topic Virex.NET
```

The simulator lets you configure IP/port settings, update WaferInfo, run simulated state transitions, emit results, and emit errors.

## C# SDK Example

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
    MqttTopic = "Virex.NET",
});

await client.SetWaferInfoAsync(new WaferInfo
{
    LotId = "LOT-001",
    WaferId = "W01",
    RecipeId = "RCP-A",
    Slot = "1",
    FoupId = "FOUP-A",
    ChamberId = "CH-1",
});

await client.StartAsync();
```

More examples are under `samples/`.

## Customer Documentation Website

The customer-facing SDK and protocol documentation is available as a static GitHub Pages site from the `docs` folder.

To publish it after merging documentation changes to `main`, configure the repository in GitHub:

```text
Settings > Pages > Build and deployment > Deploy from a branch
Branch: main
Folder: /docs
```

The published URL will use the standard project Pages format:

```text
https://<owner>.github.io/<repository>/
```

## License

This repository is licensed under the MIT License. See [LICENSE](LICENSE).
