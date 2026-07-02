# MQTT 通訊協定

MQTT 是雙向整合通道。服務會把事件發布到 `virex/{eventName}`；用戶端可以把等價於 RESTful API 的命令與查詢發布到 `virex/commands/...`，並從 `virex/responses/{correlationId}` 接收對應回應。

## 基本資訊

| 項目 | 值 |
| --- | --- |
| 預設 broker | `127.0.0.1:1883` |
| 預設 topic 前綴 | `virex` |
| topic 格式 | 事件：`virex/{eventName}`；命令：`virex/commands/...`；回應：`virex/responses/{correlationId}` |
| 資料格式 | JSON |
| 方向 | 服務發布事件；用戶端發布命令與查詢要求 |

模擬器按 **Start Servers** 後會啟動內嵌 MQTT broker。本機用戶端不需要另外安裝外部 broker。

## 事件 Topic 總覽

| topic | 資料內容 | 發布時機 |
| --- | --- | --- |
| `virex/statusChanged` | [SystemStatus](payloads/system/system-status.zh-Hant.md) | 公開狀態改變。 |
| `virex/productInfoChanged` | [ProductInfo](payloads/product/product-info.zh-Hant.md) | ProductInfo 更新完成。 |
| `virex/runStarted` | [SystemStatus](payloads/system/system-status.zh-Hant.md) | 狀態進入 `Running`。 |
| `virex/runCompleted` | [SystemStatus](payloads/system/system-status.zh-Hant.md) | 一次執行離開 `Running` 並回到 `Ready`。 |
| `virex/resultCreated` | [ResultSummary](payloads/results/result-summary.zh-Hant.md) | 建立結果摘要。 |
| `virex/errorChanged` | [ErrorInfo](payloads/system/error-info.zh-Hant.md) | 公開錯誤狀態改變。 |
| `virex/commandRejected` | [CommandResponse](payloads/commands/command-response.zh-Hant.md) | 命令因狀態規則或驗證失敗被拒絕。 |

## 命令 Topic 總覽

每個命令或查詢 payload 建議包含 `correlationId`。服務會把回應發布到 `virex/responses/{correlationId}`。如果省略 `correlationId`，服務會自動產生一個，但用戶端通常應該自行提供，才能穩定訂閱並關聯回應。

| RESTful API 等價操作 | MQTT command topic | 回應 payload 欄位 |
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

## 命令要求 Envelope

所有 MQTT command topic 都接收 UTF-8 JSON 物件。共用欄位如下：

| 欄位 | 使用 topic | 說明 |
| --- | --- | --- |
| `correlationId` | 所有 command topic | 用戶端提供的 request id。回應會發布到 `virex/responses/{correlationId}`。 |
| `productInfo` | `commands/product-info/set` | 選用巢狀 [ProductInfo](payloads/product/product-info.zh-Hant.md)。服務也相容接受扁平 ProductInfo payload。 |
| `condition` | `commands/system/start` | 選用執行條件，會複製到結果摘要。 |
| `runMode` | `commands/system/start` | 選用執行模式。支援值請參考 [ControlRunModes](payloads/commands/control-run-modes.zh-Hant.md)。 |
| `reason` | `commands/system/stop` | 選用停止原因。 |
| `lotID` | `commands/results/query` | 選用結果查詢篩選條件。 |
| `waferID` | `commands/results/query` | 選用結果查詢篩選條件。 |
| `recipe` | `commands/results/query` | 選用結果查詢篩選條件。 |

要求範例：

```json
{"correlationId":"start-1","condition":"golden-sample","runMode":"continue"}
```

## 命令回應 Envelope

所有命令回應 payload 使用相同 envelope：

| 欄位 | 說明 |
| --- | --- |
| `correlationId` | 用來建立 response topic 的 request id。 |
| `topic` | base topic 底下的命令 topic，例如 `commands/status/get`。 |
| `accepted` | 命令或查詢被接受時為 `true`。對生命週期命令來說，這會對應 `commandResponse.accepted`。 |
| `errorCode` | MQTT 層級失敗時出現，例如 `unknown_topic`。 |
| `message` | 選用 MQTT 層級訊息。 |
| `status` | `commands/status/get` 的回應欄位。 |
| `error` | `commands/error/get` 的回應欄位。 |
| `productInfo` | `commands/product-info/get` 的回應欄位。 |
| `commandResponse` | initialize、set ProductInfo、start、stop、deinitialize 等改變狀態命令的回應欄位。 |
| `results` | `commands/results/query` 的回應欄位。 |

