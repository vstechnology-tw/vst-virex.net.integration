# Samples

The sample projects provide guided demonstrations of the public ProductInfo and state-machine contract.

Before executing any examples:

1. Start the simulator:

   ```powershell
   dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
   ```

2. Keep the default endpoint.
3. Press **Start Servers**.

## C# Examples

| Example | Command | Purpose |
| --- | --- | --- |
| `samples/csharp-sdk` | `dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj` | Recommended .NET entry point. Demonstrates initialization, ProductInfo, start, stop, and result query. |
| `samples/csharp-raw-rest` | `dotnet run --project samples\csharp-raw-rest\CSharpRawRestSample.csproj` | Calls REST state query, ProductInfo, system commands, and result query directly. |
| `samples/csharp-raw-tcp` | `dotnet run --project samples\csharp-raw-tcp\CSharpRawTcpSample.csproj` | Demonstrates TCP/NDJSON initial frames and ProductInfo update events. |
| `samples/csharp-raw-mqtt` | `dotnet run --project samples\csharp-raw-mqtt\CSharpRawMqttSample.csproj` | Observes MQTT state, ProductInfo, run, result, and rejection events. |

## Python Examples

| Example | Command | Purpose |
| --- | --- | --- |
| `samples/python-raw-rest` | `python samples\python-raw-rest\main.py` | Uses HTTP support from the Python standard library. |
| `samples/python-raw-tcp` | `python samples\python-raw-tcp\main.py` | TCP/NDJSON socket demonstration. |
| `samples/python-raw-mqtt` | `python samples\python-raw-mqtt\main.py` | Demonstrates MQTT event observation. |

## C++ Example

Build from Visual Studio Developer PowerShell:

```powershell
cmake -S samples\cpp-raw-rest -B samples\cpp-raw-rest\build
cmake --build samples\cpp-raw-rest\build --config Release
samples\cpp-raw-rest\build\Release\cpp-raw-rest.exe
```

The TCP and MQTT examples use the same CMake pattern.

## Expected Behavior

| Flow | Expected Behavior |
| --- | --- |
| SDK and REST | Initialization moves from `Uninitialized` to `Ready`; ProductInfo update returns to `Ready`; start returns `Running`; results can be queried after the run completes. |
| TCP | The sample connects to `5089`, reads the initial state/ProductInfo frames, sends the ProductInfo NDJSON frame, and prints the update event. |
| MQTT | The sample subscribes to `virex/#` and prints `statusChanged`, `productInfoChanged`, `runStarted`, `runCompleted`, `resultCreated`, and `commandRejected`. |
