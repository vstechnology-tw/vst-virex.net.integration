# Python Raw MQTT Sample

Minimal Python client for the public Virex.NET MQTT event topics. It implements the small MQTT 3.1.1 subset needed to connect and subscribe, so no third-party Python packages are required.

## Prerequisites

- Python 3.8 or newer.
- The Virex.NET simulator running with MQTT enabled on `127.0.0.1:1883`.

## Run

From the repository root:

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
```

In the simulator window, click **Start Servers**. Then open a second terminal from the repository root and run:

```powershell
python samples\python-raw-mqtt\main.py
```

While the sample is running, click **Apply WaferInfo**, **Start Cycle**, **Emit Fake Result**, or **Emit Error** in the simulator.

Optional host, port, base topic, and listen duration in seconds:

```powershell
python samples\python-raw-mqtt\main.py 127.0.0.1 1883 Virex.NET 30
```

Expected result:

```text
Subscribed to Virex.NET/# for 30 seconds.
Virex.NET/wafer-info: {"lotId":"LOT-001",...}
```

