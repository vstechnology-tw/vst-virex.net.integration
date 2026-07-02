# RESTful API

The RESTful API is used to read state, manage ProductInfo, send system commands, and query result summaries.

## Basic information

| item | value |
| --- | --- |
| Base URL | `http://127.0.0.1:5088` |
| OpenAPI JSON | `http://127.0.0.1:5088/openapi/v1.json` |
| Scalar UI | `http://127.0.0.1:5088/scalar` |
| Content-Type | `application/json` |

All request and response bodies use JSON. `POST` without body can send empty body.

## Endpoint Overview

| Category | Method | Route | Purpose | Valid State | Successful Response |
| --- | --- | --- | --- | --- | --- |
| Status | GET | `/api/status` | Read the current system state. | Any | [SystemStatus](payloads/system/system-status.md) |
| Error | GET | `/api/error` | Read the current public error state. | Any | [ErrorInfo](payloads/system/error-info.md) |
| ProductInfo | GET | `/api/product-info` | Read the current ProductInfo. | Any | [ProductInfo](payloads/product/product-info.md) |
| ProductInfo | POST | `/api/product-info` | Updates the current ProductInfo. | `Ready` | [CommandResponse](payloads/commands/command-response.md) |
| System | POST | `/api/system/initialize` | Initialize the system. | `Uninitialized` | [CommandResponse](payloads/commands/command-response.md) |
| System | POST | `/api/system/deinitialize` | Deinitialize the system. | `Ready` | [CommandResponse](payloads/commands/command-response.md) |
| System | POST | `/api/system/start` | Start a run. | `Ready` | [CommandResponse](payloads/commands/command-response.md) |
| System | POST | `/api/system/stop` | Stop the current run. | `Running` | [CommandResponse](payloads/commands/command-response.md) |
| Results | GET | `/api/results` | Query result summaries. | Any | [ResultList](payloads/results/result-list.md) |

## GET /api/status

### Purpose

Read the current public system state. The client can use this state to decide whether the next command can be sent.

### Request

| item | value |
| --- | --- |
| Method | `GET` |
| Route | `/api/status` |
| Query | none |
| Body | none |

### Response

| HTTP status | Body | Description |
| --- | --- | --- |
| `200 OK` | [SystemStatus](payloads/system/system-status.md) | Returns the current state. |

### Example

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


### State Restrictions

Can be called in any state.

### Error handling

Generally, [CommandResponse](payloads/commands/command-response.md) will not be returned. Connection failures, service not started, or non-JSON responses should be considered transport / HTTP failures.

## GET /api/error

### Purpose

Read the current public error information. This is a query, not a lifecycle command; it can be used at any time to show whether the compatible service is currently reporting an application-level error.

### Request

| item | value |
| --- | --- |
| Method | `GET` |
| Route | `/api/error` |
| Query | none |
| Body | none |

### Response

| HTTP status | Body | Description |
| --- | --- | --- |
| `200 OK` | [ErrorInfo](payloads/system/error-info.md) | Returns the current public error information. |

### Example

=== "C# SDK"

    ```csharp
    var error = await client.GetErrorAsync();
    // 200 OK body: {"hasError":false,"state":"Ready"}
    Console.WriteLine(error.HasError);
    ```

=== "C# Raw"

    ```csharp
    var error = await http.GetFromJsonAsync<ErrorInfo>("/api/error");
    // 200 OK body: {"hasError":false,"state":"Ready"}
    Console.WriteLine(error?.HasError);
    ```

=== "Python"

    ```python
    with urllib.request.urlopen("http://127.0.0.1:5088/api/error") as response:
        error = json.loads(response.read().decode("utf-8"))
        # 200 OK body: {"hasError":false,"state":"Ready"}
        print(error["hasError"])
    ```

=== "C++"

    ```cpp
    const auto response = SendRequest(url, L"GET", L"/api/error");
    // HTTP 200 body: {"hasError":false,"state":"Ready"}
    std::cout << response.body << std::endl;
    ```


### State Restrictions

Can be called in any state.

### Error handling

Generally, [CommandResponse](payloads/commands/command-response.md) will not be returned. Connection failures, service not started, or non-JSON responses should be considered transport / HTTP failures.

## GET /api/product-info

### Purpose

Read the current ProductInfo. ProductInfo is saved as a result snapshot when `Start` is accepted.

### Request

| item | value |
| --- | --- |
| Method | `GET` |
| Route | `/api/product-info` |
| Query | none |
| Body | none |

### Response

| HTTP status | Body | Description |
| --- | --- | --- |
| `200 OK` | [ProductInfo](payloads/product/product-info.md) | Return the current ProductInfo. |

### Example

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


### State Restrictions

Can be called in any state.

### Error handling

Generally, [CommandResponse](payloads/commands/command-response.md) will not be returned. Connection failures, service not started, or non-JSON responses should be considered transport / HTTP failures.

## POST /api/product-info

### Purpose

Update current ProductInfo. This API will wait until `ProductInfoUpdateCompleted` before responding, so the successfully responded `state` will be `Ready`.

### Request

| item | value |
| --- | --- |
| Method | `POST` |
| Route | `/api/product-info` |
| Query | none |
| Body | [ProductInfo](payloads/product/product-info.md) |

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

| HTTP status | Body | Description |
| --- | --- | --- |
| `200 OK` | [CommandResponse](payloads/commands/command-response.md) | ProductInfo update completed. |
| `409 Conflict` | [CommandResponse](payloads/commands/command-response.md) | The current state does not allow ProductInfo updates. |
| `400 Bad Request` | error text | Request body is not a valid ProductInfo. |

