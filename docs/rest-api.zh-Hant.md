# RESTful API

RESTful API 用於讀取狀態、管理 ProductInfo、送出系統命令，以及查詢結果摘要。

## 基本資訊

| 項目 | 值 |
| --- | --- |
| Base URL | `http://127.0.0.1:5088` |
| OpenAPI JSON | `http://127.0.0.1:5088/openapi/v1.json` |
| Scalar UI | `http://127.0.0.1:5088/scalar` |
| Content-Type | `application/json` |

所有 request 與 response body 都使用 JSON。沒有 body 的 `POST` 可以送空 body。

## Endpoint 總覽

| 分類 | Method | Route | 用途 | 合法狀態 | 成功回應 |
| --- | --- | --- | --- | --- | --- |
| Status | GET | `/api/status` | 讀取目前系統狀態。 | Any | [SystemStatus](payloads/system/system-status.zh-Hant.md) |
| ProductInfo | GET | `/api/product-info` | 讀取目前 ProductInfo。 | Any | [ProductInfo](payloads/product/product-info.zh-Hant.md) |
| ProductInfo | POST | `/api/product-info` | 更新目前 ProductInfo。 | `Ready` | [CommandResponse](payloads/commands/command-response.zh-Hant.md) |
| System | POST | `/api/system/initialize` | 初始化系統。 | `Uninitialized` | [CommandResponse](payloads/commands/command-response.zh-Hant.md) |
| System | POST | `/api/system/deinitialize` | 反初始化系統。 | `Ready` | [CommandResponse](payloads/commands/command-response.zh-Hant.md) |
| System | POST | `/api/system/start` | 啟動一次執行。 | `Ready` | [CommandResponse](payloads/commands/command-response.zh-Hant.md) |
| System | POST | `/api/system/stop` | 停止目前執行。 | `Running` | [CommandResponse](payloads/commands/command-response.zh-Hant.md) |
| Results | GET | `/api/results` | 查詢結果摘要。 | Any | [ResultList](payloads/results/result-list.zh-Hant.md) |

## GET /api/status

### 用途

讀取目前公開系統狀態。用戶端可依照這個狀態決定下一個命令是否可以送出。

### Request

| 項目 | 值 |
| --- | --- |
| Method | `GET` |
| Route | `/api/status` |
| Query | none |
| Body | none |

### Response

| HTTP status | Body | 說明 |
| --- | --- | --- |
| `200 OK` | [SystemStatus](payloads/system/system-status.zh-Hant.md) | 回傳目前狀態。 |

### 範例

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


### 狀態限制

任何狀態都可以呼叫。

### 錯誤處理

一般情況不會回傳 [CommandResponse](payloads/commands/command-response.zh-Hant.md)。連線失敗、服務未啟動或非 JSON 回應應視為傳輸或 HTTP 錯誤。

## GET /api/product-info

### 用途

讀取目前的 ProductInfo。ProductInfo 會在 `Start` 被接受時保存為結果快照。

### Request

| 項目 | 值 |
| --- | --- |
| Method | `GET` |
| Route | `/api/product-info` |
| Query | none |
| Body | none |

### Response

| HTTP status | Body | 說明 |
| --- | --- | --- |
| `200 OK` | [ProductInfo](payloads/product/product-info.zh-Hant.md) | 回傳目前 ProductInfo。 |

### 範例

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


### 狀態限制

任何狀態都可以呼叫。

### 錯誤處理

一般情況不會回傳 [CommandResponse](payloads/commands/command-response.zh-Hant.md)。連線失敗、服務未啟動或非 JSON 回應應視為傳輸或 HTTP 錯誤。

## POST /api/product-info

### 用途

更新目前 ProductInfo。這個 API 會等到 `ProductInfoUpdateCompleted` 後才回應，因此成功回應的 `state` 會是 `Ready`。

### Request

| 項目 | 值 |
| --- | --- |
| Method | `POST` |
| Route | `/api/product-info` |
| Query | none |
| Body | [ProductInfo](payloads/product/product-info.zh-Hant.md) |

### Request Body

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

### Response

| HTTP status | Body | 說明 |
| --- | --- | --- |
| `200 OK` | [CommandResponse](payloads/commands/command-response.zh-Hant.md) | ProductInfo 更新完成。 |
| `409 Conflict` | [CommandResponse](payloads/commands/command-response.zh-Hant.md) | 目前狀態不允許更新 ProductInfo。 |
| `400 Bad Request` | error text | Request body 不是有效 ProductInfo。 |

### 範例

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


### 狀態限制

只允許在 `Ready` 呼叫。

### 錯誤處理

如果目前狀態不是 `Ready`，回傳：

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

### 用途

初始化系統。API 會等到 `InitializationCompleted` 後才回應，因此成功回應的 `state` 會是 `Ready`。

### Request

| 項目 | 值 |
| --- | --- |
| Method | `POST` |
| Route | `/api/system/initialize` |
| Query | none |
| Body | none |

### Response

| HTTP status | Body | 說明 |
| --- | --- | --- |
| `200 OK` | [CommandResponse](payloads/commands/command-response.zh-Hant.md) | 初始化完成。 |
| `409 Conflict` | [CommandResponse](payloads/commands/command-response.zh-Hant.md) | 目前狀態不允許初始化。 |

### 範例

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


### 狀態限制

只允許在 `Uninitialized` 呼叫。

