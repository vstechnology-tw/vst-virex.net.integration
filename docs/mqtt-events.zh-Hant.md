# MQTT 事件

MQTT 是雙向整合通道。服務會發布事件到 `virex/{eventName}`；用戶端可以發布 RESTful API 對應的命令與查詢到 `virex/commands/...`，並在 `virex/responses/{correlationId}` 收到對應回應。

## 基本資訊

| 項目 | 值 |
| --- | --- |
| 預設 broker | `127.0.0.1:1883` |
| 預設 topic 前綴 | `virex` |
| topic 格式 | `virex/{eventName}` |
| 資料格式 | JSON |
| 方向 | 服務發布事件；用戶端發布命令／查詢 |

模擬器按 **Start Servers** 後會啟動內嵌 MQTT broker。本機用戶端不需要另外安裝外部 broker。

## 主題總覽

| topic | 資料內容 | 發布時機 |
| --- | --- | --- |
| `virex/statusChanged` | [SystemStatus](payloads/system/system-status.zh-Hant.md) | 公開狀態改變。 |
| `virex/productInfoChanged` | [ProductInfo](payloads/product/product-info.zh-Hant.md) | ProductInfo 更新完成。 |
| `virex/runStarted` | [SystemStatus](payloads/system/system-status.zh-Hant.md) | 狀態進入 `Running`。 |
| `virex/runCompleted` | [SystemStatus](payloads/system/system-status.zh-Hant.md) | 一次執行離開 `Running` 並回到 `Ready`。 |
| `virex/resultCreated` | [ResultSummary](payloads/results/result-summary.zh-Hant.md) | 建立結果摘要。 |
| `virex/errorChanged` | [ErrorInfo](payloads/system/error-info.zh-Hant.md) | 公開錯誤狀態改變。 |
| `virex/commandRejected` | [CommandResponse](payloads/commands/command-response.zh-Hant.md) | 命令因狀態規則或驗證失敗被拒絕。 |

## 命令 topic 總覽

每個命令 payload 都應包含 `correlationId`。回應會發布到 `virex/responses/{correlationId}`。

| RESTful API 對應 | MQTT 命令 topic | 回應 payload 欄位 |
| --- | --- | --- |
| `GET /api/status` | `virex/commands/status/get` | `status` |
| `GET /api/error` | `virex/commands/error/get` | `error` |
| `GET /api/product-info` | `virex/commands/product-info/get` | `productInfo` |
| `POST /api/product-info` | `virex/commands/product-info/set` | `commandResponse` |
| `POST /api/system/initialize` | `virex/commands/system/initialize` | `commandResponse` |
| `POST /api/system/deinitialize` | `virex/commands/system/deinitialize` | `commandResponse` |
| `POST /api/system/start` | `virex/commands/system/start` | `commandResponse` |
| `POST /api/system/stop` | `virex/commands/system/stop` | `commandResponse` |
| `GET /api/results` | `virex/commands/results/query` | `results` |

## 訂閱範例

=== "C# SDK"

    ```csharp
    var subscriber = new VirexMqttEventSubscriber(new VirexClientOptions
    {
        MqttHost = "127.0.0.1",
        MqttPort = 1883,
        MqttTopic = "virex",
    });

    subscriber.EventReceived += (_, e) =>
    {
        Console.WriteLine(e.Type);
    };

    await subscriber.RunAsync(cancellationToken);
    ```

=== "C# Raw"

    ```csharp
    var factory = new MqttFactory();
    using var client = factory.CreateMqttClient();
    client.ApplicationMessageReceivedAsync += e =>
    {
        var json = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
        Console.WriteLine($"{e.ApplicationMessage.Topic}: {json}");
        return Task.CompletedTask;
    };

    var options = new MqttClientOptionsBuilder()
        .WithTcpServer("127.0.0.1", 1883)
        .Build();

    await client.ConnectAsync(options);
    await client.SubscribeAsync("virex/#");
    ```

=== "Python"

    ```python
    import paho.mqtt.client as mqtt

    def on_message(client, userdata, message):
        print(message.topic, message.payload.decode("utf-8"))

    client = mqtt.Client()
    client.on_message = on_message
    client.connect("127.0.0.1", 1883)
    client.subscribe("virex/#")
    client.loop_forever()
    ```

=== "C++"

    ```cpp
    // 使用專案採用的 MQTT 用戶端函式庫訂閱 virex/#。
    // 每個訊息 payload 都是 UTF-8 JSON。
    OnMqttMessage([](const std::string& topic, const std::string& payload)
    {
        std::cout << topic << ": " << payload << std::endl;
    });
    ```

## statusChanged

### 用途

通知用戶端公開系統狀態已改變。

### Topic

