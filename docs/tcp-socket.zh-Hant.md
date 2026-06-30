# TCP Socket 通訊協定

TCP Socket 是雙向整合通道，適合需要用同一個簡單串流協定送出命令並接收事件的用戶端。

## 基本資訊

| 項目 | 值 |
| --- | --- |
| 預設 host | `127.0.0.1` |
| 預設 port | `5089` |
| 分幀方式 | NDJSON |
| 編碼 | UTF-8 |
| 方向 | 用戶端傳送命令資料框；服務傳送事件資料框 |

每個資料框都是一個 JSON 物件，並以 `\n` 結尾。

```text
{"type":"start","condition":"golden-sample","runMode":"continue"}\n
```

C# SDK 讀取 TCP/NDJSON 時，會對單一資料框套用閒置逾時。兩個完整資料框之間可以有較長等待；但只要某個資料框已經收到任何位元組，剩餘內容與結尾換行必須在 `VirexClientOptions.TcpFrameTimeoutMs` 內抵達，否則 TCP 事件讀取器會回報逾時。

## 資料框總覽

### 傳入命令

| 資料框類型 | 資料內容 | 合法狀態 | 結果 |
| --- | --- | --- | --- |
| `initialize` | `{"type":"initialize"}` | `Uninitialized` | 進入 `Initializing`；完成後送出狀態為 `Ready` 的 `statusChanged`。 |
| `deinitialize` | `{"type":"deinitialize"}` | `Ready` | 進入 `Deinitializing`；完成後送出狀態為 `Uninitialized` 的 `statusChanged`。 |
| `productInfo` | [ProductInfo](payloads/product/product-info.zh-Hant.md) 加上 `type` | `Ready` | 更新 ProductInfo 並發出 `productInfoChanged`。 |
| `start` | [SystemStartRequest](payloads/commands/system-start-request.zh-Hant.md) 加上 `type` | `Ready` | 進入 `Running`；完成結果由事件與結果查詢提供。 |
| `stop` | [SystemStopRequest](payloads/commands/system-stop-request.zh-Hant.md) 加上 `type` | `Running` | 停止執行並回到 `Ready`。 |

### 傳出事件

| 資料框類型 | 資料內容 | 發送時機 |
| --- | --- | --- |
| `statusChanged` | [SystemStatus](payloads/system/system-status.zh-Hant.md) 加上 `type` | 公開狀態改變。 |
| `productInfoChanged` | [ProductInfo](payloads/product/product-info.zh-Hant.md) 加上 `type` | ProductInfo 更新完成。 |
| `runStarted` | [SystemStatus](payloads/system/system-status.zh-Hant.md) 加上 `type` | 狀態進入 `Running`。 |
| `runCompleted` | [SystemStatus](payloads/system/system-status.zh-Hant.md) 加上 `type` | 一次執行離開 `Running` 並回到 `Ready`。 |
| `resultCreated` | [ResultSummary](payloads/results/result-summary.zh-Hant.md) 加上 `type` | 建立結果摘要。 |
| `errorChanged` | [ErrorInfo](payloads/system/error-info.zh-Hant.md) 加上 `type` | 公開錯誤狀態改變。 |
| `commandRejected` | [CommandResponse](payloads/commands/command-response.zh-Hant.md) 加上 `type` | 命令被拒絕。 |

## 連線範例

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

## initialize 命令

### 用途

透過 TCP 初始化系統。命令會等到 `InitializationCompleted` 讓公開狀態變成 `Ready` 後完成。

### 資料框

```json
{"type":"initialize"}
```

### 資料內容

除了 `type: "initialize"` 之外，不需要其他欄位。

### 狀態限制

只在 `Uninitialized` 合法。

### 成功事件

服務會送出：

```json
{"type":"statusChanged","state":"Ready"}
```

### 錯誤處理

如果目前狀態不是 `Uninitialized`，服務會送出 `commandRejected`。

## deinitialize 命令

### 用途

透過 TCP 反初始化系統。命令會等到 `DeinitializationCompleted` 讓公開狀態變成 `Uninitialized` 後完成。

### 資料框

```json
{"type":"deinitialize"}
```

### 資料內容

除了 `type: "deinitialize"` 之外，不需要其他欄位。

### 狀態限制

只在 `Ready` 合法。

### 成功事件

服務會送出：

```json
{"type":"statusChanged","state":"Uninitialized"}
```

### 錯誤處理

如果目前狀態不是 `Ready`，服務會送出 `commandRejected`。

## productInfo 命令

### 用途

透過 TCP 更新目前 ProductInfo。

### 資料框

```json
{"type":"productInfo","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1"}
```

### 資料內容

[ProductInfo](payloads/product/product-info.zh-Hant.md) 加上 `type: "productInfo"`。

### 狀態限制

只允許在 `Ready` 呼叫。

### 成功事件

服務會發出：

```json
{"type":"productInfoChanged","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1"}
```

### 錯誤處理

如果目前狀態不是 `Ready`，服務會送出 `commandRejected`。

## start 命令

### 用途

