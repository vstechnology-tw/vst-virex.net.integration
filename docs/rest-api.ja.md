# REST API

REST API は、状態の読み取り、ProductInfo の管理、システム コマンドの送信、および結果の概要のクエリに使用されます。

## 基本情報

|項目 |値 |
| --- | --- |
|ベース URL | `http://127.0.0.1:5088` |
| OpenAPI JSON | `http://127.0.0.1:5088/openapi/v1.json` |
| Scalar UI | `http://127.0.0.1:5088/scalar` |
|Content-Type | `application/json` |

すべての要求および応答の本文は JSON を使用します。本文のない `POST` は空の本文を送信できます。

## エンドポイントの概要

|分類 |方法 |ルート |目的 |有効な状態 |成功応答 |
| --- | --- | --- | --- | --- | --- |
|状態 | GET | `/api/status` |現在のシステム状態を読み取ります。 |すべて | [SystemStatus](payloads/system/system-status.ja.md) |
| ProductInfo | GET | `/api/product-info` |現在の ProductInfo を読み取ります。 |すべて | [ProductInfo](payloads/product/product-info.ja.md) |
| ProductInfo | POST | `/api/product-info` |現在の ProductInfo を更新します。 | `Ready` | [CommandResponse](payloads/commands/command-response.ja.md) |
|システム | POST | `/api/system/initialize` | システムを初期化します。 | `Uninitialized` | [CommandResponse](payloads/commands/command-response.ja.md) |
|システム | POST | `/api/system/deinitialize` | システムの初期化を解除します。 | `Ready` | [CommandResponse](payloads/commands/command-response.ja.md) |
|システム | POST | `/api/system/start` |実行を開始します。 | `Ready` | [CommandResponse](payloads/commands/command-response.ja.md) |
|システム | POST | `/api/system/stop` |現在の実行を停止します。 | `Running` | [CommandResponse](payloads/commands/command-response.ja.md) |
|結果 | GET | `/api/results` |結果の概要を照会します。 |すべて | [ResultList](payloads/results/result-list.ja.md) |

## GET /api/status

### 目的

現在の公開システム状態を読み取ります。クライアントはこの状態を使用して、次のコマンドを送信できるかどうかを決定できます。

### 要求

|項目 |値 |
| --- | --- |
|方法 | `GET` |
|ルート | `/api/status` |
|クエリ |なし |
|本文 |なし |

### 応答

| HTTP ステータス |本文 |説明 |
| --- | --- | --- |
| `200 OK` | [SystemStatus](payloads/system/system-status.ja.md) |現在の状態を返します。 |

### 例

=== "C# SDK"

    ```csharp
    var status = await client.GetStatusAsync();
    // 200 OK body: {"state":"Ready"}
    Console.WriteLine(status.State);
    ```

=== "C# Raw"

    ```csharp
    var status = await http.GetFromJsonAsync<SystemStatus>("/api/status");
    // 200 OK: {"state":"Ready"}
    Console.WriteLine(status?.State);
    ```

=== "Python"

    ```python
    with urllib.request.urlopen("http://127.0.0.1:5088/api/status") as response:
        status = json.loads(response.read().decode("utf-8"))
        # 200 OK: {"state":"Ready"}
        print(status["state"])
    ```

=== "C++"

    ```cpp
    const auto response = SendRequest(url, L"GET", L"/api/status");
    // HTTP 200 body: {"state":"Ready"}
    std::cout << response.body << std::endl;
    ```


### 状態の制約

どの状態でも呼び出すことができます。

### エラー処理

通常、[CommandResponse](payloads/commands/command-response.ja.md) は返されません。接続の失敗、サービスが開始されていない、または JSON 以外の応答は、通信/HTTP エラーと見なす必要があります。

## GET /api/product-info

### 目的

現在の ProductInfo を読み取ります。 ProductInfo は、`Start` が受け入れられると、結果スナップショットとして保存されます。

### 要求

|項目 |値 |
| --- | --- |
|方法 | `GET` |
|ルート | `/api/product-info` |
|クエリ |なし |
|本文 |なし |

### 応答

| HTTP ステータス |本文 |説明 |
| --- | --- | --- |
| `200 OK` | [ProductInfo](payloads/product/product-info.ja.md) |現在の ProductInfo を返します。 |

### 例

=== "C# SDK"

    ```csharp
    var productInfo = await client.GetProductInfoAsync();
    // 200 OK body: {"lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1"}
    Console.WriteLine(productInfo.LotID);
    ```