```text
virex/statusChanged
```

### 資料內容

[SystemStatus](payloads/system/system-status.zh-Hant.md)

### 範例

```json
{"state":"Ready"}
```

### 說明

用戶端可用這個事件更新 UI 狀態，並判斷下一個命令是否有效。

## productInfoChanged

### 用途

通知用戶端 `POST /api/product-info` 或 TCP ProductInfo 命令已完成。

### Topic

```text
virex/productInfoChanged
```

### 資料內容

[ProductInfo](payloads/product/product-info.zh-Hant.md)

### 範例

```json
{"lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1"}
```

### 說明

這個事件只包含公開 ProductInfo，不包含結果資料。

## runStarted

### 用途

通知用戶端 `Start` 命令已被接受，且系統已進入 `Running`。

### Topic

```text
virex/runStarted
```

### 資料內容

[SystemStatus](payloads/system/system-status.zh-Hant.md)

### 範例

```json
{"state":"Running"}
```

### 說明

`Start` 會在執行完成前先回應。用戶端應等待 `resultCreated`、`runCompleted`，或查詢 [GET /api/results](rest-api.zh-Hant.md#get-apiresults)。

## runCompleted

### 用途

通知用戶端目前執行已結束，公開狀態已回到 `Ready`。

### Topic

```text
virex/runCompleted
```

### 資料內容

[SystemStatus](payloads/system/system-status.zh-Hant.md)

### 範例

```json
{"state":"Ready"}
```

### 說明

這代表執行生命週期完成。結果細節由 `resultCreated` 傳遞。

## resultCreated

### 用途

通知用戶端已建立公開結果摘要。

### Topic

```text
virex/resultCreated
```

### 資料內容

[ResultSummary](payloads/results/result-summary.zh-Hant.md)

### 範例

```json
{"resultId":"RID-1","timestamp":"2026-06-20T15:30:12+08:00","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1","condition":"golden-sample","overallResult":"OK","defectCount":0}
```

### 說明

結果包含 `Start` 被接受當下保存的 ProductInfo 快照與 `condition`。它只提供摘要，不包含瑕疵清單、裁切清單、影像二進位資料或私有檢測內部資料。

## errorChanged

### 用途

通知用戶端公開錯誤狀態已改變。

### Topic

```text
virex/errorChanged
```

### 資料內容

[ErrorInfo](payloads/system/error-info.zh-Hant.md)

### 範例

```json
{"hasError":true,"message":"Camera timeout.","state":"Running"}
```

### 說明

這個事件只回報公開錯誤狀態。連線中斷、broker 不可用、訂閱失敗屬於 MQTT 用戶端連線層錯誤。

## commandRejected

### 用途

通知用戶端命令因狀態規則或驗證失敗被拒絕。

### Topic

```text
virex/commandRejected
```

### 資料內容

[CommandResponse](payloads/commands/command-response.zh-Hant.md)

### 範例

```json
{"accepted":false,"state":"Running","command":"SetProductInfo","errorCode":"invalid_state","message":"SetProductInfo is not valid while state is Running."}
```

### 說明

可用這個事件關聯 RESTful API、TCP 或 UI 命令被拒絕的情境。所有傳輸方式都使用相同狀態規則。

## RecoveryAction 與用戶端復原

`statusChanged`、`errorChanged` 與 `commandRejected` 都可能包含選填的 `recoveryAction` 欄位。擷取失敗需要清理時，公開協定會回報 `state: "Deinitializing"` 與 `recoveryAction: "Deinitialize"`；內部的 `Faulted` 狀態不會提供給客戶端。

```json
{"state":"Deinitializing","recoveryAction":"Deinitialize"}
```

```json
{"hasError":true,"message":"Camera acquisition failed.","state":"Deinitializing","recoveryAction":"Deinitialize"}
```

```json
{"accepted":false,"state":"Deinitializing","command":"Start","errorCode":"requires_deinitialize","recoveryAction":"Deinitialize","message":"Deinitialize is required before another command can be accepted."}
```

用戶端應保持 **Deinitialize** 操作可按，直到服務回傳 `Uninitialized`。只有 Deinitialize 無法復原服務時，App 重啟才是 UI 層的最後手段。

事件可以包含選填的 `recoveryStartedAt`、`recoverySource`、`recoveryPhase`、已清理的
`recoveryDetails`；錯誤與拒絕回應也可以包含穩定的 `errorCode`。用戶端應忽略未知的
additive 欄位。

## 錯誤處理

MQTT 事件沒有 HTTP status code。JSON 格式錯誤、未知 topic、broker 斷線、訂閱失敗都應視為傳輸層錯誤。`commandRejected` 則是 Virex.NET 相容服務回報的應用層拒絕。