### 錯誤處理

如果目前狀態不是 `Uninitialized`，回傳 `accepted=false` 與 `errorCode=invalid_state`。

## POST /api/system/deinitialize

### 用途

反初始化系統。API 會等到 `DeinitializationCompleted` 後才回應，因此成功回應的 `state` 會是 `Uninitialized`。

### Request

| 項目 | 值 |
| --- | --- |
| Method | `POST` |
| Route | `/api/system/deinitialize` |
| Query | none |
| Body | none |

### Response

| HTTP status | Body | 說明 |
| --- | --- | --- |
| `200 OK` | [CommandResponse](payloads/commands/command-response.zh-Hant.md) | 反初始化完成。 |
| `409 Conflict` | [CommandResponse](payloads/commands/command-response.zh-Hant.md) | 目前狀態不允許反初始化。 |

### 範例

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


### 狀態限制

只允許在 `Ready` 呼叫。

### 錯誤處理

如果目前狀態不是 `Ready`，回傳 `accepted=false` 與 `errorCode=invalid_state`。

## POST /api/system/start

### 用途

啟動一次執行。API 在系統進入 `Running` 後立即回應；執行完成需要透過事件或 `GET /api/results` 觀察。

`Start` 被接受時會立即保存目前 ProductInfo 快照，後續產生的結果會使用這份快照。

### Request

| 項目 | 值 |
| --- | --- |
| Method | `POST` |
| Route | `/api/system/start` |
| Query | none |
| Body | [SystemStartRequest](payloads/commands/system-start-request.zh-Hant.md) 或無 |

### Request Body

```json
{
  "condition": "golden-sample",
  "runMode": "continue"
}
```

`condition` 與 `runMode` 都是選用欄位。`runMode` 省略或空白時預設為 `continue`，也支援 `single`。

### Response

| HTTP status | Body | 說明 |
| --- | --- | --- |
| `200 OK` | [CommandResponse](payloads/commands/command-response.zh-Hant.md) | 已進入 `Running`。 |
| `409 Conflict` | [CommandResponse](payloads/commands/command-response.zh-Hant.md) | 目前狀態不允許啟動。 |

### 範例

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


### 狀態限制

只允許在 `Ready` 呼叫。

### 錯誤處理

如果目前狀態不是 `Ready`，回傳 `accepted=false` 與 `errorCode=invalid_state`。

## POST /api/system/stop

### 用途

停止目前執行。API 在狀態回到 `Ready` 後回應。

### Request

| 項目 | 值 |
| --- | --- |
| Method | `POST` |
| Route | `/api/system/stop` |
| Query | none |
| Body | [SystemStopRequest](payloads/commands/system-stop-request.zh-Hant.md) 或無 |

### Request Body

```json
{
  "reason": "operator-request"
}
```

`reason` 是選用欄位。

### Response

| HTTP status | Body | 說明 |
| --- | --- | --- |
| `200 OK` | [CommandResponse](payloads/commands/command-response.zh-Hant.md) | 已停止並回到 `Ready`。 |
| `409 Conflict` | [CommandResponse](payloads/commands/command-response.zh-Hant.md) | 目前狀態不允許停止。 |

### 範例

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


### 狀態限制

只允許在 `Running` 呼叫。

### 錯誤處理

如果目前狀態不是 `Running`，回傳 `accepted=false` 與 `errorCode=invalid_state`。

## GET /api/results

### 用途

查詢結果摘要。結果只包含公開 summary，不包含瑕疵清單、die 清單、裁切清單、影像二進位資料或私有檢測內部資料。

### Request

| 項目 | 值 |
| --- | --- |
| Method | `GET` |
| Route | `/api/results` |
| Query | `waferID`, `lotID`, `recipe` |
| Body | none |

### Query Parameters

| Name | Required | 說明 |
| --- | --- | --- |
| `waferID` | No | 依 Wafer ID 篩選。 |
| `lotID` | No | 依 Lot ID 篩選。 |
| `recipe` | No | 依 Recipe 篩選。 |

多個 query parameter 以 AND 組合。

### Response

| HTTP status | Body | 說明 |
| --- | --- | --- |
| `200 OK` | [ResultList](payloads/results/result-list.zh-Hant.md) | 回傳符合條件的結果摘要。 |
| `400 Bad Request` | error text | Query parameter 不支援。 |

### 範例

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


### 狀態限制

任何狀態都可以呼叫。

### 錯誤處理

如果 query parameter 不是 `waferID`、`lotID` 或 `recipe`，回傳 `400 Bad Request`。

## 共通錯誤處理

### invalid_state

系統命令或 ProductInfo 更新在不合法狀態下呼叫時，會回傳 `409 Conflict` 與 [CommandResponse](payloads/commands/command-response.zh-Hant.md)：

```json
{
  "accepted": false,
  "state": "Running",
  "command": "SetProductInfo",
  "errorCode": "invalid_state",
  "message": "SetProductInfo is not valid while state is Running."
}
```

用戶端應先讀取 `GET /api/status`，等狀態允許後再重送命令。

### bad_request

Request body 無法解析，或 query parameter 不支援時，會回傳 `400 Bad Request`。

### 傳輸或 HTTP 錯誤

連線失敗、服務未啟動、逾時、非 JSON 回應，或非預期 HTTP status，應視為傳輸或 HTTP 錯誤，而不是設備狀態拒絕。