=== "C# Raw"

    ```csharp
    var productInfo = await http.GetFromJsonAsync<ProductInfo>("/api/product-info");
    // 200 OK body: {"lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1"}
    Console.WriteLine(productInfo?.LotID);
    ```

=== "Python"

    ```python
    with urllib.request.urlopen("http://127.0.0.1:5088/api/product-info") as response:
        product_info = json.loads(response.read().decode("utf-8"))
        # 200 OK body: {"lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1"}
        print(product_info["lotID"])
    ```

=== "C++"

    ```cpp
    const auto response = SendRequest(url, L"GET", L"/api/product-info");
    // HTTP 200 body: {"lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1"}
    std::cout << response.body << std::endl;
    ```


### 状態の制約

どの状態でも呼び出すことができます。

### エラー処理

通常、[CommandResponse](payloads/commands/command-response.ja.md) は返されません。接続の失敗、サービスが開始されていない、または JSON 以外の応答は、通信/HTTP エラーと見なす必要があります。

## POST /api/product-info

### 目的

現在の製品情報を更新します。この API は、`ProductInfoUpdateCompleted` まで待機してから応答するため、正常に応答された `state` は `Ready` になります。

### 要求

|項目 |値 |
| --- | --- |
|方法 | `POST` |
|ルート | `/api/product-info` |
|クエリ |なし |
|本文 | [ProductInfo](payloads/product/product-info.ja.md) |

### 要求本文

```json
{
  "lotID": "LOT-001",
  "waferID": "W01",
  "recipe": "RCP-A",
  "slot": "1",
  "foupID": "FOUP-A",
  "chamberID": "CH-1"
}
```

### 応答

| HTTP ステータス |本文 |説明 |
| --- | --- | --- |
| `200 OK` | [CommandResponse](payloads/commands/command-response.ja.md) | ProductInfo のアップデートが完了しました。 |
| `409 Conflict` | [CommandResponse](payloads/commands/command-response.ja.md) |現在の状態では、ProductInfo の更新は許可されていません。 |
| `400 Bad Request` |エラーテキスト |要求本文は有効な ProductInfo ではありません。 |

### 例

=== "C# SDK"

    ```csharp
    await client.SetProductInfoAsync(new ProductInfo
    {
        LotID = "LOT-001",
        WaferID = "W01",
        Recipe = "RCP-A",
        Slot = "1",
        FoupID = "FOUP-A",
        ChamberID = "CH-1",
    });
    // 200 OK body: {"accepted":true,"state":"Ready","command":"SetProductInfo","message":"SetProductInfo accepted."}
    ```

=== "C# Raw"

    ```csharp
    var productInfo = new ProductInfo
    {
        LotID = "LOT-001",
        WaferID = "W01",
        Recipe = "RCP-A",
        Slot = "1",
        FoupID = "FOUP-A",
        ChamberID = "CH-1",
    };

    var response = await http.PostAsJsonAsync("/api/product-info", productInfo);
    // 200 OK body: {"accepted":true,"state":"Ready","command":"SetProductInfo","message":"SetProductInfo accepted."}
    ```

=== "Python"

    ```python
    payload = {
        "lotID": "LOT-001",
        "waferID": "W01",
        "recipe": "RCP-A",
        "slot": "1",
        "foupID": "FOUP-A",
        "chamberID": "CH-1",
    }
    data = json.dumps(payload).encode("utf-8")
    request = urllib.request.Request(
        "http://127.0.0.1:5088/api/product-info",
        data=data,
        headers={"Content-Type": "application/json"},
        method="POST")
    with urllib.request.urlopen(request) as response:
        # 200 OK body: {"accepted":true,"state":"Ready","command":"SetProductInfo","message":"SetProductInfo accepted."}
        print(response.read().decode("utf-8"))
    ```

=== "C++"

    ```cpp
    const std::string body =
        R"({"lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1"})";
    const auto response = SendRequest(url, L"POST", L"/api/product-info", body);
    // HTTP 200 body: {"accepted":true,"state":"Ready","command":"SetProductInfo","message":"SetProductInfo accepted."}
    std::cout << response.body << std::endl;
    ```


### 状態の制約

`Ready` での呼び出しのみが許可されます。

### エラー処理

現在の状態が `Ready` ではない場合、次を返します。

