# C# Raw TCP Sample

Demonstrates TCP/NDJSON frames for the current public protocol.

Run the simulator first and press **Start Servers**.

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
dotnet run --project samples\csharp-raw-tcp\CSharpRawTcpSample.csproj
```

The sample connects to `127.0.0.1:5089`, reads initial event frames, sends a `productInfo` frame, and sends `start` / `stop` frames.

Expected events include:

- `statusChanged`
- `productInfoChanged`
- `runStarted`
- `runCompleted`
- `resultCreated`
