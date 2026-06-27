# 샘플

샘플 프로젝트는 공개 ProductInfo 및 상태 머신 계약에 대한 안내 데모를 제공합니다.

예제를 실행하기 전에:

1. 시뮬레이터를 시작합니다:

   ```powershell
   dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
   ```

2. 기본 엔드포인트를 유지합니다.
3. **Start Servers**를 누르세요.

## C# 예

| 예 | 명령 | 목적 |
| --- | --- | --- |
| `samples/csharp-sdk` | `dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj` | 권장되는 .NET 진입점입니다. 초기화, ProductInfo, 시작, 중지 및 결과 쿼리를 보여줍니다. |
| `samples/csharp-raw-rest` | `dotnet run --project samples\csharp-raw-rest\CSharpRawRestSample.csproj` | REST 상태 쿼리, ProductInfo, 시스템 명령, 결과 쿼리를 직접 호출합니다. |
| `samples/csharp-raw-tcp` | `dotnet run --project samples\csharp-raw-tcp\CSharpRawTcpSample.csproj` | TCP/NDJSON 초기 프레임 및 ProductInfo 업데이트 이벤트를 보여줍니다. |
| `samples/csharp-raw-mqtt` | `dotnet run --project samples\csharp-raw-mqtt\CSharpRawMqttSample.csproj` | MQTT 상태, ProductInfo, 실행, 결과 및 거부 이벤트를 관찰합니다. |

## Python 예

| 예 | 명령 | 목적 |
| --- | --- | --- |
| `samples/python-raw-rest` | `python samples\python-raw-rest\main.py` | Python 표준 라이브러리의 HTTP 지원을 사용합니다. |
| `samples/python-raw-tcp` | `python samples\python-raw-tcp\main.py` | TCP/NDJSON 소켓 데모. |
| `samples/python-raw-mqtt` | `python samples\python-raw-mqtt\main.py` | MQTT 이벤트 관찰을 보여줍니다. |

## C++ 예

Visual Studio 개발자 PowerShell에서 빌드:

```powershell
cmake -S samples\cpp-raw-rest -B samples\cpp-raw-rest\build
cmake --build samples\cpp-raw-rest\build --config Release
samples\cpp-raw-rest\build\Release\cpp-raw-rest.exe
```

TCP 및 MQTT 예제는 동일한 CMake 패턴을 사용합니다.

## 예상되는 동작

| 흐름 | 예상되는 동작 |
| --- | --- |
| SDK 및 REST | 초기화가 `Uninitialized`에서 `Ready`로 이동합니다. ProductInfo 업데이트가 `Ready`로 반환됩니다. start는 `Running`를 반환합니다. 실행이 완료된 후 결과를 쿼리할 수 있습니다. |
| TCP | 샘플은 `5089`에 연결하고, 초기 상태/ProductInfo 프레임을 읽고, ProductInfo NDJSON 프레임을 보내고, 업데이트 이벤트를 인쇄합니다. |
| MQTT | 샘플은 `virex/#`를 구독하고 `statusChanged`, `productInfoChanged`, `runStarted`, `runCompleted`, `resultCreated` 및 `commandRejected`를 인쇄합니다. |
