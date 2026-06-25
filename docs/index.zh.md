# Virex.NET Integration Kit

Virex.NET Integration Kit 是提供給客戶端系統整合 Virex.NET 相容服務的公開 SDK、simulator、範例程式與通訊文件套件。本文件只描述公開通訊契約與 simulator 可驗證的行為。

此 repository 不包含私有 Virex.NET 應用程式。

## Simulator 預設端點

| 介面 | 預設值 |
| --- | --- |
| REST | `http://127.0.0.1:5088` |
| TCP | `127.0.0.1:5089` |
| MQTT | `127.0.0.1:1883`，base topic `virex` |
| SDK | `Virex.NET.Client` |

## 產品定位

使用此套件在連接 production-compatible service 之前，先建立並驗證客戶端整合流程。

| 區域 | 用途 |
| --- | --- |
| `Virex.NET.Contracts` | 公開 DTO、route、topic 名稱、TCP/NDJSON formatter 與 parser。 |
| `Virex.NET.Client` | C# REST command/query、TCP event、MQTT event client wrapper。 |
| `Virex.NET.Simulator.WPF` | 提供 REST、TCP、MQTT 端點的本機 simulator。 |
| `samples` | C#、Python、C++ guided demos。 |
| `docs` | 客戶文件與 raw protocol reference。 |

## 快速開始

第一次使用時，建議先跑 simulator 與 C# SDK sample。Sample 會引導測試者先按 **Start Servers**，示範 **Initialize** 前預期的 `not_initialized`，再走正常 cycle。

預先編譯 simulator 下載、NuGet packages 與 sample code 連結請看 [安裝 / 下載](installation.md)。

1. 建置並測試套件：

   ```powershell
   dotnet test Virex.NET.Integration.slnx
   ```

2. 啟動 simulator：

   ```powershell
   dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
   ```

3. 在 simulator 視窗中保留預設端點，按 **Start Servers**。

4. 在第二個 terminal 執行 SDK sample：

   ```powershell
   dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj
   ```

5. 依照 sample prompt 操作。它會先示範 `HTTP 409 not_initialized`，再請你按 **Initialize**，接著送 WaferInfo、start cycle 並查詢 result。

Event Log 預期範例：

```text
WaferInfo updated from REST: lotId=LOT-RAW-REST-001, waferId=W01, recipeId=RCP-A, slot=1, foupId=FOUP-A, chamberId=CH-1
WaferInfo updated from TCP: lotId=LOT-RAW-TCP-001, waferId=W01, recipeId=RCP-A, slot=1, foupId=FOUP-A, chamberId=CH-1
```

完整按鈕操作流程請看 [Simulator 操作手冊](simulator.md)。

## SDK 使用方式

C# 應用程式建議從 `VirexClient` 開始。它集中管理 REST、TCP、MQTT 設定，並提供整合常用操作。

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
    TimeoutMs = 5000,
    TcpFrameTimeoutMs = 5000,
});

var status = await client.GetStatusAsync();

// REST command/query helper 是 SDK 預設的高階路徑。
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
await client.StartAsync("golden-sample", ControlRunModes.Continue);

var results = await client.QueryResultsAsync(lotId: "LOT-001");

// TCP 要明確透過 TcpEvents 選用。
await client.TcpEvents.SendWaferInfoAsync(new WaferInfo
{
    LotId = "LOT-TCP-001",
    WaferId = "W01",
    RecipeId = "RCP-A",
});
await client.TcpEvents.SendStartAsync("golden-sample", ControlRunModes.Continue);

// TCP 與 MQTT event listener 也需要明確啟動。
using var eventCts = new CancellationTokenSource();
client.TcpEvents.EventReceived += (_, value) =>
    Console.WriteLine("TCP event: " + value.Type);
client.MqttEvents.EventReceived += (_, value) =>
    Console.WriteLine("MQTT event: " + value.Type);

_ = client.TcpEvents.RunAsync(eventCts.Token);
_ = client.MqttEvents.RunAsync(eventCts.Token);

