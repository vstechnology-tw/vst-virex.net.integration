# RESTful API

RESTful API는 상태를 읽고, ProductInfo를 관리하고, 시스템 명령을 보내고, 결과 요약을 쿼리하는 데 사용됩니다.

## 기본 정보

| 항목 | 값 |
| --- | --- |
| 기본 URL | `http://127.0.0.1:5088` |
| OpenAPI JSON | `http://127.0.0.1:5088/openapi/v1.json` |
| Scalar UI | `http://127.0.0.1:5088/scalar` |
| 콘텐츠 형식 | `application/json` |

모든 요청 및 응답 본문은 JSON을 사용합니다. 본문이 없는 `POST`는 빈 본문을 보낼 수 있습니다.

## 엔드포인트 개요

| 분류 | 메서드 | 경로 | 목적 | 유효한 상태 | 성공 응답 |
| --- | --- | --- | --- | --- | --- |
| 상태 | GET | `/api/status` | 현재 시스템 상태를 읽습니다. | 모두 | [SystemStatus](payloads/system/system-status.ko.md) |
| 오류 | GET | `/api/error` | 현재 공개 오류 정보를 읽습니다. | 모두 | [ErrorInfo](payloads/system/error-info.ko.md) |
| ProductInfo | GET | `/api/product-info` | 현재 ProductInfo를 읽습니다. | 모두 | [ProductInfo](payloads/product/product-info.ko.md) |
| ProductInfo | POST | `/api/product-info` | 현재 ProductInfo를 업데이트합니다. | `Ready` | [CommandResponse](payloads/commands/command-response.ko.md) |
| 시스템 | POST | `/api/system/initialize` | 시스템을 초기화합니다. | `Uninitialized` | [CommandResponse](payloads/commands/command-response.ko.md) |
| 시스템 | POST | `/api/system/deinitialize` | 시스템 초기화를 해제합니다. | `Ready` | [CommandResponse](payloads/commands/command-response.ko.md) |
| 시스템 | POST | `/api/system/start` | 실행을 시작합니다. | `Ready` | [CommandResponse](payloads/commands/command-response.ko.md) |
| 시스템 | POST | `/api/system/stop` | 현재 실행을 중지합니다. | `Running` | [CommandResponse](payloads/commands/command-response.ko.md) |
| 결과 | GET | `/api/results` | 결과 요약을 조회합니다. | 모두 | [ResultList](payloads/results/result-list.ko.md) |

## GET /api/status

### 목적

현재 공개 시스템 상태를 읽습니다. 클라이언트는 이 상태를 사용하여 다음 명령을 보낼 수 있는지 여부를 결정할 수 있습니다.

### 요청

| 항목 | 값 |
| --- | --- |
| 메서드 | `GET` |
| 경로 | `/api/status` |
| 쿼리 | 없음 |
| 본문 | 없음 |

### 응답

| HTTP 상태 | 본문 | 설명 |
| --- | --- | --- |
| `200 OK` | [SystemStatus](payloads/system/system-status.ko.md) | 현재 상태를 반환합니다. |

### 예

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


### 상태 제한

모든 상태에서 호출할 수 있습니다.

### 오류 처리

일반적으로 [CommandResponse](payloads/commands/command-response.ko.md)는 반환되지 않습니다. 연결 실패, 서비스가 시작되지 않음 또는 JSON이 아닌 응답은 전송 방식/HTTP 오류로 간주되어야 합니다.

## GET /api/error

### 목적

현재 공개 오류 정보를 읽습니다. 이는 수명 주기 명령이 아니라 쿼리이며, 호환 서비스가 현재 애플리케이션 수준 오류를 보고하는지 표시하기 위해 모든 상태에서 호출할 수 있습니다.

### 요청

| 항목 | 값 |
| --- | --- |
| 메서드 | `GET` |
| 경로 | `/api/error` |
| 쿼리 | 없음 |
| 본문 | 없음 |

### 응답

| HTTP 상태 | 본문 | 설명 |
| --- | --- | --- |
| `200 OK` | [ErrorInfo](payloads/system/error-info.ko.md) | 현재 공개 오류 정보를 반환합니다. |

### 예

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


### 상태 제한

모든 상태에서 호출할 수 있습니다.

### 오류 처리

일반적으로 [CommandResponse](payloads/commands/command-response.ko.md)는 반환되지 않습니다. 연결 실패, 서비스가 시작되지 않음 또는 JSON이 아닌 응답은 전송 방식/HTTP 오류로 간주되어야 합니다.

## GET /api/product-info

### 목적

현재 ProductInfo를 읽습니다. ProductInfo는 `Start`가 수락되면 결과 스냅샷으로 저장됩니다.

### 요청

