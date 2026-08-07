# MQTT プロトコル

MQTT は双方向の統合チャネルです。サービスはイベントを `virex/{eventName}` に発行します。クライアントは RESTful API と同等のコマンドやクエリを `virex/commands/...` に発行し、対応する応答を `virex/responses/{correlationId}` で受け取れます。

## 基本情報

|項目 |値 |
| --- | --- |
|既定のブローカー | `127.0.0.1:1883` |
|既定のトピックプレフィックス | `virex` |
|トピックの形式 |イベント: `virex/{eventName}`、コマンド: `virex/commands/...`、応答: `virex/responses/{correlationId}` |
|データ形式 | JSON |
|方向 |サービスがイベントを発行し、クライアントがコマンド/クエリ要求を発行します |

**Start Servers** が押された後、シミュレーターは組み込みの MQTT ブローカーを開始します。ローカル クライアントには外部ブローカーは必要ありません。

## イベント トピックの概要

|トピック |ペイロード |発行時 |
| --- | --- | --- |
| `virex/statusChanged` | [SystemStatus](payloads/system/system-status.ja.md) |公開状態が変化します。 |
| `virex/productInfoChanged` | [ProductInfo](payloads/product/product-info.ja.md) | ProductInfo のアップデートが完了しました。 |
| `virex/runStarted` | [SystemStatus](payloads/system/system-status.ja.md) |状態は `Running` になります。 |
| `virex/runCompleted` | [SystemStatus](payloads/system/system-status.ja.md) |実行は `Running` を出て、`Ready` に戻ります。 |
| `virex/resultCreated` | [ResultSummary](payloads/results/result-summary.ja.md) |結果の概要が作成されます。 |
| `virex/errorChanged` | [ErrorInfo](payloads/system/error-info.ja.md) |公開エラー情報が変更されます。 |
| `virex/commandRejected` | [CommandResponse](payloads/commands/command-response.ja.md) |コマンドは状態の規則または検証によって拒否されます。 |

## コマンド トピックの概要

各コマンドまたはクエリの payload には `correlationId` を含めることを推奨します。応答は `virex/responses/{correlationId}` に発行されます。`correlationId` を省略した場合、サービスが生成しますが、クライアントは通常、自分で指定して応答を確実に関連付けます。

| RESTful API 相当 | MQTT コマンド topic | 応答 payload フィールド |
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

## コマンド要求 envelope

すべての MQTT コマンド topic は UTF-8 JSON オブジェクトを受け取ります。共通フィールドは次のとおりです。

|フィールド |使用 topic |説明 |
| --- | --- | --- |
| `correlationId` |すべてのコマンド topic |クライアント指定の request id。応答は `virex/responses/{correlationId}` に発行されます。 |
| `productInfo` | `commands/product-info/set` |任意のネストされた [ProductInfo](payloads/product/product-info.ja.md)。互換性のため、フラットな ProductInfo payload も受け付けます。 |
| `condition` | `commands/system/start` |任意の実行条件。結果サマリーにコピーされます。 |
| `runMode` | `commands/system/start` |任意の実行モード。対応値は [ControlRunModes](payloads/commands/control-run-modes.ja.md) を参照してください。 |
| `reason` | `commands/system/stop` |任意の停止理由。 |
| `lotID` | `commands/results/query` |任意の結果クエリ フィルター。 |
| `waferID` | `commands/results/query` |任意の結果クエリ フィルター。 |
| `recipe` | `commands/results/query` |任意の結果クエリ フィルター。 |

要求例:

```json
{"correlationId":"start-1","condition":"golden-sample","runMode":"continue"}
```

## コマンド応答 envelope

すべてのコマンド応答 payload は同じ envelope を使用します。

