# Samples

The sample projects are guided demos. They tell the tester which simulator button to press, what state is being tested, and what output to expect.

Before running any sample:

1. Start the simulator:

   ```powershell
   dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
   ```

2. Keep the default endpoints.
3. Press **Start Servers**.

## C# Samples

| Sample | Command | Purpose |
| --- | --- | --- |
| `samples/csharp-sdk` | `dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj` | Recommended .NET entry point. Demonstrates `not_initialized`, **Initialize**, WaferInfo, start, and result query. |
| `samples/csharp-raw-rest` | `dotnet run --project samples\csharp-raw-rest\CSharpRawRestSample.csproj` | Direct REST status, WaferInfo, start, and result query. |
| `samples/csharp-raw-tcp` | `dotnet run --project samples\csharp-raw-tcp\CSharpRawTcpSample.csproj` | TCP/NDJSON initial frames and WaferInfo update event. |
| `samples/csharp-raw-mqtt` | `dotnet run --project samples\csharp-raw-mqtt\CSharpRawMqttSample.csproj` | MQTT status, wafer-info, result, and error event observation. |

## Python Samples

| Sample | Command | Purpose |
| --- | --- | --- |
| `samples/python-raw-rest` | `python samples\python-raw-rest\main.py` | REST calls using Python standard library HTTP support. |
| `samples/python-raw-tcp` | `python samples\python-raw-tcp\main.py` | TCP/NDJSON socket demo. |
| `samples/python-raw-mqtt` | `python samples\python-raw-mqtt\main.py` | MQTT event observation demo. |

## C++ Samples

Build C++ samples from a Visual Studio Developer PowerShell.

```powershell
cmake -S samples\cpp-raw-rest -B samples\cpp-raw-rest\build
cmake --build samples\cpp-raw-rest\build --config Release
samples\cpp-raw-rest\build\Release\cpp-raw-rest.exe

cmake -S samples\cpp-raw-tcp -B samples\cpp-raw-tcp\build
cmake --build samples\cpp-raw-tcp\build --config Release
samples\cpp-raw-tcp\build\Release\cpp-raw-tcp.exe

cmake -S samples\cpp-raw-mqtt -B samples\cpp-raw-mqtt\build
cmake --build samples\cpp-raw-mqtt\build --config Release
samples\cpp-raw-mqtt\build\Release\cpp-raw-mqtt.exe
```

## Expected Guided Behavior

| Flow | Expected behavior |
| --- | --- |
| SDK and REST | Before **Initialize**, start returns `HTTP 409 not_initialized`. After **Initialize**, the sample updates WaferInfo, starts a cycle, and queries results. |
| TCP | The sample connects to port `5089`, reads initial status and wafer-info frames, sends a WaferInfo NDJSON frame, and prints the update event returned by the simulator. |
| MQTT | The sample subscribes to `virex/#`. While it is running, press **Apply WaferInfo**, **Initialize**, **Start Cycle**, **Emit Fake Result**, or **Emit Error** to observe matching events. |

## Troubleshooting

| Symptom | Resolution |
| --- | --- |
| Server not started | Press **Start Servers** in the simulator before running samples. |
| `not_initialized` | Expected before **Initialize** for SDK and REST guided demos. Press **Initialize** and continue. |
| No MQTT events | Verify base topic `virex`, broker `127.0.0.1:1883`, and that the sample is still listening. |
| No result returned | Press **Start Cycle** or **Emit Fake Result**, then query using matching WaferInfo fields. |