```json
{
  "accepted": false,
  "state": "Running",
  "command": "SetProductInfo",
  "errorCode": "invalid_state",
  "message": "SetProductInfo is not valid while state is Running."
}
```

## POST /api/system/initialize

### 目的

システムを初期化します。 API は `InitializationCompleted` まで待機してから応答するため、正常に応答された `state` は `Ready` になります。

### 要求

|項目 |値 |
| --- | --- |
|方法 | `POST` |
|ルート | `/api/system/initialize` |
|クエリ |なし |
|本文 |なし |

### 応答

| HTTP ステータス |本文 |説明 |
| --- | --- | --- |
| `200 OK` | [CommandResponse](payloads/commands/command-response.ja.md) |初期化が完了しました。 |
| `409 Conflict` | [CommandResponse](payloads/commands/command-response.ja.md) |現在の状態では初期化ができません。 |

### 例

=== "C# SDK"

    ```csharp
    var response = await client.InitializeAsync();
    // 200 OK body: {"accepted":true,"state":"Ready","command":"Initialize","message":"Initialize accepted."}
    Console.WriteLine(response.State);
    ```

=== "C# Raw"

    ```csharp
    var response = await http.PostAsync("/api/system/initialize", null);
    // 200 OK body: {"accepted":true,"state":"Ready","command":"Initialize","message":"Initialize accepted."}
    Console.WriteLine(await response.Content.ReadAsStringAsync());
    ```

=== "Python"

    ```python
    request = urllib.request.Request(
        "http://127.0.0.1:5088/api/system/initialize",
        method="POST")
    with urllib.request.urlopen(request) as response:
        # 200 OK body: {"accepted":true,"state":"Ready","command":"Initialize","message":"Initialize accepted."}
        print(response.read().decode("utf-8"))
    ```

=== "C++"

    ```cpp
    const auto response = SendRequest(url, L"POST", L"/api/system/initialize");
    // HTTP 200 body: {"accepted":true,"state":"Ready","command":"Initialize","message":"Initialize accepted."}
    std::cout << response.body << std::endl;
    ```


### 状態の制約

`Uninitialized` での呼び出しのみが許可されます。

### エラー処理

現在の状態が `Uninitialized` ではない場合、`accepted=false` および `errorCode=invalid_state` を返します。

## POST /api/system/deinitialize

### 目的

初期化解除システム。 API は `DeinitializationCompleted` まで待機してから応答するため、正常に応答された `state` は `Uninitialized` になります。

### 要求

|項目 |値 |
| --- | --- |
|方法 | `POST` |
|ルート | `/api/system/deinitialize` |
|クエリ |なし |
|本文 |なし |

### 応答

| HTTP ステータス |本文 |説明 |
| --- | --- | --- |
| `200 OK` | [CommandResponse](payloads/commands/command-response.ja.md) |初期化解除が完了しました。 |
| `409 Conflict` | [CommandResponse](payloads/commands/command-response.ja.md) |現在の状態では初期化を解除できません。 |

### 例

=== "C# SDK"

    ```csharp
    var response = await client.DeinitializeAsync();
    // 200 OK body: {"accepted":true,"state":"Uninitialized","command":"Deinitialize","message":"Deinitialize accepted."}
    Console.WriteLine(response.State);
    ```

=== "C# Raw"

    ```csharp
    var response = await http.PostAsync("/api/system/deinitialize", null);
    // 200 OK body: {"accepted":true,"state":"Uninitialized","command":"Deinitialize","message":"Deinitialize accepted."}
    Console.WriteLine(await response.Content.ReadAsStringAsync());
    ```

=== "Python"

    ```python
    request = urllib.request.Request(
        "http://127.0.0.1:5088/api/system/deinitialize",
        method="POST")
    with urllib.request.urlopen(request) as response:
        # 200 OK body: {"accepted":true,"state":"Uninitialized","command":"Deinitialize","message":"Deinitialize accepted."}
        print(response.read().decode("utf-8"))
    ```

=== "C++"

    ```cpp
    const auto response = SendRequest(url, L"POST", L"/api/system/deinitialize");
    // HTTP 200 body: {"accepted":true,"state":"Uninitialized","command":"Deinitialize","message":"Deinitialize accepted."}
    std::cout << response.body << std::endl;
    ```


### 状態の制約

`Ready` での呼び出しのみが許可されます。

### エラー処理

現在の状態が `Ready` ではない場合、`accepted=false` および `errorCode=invalid_state` を返します。

## POST /api/system/start

### 目的