// MQTT 只做 event，不用於 command/control。
// 應用程式不再監聽時呼叫 eventCts.Cancel()。
```

主要 SDK 方法：

- `GetStatusAsync`
- `SetWaferInfoAsync`
- `InitializeAsync`
- `TerminateAsync`
- `StartAsync`
- `StopAsync`
- `QueryResultsAsync`

REST 回應非成功狀態時會丟出 `VirexClientException`，其中包含 HTTP status code 與 response body。在 guided sample 中，**Initialize** 前看到 `HTTP 409 not_initialized` 是預期 simulator state，不是連線失敗。

`TcpFrameTimeoutMs` 用來保護 TCP/NDJSON partial frame 讀取。兩筆完整 frame 之間可以長時間 idle；但 client 一旦收到某筆 frame 的第一個 byte，該 frame 剩餘內容必須在設定時間內補齊並以 `\n` 結尾。

## 介面選擇

| 介面 | 適合情境 | 方向 | 典型用途 |
| --- | --- | --- | --- |
| REST | Command/query | Client to service | 讀 status、送 WaferInfo、控制 cycle、查詢 results。 |
| TCP / NDJSON | 直接 socket 整合 | 雙向 | 送 command frames，接收 status、wafer-info、result、error events。 |
| MQTT | Event subscription | 只做 outbound events | 透過 broker 訂閱 status、wafer-info、result、error。MQTT 不做 command/control。 |

完整 JSON body、欄位、topic 與 frame 請看 [傳送內容 / Payloads](payloads.md)。

## 典型使用情境

每個 use case 都要用 simulator UI state 與 sample output 一起驗證。先確認條件，再按指定 UI button，最後比對 console output 與 Event Log。

| Use case | Required state | UI action | Sample | Expected output |
| --- | --- | --- | --- | --- |
| 啟動通訊服務 | Simulator 已開啟 | 按 **Start Servers** | 任一 sample | Event Log 顯示 REST/TCP/MQTT listening；sample 可連線。 |
| `not_initialized` | 已按 **Start Servers**，尚未按 **Initialize** | 先不要按 **Initialize** | C# SDK、raw REST | Start 回 HTTP `409` / `not_initialized`。 |
| 正常 cycle | `processState=ready` | 按 **Initialize**，再讓 sample 繼續 | C# SDK、raw REST | 狀態依序經過 `capturing`、`inspecting`、`saving`、`ready`；產生 result。 |
| WaferInfo 更新驗證 | 已按 **Start Servers** | 按 **Apply WaferInfo** 或由 sample 送出 | SDK、REST、TCP | Event Log 同一行列出 `lotId`、`waferId`、`recipeId`、`slot`、`foupId`、`chamberId`。 |
| MQTT event 觀察 | MQTT sample 已訂閱 `virex/#` | 按 **Apply WaferInfo**、**Initialize**、**Start Cycle**、**Emit Fake Result**、**Emit Error** | Raw MQTT | Console 顯示 `wafer-info`、`status`、`result`、`error` topics。 |
| Result 查詢 | cycle 完成或已按 **Emit Fake Result** | 由 SDK/REST sample 執行 result query | SDK、REST | Console 顯示依 wafer context filter 後的 result count。 |

## 主要資料契約

| Contract | 公開欄位 |
| --- | --- |
| WaferInfo | `lotId`、`waferId`、`recipeId`、`slot`、`foupId`、`chamberId` |
| Status | `initialized`、`processState`、`recipe` |
| Result summary | `resultId`、`timestamp`、wafer context、`overallResult`、總 `defectCount`、result/image paths |
| Error | `hasError`、`message`、`initialized`、`processState`、`recipe` |

Result response 與 result event 只提供 summary，不包含 defect lists、die lists、crop lists、image binaries 或 private inspection details。

## 疑難排解

| 症狀 | 檢查方式 |
| --- | --- |
| REST request 失敗 | 確認 simulator 正在執行、`RestBaseUrl` 正確、防火牆允許連線，並檢查 `VirexClientException` response body。 |
| TCP events 沒收到 | 確認 host/port、每筆 NDJSON frame 都以換行結尾，且 event loop 持續執行。 |
| MQTT events 沒收到 | 確認 broker、port `1883`、base topic `virex` 與 subscribed topic tree。 |
| Result query 空白 | 確認 simulator 已產生 result，且 `lotId`、`waferId`、`recipeId` filters 與送出的 WaferInfo 一致。 |

## 參考資料

- [安裝 / 下載](installation.md)
- [Simulator 操作手冊](simulator.md)
- [狀態機](state-machine.md)
- [傳送內容 / Payloads](payloads.md)
- [範例程式](samples.md)
- [REST API](rest-api.md)
- [TCP Socket Protocol](tcp-socket.md)
- [MQTT Events](mqtt-events.md)
- [Protocol 版本規則](protocol-versioning.md)
