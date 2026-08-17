# 範例程式

範例專案提供完整、可直接複製執行的 ProductInfo、生命週期、事件與結果合約示範。

執行任何範例前：

1. 啟動模擬器：

   ```powershell
   dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
   ```

2. 保留預設端點設定。
3. 按 **Start Servers**。

## 可直接複製的完整原始碼

以下連結都是完整檔案，不是省略片段，並且包含必要的 `using`、`import` 與 `#include`。請搭配表格中的專案或建置設定一起使用。

| 語言 / 傳輸 | 完整原始碼 | 相依項目與建置說明 |
| --- | --- | --- |
| C# SDK | [Program.cs](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/csharp-sdk/Program.cs) · [project](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/csharp-sdk/CSharpSdkSample.csproj) | .NET 8 與 `Virex.NET.Client` project reference。 |
| C# raw REST | [Program.cs](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/csharp-raw-rest/Program.cs) · [project](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/csharp-raw-rest/CSharpRawRestSample.csproj) | .NET 8 與 `Virex.NET.Contracts` project reference。 |
| C# raw TCP | [Program.cs](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/csharp-raw-tcp/Program.cs) · [project](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/csharp-raw-tcp/CSharpRawTcpSample.csproj) | .NET 8 與 `Virex.NET.Contracts` project reference。 |
| C# raw MQTT | [Program.cs](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/csharp-raw-mqtt/Program.cs) · [project](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/csharp-raw-mqtt/CSharpRawMqttSample.csproj) | .NET 8、MQTTnet 與 `Virex.NET.Contracts` project reference。 |
| Python raw REST | [main.py](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/python-raw-rest/main.py) | 只有 Python 標準函式庫。 |
| Python raw TCP | [main.py](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/python-raw-tcp/main.py) | 只有 Python 標準函式庫。 |
| Python raw MQTT | [main.py](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/python-raw-mqtt/main.py) | 只有 Python 標準函式庫；範例直接實作必要的 MQTT 交換。 |
| C++ raw REST | [main.cpp](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/cpp-raw-rest/main.cpp) · [CMakeLists.txt](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/cpp-raw-rest/CMakeLists.txt) | Windows SDK WinHTTP (`winhttp.lib`)。`SendRequest` 是 `main.cpp` 內的本機 helper，不是第三方 library。 |
| C++ raw TCP | [main.cpp](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/cpp-raw-tcp/main.cpp) · [CMakeLists.txt](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/cpp-raw-tcp/CMakeLists.txt) | Windows SDK Winsock2 (`ws2_32.lib`)。 |
| C++ raw MQTT | [main.cpp](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/cpp-raw-mqtt/main.cpp) · [CMakeLists.txt](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/cpp-raw-mqtt/CMakeLists.txt) | Windows SDK Winsock2 (`ws2_32.lib`)；不需要第三方 MQTT library。 |

RESTful API、TCP 與 MQTT 參考頁的 language tabs 是用來說明單一操作的短片段，不是完整程式。需要直接貼上執行時，請使用上面的完整原始碼連結。

## C# 範例

| 範例 | 命令 | 用途 |
| --- | --- | --- |
| `samples/csharp-sdk` | `dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj` | 建議的 .NET 入口。示範初始化、ProductInfo、啟動、停止、結果查詢。 |
| `samples/csharp-raw-rest` | `dotnet run --project samples\csharp-raw-rest\CSharpRawRestSample.csproj` | 直接呼叫 RESTful API 狀態查詢、ProductInfo、系統命令、結果查詢。 |
| `samples/csharp-raw-tcp` | `dotnet run --project samples\csharp-raw-tcp\CSharpRawTcpSample.csproj` | 示範 TCP/NDJSON 初始資料框、命令與事件資料框。 |
| `samples/csharp-raw-mqtt` | `dotnet run --project samples\csharp-raw-mqtt\CSharpRawMqttSample.csproj` | 觀察 MQTT 事件並發布對應的命令/查詢。 |

## Python 範例

| 範例 | 命令 | 用途 |
| --- | --- | --- |
| `samples/python-raw-rest` | `python samples\python-raw-rest\main.py` | 使用 Python 標準函式庫的 HTTP 支援。 |
| `samples/python-raw-tcp` | `python samples\python-raw-tcp\main.py` | TCP/NDJSON socket 命令與查詢示範。 |
| `samples/python-raw-mqtt` | `python samples\python-raw-mqtt\main.py` | MQTT 事件觀察與命令/查詢 topic 示範。 |

## C++ 範例

從 Visual Studio Developer PowerShell 建置：

```powershell
cmake -S samples\cpp-raw-rest -B samples\cpp-raw-rest\build
cmake --build samples\cpp-raw-rest\build --config Release
samples\cpp-raw-rest\build\Release\cpp-raw-rest.exe
```

TCP 與 MQTT 範例使用相同 CMake 模式。C++ 原始碼已包含必要的 Windows headers 與 library directives；REST 的 `SendRequest` 已定義在原始碼內。

## 預期行為

| 流程 | 預期行為 |
| --- | --- |
| SDK 與 RESTful API | 初始化從 `Uninitialized` 進入 `Ready`，ProductInfo 更新後回到 `Ready`，啟動後回傳 `Running`，執行完成後可以查詢結果。 |
| TCP | 範例連到 `5089`，讀取初始狀態/ProductInfo 資料框，送出 ProductInfo/start/stop 資料框，並觀察包含 `imageGrabbed` 與 `resultCreated` 的事件資料框。 |
| MQTT | 範例訂閱 `virex/#`，印出包含 `imageGrabbed` 的事件，發布 `commands/status/get`，並印出關聯的 `responses/{correlationId}` payload。 |