# C++ Raw TCP Sample

Minimal Windows C++ client for the public Virex.NET TCP/NDJSON protocol. It uses Winsock and does not require third-party packages.

## Prerequisites

- Windows.
- CMake 3.20 or newer.
- Visual Studio Build Tools or Visual Studio with the C++ desktop workload.
- The Virex.NET simulator running with TCP enabled on `127.0.0.1:5089`.
- Until the embedded MQTT broker is added, the simulator also expects a local MQTT broker on `127.0.0.1:1883` when **Start Servers** is clicked.

## Build

From a Visual Studio Developer PowerShell:

```powershell
cmake -S samples\cpp-raw-tcp -B samples\cpp-raw-tcp\build
cmake --build samples\cpp-raw-tcp\build --config Release
```

## Run

From the repository root, start the simulator:

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
```

In the simulator window, click **Start Servers**. Then run:

```powershell
samples\cpp-raw-tcp\build\Release\cpp-raw-tcp.exe
```

Optional host and port:

```powershell
samples\cpp-raw-tcp\build\Release\cpp-raw-tcp.exe 127.0.0.1 5089
```

Expected result:

```text
{"type":"status",...}
{"type":"waferInfo",...}
Sent waferInfo frame. Waiting for echo/update event...
{"type":"waferInfo",...}
```
