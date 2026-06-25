# 安裝 / 下載

本頁說明如何取得公開的 Virex.NET integration 工具：

- 從 GitHub Releases 下載預先編譯的 Windows simulator。
- 從 NuGet 安裝 C# SDK packages。
- 執行本 repository 內的 sample code。

## GitHub Releases: 預先編譯 Simulator EXE

Simulator 會以 ZIP asset 發佈在 repository release page：

[下載最新 simulator release](https://github.com/vstechnology-tw/vst-virex.net.integration/releases/latest)

每個 ZIP 都包含 `Virex.NET.Simulator.WPF.exe`。請依目標 runtime 選擇：

| Asset | Runtime | 使用情境 |
| --- | --- | --- |
| `Virex.NET.Simulator-vX.Y.Z-net48-win-x64.zip` | .NET Framework 4.8 | Windows 機台已安裝 .NET Framework 4.8 時使用。 |
| `Virex.NET.Simulator-vX.Y.Z-net8.0-windows-win-x64-self-contained.zip` | .NET 8 Windows | 需要 runtime 已打包在 release asset 中的 Windows build 時使用。 |
| `Virex.NET.Simulator-vX.Y.Z-net10.0-windows-win-x64-self-contained.zip` | .NET 10 Windows | 需要驗證最新支援 Windows target 時使用。 |

下載後：

1. 解壓縮 ZIP。
2. 執行 `Virex.NET.Simulator.WPF.exe`。
3. 除非測試環境需要不同 port，否則保留預設 endpoints。
4. 執行 SDK 或 raw protocol samples 前，先按 **Start Servers**。

預設 endpoints：

| 介面 | 預設值 |
| --- | --- |
| REST | `http://127.0.0.1:5088` |
| TCP | `127.0.0.1:5089` |
| MQTT | `127.0.0.1:1883`，base topic `virex` |

完整 simulator 按鈕流程請看 [Simulator 操作手冊](simulator.md)。

## NuGet Packages

C# integration packages 會發佈到 NuGet：

| Package | 用途 |
| --- | --- |
| [Virex.NET.Contracts](https://www.nuget.org/packages/Virex.NET.Contracts/) | 公開 DTO、REST routes、MQTT topic 名稱、TCP/NDJSON formatter 與 parser。 |
| [Virex.NET.Client](https://www.nuget.org/packages/Virex.NET.Client/) | REST command/query、TCP events、MQTT events 的 C# SDK wrapper。 |

在 .NET 專案安裝 SDK package：

```powershell
dotnet add package Virex.NET.Client
```

如果只需要公開 payload types 與 protocol helpers，不需要 SDK wrapper，可只安裝 shared contracts：

```powershell
dotnet add package Virex.NET.Contracts
```

`Virex.NET.Client` 與 `Virex.NET.Contracts` target `netstandard2.0`，可被支援 `netstandard2.0` 的 .NET Framework 與現代 .NET 應用程式引用。

## Sample Code

Samples 位於 repository 的 `samples/`：

[在 GitHub 瀏覽 sample code](https://github.com/vstechnology-tw/vst-virex.net.integration/tree/main/samples)

建議第一次先執行：

```powershell
dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj
```

其他 samples：

| Sample | Command |
| --- | --- |
| C# raw REST | `dotnet run --project samples\csharp-raw-rest\CSharpRawRestSample.csproj` |
| C# raw TCP | `dotnet run --project samples\csharp-raw-tcp\CSharpRawTcpSample.csproj` |
| C# raw MQTT | `dotnet run --project samples\csharp-raw-mqtt\CSharpRawMqttSample.csproj` |
| Python raw REST | `python samples\python-raw-rest\main.py` |
| Python raw TCP | `python samples\python-raw-tcp\main.py` |
| Python raw MQTT | `python samples\python-raw-mqtt\main.py` |

完整 guided sample workflow 請看 [範例程式](samples.md)。

## 最小 C# SDK 範例

```csharp
using Virex.NET.Client;
using Virex.NET.Contracts;

using var client = new VirexClient(new VirexClientOptions
{
    RestBaseUrl = "http://127.0.0.1:5088",
    TcpHost = "127.0.0.1",
    TcpPort = 5089,
    MqttHost = "127.0.0.1",
    MqttPort = 1883,
    MqttTopic = "virex",
});

await client.SetWaferInfoAsync(new WaferInfo
{
    LotId = "LOT-001",
    WaferId = "W01",
    RecipeId = "RCP-A",
    Slot = "1",
    FoupId = "FOUP-A",
    ChamberId = "CH-1",
});

await client.InitializeAsync();
await client.StartAsync();
```
