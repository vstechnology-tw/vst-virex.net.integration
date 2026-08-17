# 샘플

샘플 프로젝트는 공개 ProductInfo, 수명 주기, 이벤트 및 결과 계약을 완전하고 실행 가능한 형태로 보여줍니다.

예제를 실행하기 전에:

1. 시뮬레이터를 시작합니다:

   ```powershell
   dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
   ```

2. 기본 엔드포인트 설정을 유지합니다.
3. **Start Servers**를 누릅니다.

## 바로 복사하여 실행할 수 있는 전체 소스

아래 링크는 축약된 조각이 아닌 완전한 파일이며 필요한 `using`, `import`, `#include` 지시문을 포함합니다. 표시된 프로젝트 또는 빌드 설정과 함께 사용하세요.

| 언어 / transport | 전체 소스 | 종속성 및 빌드 |
| --- | --- | --- |
| C# SDK | [Program.cs](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/csharp-sdk/Program.cs) · [project](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/csharp-sdk/CSharpSdkSample.csproj) | .NET 8 및 `Virex.NET.Client` project reference. |
| C# raw REST | [Program.cs](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/csharp-raw-rest/Program.cs) · [project](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/csharp-raw-rest/CSharpRawRestSample.csproj) | .NET 8 및 `Virex.NET.Contracts` project reference. |
| C# raw TCP | [Program.cs](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/csharp-raw-tcp/Program.cs) · [project](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/csharp-raw-tcp/CSharpRawTcpSample.csproj) | .NET 8 및 `Virex.NET.Contracts` project reference. |
| C# raw MQTT | [Program.cs](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/csharp-raw-mqtt/Program.cs) · [project](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/csharp-raw-mqtt/CSharpRawMqttSample.csproj) | .NET 8, MQTTnet 및 `Virex.NET.Contracts` project reference. |
| Python raw REST | [main.py](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/python-raw-rest/main.py) | Python 표준 라이브러리만 사용합니다. |
| Python raw TCP | [main.py](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/python-raw-tcp/main.py) | Python 표준 라이브러리만 사용합니다. |
| Python raw MQTT | [main.py](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/python-raw-mqtt/main.py) | Python 표준 라이브러리만 사용하며 필요한 MQTT 교환을 직접 구현합니다. |
| C++ raw REST | [main.cpp](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/cpp-raw-rest/main.cpp) · [CMakeLists.txt](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/cpp-raw-rest/CMakeLists.txt) | Windows SDK WinHTTP (`winhttp.lib`). `SendRequest`는 `main.cpp` 안의 로컬 helper이며 타사 라이브러리가 아닙니다. |
| C++ raw TCP | [main.cpp](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/cpp-raw-tcp/main.cpp) · [CMakeLists.txt](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/cpp-raw-tcp/CMakeLists.txt) | Windows SDK Winsock2 (`ws2_32.lib`). |
| C++ raw MQTT | [main.cpp](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/cpp-raw-mqtt/main.cpp) · [CMakeLists.txt](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/cpp-raw-mqtt/CMakeLists.txt) | Windows SDK Winsock2 (`ws2_32.lib`); 외부 MQTT 라이브러리가 필요하지 않습니다. |

RESTful API, TCP, MQTT 참조 페이지의 language tabs는 각 작업을 설명하기 위한 짧은 요청 조각입니다. 바로 붙여넣어 실행하려면 위의 전체 소스 링크를 사용하세요.

## C# 예

| 예 | 명령 | 목적 |
| --- | --- | --- |
| `samples/csharp-sdk` | `dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj` | 권장되는 .NET 진입점입니다. 초기화, ProductInfo, 시작, 중지 및 결과 쿼리를 보여줍니다. |
| `samples/csharp-raw-rest` | `dotnet run --project samples\csharp-raw-rest\CSharpRawRestSample.csproj` | RESTful API 상태 쿼리, ProductInfo, 시스템 명령, 결과 쿼리를 직접 호출합니다. |
| `samples/csharp-raw-tcp` | `dotnet run --project samples\csharp-raw-tcp\CSharpRawTcpSample.csproj` | TCP/NDJSON 초기 프레임, 명령 및 이벤트 프레임을 보여줍니다. |
| `samples/csharp-raw-mqtt` | `dotnet run --project samples\csharp-raw-mqtt\CSharpRawMqttSample.csproj` | MQTT 이벤트를 관찰하고 해당 명령/쿼리를 게시합니다. |

## Python 예

| 예 | 명령 | 목적 |
| --- | --- | --- |
| `samples/python-raw-rest` | `python samples\python-raw-rest\main.py` | Python 표준 라이브러리의 HTTP 지원을 사용합니다. |
| `samples/python-raw-tcp` | `python samples\python-raw-tcp\main.py` | TCP/NDJSON 소켓 명령 및 쿼리 데모입니다. |
| `samples/python-raw-mqtt` | `python samples\python-raw-mqtt\main.py` | MQTT 이벤트 관찰 및 명령/쿼리 topic 데모입니다. |

## C++ 예

Visual Studio 개발자 PowerShell에서 빌드:

```powershell
cmake -S samples\cpp-raw-rest -B samples\cpp-raw-rest\build
cmake --build samples\cpp-raw-rest\build --config Release
samples\cpp-raw-rest\build\Release\cpp-raw-rest.exe
```

TCP 및 MQTT 예제는 동일한 CMake 패턴을 사용합니다. C++ 소스에는 필요한 Windows 헤더와 library directive가 포함되어 있으며 REST `SendRequest`는 소스 파일 안에 정의되어 있습니다.

## 예상되는 동작

| 흐름 | 예상되는 동작 |
| --- | --- |
| SDK 및 RESTful API | 초기화가 `Uninitialized`에서 `Ready`로 이동합니다. ProductInfo 업데이트가 `Ready`로 반환됩니다. start는 `Running`를 반환합니다. 실행이 완료된 후 결과를 쿼리할 수 있습니다. |
| TCP | 샘플은 `5089`에 연결하고 초기 상태/ProductInfo 프레임을 읽고 ProductInfo/start/stop 프레임을 보내며 `imageGrabbed` 및 `resultCreated`를 포함한 이벤트 프레임을 관찰합니다. |
| MQTT | 샘플은 `virex/#`를 구독하고 `imageGrabbed`를 포함한 이벤트를 인쇄하며 `commands/status/get`을 게시하고 연결된 `responses/{correlationId}` payload를 인쇄합니다. |