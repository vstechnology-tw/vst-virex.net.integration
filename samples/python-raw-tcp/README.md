# Python Raw TCP Sample

Minimal Python client for the public Virex.NET TCP/NDJSON protocol.

## Prerequisites

- Python 3.8 or newer.
- The Virex.NET simulator running with TCP enabled on `127.0.0.1:5089`.
- No third-party Python packages are required.
- Until the embedded MQTT broker is added, the simulator also expects a local MQTT broker on `127.0.0.1:1883` when **Start Servers** is clicked.

## Run

From the repository root:

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
```

In the simulator window, click **Start Servers**. Then open a second terminal from the repository root and run:

```powershell
python samples\python-raw-tcp\main.py
```

Optional host and port:

```powershell
python samples\python-raw-tcp\main.py 127.0.0.1 5089
```

Expected result:

```text
{"type":"status",...}
{"type":"waferInfo",...}
Sent waferInfo frame. Waiting for echo/update event...
{"type":"waferInfo",...}
```
