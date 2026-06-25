# 샘플

샘플 프로젝트는 안내형 데모입니다. 테스터에게 어떤 시뮬레이터 버튼을 눌러야 하는지, 어떤 상태를 테스트하는지, 어떤 출력을 기대해야 하는지 알려 줍니다.

샘플을 실행하기 전에:

1. 시뮬레이터를 시작합니다.

   ```powershell
   dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
   ```

2. 기본 엔드포인트를 유지합니다.
3. **Start Servers** 를 누릅니다.

## C# 샘플

| 샘플 | 명령 | 목적 |
| --- | --- | --- |
| `samples/csharp-sdk` | `dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj` | 권장 .NET 진입점입니다. `not_initialized`, **Initialize**, WaferInfo, 시작, 결과 조회를 보여 줍니다. |
| `samples/csharp-raw-rest` | `dotnet run --project samples\csharp-raw-rest\CSharpRawRestSample.csproj` | REST 상태, WaferInfo, 시작, 결과 조회를 직접 보여 줍니다. |
| `samples/csharp-raw-tcp` | `dotnet run --project samples\csharp-raw-tcp\CSharpRawTcpSample.csproj` | TCP/NDJSON 초기 프레임과 WaferInfo 업데이트 이벤트를 확인합니다. |
| `samples/csharp-raw-mqtt` | `dotnet run --project samples\csharp-raw-mqtt\CSharpRawMqttSample.csproj` | MQTT status, wafer-info, result, error 이벤트 관측을 확인합니다. |

## Python 샘플

| 샘플 | 명령 | 목적 |
| --- | --- | --- |
| `samples/python-raw-rest` | `python samples\python-raw-rest\main.py` | Python 표준 라이브러리 HTTP 기능을 사용한 REST 호출. |
| `samples/python-raw-tcp` | `python samples\python-raw-tcp\main.py` | TCP/NDJSON 소켓 데모. |
| `samples/python-raw-mqtt` | `python samples\python-raw-mqtt\main.py` | MQTT 이벤트 관측 데모. |

## C++ 샘플

Visual Studio Developer PowerShell 에서 C++ 샘플을 빌드합니다.

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

## 예상 안내 동작

| 흐름 | 예상 동작 |
| --- | --- |
| SDK 및 REST | **Initialize** 전에는 start 가 `HTTP 409 not_initialized` 를 반환합니다. **Initialize** 후 샘플은 WaferInfo 를 업데이트하고, 사이클을 시작하고, 결과를 조회합니다. |
| TCP | 샘플은 포트 `5089` 에 연결하고 초기 status 및 wafer-info 프레임을 읽은 뒤, WaferInfo NDJSON 프레임을 보내고 시뮬레이터가 반환한 업데이트 이벤트를 출력합니다. |
| MQTT | 샘플은 `virex/#` 를 구독합니다. 실행 중에 **Apply WaferInfo**, **Initialize**, **Start Cycle**, **Emit Fake Result**, **Emit Error** 를 누르면 대응하는 이벤트를 확인할 수 있습니다. |

## 문제 해결

| 증상 | 해결 방법 |
| --- | --- |
| 서버가 시작되지 않음 | 샘플을 실행하기 전에 시뮬레이터에서 **Start Servers** 를 누릅니다. |
| `not_initialized` | SDK 및 REST 안내형 데모에서 **Initialize** 전의 예상 동작입니다. **Initialize** 를 누르고 계속 진행합니다. |
| MQTT 이벤트가 없음 | 베이스 토픽 `virex`, 브로커 `127.0.0.1:1883`, 샘플이 아직 수신 중인지 확인합니다. |
| 결과가 반환되지 않음 | **Start Cycle** 또는 **Emit Fake Result** 를 누른 뒤 일치하는 WaferInfo 필드로 조회합니다. |
