# 範例程式

範例專案是公開 ProductInfo/狀態機合約的導引式示範。

執行任何範例前：

1. 啟動模擬器：

   ```powershell
   dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
   ```

2. 保留預設端點。
3. 按 **Start Servers**。

## C# 範例

| 範例 | 命令 | 用途 |
| --- | --- | --- |
| `samples/csharp-sdk` | `dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj` | 建議的 .NET 入口。示範初始化、ProductInfo、啟動、停止、結果查詢。 |
| `samples/csharp-raw-rest` | `dotnet run --project samples\csharp-raw-rest\CSharpRawRestSample.csproj` | 直接呼叫 REST 狀態查詢、ProductInfo、系統命令、結果查詢。 |
| `samples/csharp-raw-tcp` | `dotnet run --project samples\csharp-raw-tcp\CSharpRawTcpSample.csproj` | TCP/NDJSON 初始資料框與 ProductInfo 更新事件。 |
| `samples/csharp-raw-mqtt` | `dotnet run --project samples\csharp-raw-mqtt\CSharpRawMqttSample.csproj` | 觀察 MQTT 狀態、ProductInfo、執行、結果、拒絕事件。 |

## Python 範例

| 範例 | 命令 | 用途 |
| --- | --- | --- |
| `samples/python-raw-rest` | `python samples\python-raw-rest\main.py` | 使用 Python 標準函式庫的 HTTP 支援。 |
| `samples/python-raw-tcp` | `python samples\python-raw-tcp\main.py` | TCP/NDJSON socket 示範。 |
| `samples/python-raw-mqtt` | `python samples\python-raw-mqtt\main.py` | MQTT 事件觀察示範。 |

## C++ 範例

從 Visual Studio Developer PowerShell 建置：

```powershell
cmake -S samples\cpp-raw-rest -B samples\cpp-raw-rest\build
cmake --build samples\cpp-raw-rest\build --config Release
samples\cpp-raw-rest\build\Release\cpp-raw-rest.exe
```

TCP 與 MQTT 範例使用相同 CMake 模式。

## 預期行為

| 流程 | 預期行為 |
| --- | --- |
| SDK 與 REST | 初始化從 `Uninitialized` 進入 `Ready`，ProductInfo 更新後回到 `Ready`，啟動後回傳 `Running`，執行完成後可以查詢結果。 |
| TCP | 範例連到 `5089`，讀取初始狀態/ProductInfo 資料框，送出 ProductInfo NDJSON 資料框，印出更新事件。 |
| MQTT | 範例訂閱 `virex/#`，印出 `statusChanged`、`productInfoChanged`、`runStarted`、`runCompleted`、`resultCreated`、`commandRejected`。 |
