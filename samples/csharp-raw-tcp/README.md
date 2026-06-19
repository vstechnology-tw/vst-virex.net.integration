# C# Raw TCP Sample

Minimal C# client for the public Virex.NET TCP/NDJSON protocol. It uses `TcpClient` directly instead of the SDK wrapper.

## Prerequisites

- .NET SDK.
- The Virex.NET simulator running with TCP enabled on `127.0.0.1:5089`.

## Run

From the repository root, start the simulator:

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
```

In the simulator window, click **Start Servers**. Then open a second terminal from the repository root and run:

```powershell
dotnet run --project samples\csharp-raw-tcp\CSharpRawTcpSample.csproj
```

Optional host and port:

```powershell
dotnet run --project samples\csharp-raw-tcp\CSharpRawTcpSample.csproj -- 127.0.0.1 5089
```

Expected result:

```text
{"type":"status",...}
{"type":"waferInfo",...}
Sent waferInfo frame. Waiting for echo/update event...
{"type":"waferInfo",...}
```
