# Python Raw REST Sample

Demonstrates the current REST API using Python standard library HTTP support.

Run the simulator first and press **Start Servers**.

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
python samples\python-raw-rest\main.py
```

The sample calls status, initialize, ProductInfo update, start, stop, and results endpoints.

Expected result: command responses use `accepted`, `state`, `command`, and optional `errorCode`.