実行を開始します。 API は、システムが `Running` に入るとすぐに応答します。実行の完了は、イベントまたは `GET /api/results` を通じて確認する必要があります。

`Start` が受け入れられると、現在の ProductInfo スナップショットがすぐに保存され、後続の結果ではこのスナップショットが使用されます。

### 要求

|項目 |値 |
| --- | --- |
|方法 | `POST` |
|ルート | `/api/system/start` |
|クエリ |なし |
|本文 | [SystemStartRequest](payloads/commands/system-start-request.ja.md) またはなし |

### 要求本文

```json
{
  "condition": "golden-sample",
  "runMode": "continue"
}
```

`condition` および `runMode` はオプションのフィールドです。 `runMode` は、省略または空白の場合、既定で `continue` になり、`single` もサポートされます。

### 応答

| HTTP ステータス |本文 |説明 |
| --- | --- | --- |
| `200 OK` | [CommandResponse](payloads/commands/command-response.ja.md) | `Running` に遷移しました。 |
| `409 Conflict` | [CommandResponse](payloads/commands/command-response.ja.md) |現在の状態では開始できません。 |

### 例

=== "C# SDK"

    ```csharp
    var response = await client.StartAsync("golden-sample", ControlRunModes.Continue);
    // 200 OK body: {"accepted":true,"state":"Running","command":"Start","message":"Start accepted."}
    Console.WriteLine(response.State);
    ```

=== "C# Raw"

    ```csharp
    var request = new SystemStartRequest
    {
        Condition = "golden-sample",
        RunMode = ControlRunModes.Continue,
    };

    var response = await http.PostAsJsonAsync("/api/system/start", request);
    // 200 OK body: {"accepted":true,"state":"Running","command":"Start","message":"Start accepted."}
    ```

=== "Python"

    ```python
    payload = {"condition": "golden-sample", "runMode": "continue"}
    data = json.dumps(payload).encode("utf-8")
    request = urllib.request.Request(
        "http://127.0.0.1:5088/api/system/start",
        data=data,
        headers={"Content-Type": "application/json"},
        method="POST")
    with urllib.request.urlopen(request) as response:
        # 200 OK body: {"accepted":true,"state":"Running","command":"Start","message":"Start accepted."}
        print(response.read().decode("utf-8"))
    ```

=== "C++"

    ```cpp
    const std::string body = R"({"condition":"golden-sample","runMode":"continue"})";
    const auto response = SendRequest(url, L"POST", L"/api/system/start", body);
    // HTTP 200 body: {"accepted":true,"state":"Running","command":"Start","message":"Start accepted."}
    std::cout << response.body << std::endl;
    ```


### 状態の制約

`Ready` での呼び出しのみが許可されます。

### エラー処理

現在の状態が `Ready` ではない場合、`accepted=false` および `errorCode=invalid_state` を返します。

## POST /api/system/stop

### 目的

現在の実行を停止します。 API は、状態が `Ready` に戻った後に応答します。

### 要求

|項目 |値 |
| --- | --- |
|方法 | `POST` |
|ルート | `/api/system/stop` |
|クエリ |なし |
|本文 | [SystemStopRequest](payloads/commands/system-stop-request.ja.md) またはなし |

### 要求本文

```json
{
  "reason": "operator-request"
}
```

`reason` はオプションのフィールドです。

### 応答

| HTTP ステータス |本文 |説明 |
| --- | --- | --- |
| `200 OK` | [CommandResponse](payloads/commands/command-response.ja.md) |停止して `Ready` に戻りました。 |
| `409 Conflict` | [CommandResponse](payloads/commands/command-response.ja.md) |現在の状態では停止はできません。 |

### 例

=== "C# SDK"

    ```csharp
    var response = await client.StopAsync("operator-request");
    // 200 OK body: {"accepted":true,"state":"Ready","command":"Stop","message":"Stop accepted."}
    Console.WriteLine(response.State);
    ```

=== "C# Raw"

    ```csharp
    var request = new SystemStopRequest { Reason = "operator-request" };
    var response = await http.PostAsJsonAsync("/api/system/stop", request);
    // 200 OK body: {"accepted":true,"state":"Ready","command":"Stop","message":"Stop accepted."}
    ```