回應 topic 範例：

```text
virex/responses/start-1
```

回應 payload 範例：

```json
{"correlationId":"start-1","topic":"commands/system/start","accepted":true,"commandResponse":{"accepted":true,"state":"Running","command":"Start","message":"Started."}}
```

## 命令 Topic 詳細說明

### 查詢 status

發布到：

```text
virex/commands/status/get
```

要求 payload：

```json
{"correlationId":"status-1"}
```

回應：

```text
virex/responses/status-1
```

```json
{"correlationId":"status-1","topic":"commands/status/get","accepted":true,"status":{"state":"Ready"}}
```

### 查詢 error

發布到：

```text
virex/commands/error/get
```

要求 payload：

```json
{"correlationId":"error-1"}
```

回應 payload 欄位：`error`。

```json
{"correlationId":"error-1","topic":"commands/error/get","accepted":true,"error":{"hasError":false,"state":"Ready"}}
```

### 查詢 ProductInfo

發布到：

```text
virex/commands/product-info/get
```

要求 payload：

```json
{"correlationId":"product-get-1"}
```

回應 payload 欄位：`productInfo`。

```json
{"correlationId":"product-get-1","topic":"commands/product-info/get","accepted":true,"productInfo":{"lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1"}}
```

### 設定 ProductInfo

發布到：

```text
virex/commands/product-info/set
```

要求 payload：

```json
{"correlationId":"product-set-1","productInfo":{"lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1"}}
```

回應 payload 欄位：`commandResponse`。

```json
{"correlationId":"product-set-1","topic":"commands/product-info/set","accepted":true,"commandResponse":{"accepted":true,"state":"Ready","command":"SetProductInfo","message":"ProductInfo updated."}}
```

### Initialize

發布到：

```text
virex/commands/system/initialize
```

要求 payload：

```json
{"correlationId":"initialize-1"}
```

回應 payload 欄位：`commandResponse`。

### Start run

發布到：

```text
virex/commands/system/start
```

要求 payload：

```json
{"correlationId":"start-1","condition":"golden-sample","runMode":"continue"}
```

回應 payload 欄位：`commandResponse`。命令被接受後，服務也會發布 `statusChanged`、`runStarted`，稍後發布 `resultCreated` / `runCompleted` 事件。

### Stop run

發布到：

```text
virex/commands/system/stop
```

要求 payload：

```json
{"correlationId":"stop-1","reason":"operator-request"}
```

回應 payload 欄位：`commandResponse`。

### 查詢 results

發布到：

```text
virex/commands/results/query
```

要求 payload：

```json
{"correlationId":"results-1","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A"}
```

回應 payload 欄位：`results`。

```json
{"correlationId":"results-1","topic":"commands/results/query","accepted":true,"results":{"items":[],"count":0}}
```

### Deinitialize

發布到：

```text
virex/commands/system/deinitialize
```

要求 payload：

```json
{"correlationId":"deinitialize-1"}
```

回應 payload 欄位：`commandResponse`。

## 命令範例

=== "C# SDK"

    ```csharp
    var commands = new VirexMqttCommandClient(new VirexClientOptions
    {
        MqttHost = "127.0.0.1",
        MqttPort = 1883,
        MqttTopic = "virex",
    });

    var status = await commands.GetStatusAsync();
    var error = await commands.GetErrorAsync();
    var productInfo = await commands.GetProductInfoAsync();
    var results = await commands.QueryResultsAsync(lotID: "LOT-001");
    ```

=== "C# Raw"

    ```csharp
    var correlationId = "status-1";
    await client.SubscribeAsync($"virex/responses/{correlationId}");
    var message = new MqttApplicationMessageBuilder()
        .WithTopic("virex/commands/status/get")
        .WithPayload(JsonSerializer.Serialize(new { correlationId }))
        .Build();
    await client.PublishAsync(message);
    ```

=== "Python"

    ```python
    correlation_id = "status-1"
    client.subscribe(f"virex/responses/{correlation_id}")
    client.publish(
        "virex/commands/status/get",
        json.dumps({"correlationId": correlation_id}))
    ```

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

## 錯誤處理

MQTT 事件沒有 HTTP status code。JSON 格式錯誤、未知 topic、broker 斷線、訂閱失敗都應視為傳輸層錯誤。`commandRejected` 則是 Virex.NET 相容服務回報的應用層拒絕。
