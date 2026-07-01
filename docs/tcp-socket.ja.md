# TCP ソケットプロトコル

TCP ソケットは、同じ単純なストリーミング プロトコルを介してコマンドを送信し、イベントを受信する必要があるクライアント用の双方向統合チャネルです。

## 基本情報

|項目 |値 |
| --- | --- |
|既定のホスト | `127.0.0.1` |
|既定のポート | `5089` |
|フレーミング方法 | NDJSON |
|エンコーディング | UTF-8 |
|方向 |クライアントはコマンド フレームを送信します。サービスはイベントフレームを送信します |

各フレームは JSON オブジェクトで、`\n` で終わります。

```text
{"type":"start","condition":"golden-sample","runMode":"continue"}\n
```

TCP/NDJSON を読み取る場合、C# SDK はフレームごとにアイドルタイムアウトを適用します。完全なフレーム間には長い待ち時間が発生する可能性がありますが、フレームのいずれかのバイトが到着すると、残りのコンテンツと末尾の改行は `VirexClientOptions.TcpFrameTimeoutMs` 内に到着する必要があります。それ以外の場合、TCP イベント リーダーはタイムアウトを報告します。

## フレームの概要

### 受信コマンド

|フレームタイプ |ペイロード |有効な状態 |結果 |
| --- | --- | --- | --- |
| `initialize` | [SystemInitializeRequest](payloads/commands/system-initialize-request.ja.md) と `type` | `Uninitialized` | `Initializing` に遷移します。完了後、`Ready` の `statusChanged` を送信します。 |
| `deinitialize` | [SystemDeinitializeRequest](payloads/commands/system-deinitialize-request.ja.md) と `type` | `Ready` | `Deinitializing` に遷移します。完了後、`Uninitialized` の `statusChanged` を送信します。 |
| `productInfo` | [ProductInfo](payloads/product/product-info.ja.md) と `type` | `Ready` | ProductInfo を更新し、`productInfoChanged` を発行します。 |
| `start` | [SystemStartRequest](payloads/commands/system-start-request.ja.md) と `type` | `Ready` | `Running` に遷移します。完了はイベントと結果によって報告されます。 |
| `stop` | [SystemStopRequest](payloads/commands/system-stop-request.ja.md) と `type` | `Running` |実行を停止し、`Ready` に戻ります。 |

### 発信イベント

|フレームタイプ |ペイロード |送信時 |
| --- | --- | --- |
| `statusChanged` | [SystemStatus](payloads/system/system-status.ja.md) と `type` |公開状態が変化します。 |
| `productInfoChanged` | [ProductInfo](payloads/product/product-info.ja.md) と `type` | ProductInfo のアップデートが完了しました。 |
| `runStarted` | [SystemStatus](payloads/system/system-status.ja.md) と `type` |状態は `Running` になります。 |
| `runCompleted` | [SystemStatus](payloads/system/system-status.ja.md) と `type` |実行は `Running` を出て、`Ready` に戻ります。 |
| `resultCreated` | [ResultSummary](payloads/results/result-summary.ja.md) と `type` |結果の概要が作成されます。 |
| `errorChanged` | [ErrorInfo](payloads/system/error-info.ja.md) と `type` |公開エラー情報が変更されます。 |
| `commandRejected` | [CommandResponse](payloads/commands/command-response.ja.md) と `type` |コマンドが拒否されます。 |

## 接続例

=== "C# SDK"

    ```csharp
    var tcp = new VirexTcpEventClient(new VirexClientOptions
    {
        TcpHost = "127.0.0.1",
        TcpPort = 5089,
    });

    tcp.EventReceived += (_, e) =>
    {
        Console.WriteLine(e.Type);
    };

    await tcp.SendStartAsync("golden-sample", ControlRunModes.Continue);
    await tcp.RunAsync(cancellationToken);
    ```

