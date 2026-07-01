# Python Raw MQTT Sample

Demonstrates MQTT event subscription plus RESTful API equivalent command/query topics for the current public protocol.

Run the simulator first and press **Start Servers**.

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
python samples\python-raw-mqtt\main.py
```

The sample subscribes to `virex/#` and prints received event topics.

Expected topics include `statusChanged`, `productInfoChanged`, `runStarted`, `runCompleted`, `resultCreated`, and `commandRejected`.
