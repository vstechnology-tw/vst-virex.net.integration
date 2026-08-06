# MQTT イベント

MQTT は双方向の統合チャネルです。サービスは `virex/{eventName}` にイベントを発行します。クライアントは RESTful API に相当するコマンドとクエリを `virex/commands/...` に発行し、`virex/responses/{correlationId}` で対応する応答を受信できます。

## 基本情報

|項目 |値 |
| --- | --- |
|既定のブローカー | `127.0.0.1:1883` |
|既定のトピックプレフィックス | `virex` |
|トピックの形式 | `virex/{eventName}` |
|データ形式 | JSON |
|方向 |サービスがイベントを発行し、クライアントがコマンド/クエリを発行します |

**Start Servers** が押された後、シミュレーターは組み込みの MQTT ブローカーを開始します。ローカル クライアントには外部ブローカーは必要ありません。

## トピックの概要

|トピック |ペイロード |発行時 |
| --- | --- | --- |
| `virex/statusChanged` | [SystemStatus](payloads/system/system-status.ja.md) |公開状態が変化します。 |
| `virex/productInfoChanged` | [ProductInfo](payloads/product/product-info.ja.md) | ProductInfo のアップデートが完了しました。 |
| `virex/runStarted` | [SystemStatus](payloads/system/system-status.ja.md) |状態は `Running` になります。 |
| `virex/runCompleted` | [SystemStatus](payloads/system/system-status.ja.md) |実行は `Running` を出て、`Ready` に戻ります。 |
| `virex/resultCreated` | [ResultSummary](payloads/results/result-summary.ja.md) |結果の概要が作成されます。 |
| `virex/errorChanged` | [ErrorInfo](payloads/system/error-info.ja.md) |公開エラー情報が変更されます。 |
| `virex/commandRejected` | [CommandResponse](payloads/commands/command-response.ja.md) |コマンドは状態の規則または検証によって拒否されます。 |

## コマンドトピックの概要

各コマンド ペイロードには `correlationId` を含める必要があります。応答は `virex/responses/{correlationId}` に発行されます。

| RESTful API 相当 | MQTT コマンドトピック | 応答ペイロード フィールド |
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

## RecoveryAction とクライアントの復旧

`statusChanged`、`errorChanged`、`commandRejected` にはオプションの `recoveryAction` フィールドを含めることができます。取得エラーの後にクリーンアップが必要な場合、公開契約では `state: "Deinitializing"` と `recoveryAction: "Deinitialize"` を返します。内部状態 `Faulted` はクライアントには公開されません。

```json
{"state":"Deinitializing","recoveryAction":"Deinitialize"}
```

```json
{"hasError":true,"message":"Camera acquisition failed.","state":"Deinitializing","recoveryAction":"Deinitialize"}
```

```json
{"accepted":false,"state":"Deinitializing","command":"Start","errorCode":"requires_deinitialize","recoveryAction":"Deinitialize","message":"Deinitialize is required before another command can be accepted."}
```

クライアントは **Deinitialize** 操作を有効なままにし、サービスが `Uninitialized` を返すまで再試行してください。アプリケーションの再起動は、Deinitialize で復旧できない場合に限る UI の最終手段です。

## エラー処理

MQTT イベントには、HTTP ステータス コードがありません。不正な形式の JSON、不明なトピック、ブローカーの切断、およびサブスクリプションの失敗は、トランスポート層のエラーとして扱う必要があります。 `commandRejected` は、Virex.NET 互換サービスによって報告されるアプリケーション層の拒否です。