|フィールド |説明 |
| --- | --- |
| `correlationId` |応答 topic の作成に使用された request id。 |
| `topic` |base topic 配下のコマンド topic。例: `commands/status/get`。 |
| `accepted` |コマンド/クエリが受け付けられた場合は `true`。ライフサイクル コマンドでは `commandResponse.accepted` と対応します。 |
| `errorCode` |`unknown_topic` など、MQTT レベルの失敗時に出現します。 |
| `message` |任意の MQTT レベル メッセージ。 |
| `status` |`commands/status/get` の応答フィールド。 |
| `error` |`commands/error/get` の応答フィールド。 |
| `productInfo` |`commands/product-info/get` の応答フィールド。 |
| `commandResponse` |initialize、set ProductInfo、start、stop、deinitialize など状態変更コマンドの応答フィールド。 |
| `results` |`commands/results/query` の応答フィールド。 |

応答 topic 例:

```text
virex/responses/start-1
```

応答 payload 例:

```json
{"correlationId":"start-1","topic":"commands/system/start","accepted":true,"commandResponse":{"accepted":true,"state":"Running","command":"Start","message":"Started."}}
```

## コマンド topic の詳細

### status のクエリ

発行先:

```text
virex/commands/status/get
```

要求 payload:

```json
{"correlationId":"status-1"}
```

応答:

```text
virex/responses/status-1
```

```json
{"correlationId":"status-1","topic":"commands/status/get","accepted":true,"status":{"state":"Ready"}}
```

### error のクエリ

発行先:

```text
virex/commands/error/get
```

要求 payload:

```json
{"correlationId":"error-1"}
```

応答 payload フィールド: `error`。

```json
{"correlationId":"error-1","topic":"commands/error/get","accepted":true,"error":{"hasError":false,"state":"Ready"}}
```

### ProductInfo のクエリ

発行先:

```text
virex/commands/product-info/get
```

要求 payload:

```json
{"correlationId":"product-get-1"}
```

応答 payload フィールド: `productInfo`。

```json
{"correlationId":"product-get-1","topic":"commands/product-info/get","accepted":true,"productInfo":{"lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1"}}
```

### ProductInfo の設定

発行先:

```text
virex/commands/product-info/set
```

要求 payload:

```json
{"correlationId":"product-set-1","productInfo":{"lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1"}}
```

応答 payload フィールド: `commandResponse`。

```json
{"correlationId":"product-set-1","topic":"commands/product-info/set","accepted":true,"commandResponse":{"accepted":true,"state":"Ready","command":"SetProductInfo","message":"ProductInfo updated."}}
```

### Initialize

発行先:

```text
virex/commands/system/initialize
```

要求 payload:

```json
{"correlationId":"initialize-1"}
```

応答 payload フィールド: `commandResponse`。

### Start run

発行先:

```text
virex/commands/system/start
```

要求 payload:

```json
{"correlationId":"start-1","condition":"golden-sample","runMode":"continue"}
```

応答 payload フィールド: `commandResponse`。コマンドが受け付けられた後、サービスは `statusChanged`、`runStarted`、その後 `resultCreated` / `runCompleted` イベントも発行します。

### Stop run

発行先:

```text
virex/commands/system/stop
```

要求 payload:

```json
{"correlationId":"stop-1","reason":"operator-request"}
```

応答 payload フィールド: `commandResponse`。

### results のクエリ

発行先:

```text
virex/commands/results/query
```

要求 payload:

```json
{"correlationId":"results-1","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A"}
```

応答 payload フィールド: `results`。

```json
{"correlationId":"results-1","topic":"commands/results/query","accepted":true,"results":{"items":[],"count":0}}
```

### Deinitialize

発行先:

```text
virex/commands/system/deinitialize
```

要求 payload:

```json
{"correlationId":"deinitialize-1"}
```

応答 payload フィールド: `commandResponse`。

## コマンド例

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