=== "C# Raw"

    ```csharp
    using var client = new TcpClient();
    await client.ConnectAsync("127.0.0.1", 5089);
    await using var stream = client.GetStream();

    var frame = Encoding.UTF8.GetBytes(
        "{\"type\":\"start\",\"condition\":\"golden-sample\",\"runMode\":\"continue\"}\n");
    await stream.WriteAsync(frame, 0, frame.Length);
    ```

=== "Python"

    ```python
    import socket

    with socket.create_connection(("127.0.0.1", 5089)) as sock:
        frame = b'{"type":"start","condition":"golden-sample","runMode":"continue"}\n'
        sock.sendall(frame)
    ```

=== "C++"

    ```cpp
    const std::string frame =
        R"({"type":"start","condition":"golden-sample","runMode":"continue"})"
        "\n";
    SendTcpFrame("127.0.0.1", 5089, frame);
    ```

## initialize コマンド

### 目的

TCP 経由でシステムを初期化します。`InitializationCompleted` により公開状態が `Ready` になると、コマンドは完了します。

### フレーム

```json
{"type":"initialize"}
```

### ペイロード

`type: "initialize"` 以外のフィールドは不要です。

### 状態制限

`Uninitialized` でのみ有効です。

### 成功イベント

サービスは次を送信します。

```json
{"type":"statusChanged","state":"Ready"}
```

### エラー処理

現在の状態が `Uninitialized` ではない場合、サービスは `commandRejected` を送信します。

## deinitialize コマンド

### 目的

TCP 経由でシステムを非初期化します。`DeinitializationCompleted` により公開状態が `Uninitialized` になると、コマンドは完了します。

### フレーム

```json
{"type":"deinitialize"}
```

### ペイロード

`type: "deinitialize"` 以外のフィールドは不要です。

### 状態制限

`Ready` でのみ有効です。

### 成功イベント

サービスは次を送信します。

```json
{"type":"statusChanged","state":"Uninitialized"}
```

### エラー処理

現在の状態が `Ready` ではない場合、サービスは `commandRejected` を送信します。

## productInfo コマンド

### 目的

TCP 経由で現在の ProductInfo を更新します。

### フレーム

```json
{"type":"productInfo","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1"}
```

### ペイロード

[ProductInfo](payloads/product/product-info.ja.md) と `type: "productInfo"`。

### 状態の制約

`Ready` でのみ有効です。

### 成功イベント

サービスは以下を送信します。

```json
{"type":"productInfoChanged","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1"}
```

### エラー処理

現在の状態が `Ready` ではない場合、サービスは `commandRejected` を送信します。

## start コマンド

### 目的

TCP 上で実行を開始します。システムが `Running` に遷移すると、コマンドが受け入れられます。実行の完了は、後でイベント フレームと結果フレームを通じて配信されます。

### フレーム

```json
{"type":"start","condition":"golden-sample","runMode":"continue"}
```

### ペイロード

[SystemStartRequest](payloads/commands/system-start-request.ja.md) と `type: "start"`。

### 状態の制約

`Ready` でのみ有効です。

### 成功イベント

このサービスは、`statusChanged`、`runStarted`、そしてその後の `resultCreated` と `runCompleted` を発行します。

```json
{"type":"runStarted","state":"Running"}
```

### エラー処理

現在の状態が `Ready` ではない場合、または `runMode` が無効な場合、サービスは `commandRejected` を送信します。

## stop コマンド

### 目的

TCP 上での現在の実行を停止します。

### フレーム

```json
{"type":"stop","reason":"operator-request"}
```

### ペイロード

[SystemStopRequest](payloads/commands/system-stop-request.ja.md) と `type: "stop"`。

### 状態の制約

`Running` でのみ有効です。

### 成功イベント

サービスは以下を送信します。

```json
{"type":"statusChanged","state":"Ready"}
```

### エラー処理

現在の状態が `Running` ではない場合、サービスは `commandRejected` を送信します。

## statusChanged イベント

### 目的

公開システムの状態が変更されたことをクライアントに通知します。

