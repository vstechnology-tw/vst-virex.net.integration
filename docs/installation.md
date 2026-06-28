# Installation / Download

This page explains how to obtain the public Virex.NET integration tools:

- Download the Windows simulator from GitHub Releases.
- Install the C# SDK package from NuGet.
- Select the next step in the verification process.

## GitHub Releases: Pre-built simulator EXEs

The current simulator release is `v2.0.3`:

[Download Virex.NET Simulator v2.0.3](https://github.com/vstechnology-tw/vst-virex.net.integration/releases/tag/v2.0.3)

Each ZIP contains `Virex.NET.Simulator.WPF.exe`. Please select the package according to the target environment:

| File | Runtime | When To Use |
| --- | --- | --- |
| `Virex.NET.Simulator-v2.0.3-net48-win-x64.zip` | .NET Framework 4.8 | Use when the target Windows machine already has .NET Framework 4.8 installed. |
| `Virex.NET.Simulator-v2.0.3-net8.0-windows-win-x64-self-contained.zip` | .NET 8 Windows | Use when you need a current Windows build that includes the runtime. |
| `Virex.NET.Simulator-v2.0.3-net10.0-windows-win-x64-self-contained.zip` | .NET 10 Windows | Use when you need to validate the latest supported Windows target. |

After downloading:

1. Unzip the ZIP.
2. Execute `Virex.NET.Simulator.WPF.exe`.
3. Keep the default endpoints unless your test environment requires a different port.
4. Before executing the SDK or original protocol example, press **Start Servers**.

For simulator buttons, default endpoints, and operating procedures, see [Simulator Guide](simulator.md).

## NuGet Packages

| Package | Purpose |
| --- | --- |
| [Virex.NET.Contracts](https://www.nuget.org/packages/Virex.NET.Contracts/) | Provides public C# data models, REST routes, MQTT topic names, and TCP/NDJSON formatting and parsing tools. |
| [Virex.NET.Client](https://www.nuget.org/packages/Virex.NET.Client/) | C# SDK wrapper for REST, TCP, and MQTT. |

Install SDK:

```powershell
dotnet add package Virex.NET.Client --version 2.0.3
```

Install only the shared contract package:

```powershell
dotnet add package Virex.NET.Contracts --version 2.0.3
```

`Virex.NET.Client` and `Virex.NET.Contracts` target `netstandard2.0`.

## Next Steps

- First integration verification flow: see [Quick Start](quick-start.md).
- Complete C# SDK usage: see [C# SDK Guide](csharp-sdk.md).
- Other languages or raw protocol examples: see [Samples](samples.md).
