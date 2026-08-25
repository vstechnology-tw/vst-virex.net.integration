# Samples

The sample projects provide complete, copy-ready demonstrations of the public ProductInfo, lifecycle, event, and result contracts.

Before executing any example:

1. Start the simulator:

   ```powershell
   dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
   ```

2. Keep the default endpoint settings.
3. Press **Start Servers**.

## Copy-ready source files

The source links below point to complete files, not abbreviated snippets. They include their required `using`, `import`, and `#include` directives. Copy the complete source file together with the listed project/build configuration.

| Language / transport | Complete source | Dependencies and build notes |
| --- | --- | --- |
| C# SDK | [Program.cs](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/csharp-sdk/Program.cs) · [project](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/csharp-sdk/CSharpSdkSample.csproj) | .NET 8 and the `Virex.NET.Client` project reference. |
| C# raw REST | [Program.cs](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/csharp-raw-rest/Program.cs) · [project](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/csharp-raw-rest/CSharpRawRestSample.csproj) | .NET 8 and the `Virex.NET.Contracts` project reference. |
| C# raw TCP | [Program.cs](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/csharp-raw-tcp/Program.cs) · [project](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/csharp-raw-tcp/CSharpRawTcpSample.csproj) | .NET 8 and the `Virex.NET.Contracts` project reference. |
| C# raw MQTT | [Program.cs](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/csharp-raw-mqtt/Program.cs) · [project](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/csharp-raw-mqtt/CSharpRawMqttSample.csproj) | .NET 8, MQTTnet, and the `Virex.NET.Contracts` project reference. |
| Python raw REST | [main.py](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/python-raw-rest/main.py) | Python standard library only. |
| Python raw TCP | [main.py](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/python-raw-tcp/main.py) | Python standard library only. |
| Python raw MQTT | [main.py](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/python-raw-mqtt/main.py) | Python standard library only; the sample implements the small MQTT exchange directly. |
| C++ raw REST | [main.cpp](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/cpp-raw-rest/main.cpp) · [CMakeLists.txt](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/cpp-raw-rest/CMakeLists.txt) | Windows SDK WinHTTP (`winhttp.lib`). `SendRequest` is a local helper in `main.cpp`, not a third-party library. |
| C++ raw TCP | [main.cpp](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/cpp-raw-tcp/main.cpp) · [CMakeLists.txt](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/cpp-raw-tcp/CMakeLists.txt) | Windows SDK Winsock2 (`ws2_32.lib`). |
| C++ raw MQTT | [main.cpp](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/cpp-raw-mqtt/main.cpp) · [CMakeLists.txt](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/cpp-raw-mqtt/CMakeLists.txt) | Windows SDK Winsock2 (`ws2_32.lib`); no external MQTT library is required. |

The language tabs on the RESTful API, TCP, and MQTT reference pages are short request fragments for explaining one operation. They intentionally do not pretend to be complete programs; use the complete source links above when you want to paste and run an example.

## C# Examples

| Example | Command | Purpose |
| --- | --- | --- |
| `samples/csharp-sdk` | `dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj` | Recommended .NET entry point. Demonstrates initialization, ProductInfo, start, stop, and result query. |
| `samples/csharp-raw-rest` | `dotnet run --project samples\csharp-raw-rest\CSharpRawRestSample.csproj` | Calls RESTful API state query, ProductInfo, system commands, and result query directly. |
| `samples/csharp-raw-tcp` | `dotnet run --project samples\csharp-raw-tcp\CSharpRawTcpSample.csproj` | Demonstrates TCP/NDJSON initial frames, commands, and event frames. |
| `samples/csharp-raw-mqtt` | `dotnet run --project samples\csharp-raw-mqtt\CSharpRawMqttSample.csproj` | Observes MQTT events and publishes RESTful API equivalent command/query requests. |

## Python Examples

| Example | Command | Purpose |
| --- | --- | --- |
| `samples/python-raw-rest` | `python samples\python-raw-rest\main.py` | Uses HTTP support from the Python standard library. |
| `samples/python-raw-tcp` | `python samples\python-raw-tcp\main.py` | TCP/NDJSON socket command and query demonstration. |
| `samples/python-raw-mqtt` | `python samples\python-raw-mqtt\main.py` | Demonstrates MQTT event observation plus command/query request topics. |

## C++ Examples

Build from Visual Studio Developer PowerShell:

```powershell
cmake -S samples\cpp-raw-rest -B samples\cpp-raw-rest\build
cmake --build samples\cpp-raw-rest\build --config Release
samples\cpp-raw-rest\build\Release\cpp-raw-rest.exe
```

The TCP and MQTT examples use the same CMake pattern. The C++ source files contain their required Windows headers and library directives; the REST helper `SendRequest` is defined in the source file itself.

## Expected Behavior

| Flow | Expected Behavior |
| --- | --- |
| SDK and RESTful API | Initialization moves from `Uninitialized` to `Ready`; ProductInfo update returns to `Ready`; start returns `Running`; results can be queried after the run completes. |
| TCP | The sample connects to `5089`, reads initial state/ProductInfo frames, sends query frames, sends ProductInfo/start/stop frames, and observes event frames including `imageGrabbed` and `resultCreated`. |
| MQTT | The sample subscribes to `virex/#`, prints events including `imageGrabbed`, publishes `commands/status/get`, and prints the correlated `responses/{correlationId}` payload. |