透過 TCP 啟動一次執行。系統進入 `Running` 時命令即視為接受；執行完成會稍後透過事件與結果資料框傳遞。

### 資料框

```json
{"type":"start","condition":"golden-sample","runMode":"continue"}
```

### 資料內容

[SystemStartRequest](payloads/commands/system-start-request.zh-Hant.md) 加上 `type: "start"`。

### 狀態限制

只允許在 `Ready` 呼叫。

### 成功事件

服務會發出 `statusChanged`、`runStarted`，稍後再發出 `resultCreated` 與 `runCompleted`。

```json
{"type":"runStarted","state":"Running"}
```

### 錯誤處理

如果目前狀態不是 `Ready`，或 `runMode` 不合法，服務會送出 `commandRejected`。

## stop 命令

### 用途

透過 TCP 停止目前執行。

### 資料框

```json
{"type":"stop","reason":"operator-request"}
```

### 資料內容

[SystemStopRequest](payloads/commands/system-stop-request.zh-Hant.md) 加上 `type: "stop"`。

### 狀態限制

只允許在 `Running` 呼叫。

### 成功事件

服務會發出：

```json
{"type":"statusChanged","state":"Ready"}
```

### 錯誤處理

如果目前狀態不是 `Running`，服務會送出 `commandRejected`。

## statusChanged 事件

### 用途

通知用戶端公開系統狀態已改變。

### 資料框

```json
{"type":"statusChanged","state":"Ready"}
```

### 資料內容

[SystemStatus](payloads/system/system-status.zh-Hant.md) 加上 `type: "statusChanged"`。

### 說明

用這個事件同步用戶端 UI 與命令可用性。

## productInfoChanged 事件

### 用途

通知用戶端 ProductInfo 更新完成。

### 資料框

```json
{"type":"productInfoChanged","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1"}
```

### 資料內容

[ProductInfo](payloads/product/product-info.zh-Hant.md) 加上 `type: "productInfoChanged"`。

### 說明

這個事件只包含公開 ProductInfo。

## runStarted 事件

### 用途

通知用戶端執行已開始，狀態為 `Running`。

### 資料框

```json
{"type":"runStarted","state":"Running"}
```

### 資料內容

[SystemStatus](payloads/system/system-status.zh-Hant.md) 加上 `type: "runStarted"`。

### 說明

收到這個事件時，執行仍在進行中。

## runCompleted 事件

### 用途

通知用戶端執行生命週期已完成，狀態回到 `Ready`。

### 資料框

```json
{"type":"runCompleted","state":"Ready"}
```

### 資料內容

[SystemStatus](payloads/system/system-status.zh-Hant.md) 加上 `type: "runCompleted"`。

### 說明

結果細節由 `resultCreated` 傳遞。

## resultCreated 事件

### 用途

通知用戶端已建立公開結果摘要。

### 資料框

```json
{"type":"resultCreated","resultId":"RID-1","timestamp":"2026-06-20T15:30:12+08:00","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1","condition":"golden-sample","overallResult":"OK","defectCount":0,"imageRelativePath":"20260620/LOT-001/20260620_153012_W01.bmp","resultRelativePath":"20260620/LOT-001/20260620_153012_W01.json","imagePath":"/data/virex-results/20260620/LOT-001/20260620_153012_W01.bmp","previewImagePath":"/data/virex-results/20260620/LOT-001/20260620_153012_W01.jpg","resultPath":"/data/virex-results/20260620/LOT-001/20260620_153012_W01.json"}
```

### 資料內容

[ResultSummary](payloads/results/result-summary.zh-Hant.md) 加上 `type: "resultCreated"`。

### 說明

結果包含 `Start` 被接受當下保存的 ProductInfo 快照與 `condition`。它只提供摘要，不包含瑕疵清單、裁切清單、影像二進位資料或私有檢測內部資料。

## errorChanged 事件

### 用途

通知用戶端公開錯誤狀態已改變。

### 資料框

```json
{"type":"errorChanged","hasError":true,"message":"Camera timeout.","state":"Running"}
```

### 資料內容

[ErrorInfo](payloads/system/error-info.zh-Hant.md) 加上 `type: "errorChanged"`。

### 說明

這是應用層事件。Socket 斷線、逾時、JSON 格式錯誤、資料框不完整屬於傳輸錯誤。

## commandRejected 事件

### 用途

通知用戶端命令被拒絕。

### 資料框

```json
{"type":"commandRejected","accepted":false,"state":"Running","command":"SetProductInfo","errorCode":"invalid_state","message":"SetProductInfo is not valid while state is Running."}
```

### 資料內容

[CommandResponse](payloads/commands/command-response.zh-Hant.md) 加上 `type: "commandRejected"`。

### 說明

用戶端不應把這個事件視為傳輸失敗。它是有效的應用層回應，代表命令未被接受。

## 錯誤處理

JSON 格式錯誤、缺少換行結尾、不支援的資料框類型、socket 斷線與讀取逾時都屬於傳輸或通訊協定錯誤。狀態不合法、run mode 不合法、命令被拒絕則由 `commandRejected` 回報。
