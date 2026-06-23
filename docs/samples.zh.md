# 範例程式

Sample projects 是 guided demos。它們會告訴測試者要按哪個 simulator button、正在測什麼 state、以及預期看到什麼 output。

執行任何 sample 前：

1. 啟動 simulator：

   ```powershell
   dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
   ```

2. 保留預設 endpoints。
3. 按 **Start Servers**。

## C# Samples

| Sample | Command | Purpose |
| --- | --- | --- |
| `samples/csharp-sdk` | `dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj` | 建議的 .NET 入口。示範 `not_initialized`、**Initialize**、WaferInfo、start、result query。 |
| `samples/csharp-raw-rest` | `dotnet run --project samples\csharp-raw-rest\CSharpRawRestSample.csproj` | 直接示範 REST status、WaferInfo、start、result query。 |
| `samples/csharp-raw-tcp` | `dotnet run --project samples\csharp-raw-tcp\CSharpRawTcpSample.csproj` | TCP/NDJSON initial frames 與 WaferInfo update event。 |
| `samples/csharp-raw-mqtt` | `dotnet run --project samples\csharp-raw-mqtt\CSharpRawMqttSample.csproj` | MQTT status、wafer-info、result、error event 觀察。 |

## Python Samples

| Sample | Command | Purpose |
| --- | --- | --- |
| `samples/python-raw-rest` | `python samples\python-raw-rest\main.py` | 使用 Python standard library HTTP support 呼叫 REST。 |
| `samples/python-raw-tcp` | `python samples\python-raw-tcp\main.py` | TCP/NDJSON socket demo。 |
| `samples/python-raw-mqtt` | `python samples\python-raw-mqtt\main.py` | MQTT event observation demo。 |

## C++ Samples

請從 Visual Studio Developer PowerShell build C++ samples。

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

## 預期 Guided Behavior

| Flow | Expected behavior |
| --- | --- |
| SDK and REST | **Initialize** 前，start 回 `HTTP 409 not_initialized`。按 **Initialize** 後，sample 會更新 WaferInfo、start cycle、query results。 |
| TCP | Sample 連到 port `5089`，讀取 initial status 與 wafer-info frames，送出 WaferInfo NDJSON frame，並印出 simulator 回傳的 update event。 |
| MQTT | Sample 訂閱 `virex/#`。執行中按 **Apply WaferInfo**、**Initialize**、**Start Cycle**、**Emit Fake Result** 或 **Emit Error**，可看到對應 events。 |

## 疑難排解

| 症狀 | 解法 |
| --- | --- |
| Server not started | 執行 samples 前，先在 simulator 按 **Start Servers**。 |
| `not_initialized` | SDK and REST guided demos 在 **Initialize** 前預期會看到此結果。按 **Initialize** 後繼續。 |
| 沒有 MQTT events | 確認 base topic `virex`、broker `127.0.0.1:1883`，且 sample 仍在 listening。 |
| 沒有 result | 按 **Start Cycle** 或 **Emit Fake Result**，再使用 matching WaferInfo fields 查詢。 |
