# Virex.NET 整合套件

Virex.NET 整合套件提供外部系統整合 Virex.NET 相容服務時需要的公開合約、C# SDK、模擬器、範例與文件。這個儲存庫不包含 Virex.NET 正式產品的私有實作細節。

模擬器與正式 Virex.NET 端點應該實作同一份 `Virex.NET.Contracts`。外部廠商如果能透過 `Virex.NET.Simulator.WPF` 完成整合，理論上切到正式端點時只需要調整 REST/TCP/MQTT 端點設定。

## 專案目的

| 區域 | 用途 |
| --- | --- |
| `Virex.NET.Contracts` | 公開資料模型、REST 路由、MQTT 主題名稱、TCP/NDJSON 格式化與解析工具。 |
| `Virex.NET.Client` | C# SDK 包裝層，提供 REST 命令/查詢、TCP 事件、MQTT 事件。 |
| `Virex.NET.Simulator.Core` | 模擬器專用的狀態機與工作階段行為，供本機模擬器使用。 |
| `Virex.NET.Simulator.WPF` | 本機模擬器，對外提供公開 REST/TCP/MQTT 合約。 |
| `samples` | C#、Python、C++ 整合範例。 |
| `docs` | 對外通訊協定與模擬器文件。 |

## 預設模擬器端點

| 介面 | 預設值 |
| --- | --- |
| REST | `http://127.0.0.1:5088` |
| TCP | `127.0.0.1:5089` |
| MQTT | `127.0.0.1:1883`，topic 前綴 `virex` |

## 快速開始

1. 建置並測試：

   ```powershell
   dotnet test Virex.NET.Integration.sln
   ```

2. 啟動模擬器：

   ```powershell
   dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
   ```

3. 在模擬器中按 **Start Servers**，啟動 REST/TCP/MQTT 服務。

4. 開啟另一個終端機執行 SDK 範例：

   ```powershell
   dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj
   ```

範例會依序執行初始化、更新 ProductInfo、啟動執行、等待執行完成、查詢結果。

## 主要 SDK 方法

- `GetStatusAsync`
- `GetProductInfoAsync`
- `SetProductInfoAsync`
- `InitializeAsync`
- `DeinitializeAsync`
- `StartAsync`
- `StopAsync`
- `QueryResultsAsync`

## 主要公開資料模型

| 資料模型 | 用途 |
| --- | --- |
| [ProductInfo](payloads/product/product-info.zh-Hant.md) | 執行與結果關聯使用的產品資訊。 |
| [SystemStatus](payloads/system/system-status.zh-Hant.md) | 目前生命週期狀態。 |
| [CommandResponse](payloads/commands/command-response.zh-Hant.md) | 命令接受或拒絕的結果。 |
| [ResultSummary](payloads/results/result-summary.zh-Hant.md) | 已完成執行的公開摘要資料。 |
| [ErrorInfo](payloads/system/error-info.zh-Hant.md) | 目前作用中的錯誤資訊。 |

這些是 JSON 資料模型。廠商可以使用 `Virex.NET.Contracts` 裡的 C# 類別，也可以在自己的語言裡定義等價模型。

## 參考文件

- [安裝 / 下載](installation.zh-Hant.md)
- [模擬器操作手冊](simulator.zh-Hant.md)
- [系統狀態機](state-machine.zh-Hant.md)
- [資料模型參考](payloads.zh-Hant.md)
- [REST API](rest-api.zh-Hant.md)
- [TCP Socket 通訊協定](tcp-socket.zh-Hant.md)
- [MQTT 事件](mqtt-events.zh-Hant.md)
- [範例程式](samples.zh-Hant.md)