### Example

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


### State Restrictions

Only calls allowed at `Ready`.

### Error handling

If the current state is not `Ready`, returns:

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

### Purpose

Initialize the system. The API will wait until `InitializationCompleted` before responding, so the successfully responded `state` will be `Ready`.

### Request

| item | value |
| --- | --- |
| Method | `POST` |
| Route | `/api/system/initialize` |
| Query | none |
| Body | none |

### Response

| HTTP status | Body | Description |
| --- | --- | --- |
| `200 OK` | [CommandResponse](payloads/commands/command-response.md) | Initialization completed. |
| `409 Conflict` | [CommandResponse](payloads/commands/command-response.md) | The current state does not allow initialization. |

### Example

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


### State Restrictions

Only calls allowed at `Uninitialized`.

### Error handling

If the current state is not `Uninitialized`, returns `accepted=false` and `errorCode=invalid_state`.

## POST /api/system/deinitialize

### Purpose

De-initialization system. The API will wait until `DeinitializationCompleted` before responding, so the successfully responded `state` will be `Uninitialized`.

### Request

| item | value |
| --- | --- |
| Method | `POST` |
| Route | `/api/system/deinitialize` |
| Query | none |
| Body | none |

### Response

| HTTP status | Body | Description |
| --- | --- | --- |
| `200 OK` | [CommandResponse](payloads/commands/command-response.md) | Deinitialization completed. |
| `409 Conflict` | [CommandResponse](payloads/commands/command-response.md) | The current state does not allow deinitialization. |

### Example

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


### State Restrictions

Only calls allowed at `Ready`.

### Error handling

If the current state is not `Ready`, returns `accepted=false` and `errorCode=invalid_state`.

## POST /api/system/start

### Purpose

Starts a run. The API responds immediately after the system enters `Running`; run completion should be observed through events or `GET /api/results`.

When `Start` is accepted, the current ProductInfo snapshot will be saved immediately, and subsequent results will use this snapshot.

### Request

| item | value |
| --- | --- |
| Method | `POST` |
| Route | `/api/system/start` |
| Query | none |
| Body | [SystemStartRequest](payloads/commands/system-start-request.md) or none |

### Request Body

```json
{
  "condition": "golden-sample",
  "runMode": "continue"
}
```

`condition` and `runMode` are optional fields. `runMode` defaults to `continue` when omitted or blank, and `single` is also supported.

### Response

| HTTP status | Body | Description |
| --- | --- | --- |
| `200 OK` | [CommandResponse](payloads/commands/command-response.md) | Entered `Running`. |
| `409 Conflict` | [CommandResponse](payloads/commands/command-response.md) | The current state does not allow start. |

### Example

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


### State Restrictions

Only calls allowed at `Ready`.

### Error handling

If the current state is not `Ready`, returns `accepted=false` and `errorCode=invalid_state`.

## POST /api/system/stop

### Purpose

Stops the current run. The API responds after the state returns to `Ready`.

### Request

| item | value |
| --- | --- |
| Method | `POST` |
| Route | `/api/system/stop` |
| Query | none |
| Body | [SystemStopRequest](payloads/commands/system-stop-request.md) or none |

### Request Body

```json
{
  "reason": "operator-request"
}
```

`reason` is the optional field.

### Response

| HTTP status | Body | Description |
| --- | --- | --- |
| `200 OK` | [CommandResponse](payloads/commands/command-response.md) | Stopped and returned to `Ready`. |
| `409 Conflict` | [CommandResponse](payloads/commands/command-response.md) | The current state does not allow stop. |

### Example

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


### State Restrictions

Only calls allowed at `Running`.

### Error handling

If the current state is not `Running`, returns `accepted=false` and `errorCode=invalid_state`.

## GET /api/results

### Purpose

Queries result summaries. Results include public summaries only; they do not include defect lists, die lists, crop lists, image binaries, or private inspection internals.

### Request

| item | value |
| --- | --- |
| Method | `GET` |
| Route | `/api/results` |
| Query | `waferID`, `lotID`, `recipe` |
| Body | none |

### Query Parameters

| Name | Required | Description |
| --- | --- | --- |
| `waferID` | No | Filter by Wafer ID. |
| `lotID` | No | Filter by Lot ID. |
| `recipe` | No | Filter by Recipe. |

Multiple query parameters are combined with AND.

### Response

| HTTP status | Body | Description |
| --- | --- | --- |
| `200 OK` | [ResultList](payloads/results/result-list.md) | Returns a summary of qualified results. |
| `400 Bad Request` | error text | Query parameter is not supported. |

### Example

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


### State Restrictions

Can be called in any state.

### Error handling

If the query parameter is not `waferID`, `lotID` or `recipe`, `400 Bad Request` is returned.

## Common error handling

### invalid_state

When a system command or ProductInfo update is called in an invalid state, the API returns `409 Conflict` and [CommandResponse](payloads/commands/command-response.md):

```json
{
  "accepted": false,
  "state": "Running",
  "command": "SetProductInfo",
  "errorCode": "invalid_state",
  "message": "SetProductInfo is not valid while state is Running."
}
```

The client should read `GET /api/status` first, then resend the command when the state allows it.

### bad_request

When the Request body cannot be parsed or the query parameter is not supported, `400 Bad Request` will be returned.

### transport / HTTP failure

Connection failures, service not started, timeouts, non-JSON responses, or unexpected HTTP status codes should be treated as transport / HTTP failures, not state-based command rejections.
