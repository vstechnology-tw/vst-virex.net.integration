# Virex.NET Integration Kit

[![Virex.NET.Contracts](https://img.shields.io/nuget/v/Virex.NET.Contracts?label=Virex.NET.Contracts)](https://www.nuget.org/packages/Virex.NET.Contracts/)
[![Virex.NET.Client](https://img.shields.io/nuget/v/Virex.NET.Client?label=Virex.NET.Client)](https://www.nuget.org/packages/Virex.NET.Client/)

Public integration kit for customers who need to connect to Virex.NET-compatible systems.

This repository contains:

- `Virex.NET.Contracts`: public DTOs, routes, topic names, TCP/NDJSON formatters, and parsers.
- `Virex.NET.Client`: C# SDK for REST, TCP socket events, and MQTT events.
- `Virex.NET.Simulator.WPF`: WPF simulator that behaves like the Virex.NET communication surface.
- `samples`: small client examples using the C# SDK or raw REST/TCP/MQTT calls from C#, Python, and C++.
- `docs`: protocol documentation for non-C# clients.

The private Virex.NET application is not included. This repository only contains public communication contracts and integration tooling.

## NuGet Packages

- [Virex.NET.Contracts](https://www.nuget.org/packages/Virex.NET.Contracts/): public DTOs, routes, topic names, TCP/NDJSON formatters, and parsers.
- [Virex.NET.Client](https://www.nuget.org/packages/Virex.NET.Client/): C# SDK for REST, TCP socket events, and MQTT events.

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

The simulator starts an embedded MQTT broker when you click **Start Servers**, so no external broker is required for local testing.

## Guided Demo Flow

The simulator UI controls the equipment-like state used by every sample. Samples are guided demos: they tell the tester which simulator button to press, what condition is being tested, and what return value or event is expected.

Use this baseline flow:

1. Start the simulator.
2. Confirm the default endpoints:
   - REST: `http://127.0.0.1:5088/`
   - TCP: `5089`
   - MQTT: `127.0.0.1:1883`
   - Topic: `Virex.NET`
3. Press **Start Servers** before running any sample.
4. For SDK and REST samples, leave **Initialize** unpressed first. The sample intentionally calls start and expects `HTTP 409 not_initialized`.
5. When prompted, press **Initialize** and confirm `initialized=True, processState=ready`.
6. Continue the sample. It will send WaferInfo, start a cycle, and query results where applicable.
7. For MQTT samples, keep the sample running and press **Apply WaferInfo**, **Initialize**, **Start Cycle**, **Emit Fake Result**, and **Emit Error** to observe events.

Expected Event Log examples:

```text
WaferInfo updated from REST: lotId=LOT-RAW-REST-001, waferId=W01, recipeId=RCP-A, slot=1, foupId=FOUP-A, chamberId=CH-1
WaferInfo updated from TCP: lotId=LOT-RAW-TCP-001, waferId=W01, recipeId=RCP-A, slot=1, foupId=FOUP-A, chamberId=CH-1
```

Detailed UI SOPs are in `docs/simulator.html`.

## Run Samples

1. Start the simulator:

   ```powershell
   dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
   ```

2. In the simulator window, keep the default endpoints and click **Start Servers**.

3. Open a second terminal from the repository root and run one sample. Follow the console prompts; they tell you when to press **Initialize**, **Apply WaferInfo**, **Start Cycle**, **Emit Fake Result**, or **Emit Error**:

   ```powershell
   dotnet run --project samples\csharp-raw-rest\CSharpRawRestSample.csproj
   dotnet run --project samples\csharp-raw-tcp\CSharpRawTcpSample.csproj
   dotnet run --project samples\csharp-raw-mqtt\CSharpRawMqttSample.csproj
   dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj
   python samples\python-raw-rest\main.py
   python samples\python-raw-tcp\main.py
   python samples\python-raw-mqtt\main.py
   ```

4. For C++ samples, build first from a Visual Studio Developer PowerShell:

   ```powershell
   cmake -S samples\cpp-raw-rest -B samples\cpp-raw-rest\build
   cmake --build samples\cpp-raw-rest\build --config Release
   samples\cpp-raw-rest\build\Release\cpp-raw-rest.exe

   cmake -S samples\cpp-raw-tcp -B samples\cpp-raw-tcp\build
   cmake --build samples\cpp-raw-tcp\build --config Release
   samples\cpp-raw-tcp\build\Release\cpp-raw-tcp.exe

   cmake -S samples\cpp-raw-mqtt -B samples\cpp-raw-mqtt\build
   cmake --build samples\cpp-raw-mqtt\build --config Release
   samples\cpp-raw-mqtt\build\Release\cpp-raw-mqtt.exe
   ```

The SDK and raw REST samples demonstrate the expected `not_initialized` response before **Initialize**, then continue through WaferInfo, cycle start, and result query after the tester presses **Initialize**. The raw TCP samples connect to port `5089`, read the initial status and wafer info frames, send a `waferInfo` NDJSON frame, and print the update event returned by the simulator. The raw MQTT samples subscribe to `Virex.NET/#`; while they are running, trigger events from the simulator UI with **Apply WaferInfo**, **Initialize**, **Start Cycle**, **Emit Fake Result**, or **Emit Error**.

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
- `csharp-raw-mqtt`
- `python-raw-rest`
- `python-raw-tcp`
- `python-raw-mqtt`
- `cpp-raw-rest`
- `cpp-raw-tcp`
- `cpp-raw-mqtt`

## Customer Documentation Website

Customer-facing SDK usage, sample code guidance, simulator workflow, and protocol references are available at:

https://vstechnology-tw.github.io/vst-virex.net.integration/

## License

This repository is licensed under the MIT License. See [LICENSE](LICENSE).