=== "Python"

    ```python
    payload = {"reason": "operator-request"}
    data = json.dumps(payload).encode("utf-8")
    request = urllib.request.Request(
        "http://127.0.0.1:5088/api/system/stop",
        data=data,
        headers={"Content-Type": "application/json"},
        method="POST")
    with urllib.request.urlopen(request) as response:
        # 200 OK body: {"accepted":true,"state":"Ready","command":"Stop","message":"Stop accepted."}
        print(response.read().decode("utf-8"))
    ```

=== "C++"

    ```cpp
    const std::string body = R"({"reason":"operator-request"})";
    const auto response = SendRequest(url, L"POST", L"/api/system/stop", body);
    // HTTP 200 body: {"accepted":true,"state":"Ready","command":"Stop","message":"Stop accepted."}
    std::cout << response.body << std::endl;
    ```


### 状態の制約

`Running` での呼び出しのみが許可されます。

### エラー処理

現在の状態が `Running` ではない場合、`accepted=false` および `errorCode=invalid_state` を返します。

## GET /api/results

### 目的

結果の概要をクエリします。結果には公開された概要のみが含まれます。これらには、欠陥リスト、ダイ リスト、クロップ リスト、イメージ バイナリ、または非公開検査内部は含まれません。

### 要求

|項目 |値 |
| --- | --- |
|方法 | `GET` |
|ルート | `/api/results` |
|クエリ | `waferID`、`lotID`、`recipe` |
|本文 |なし |

### クエリパラメータ

|名前 |必須 |説明 |
| --- | --- | --- |
| `waferID` |いいえ |ウェハIDでフィルタリングします。 |
| `lotID` |いいえ |ロット ID でフィルターします。 |
| `recipe` |いいえ |レシピでフィルターします。 |

複数のクエリパラメータは AND で結合されます。

### 応答

| HTTP ステータス |本文 |説明 |
| --- | --- | --- |
| `200 OK` | [ResultList](payloads/results/result-list.ja.md) |条件に一致した結果の概要を返します。 |
| `400 Bad Request` |エラーテキスト |クエリパラメータはサポートされていません。 |

### 例

=== "C# SDK"

    ```csharp
    var results = await client.QueryResultsAsync(lotID: "LOT-001");
    // 200 OK body: {"items":[{"resultId":"RID-1","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","condition":"golden-sample","overallResult":"OK","defectCount":0}],"count":1}
    Console.WriteLine(results.Count);
    ```

=== "C# Raw"

    ```csharp
    var results = await http.GetFromJsonAsync<ResultList>("/api/results?lotID=LOT-001");
    // 200 OK body: {"items":[{"resultId":"RID-1","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","condition":"golden-sample","overallResult":"OK","defectCount":0}],"count":1}
    Console.WriteLine(results?.Count);
    ```

=== "Python"

    ```python
    with urllib.request.urlopen("http://127.0.0.1:5088/api/results?lotID=LOT-001") as response:
        results = json.loads(response.read().decode("utf-8"))
        # 200 OK body: {"items":[{"resultId":"RID-1","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","condition":"golden-sample","overallResult":"OK","defectCount":0}],"count":1}
        print(results["count"])
    ```

=== "C++"

    ```cpp
    const auto response = SendRequest(url, L"GET", L"/api/results?lotID=LOT-001");
    // HTTP 200 body: {"items":[{"resultId":"RID-1","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","condition":"golden-sample","overallResult":"OK","defectCount":0}],"count":1}
    std::cout << response.body << std::endl;
    ```


### 状態の制約

どの状態でも呼び出すことができます。

### エラー処理

クエリパラメータが `waferID`、`lotID`、または `recipe` でない場合、`400 Bad Request` が返されます。

## 一般的なエラー処理

### 無効な状態

システム コマンドまたは ProductInfo 更新が無効な状態で呼び出されると、API は `409 Conflict` および [CommandResponse](payloads/commands/command-response.ja.md) を返します。

```json
{
  "accepted": false,
  "state": "Running",
  "command": "SetProductInfo",
  "errorCode": "invalid_state",
  "message": "SetProductInfo is not valid while state is Running."
}
```

クライアントは最初に `GET /api/status` を読み取り、状態が許可したときにコマンドを再送信する必要があります。

### 不正な要求

要求ボディが解析できない場合、またはクエリパラメータがサポートされていない場合は、`400 Bad Request` が返されます。

### トランスポート / HTTP の失敗

接続失敗、サービスが開始されていない、タイムアウト、非 JSON 応答、または予期しない HTTP ステータス コードは、状態ベースのコマンド拒否ではなく、トランスポート / HTTP エラーとして扱う必要があります。
