# C# SDK Sample

Minimal C# client that uses `Virex.NET.Client` instead of raw protocol calls.

## Prerequisites

- .NET SDK.
- The Virex.NET simulator running with REST enabled at `http://127.0.0.1:5088`.
- Until the embedded MQTT broker is added, the simulator also expects a local MQTT broker on `127.0.0.1:1883` when **Start Servers** is clicked.

## Run

From the repository root, start the simulator:

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
```

In the simulator window, click **Start Servers**. Then open a second terminal from the repository root and run:

```powershell
dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj
```

Optional REST base URL:

```powershell
dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj -- http://127.0.0.1:5088
```

Expected result:

```text
Reading status...
False ready recipe=Default
Updating wafer info...
Starting inspection cycle...
Querying latest results...
Result count: 0
```
