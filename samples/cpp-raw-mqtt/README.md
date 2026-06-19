# C++ Raw MQTT Sample

Minimal Windows C++ client for the public Virex.NET MQTT event topics. It implements the small MQTT 3.1.1 subset needed to connect and subscribe using Winsock, so no third-party packages are required.

## Prerequisites

- Windows.
- CMake 3.20 or newer.
- Visual Studio Build Tools or Visual Studio with the C++ desktop workload.
- The Virex.NET simulator running with MQTT enabled on `127.0.0.1:1883`.

## Build

From a Visual Studio Developer PowerShell:

```powershell
cmake -S samples\cpp-raw-mqtt -B samples\cpp-raw-mqtt\build
cmake --build samples\cpp-raw-mqtt\build --config Release
```

## Run

From the repository root, start the simulator:

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
```

In the simulator window, click **Start Servers**. Then run:

```powershell
samples\cpp-raw-mqtt\build\Release\cpp-raw-mqtt.exe
```

While the sample is running, click **Apply WaferInfo**, **Start Cycle**, **Emit Fake Result**, or **Emit Error** in the simulator.

Optional host, port, base topic, and listen duration in seconds:

```powershell
samples\cpp-raw-mqtt\build\Release\cpp-raw-mqtt.exe 127.0.0.1 1883 Virex.NET 30
```

Expected result:

```text
Subscribed to Virex.NET/# for 30 seconds.
Virex.NET/wafer-info: {"lotId":"LOT-001",...}
```

