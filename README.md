# Virex.NET Integration Kit

Public integration kit for customers who need to connect to Virex.NET-compatible systems.

This repository contains:

- `Virex.NET.Contracts`: public DTOs, routes, topic names, TCP/NDJSON formatters, and parsers.
- `Virex.NET.Client`: C# SDK for REST, TCP socket events, and MQTT events.
- `Virex.NET.Simulator.WPF`: WPF simulator that behaves like the Virex.NET communication surface.
- `samples`: small client examples using the C# SDK or raw REST/TCP calls from C#, Python, and C++.
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

Current MQTT note: until the embedded MQTT broker is added, **Start Servers** also attempts to connect to an MQTT broker at `127.0.0.1:1883`. Start a local broker first if you want the simulator startup to complete without an MQTT connection error.

## Run Samples

1. Start the simulator:

   ```powershell
   dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
   ```

2. In the simulator window, keep the default endpoints and click **Start Servers**.

   If the simulator reports an MQTT broker connection error, start a local broker on `127.0.0.1:1883` and click **Start Servers** again. This requirement will be removed when the embedded MQTT broker is added.

3. Open a second terminal from the repository root and run one sample:

   ```powershell
   dotnet run --project samples\csharp-raw-rest\CSharpRawRestSample.csproj
   dotnet run --project samples\csharp-raw-tcp\CSharpRawTcpSample.csproj
   dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj
   python samples\python-raw-rest\main.py
   python samples\python-raw-tcp\main.py
   ```

4. For C++ samples, build first from a Visual Studio Developer PowerShell:

   ```powershell
   cmake -S samples\cpp-raw-rest -B samples\cpp-raw-rest\build
   cmake --build samples\cpp-raw-rest\build --config Release
   samples\cpp-raw-rest\build\Release\cpp-raw-rest.exe

   cmake -S samples\cpp-raw-tcp -B samples\cpp-raw-tcp\build
   cmake --build samples\cpp-raw-tcp\build --config Release
   samples\cpp-raw-tcp\build\Release\cpp-raw-tcp.exe
   ```

The raw REST samples read `/api/status` and then update `/api/wafer-info`. The raw TCP samples connect to port `5089`, read the initial status and wafer info frames, send a `waferInfo` NDJSON frame, and print the update event returned by the simulator.

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

More examples are under `samples/`:

- `csharp-sdk`
- `csharp-raw-rest`
- `csharp-raw-tcp`
- `python-raw-rest`
- `python-raw-tcp`
- `cpp-raw-rest`
- `cpp-raw-tcp`

## Customer Documentation Website

Customer-facing SDK usage, sample code guidance, simulator workflow, and protocol references are available at:

https://vstechnology-tw.github.io/vst-virex.net.integration/

## License

This repository is licensed under the MIT License. See [LICENSE](LICENSE).
