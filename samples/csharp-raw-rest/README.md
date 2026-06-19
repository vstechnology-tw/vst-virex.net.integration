# C# Raw REST Sample

Minimal C# client for the public Virex.NET REST API. It uses `HttpClient` directly instead of the SDK wrapper.

## Prerequisites

- .NET SDK.
- The Virex.NET simulator running with REST enabled at `http://127.0.0.1:5088`.

## Run

From the repository root, start the simulator:

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
```

In the simulator window, click **Start Servers**. Then open a second terminal from the repository root and run:

```powershell
dotnet run --project samples\csharp-raw-rest\CSharpRawRestSample.csproj
```

Optional base URL:

```powershell
dotnet run --project samples\csharp-raw-rest\CSharpRawRestSample.csproj -- http://127.0.0.1:5088
```

Expected result:

```text
Status: initialized=False, processState=ready, recipe=Default
WaferInfo updated through raw REST.
```
