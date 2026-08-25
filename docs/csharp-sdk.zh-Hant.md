# C# SDK 指南


`Virex.NET.Client` 為公開 RESTful API、TCP、MQTT 整合 API 提供強型別封裝。

## 完整範例

需要包含所有 `using` 與 project reference、可直接執行的 C# 程式，請使用[C# SDK 範例](samples.zh-Hant.md)。本頁程式碼區塊只展示完整檔案中的個別操作。

## 安裝

```powershell
dotnet add package Virex.NET.Client
```

如果只需要公開資料模型與通訊協定輔助工具：

```powershell
dotnet add package Virex.NET.Contracts
```

## 建立用戶端

```csharp
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Virex.NET.Contracts;
using Virex.NET.Client;

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
```

## RESTful API 命令/查詢流程

```csharp
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
var status = await client.GetStatusAsync();

var initialize = await client.InitializeAsync();
if (!initialize.Accepted)
    throw new InvalidOperationException(initialize.Message);

await client.SetProductInfoAsync(new ProductInfo
{
    WaferID = "W01",
    LotID = "LOT-001",
    Recipe = "RCP-A",
    Slot = "1",
    FoupID = "FOUP-A",
    ChamberID = "CH-1",
});

var start = await client.StartAsync("golden-sample", ControlRunModes.Continue);
Console.WriteLine(start.State); // Running

var results = await client.QueryResultsAsync(lotID: "LOT-001");
```

## 復原與 Deinitialize 重試

如果來源或清理失敗需要復原，公開狀態會是 `Deinitializing`，回應/狀態會帶有 `recoveryAction: "Deinitialize"`。客戶端必須保持 Deinitialize 可操作；清理被拒絕時可再次重試，只有 Deinitialize 無法完成後，App 重啟才是 UI 層的最後手段：

```csharp
var status = await client.GetStatusAsync();
if (status.RecoveryAction == RecoveryActions.Deinitialize)
{
    var cleanup = await client.DeinitializeAsync();
    Console.WriteLine($"Recovery: accepted={cleanup.Accepted}, phase={cleanup.RecoveryPhase}, details={cleanup.RecoveryDetails}");
}
```

`Faulted` 與 `RequiresDeinitialize` 不是提供給客戶的生命週期狀態。請讀取 `state`、`recoveryAction` 與選用的復原內容欄位，不要等待內部狀態名稱。

## TCP 事件

```csharp
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
client.TcpEvents.EventReceived += (_, value) =>
{
    Console.WriteLine($"TCP {value.Type}");
};

using var cts = new CancellationTokenSource();
var tcpTask = client.TcpEvents.RunAsync(cts.Token);

await client.TcpEvents.SendProductInfoAsync(new ProductInfo
{
    WaferID = "W01",
    LotID = "LOT-TCP-001",
    Recipe = "RCP-A",
});

await client.TcpEvents.SendStartAsync("tcp-check", ControlRunModes.Continue);
```

當 `value.Type` 為 `imageGrabbed` 時讀取 `value.ImageGrabbed`。它的 `captureId` 會對應稍後的 `resultCreated`，路徑會在該事件提供。

## MQTT 事件

```csharp
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
client.MqttEvents.EventReceived += (_, value) =>
{
    Console.WriteLine($"MQTT {value.Type}");
};

using var cts = new CancellationTokenSource();
await client.MqttEvents.RunAsync(cts.Token);
```

MQTT 只用於事件。命令請使用 RESTful API 或 TCP。

當 `value.Type` 為 `imageGrabbed` 時讀取 `value.ImageGrabbed`。它的 `captureId` 會對應稍後的 `resultCreated`，路徑會在該事件提供。

## 錯誤處理

RESTful API 傳輸失敗與非成功 HTTP 回應會丟出 `VirexClientException`。通訊協定層級的拒絕會以 `CommandResponse` 表示：

```csharp
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
var response = await client.StartAsync();
if (!response.Accepted && response.ErrorCode == CommandErrorCodes.InvalidState)
{
    Console.WriteLine($"Start rejected in state {response.State}");
}
```

`invalid_state` 是正常的命令驗證行為，不是傳輸失敗。
