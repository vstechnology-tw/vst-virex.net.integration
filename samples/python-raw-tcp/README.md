# Python Raw TCP Sample

Demonstrates TCP/NDJSON frames for the current public protocol.

Run the simulator first and press **Start Servers**.

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
python samples\python-raw-tcp\main.py
```

The sample connects to `127.0.0.1:5089`, reads initial event frames, sends query frames, sends a `productInfo` frame, sends `start` / `stop` frames, and queries results.

Expected events include `statusChanged`, `productInfoChanged`, `runStarted`, `runCompleted`, and `resultCreated`.