### フレーム

```json
{"type":"statusChanged","state":"Ready"}
```

### ペイロード

[SystemStatus](payloads/system/system-status.ja.md) と `type: "statusChanged"`。

### 注記

このイベントを使用して、クライアント UI とコマンドの可用性を同期します。

## productInfoChanged イベント

### 目的

ProductInfo 更新が完了したことをクライアントに通知します。

### フレーム

```json
{"type":"productInfoChanged","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1"}
```

### ペイロード

[ProductInfo](payloads/product/product-info.ja.md) と `type: "productInfoChanged"`。

### 注記

このイベントには ProductInfo のみが含まれます。

## runStarted イベント

### 目的

実行が開始され、状態が `Running` であることをクライアントに通知します。

### フレーム

```json
{"type":"runStarted","state":"Running"}
```

### ペイロード

[SystemStatus](payloads/system/system-status.ja.md) と `type: "runStarted"`。

### 注記

このイベントを受信した時点では、実行はまだ進行中です。

## runCompleted イベント

### 目的

実行ライフサイクルが完了し、状態が `Ready` に戻ったことをクライアントに通知します。

### フレーム

```json
{"type":"runCompleted","state":"Ready"}
```

### ペイロード

[SystemStatus](payloads/system/system-status.ja.md) と `type: "runCompleted"`。

### 注記

結果の詳細は `resultCreated` によって渡されます。

## resultCreated イベント

### 目的

公開結果概要が作成されたことをクライアントに通知します。

### フレーム

```json
{"type":"resultCreated","resultId":"RID-1","timestamp":"2026-06-20T15:30:12+08:00","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1","condition":"golden-sample","overallResult":"OK","defectCount":0,"imageRelativePath":"20260620/LOT-001/20260620_153012_W01.bmp","resultRelativePath":"20260620/LOT-001/20260620_153012_W01.json","imagePath":"/data/virex-results/20260620/LOT-001/20260620_153012_W01.bmp","previewImagePath":"/data/virex-results/20260620/LOT-001/20260620_153012_W01.jpg","resultPath":"/data/virex-results/20260620/LOT-001/20260620_153012_W01.json"}
```

### ペイロード

[ResultSummary](payloads/results/result-summary.ja.md) と `type: "resultCreated"`。

### 注記

結果には、ProductInfo スナップショットと、`Start` が受け入れられたときにキャプチャされた `condition` が含まれます。これは概要のみを提供し、欠陥リスト、クロップ リスト、イメージ バイナリ、または非公開検査の内部情報は含まれません。

## errorChanged イベント

### 目的

公開エラー情報が変更されたことをクライアントに通知します。

### フレーム

```json
{"type":"errorChanged","hasError":true,"message":"Camera timeout.","state":"Running"}
```

### ペイロード

[ErrorInfo](payloads/system/error-info.ja.md) と `type: "errorChanged"`。

### 注記

これはアプリケーション層のイベントです。ソケットの切断、タイムアウト、不正な形式の JSON、および不完全なフレームはトランスポート エラーです。

## commandRejected イベント

### 目的

コマンドが拒否されたことをクライアントに通知します。

### フレーム

```json
{"type":"commandRejected","accepted":false,"state":"Running","command":"SetProductInfo","errorCode":"invalid_state","message":"SetProductInfo is not valid while state is Running."}
```

### ペイロード

[CommandResponse](payloads/commands/command-response.ja.md) と `type: "commandRejected"`。

### 注記

クライアントは、このイベントをトランスポートの失敗として扱うべきではありません。これは、コマンドが受け入れられなかったことを示す有効なアプリケーション層応答です。

## エラー処理

不正な形式の JSON、末尾の改行の欠落、サポートされていないフレーム タイプ、ソケットの切断、読み取りタイムアウトは、トランスポート/プロトコルのエラーです。無効な状態、無効な実行モード、および拒否されたコマンドは、`commandRejected` を通じて報告されます。
