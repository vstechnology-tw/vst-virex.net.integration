# Samples

Sample projects는 guided demos입니다. Tester에게 어떤 simulator button을 누를지, 어떤 state를 테스트하는지, 어떤 output을 기대해야 하는지 안내합니다.

Sample을 실행하기 전에:

1. Simulator를 시작합니다.

   ```powershell
   dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
   ```

2. Default endpoints를 유지합니다.
3. **Start Servers** 를 누릅니다.

## C# Samples

| Sample | Command | Purpose |
| --- | --- | --- |
| `samples/csharp-sdk` | `dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj` | 권장 .NET entry point입니다. `not_initialized`, **Initialize**, WaferInfo, start, result query를 보여줍니다. |
| `samples/csharp-raw-rest` | `dotnet run --project samples\csharp-raw-rest\CSharpRawRestSample.csproj` | Direct REST status, WaferInfo, start, result query. |
| `samples/csharp-raw-tcp` | `dotnet run --project samples\csharp-raw-tcp\CSharpRawTcpSample.csproj` | TCP/NDJSON initial frames 및 WaferInfo update event. |
| `samples/csharp-raw-mqtt` | `dotnet run --project samples\csharp-raw-mqtt\CSharpRawMqttSample.csproj` | MQTT status, wafer-info, result, error event observation. |

## Python Samples

| Sample | Command | Purpose |
| --- | --- | --- |
| `samples/python-raw-rest` | `python samples\python-raw-rest\main.py` | Python standard library HTTP support를 사용하는 REST calls. |
| `samples/python-raw-tcp` | `python samples\python-raw-tcp\main.py` | TCP/NDJSON socket demo. |
| `samples/python-raw-mqtt` | `python samples\python-raw-mqtt\main.py` | MQTT event observation demo. |

## C++ Samples

Visual Studio Developer PowerShell에서 C++ samples를 build합니다.

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
| SDK and REST | **Initialize** 전에는 start가 `HTTP 409 not_initialized` 를 반환합니다. **Initialize** 후 sample은 WaferInfo를 업데이트하고 cycle을 시작하며 results를 query합니다. |
| TCP | Sample은 port `5089` 에 연결하고 initial status 및 wafer-info frames를 읽고 WaferInfo NDJSON frame을 보낸 뒤 simulator가 반환한 update event를 출력합니다. |
| MQTT | Sample은 `virex/#` 를 subscribe합니다. 실행 중에 **Apply WaferInfo**, **Initialize**, **Start Cycle**, **Emit Fake Result**, **Emit Error** 를 눌러 matching events를 확인합니다. |

## Troubleshooting

| Symptom | Resolution |
| --- | --- |
| Server not started | Samples를 실행하기 전에 simulator에서 **Start Servers** 를 누르십시오. |
| `not_initialized` | SDK 및 REST guided demos에서는 **Initialize** 전의 expected behavior입니다. **Initialize** 를 누르고 계속하십시오. |
| No MQTT events | Base topic `virex`, broker `127.0.0.1:1883`, sample이 아직 listening 중인지 확인하십시오. |
| No result returned | **Start Cycle** 또는 **Emit Fake Result** 를 누른 뒤 matching WaferInfo fields로 query하십시오. |