| 항목 | 값 |
| --- | --- |
| 메서드 | `GET` |
| 경로 | `/api/product-info` |
| 쿼리 | 없음 |
| 본문 | 없음 |

### 응답

| HTTP 상태 | 본문 | 설명 |
| --- | --- | --- |
| `200 OK` | [ProductInfo](payloads/product/product-info.ko.md) | 현재 ProductInfo를 반환합니다. |

### 예

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


### 상태 제한

모든 상태에서 호출할 수 있습니다.

### 오류 처리

일반적으로 [CommandResponse](payloads/commands/command-response.ko.md)는 반환되지 않습니다. 연결 실패, 서비스가 시작되지 않음 또는 JSON이 아닌 응답은 전송 방식/HTTP 오류로 간주되어야 합니다.

## POST /api/product-info

### 목적

현재 ProductInfo를 업데이트하세요. 이 API는 응답하기 전에 `ProductInfoUpdateCompleted`까지 기다리므로 성공적으로 응답된 `state`는 `Ready`가 됩니다.

### 요청

| 항목 | 값 |
| --- | --- |
| 메서드 | `POST` |
| 경로 | `/api/product-info` |
| 쿼리 | 없음 |
| 본문 | [ProductInfo](payloads/product/product-info.ko.md) |

### 요청 본문

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

### 응답

| HTTP 상태 | 본문 | 설명 |
| --- | --- | --- |
| `200 OK` | [CommandResponse](payloads/commands/command-response.ko.md) | ProductInfo 업데이트가 완료되었습니다. |
| `409 Conflict` | [CommandResponse](payloads/commands/command-response.ko.md) | 현재 상태에서는 ProductInfo 업데이트가 허용되지 않습니다. |
| `400 Bad Request` | 오류 텍스트 | 요청 본문이 유효한 ProductInfo가 아닙니다. |

### 예

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


### 상태 제한

`Ready`에서만 호출할 수 있습니다.

### 오류 처리

현재 상태가 `Ready`가 아닌 경우 다음을 반환합니다.

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

### 목적

시스템을 초기화합니다. API는 응답하기 전에 `InitializationCompleted`까지 기다리므로 성공적으로 응답된 `state`는 `Ready`가 됩니다.

### 요청

| 항목 | 값 |
| --- | --- |
| 메서드 | `POST` |
| 경로 | `/api/system/initialize` |
| 쿼리 | 없음 |
| 본문 | 없음 |

### 응답

| HTTP 상태 | 본문 | 설명 |
| --- | --- | --- |
| `200 OK` | [CommandResponse](payloads/commands/command-response.ko.md) | 초기화가 완료되었습니다. |
| `409 Conflict` | [CommandResponse](payloads/commands/command-response.ko.md) | 현재 상태에서는 초기화가 허용되지 않습니다. |

### 예

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


### 상태 제한

`Uninitialized`에서만 호출할 수 있습니다.

### 오류 처리

현재 상태가 `Uninitialized`가 아닌 경우 `accepted=false` 및 `errorCode=invalid_state`를 반환합니다.

## POST /api/system/deinitialize

### 목적

초기화 해제 시스템. API는 응답하기 전에 `DeinitializationCompleted`까지 기다리므로 성공적으로 응답된 `state`는 `Uninitialized`가 됩니다.

### 요청

| 항목 | 값 |
| --- | --- |
| 메서드 | `POST` |
| 경로 | `/api/system/deinitialize` |
| 쿼리 | 없음 |
| 본문 | 없음 |

### 응답

| HTTP 상태 | 본문 | 설명 |
| --- | --- | --- |
| `200 OK` | [CommandResponse](payloads/commands/command-response.ko.md) | 초기화 해제가 완료되었습니다. |
| `409 Conflict` | [CommandResponse](payloads/commands/command-response.ko.md) | 현재 상태에서는 초기화 해제가 허용되지 않습니다. |

### 예

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


### 상태 제한

`Ready`에서만 호출할 수 있습니다.

### 오류 처리

현재 상태가 `Ready`가 아닌 경우 `accepted=false` 및 `errorCode=invalid_state`를 반환합니다.

## POST /api/system/start

### 목적

실행을 시작합니다. API는 시스템이 `Running` 상태로 전환된 후 즉시 응답합니다. 실행 완료는 이벤트 또는 `GET /api/results`를 통해 관찰되어야 합니다.

`Start`가 수락되면 현재 ProductInfo 스냅샷이 즉시 저장되며 후속 결과에서는 이 스냅샷을 사용합니다.

### 요청

| 항목 | 값 |
| --- | --- |
| 메서드 | `POST` |
| 경로 | `/api/system/start` |
| 쿼리 | 없음 |
| 본문 | [SystemStartRequest](payloads/commands/system-start-request.ko.md) 또는 없음 |

