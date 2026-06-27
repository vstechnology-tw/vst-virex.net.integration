# MQTT 事件

MQTT 是 Virex.NET 相容服務傳給整合 Client 的傳出事件通道，不用於命令或查詢。

## 基本資訊

| 項目 | 值 |
| --- | --- |
| 預設 broker | `127.0.0.1:1883` |
| 預設根主題 | `virex` |
| 主題格式 | `virex/{eventName}` |
| 資料格式 | JSON |
| 方向 | 服務發佈，Client 訂閱 |

模擬器按 **Start Servers** 後會啟動內嵌 MQTT broker。本機 Client 不需要另外安裝外部 broker。

## 主題總覽

| 主題 | Payload | 發佈時機 |
| --- | --- | --- |
| `virex/statusChanged` | [SystemStatus](payloads/system/system-status.md) | 公開狀態改變。 |
| `virex/productInfoChanged` | [ProductInfo](payloads/product/product-info.md) | ProductInfo 更新完成。 |
| `virex/runStarted` | [SystemStatus](payloads/system/system-status.md) | 狀態進入 `Running`。 |
| `virex/runCompleted` | [SystemStatus](payloads/system/system-status.md) | 一次執行離開 `Running` 並回到 `Ready`。 |
| `virex/resultCreated` | [ResultSummary](payloads/results/result-summary.md) | 建立結果摘要。 |
| `virex/errorChanged` | [ErrorInfo](payloads/system/error-info.md) | 公開錯誤狀態改變。 |
| `virex/commandRejected` | [CommandResponse](payloads/commands/command-response.md) | 命令因狀態規則或驗證失敗被拒絕。 |

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
    // 使用專案採用的 MQTT client library 訂閱 virex/#。
    // 每個 message payload 都是 UTF-8 JSON。
    OnMqttMessage([](const std::string& topic, const std::string& payload)
    {
        std::cout << topic << ": " << payload << std::endl;
    });
    ```

## statusChanged

### 用途

通知 Client 公開系統狀態已改變。

### 主題

```text
virex/statusChanged
```

### Payload

[SystemStatus](payloads/system/system-status.md)

### 範例

```json
{"state":"Ready"}
```

### 說明

Client 可用這個事件更新 UI 狀態，並判斷下一個命令是否有效。

## productInfoChanged

### 用途

通知 Client `POST /api/product-info` 或 TCP ProductInfo 命令已完成。

### 主題

```text
virex/productInfoChanged
```

### Payload

[ProductInfo](payloads/product/product-info.md)

### 範例

```json
{"lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1"}
```

### 說明

這個事件只包含公開 ProductInfo，不包含結果資料。

## runStarted

### 用途

通知 Client `Start` 命令已被接受，且系統已進入 `Running`。

### 主題

```text
virex/runStarted
```

### Payload

[SystemStatus](payloads/system/system-status.md)

### 範例

```json
{"state":"Running"}
```

### 說明

`Start` 會在執行完成前先回應。Client 應等待 `resultCreated`、`runCompleted`，或查詢 [GET /api/results](rest-api.md#get-apiresults)。

## runCompleted

### 用途

通知 Client 目前執行已結束，公開狀態已回到 `Ready`。

### 主題

```text
virex/runCompleted
```

### Payload

[SystemStatus](payloads/system/system-status.md)

### 範例

```json
{"state":"Ready"}
```

### 說明

這代表執行生命週期完成。結果細節由 `resultCreated` 傳遞。

## resultCreated

### 用途

通知 Client 已建立公開結果摘要。

### 主題

```text
virex/resultCreated
```

### Payload

[ResultSummary](payloads/results/result-summary.md)

### 範例

```json
{"resultId":"RID-1","timestamp":"2026-06-20T15:30:12+08:00","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1","condition":"golden-sample","overallResult":"OK","defectCount":0}
```

### 說明

結果包含 `Start` 被接受當下保存的 ProductInfo 快照與 `condition`。它只提供摘要，不包含瑕疵清單、裁切清單、影像二進位資料或私有檢測內部資料。

## errorChanged

### 用途

通知 Client 公開錯誤狀態已改變。

### 主題

```text
virex/errorChanged
```

### Payload

[ErrorInfo](payloads/system/error-info.md)

### 範例

```json
{"hasError":true,"message":"Camera timeout.","state":"Running"}
```

### 說明

這個事件只回報公開錯誤狀態。連線中斷、broker 不可用、訂閱失敗屬於 MQTT client 連線層錯誤。

## commandRejected

### 用途

通知 Client 命令因狀態規則或驗證失敗被拒絕。

### 主題

```text
virex/commandRejected
```

### Payload

[CommandResponse](payloads/commands/command-response.md)

### 範例

```json
{"accepted":false,"state":"Running","command":"SetProductInfo","errorCode":"invalid_state","message":"SetProductInfo is not valid while state is Running."}
```

### 說明

可用這個事件關聯 REST、TCP 或 UI 命令被拒絕的情境。所有 transport 使用相同狀態規則。

## 錯誤處理

MQTT 事件沒有 HTTP status code。JSON 格式錯誤、未知主題、broker 斷線、訂閱失敗都應視為 transport 層錯誤。`commandRejected` 則是 Virex.NET 相容服務回報的應用層拒絕。
