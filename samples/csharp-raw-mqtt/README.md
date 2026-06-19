# C# Raw MQTT Sample

Minimal C# client for the public Virex.NET MQTT event topics. It uses MQTTnet directly instead of the SDK wrapper.

## Prerequisites

- .NET SDK.
- The Virex.NET simulator running with MQTT enabled on `127.0.0.1:1883`.

## Run

From the repository root, start the simulator:

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
```

In the simulator window, click **Start Servers**. Then open a second terminal from the repository root and run:

```powershell
dotnet run --project samples\csharp-raw-mqtt\CSharpRawMqttSample.csproj
```

While the sample is running, click **Apply WaferInfo**, **Start Cycle**, **Emit Fake Result**, or **Emit Error** in the simulator.

Optional host, port, base topic, and listen duration in seconds:

```powershell
dotnet run --project samples\csharp-raw-mqtt\CSharpRawMqttSample.csproj -- 127.0.0.1 1883 Virex.NET 30
```

Expected result:

```text
Subscribed to Virex.NET/# for 30 seconds.
Virex.NET/wafer-info: {"lotId":"LOT-001",...}
```

