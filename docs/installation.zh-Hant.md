# 安裝 / 下載

這頁說明如何取得公開 Virex.NET 整合工具：

- 從 GitHub Releases 下載 Windows 模擬器。
- 從 NuGet 安裝 C# SDK 套件。
- 選擇下一步驗證流程。

## GitHub Releases：預先建置的模擬器 EXE

目前模擬器版本是 `v2.0.3.1`：

[下載 Virex.NET Simulator v2.0.3.1](https://github.com/vstechnology-tw/vst-virex.net.integration/releases/tag/v2.0.3.1)

每個 ZIP 都包含 `Virex.NET.Simulator.WPF.exe`。請依照目標環境選擇套件：

| 檔案 | Runtime | 使用時機 |
| --- | --- | --- |
| `Virex.NET.Simulator-v2.0.3.1-net48-win-x64.zip` | .NET Framework 4.8 | 目標 Windows 已安裝 .NET Framework 4.8 時使用。 |
| `Virex.NET.Simulator-v2.0.3.1-net8.0-windows-win-x64-self-contained.zip` | .NET 8 Windows | 需要內含 runtime 的目前 Windows 建置時使用。 |
| `Virex.NET.Simulator-v2.0.3.1-net10.0-windows-win-x64-self-contained.zip` | .NET 10 Windows | 需要驗證最新支援 Windows target 時使用。 |

下載後：

1. 解壓縮 ZIP。
2. 執行 `Virex.NET.Simulator.WPF.exe`。
3. 保留預設端點，除非測試環境需要不同的連接埠。
4. 執行 SDK 或原始通訊協定範例前，先按 **Start Servers**。

模擬器按鈕、預設端點與操作流程請看 [模擬器操作手冊](simulator.zh-Hant.md)。

## NuGet 套件

| 套件 | 用途 |
| --- | --- |
| [Virex.NET.Contracts](https://www.nuget.org/packages/Virex.NET.Contracts/) | 以 C# 模型提供公開資料模型、REST 路由、MQTT 主題名稱、TCP/NDJSON 格式化與解析工具。 |
| [Virex.NET.Client](https://www.nuget.org/packages/Virex.NET.Client/) | REST、TCP、MQTT 的 C# SDK 包裝層。 |

安裝 SDK：

```powershell
dotnet add package Virex.NET.Client --version 2.0.3
```

只需要共用合約：

```powershell
dotnet add package Virex.NET.Contracts --version 2.0.3
```

`Virex.NET.Client` 與 `Virex.NET.Contracts` 目標框架為 `netstandard2.0`。

## 下一步

- 第一次驗證整合流程：看 [快速開始](quick-start.zh-Hant.md)。
- 需要完整 C# SDK 呼叫方式：看 [C# SDK 指南](csharp-sdk.zh-Hant.md)。
- 需要不同語言或直接使用通訊協定的範例：看 [範例程式](samples.zh-Hant.md)。
