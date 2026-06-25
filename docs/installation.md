# Installation / Download

Use this page to choose how to install the public Virex.NET integration tools:

- Download a prebuilt Windows simulator from GitHub Releases.
- Install the C# SDK packages from NuGet.
- Run sample code from this repository.

## GitHub Releases: Prebuilt Simulator EXE

The simulator is published as ZIP assets on the repository release page:

[Download the latest simulator release](https://github.com/vstechnology-tw/vst-virex.net.integration/releases/latest)

Each ZIP contains `Virex.NET.Simulator.WPF.exe`. Download the package that matches the target runtime:

| Asset | Runtime | When to use |
| --- | --- | --- |
| `Virex.NET.Simulator-vX.Y.Z-net48-win-x64.zip` | .NET Framework 4.8 | Use on Windows systems that already have .NET Framework 4.8 installed. |
| `Virex.NET.Simulator-vX.Y.Z-net8.0-windows-win-x64-self-contained.zip` | .NET 8 Windows | Use when you want the current Windows build with its runtime bundled. |
| `Virex.NET.Simulator-vX.Y.Z-net10.0-windows-win-x64-self-contained.zip` | .NET 10 Windows | Use when validating against the newest supported Windows target. |

After download:

1. Extract the ZIP file.
2. Run `Virex.NET.Simulator.WPF.exe`.
3. Keep the default endpoints unless your test environment requires different ports.
4. Press **Start Servers** before running SDK or raw protocol samples.

Default endpoints:

| Interface | Default |
| --- | --- |
| REST | `http://127.0.0.1:5088` |
| TCP | `127.0.0.1:5089` |
| MQTT | `127.0.0.1:1883`, base topic `virex` |

For button-by-button simulator behavior, see [Simulator Manual](simulator.md).

## NuGet Packages

The C# integration packages are published on NuGet:

| Package | Purpose |
| --- | --- |
| [Virex.NET.Contracts](https://www.nuget.org/packages/Virex.NET.Contracts/) | Public DTOs, REST routes, MQTT topic names, TCP/NDJSON formatters, and parsers. |
| [Virex.NET.Client](https://www.nuget.org/packages/Virex.NET.Client/) | C# SDK wrappers for REST commands and queries, TCP events, and MQTT events. |

Install the SDK package in a .NET project:

```powershell
dotnet add package Virex.NET.Client
```

Install only the shared contracts when you need public payload types and protocol helpers without the SDK wrapper:

```powershell
dotnet add package Virex.NET.Contracts
```

`Virex.NET.Client` and `Virex.NET.Contracts` target `netstandard2.0`, so they can be referenced from .NET Framework and modern .NET applications that support `netstandard2.0`.

## Sample Code

Samples are included in the repository under `samples/`:

[Browse sample code on GitHub](https://github.com/vstechnology-tw/vst-virex.net.integration/tree/main/samples)

Recommended first run:

```powershell
dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj
```

Other available samples:

| Sample | Command |
| --- | --- |
| C# raw REST | `dotnet run --project samples\csharp-raw-rest\CSharpRawRestSample.csproj` |
| C# raw TCP | `dotnet run --project samples\csharp-raw-tcp\CSharpRawTcpSample.csproj` |
| C# raw MQTT | `dotnet run --project samples\csharp-raw-mqtt\CSharpRawMqttSample.csproj` |
| Python raw REST | `python samples\python-raw-rest\main.py` |
| Python raw TCP | `python samples\python-raw-tcp\main.py` |
| Python raw MQTT | `python samples\python-raw-mqtt\main.py` |

For the full guided sample workflow, see [Samples](samples.md).

## Minimal C# SDK Example

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

await client.InitializeAsync();
await client.StartAsync();
```
