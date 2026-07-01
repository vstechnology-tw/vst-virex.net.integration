# Virex.NET 整合套件

Virex.NET 整合套件提供外部系統整合 Virex.NET 相容服務時需要的公開合約、模擬器、範例與文件。這個儲存庫只包含外部可見的整合合約，不包含正式產品的私有實作細節。

模擬器與正式 Virex.NET 端點應該實作同一份 `Virex.NET.Contracts`。外部廠商如果能透過 `Virex.NET.Simulator.WPF` 完成整合，切到正式端點時理論上只需要調整端點設定。

## 通訊界面

Virex.NET 透過三種通訊界面提供同一組業務整合能力。客戶應選擇其中一種界面作為主要整合方式，並依該界面的文件實作相同的 commands、events 與 payload 行為。

| 通訊界面 | 預設模擬器端點 | 適用情境 | 文件 |
| --- | --- | --- | --- |
| RESTful API | `http://127.0.0.1:5088` | 系統偏好 HTTP request/response，並可用 polling 查詢狀態或結果。 | [RESTful API](rest-api.zh-Hant.md) |
| TCP Socket | `127.0.0.1:5089` | 系統需要透過持久 socket 收送雙向 NDJSON。 | [TCP Socket 通訊協定](tcp-socket.zh-Hant.md) |
| MQTT | `127.0.0.1:1883`，root topic `virex` | 系統已採用 broker 型 command、response、event 訊息架構。 | [MQTT 通訊協定](mqtt-events.zh-Hant.md) |

## 共同業務功能

RESTful API、TCP Socket、MQTT 都提供同一組公開業務功能。通訊名稱會不同，但生命週期規則、狀態限制、command response、event payload、result payload 都應保持一致。客戶應選擇一種通訊界面完成完整流程；除非有明確的營運理由，否則不建議混用多種通訊界面。

### Commands 與查詢

| 業務功能 | RESTful API | TCP Socket | MQTT |
| --- | --- | --- | --- |
| 查詢 status | `GET /api/status` | `{"type":"status"}` | `virex/commands/status/get` |
| 查詢 error | `GET /api/error` | `{"type":"error"}` | `virex/commands/error/get` |
| 查詢 ProductInfo | `GET /api/product-info` | `{"type":"getProductInfo"}` | `virex/commands/product-info/get` |
| Initialize | `POST /api/system/initialize` | `{"type":"initialize"}` | `virex/commands/system/initialize` |
| Set ProductInfo | `POST /api/product-info` | `{"type":"productInfo", ...}` | `virex/commands/product-info/set` |
| Start run | `POST /api/system/start` | `{"type":"start", ...}` | `virex/commands/system/start` |
| Stop run | `POST /api/system/stop` | `{"type":"stop", ...}` | `virex/commands/system/stop` |
| Query results | `GET /api/results` | `{"type":"results", ...}` | `virex/commands/results/query` |
| Deinitialize | `POST /api/system/deinitialize` | `{"type":"deinitialize"}` | `virex/commands/system/deinitialize` |

### Events

| Event | 說明 |
| --- | --- |
| `statusChanged` | 公開生命週期狀態改變。 |
| `productInfoChanged` | ProductInfo 更新完成。 |
| `runStarted` | run 進入 `Running`。 |
| `runCompleted` | run 回到 `Ready`。 |
| `resultCreated` | 已建立公開 result summary。 |
| `errorChanged` | 公開錯誤資訊改變。 |
| `commandRejected` | command 因狀態規則或驗證失敗被拒絕。 |

RESTful API 以 HTTP route 表示 commands，不提供 event stream；client 可透過 polling 觀察狀態與結果。TCP Socket 與 MQTT 會以推送訊息提供同一組 events。

## 基本封包格式

三種通訊界面都使用 UTF-8 JSON payload，並共用同一組公開資料模型。請先閱讀下列文件：

| 格式範圍 | 文件 |
| --- | --- |
| JSON model 規則與共用 payload | [資料模型參考](payloads.zh-Hant.md) |
| RESTful API route、HTTP body、response code | [RESTful API](rest-api.zh-Hant.md) |
| TCP NDJSON frame 格式與 frame type | [TCP Socket 通訊協定](tcp-socket.zh-Hant.md) |
| MQTT topic、command request envelope、correlated response | [MQTT 通訊協定](mqtt-events.zh-Hant.md) |

## 專案目的

| 區域 | 用途 |
| --- | --- |
| `Virex.NET.Contracts` | 公開資料模型、RESTful API 路由、MQTT topic 名稱、TCP/NDJSON 格式化與解析工具。 |
| `Virex.NET.Client` | C# SDK 包裝層，提供 RESTful API commands/queries、TCP socket 通訊、MQTT command/event 通訊。 |
| `Virex.NET.Simulator.Core` | 模擬器專用狀態機與工作階段行為，供本機模擬器使用。 |
| `Virex.NET.Simulator.WPF` | 本機模擬器，對外提供公開 RESTful API/TCP/MQTT 合約。 |
| `samples` | C#、Python、C++ 整合範例。 |
| `docs` | 對外通訊協定與模擬器文件。 |

## 快速開始

1. 建置並測試：

   ```powershell
   dotnet test Virex.NET.Integration.slnx
   ```

2. 啟動模擬器：

   ```powershell
   dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
   ```

3. 在模擬器中按 **Start Servers**，啟動 RESTful API/TCP/MQTT 服務。

4. 開啟另一個終端機執行 SDK 範例：

   ```powershell
   dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj
   ```

範例會遵循相同的 13-step demo flow：查詢 status、查詢 error、查詢 ProductInfo、initialize、確認 Ready、設定 ProductInfo、確認 ProductInfo、start run、觀察 run 活動、stop run、查詢 results、deinitialize、確認 Uninitialized。

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
- [RESTful API](rest-api.zh-Hant.md)
- [TCP Socket 通訊協定](tcp-socket.zh-Hant.md)
- [MQTT 通訊協定](mqtt-events.zh-Hant.md)
- [範例程式](samples.zh-Hant.md)