### 요청 본문

```json
{
  "condition": "golden-sample",
  "runMode": "continue"
}
```

`condition` 및 `runMode`는 선택 필드입니다. `runMode`는 생략되거나 공백인 경우 기본값은 `continue`이며, `single`도 지원됩니다.

### 응답

| HTTP 상태 | 본문 | 설명 |
| --- | --- | --- |
| `200 OK` | [CommandResponse](payloads/commands/command-response.ko.md) | `Running` 상태로 전환되었습니다. |
| `409 Conflict` | [CommandResponse](payloads/commands/command-response.ko.md) | 현재 상태에서는 시작할 수 없습니다. |

### 예

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


### 상태 제한

`Ready`에서만 호출할 수 있습니다.

### 오류 처리

현재 상태가 `Ready`가 아닌 경우 `accepted=false` 및 `errorCode=invalid_state`를 반환합니다.

## POST /api/system/stop

### 목적

현재 실행을 중지합니다. API는 상태가 `Ready`로 반환된 후 응답합니다.

### 요청

| 항목 | 값 |
| --- | --- |
| 메서드 | `POST` |
| 경로 | `/api/system/stop` |
| 쿼리 | 없음 |
| 본문 | [SystemStopRequest](payloads/commands/system-stop-request.ko.md) 또는 없음 |

### 요청 본문

```json
{
  "reason": "operator-request"
}
```

`reason`는 선택적 필드입니다.

### 응답

| HTTP 상태 | 본문 | 설명 |
| --- | --- | --- |
| `200 OK` | [CommandResponse](payloads/commands/command-response.ko.md) | 중지되어 `Ready`로 돌아왔습니다. |
| `409 Conflict` | [CommandResponse](payloads/commands/command-response.ko.md) | 현재 상태에서는 정지가 허용되지 않습니다. |

### 예

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


### 상태 제한

`Running`에서만 호출할 수 있습니다.

### 오류 처리

현재 상태가 `Running`가 아닌 경우 `accepted=false` 및 `errorCode=invalid_state`를 반환합니다.

## GET /api/results

### 목적

결과 요약을 조회합니다. 결과에는 공개 요약만 포함됩니다. 여기에는 결함 목록, 다이 목록, 자르기 목록, 이미지 바이너리 또는 비공개 검사 내부가 포함되지 않습니다.

### 요청

| 항목 | 값 |
| --- | --- |
| 메서드 | `GET` |
| 경로 | `/api/results` |
| 쿼리 | `waferID`, `lotID`, `recipe` |
| 본문 | 없음 |

### 쿼리 매개변수

| 이름 | 필수 | 설명 |
| --- | --- | --- |
| `waferID` | 아니요 | 웨이퍼 ID로 필터링합니다. |
| `lotID` | 아니요 | 로트 ID로 필터링합니다. |
| `recipe` | 아니요 | 레시피로 필터링하세요. |

여러 쿼리 매개변수는 AND로 결합됩니다.

### 응답

| HTTP 상태 | 본문 | 설명 |
| --- | --- | --- |
| `200 OK` | [ResultList](payloads/results/result-list.ko.md) | 조건에 맞는 결과 요약을 반환합니다. |
| `400 Bad Request` | 오류 텍스트 | 쿼리 매개변수는 지원되지 않습니다. |

### 예

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


### 상태 제한

모든 상태에서 호출할 수 있습니다.

### 오류 처리

쿼리 매개변수가 `waferID`, `lotID` 또는 `recipe`가 아닌 경우 `400 Bad Request`가 반환됩니다.

## 일반적인 오류 처리

### 무효_상태

시스템 명령 또는 ProductInfo 업데이트가 잘못된 상태에서 호출되면 API는 `409 Conflict` 및 [CommandResponse](payloads/commands/command-response.ko.md)를 반환합니다.

```json
{
  "accepted": false,
  "state": "Running",
  "command": "SetProductInfo",
  "errorCode": "invalid_state",
  "message": "SetProductInfo is not valid while state is Running."
}
```

클라이언트는 먼저 `GET /api/status`를 읽은 다음 상태에서 허용할 때 명령을 다시 보내야 합니다.

### 잘못된 요청

요청 본문을 구문 분석할 수 없거나 쿼리 매개변수가 지원되지 않는 경우 `400 Bad Request`가 반환됩니다.

### 전송 / HTTP 실패

연결 실패, 서비스가 시작되지 않음, 시간 초과, JSON이 아닌 응답 또는 예상치 못한 HTTP 상태 코드는 상태 기반 명령 거부가 아닌 전송 방식/HTTP 오류로 처리되어야 합니다.
