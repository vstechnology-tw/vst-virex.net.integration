# C# Raw MQTT Sample

Demonstrates MQTT event subscription plus RESTful API equivalent command/query topics for the current public protocol.

Run the simulator first and press **Start Servers**.

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
dotnet run --project samples\csharp-raw-mqtt\CSharpRawMqttSample.csproj
```

The sample subscribes to `virex/#`.

Expected topics include:

- `virex/statusChanged`
- `virex/productInfoChanged`
- `virex/runStarted`
- `virex/runCompleted`
- `virex/resultCreated`
- `virex/commandRejected`