## サブスクリプションの例

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
    // Subscribe to virex/# with the MQTT client library used by your project.
    // Each message payload is UTF-8 JSON.
    OnMqttMessage([](const std::string& topic, const std::string& payload)
    {
        std::cout << topic << ": " << payload << std::endl;
    });
    ```

## statusChanged

### 目的

公開システムの状態が変更されたことをクライアントに通知します。

### トピック

```text
virex/statusChanged
```

### ペイロード

[SystemStatus](payloads/system/system-status.ja.md)

### 例

```json
{"state":"Ready"}
```

### 注記

クライアントはこのイベントを使用して UI 状態を更新し、次のコマンドが有効かどうかを判断できます。

## productInfoChanged

### 目的

`POST /api/product-info` または TCP ProductInfo コマンドが完了したことをクライアントに通知します。

### トピック

```text
virex/productInfoChanged
```

### ペイロード

[ProductInfo](payloads/product/product-info.ja.md)

### 例

```json
{"lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1"}
```

### 注記

このイベントには ProductInfo のみが含まれます。結果データは含まれません。

## runStarted

### 目的

`Start` コマンドが受け入れられ、システムが `Running` に入ったことをクライアントに通知します。

### トピック

```text
virex/runStarted
```

### ペイロード

[SystemStatus](payloads/system/system-status.ja.md)

### 例

```json
{"state":"Running"}
```

### 注記

実行が完了する前に、`Start` が応答します。クライアントは、`resultCreated`、`runCompleted` を待つか、[GET /api/results](rest-api.ja.md#get-apiresults) をクエリする必要があります。

## runCompleted

### 目的

現在の実行が終了し、公開状態が `Ready` に戻ったことをクライアントに通知します。

### トピック

```text
virex/runCompleted
```

### ペイロード

[SystemStatus](payloads/system/system-status.ja.md)

### 例

```json
{"state":"Ready"}
```

### 注記

これは、実行ライフサイクルの完了を表します。結果の詳細は `resultCreated` によって配信されます。

## resultCreated

### 目的

公開結果概要が作成されたことをクライアントに通知します。

### トピック

```text
virex/resultCreated
```

### ペイロード

[ResultSummary](payloads/results/result-summary.ja.md)

### 例

```json
{"resultId":"RID-1","timestamp":"2026-06-20T15:30:12+08:00","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1","condition":"golden-sample","overallResult":"OK","defectCount":0}
```

### 注記

結果には、ProductInfo スナップショットと、`Start` が受け入れられたときにキャプチャされた `condition` が含まれます。これは概要のみを提供し、欠陥リスト、クロップ リスト、イメージ バイナリ、または非公開検査の内部情報は含まれません。

## errorChanged

### 目的

公開エラー情報が変更されたことをクライアントに通知します。

### トピック

```text
virex/errorChanged
```

### ペイロード

[ErrorInfo](payloads/system/error-info.ja.md)

### 例

```json
{"hasError":true,"message":"Camera timeout.","state":"Running"}
```

### 注記

このイベントは公開エラー情報のみを報告します。接続の中断、ブローカーの利用不可、およびサブスクリプションの失敗は、MQTT クライアント接続レベルのエラーです。

## commandRejected

### 目的

コマンドが状態ルールまたは検証によって拒否されたことをクライアントに通知します。

### トピック

```text
virex/commandRejected
```

### ペイロード

[CommandResponse](payloads/commands/command-response.ja.md)

### 例

```json
{"accepted":false,"state":"Running","command":"SetProductInfo","errorCode":"invalid_state","message":"SetProductInfo is not valid while state is Running."}
```

### 注記

このイベントを使用して、拒否された RESTful API、TCP、または UI コマンドを関連付けます。すべてのトランスポートは同じ状態ルールを使用します。

## エラー処理

MQTT イベントには、HTTP ステータス コードがありません。不正な形式の JSON、不明なトピック、ブローカーの切断、およびサブスクリプションの失敗は、トランスポート層のエラーとして扱う必要があります。 `commandRejected` は、Virex.NET 互換サービスによって報告されるアプリケーション層の拒否です。
