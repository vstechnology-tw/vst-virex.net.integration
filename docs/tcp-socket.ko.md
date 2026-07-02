# TCP 소켓 프로토콜

TCP 소켓은 동일한 단순 스트리밍 프로토콜을 통해 명령을 보내고 이벤트를 수신해야 하는 클라이언트를 위한 양방향 통합 채널입니다.

## 기본 정보

| 항목 | 값 |
| --- | --- |
| 기본 호스트 | `127.0.0.1` |
| 기본 포트 | `5089` |
| 프레이밍 방식 | NDJSON |
| 인코딩 | UTF-8 |
| 방향 | 클라이언트는 명령 및 쿼리 프레임을 보냅니다. 서비스는 직접 응답 및 이벤트 프레임을 보냅니다 |

각 프레임은 JSON 개체이며 `\n`로 끝납니다.

```text
{"type":"start","condition":"golden-sample","runMode":"continue"}\n
```

TCP/NDJSON를 읽을 때 C# SDK는 프레임당 유휴 시간 제한을 적용합니다. 완전한 프레임 사이에는 오랜 대기 시간이 있을 수 있지만 일단 프레임의 바이트가 도착하면 나머지 내용과 후행 줄 바꿈은 `VirexClientOptions.TcpFrameTimeoutMs` 내에 도착해야 합니다. 그렇지 않으면 TCP 이벤트 리더가 시간 초과를 보고합니다.

## 프레임 개요

### 수신 명령 및 쿼리

| 프레임 유형 | 페이로드 | 유효한 상태 | 결과 |
| --- | --- | --- | --- |
| `status` | `type`만 사용 | 모두 | 직접 응답 `type: "status"`를 반환합니다. |
| `error` | `type`만 사용 | 모두 | 직접 응답 `type: "error"`를 반환합니다. |
| `getProductInfo` | `type`만 사용 | 모두 | 직접 응답 `type: "productInfo"`를 반환합니다. |
| `initialize` | `type`가 포함된 [SystemInitializeRequest](payloads/commands/system-initialize-request.ko.md) | `Uninitialized` | `Initializing` 상태로 전환됩니다. 완료 후 `Ready` 상태의 `statusChanged`를 보냅니다. |
| `deinitialize` | `type`가 포함된 [SystemDeinitializeRequest](payloads/commands/system-deinitialize-request.ko.md) | `Ready` | `Deinitializing` 상태로 전환됩니다. 완료 후 `Uninitialized` 상태의 `statusChanged`를 보냅니다. |
| `productInfo` | `type`가 포함된 [ProductInfo](payloads/product/product-info.ko.md) | `Ready` | ProductInfo를 업데이트하고 `productInfoChanged`를 내보냅니다. |
| `start` | `type`가 포함된 [SystemStartRequest](payloads/commands/system-start-request.ko.md) | `Ready` | `Running` 상태로 전환됩니다. 완료는 이벤트와 결과로 보고됩니다. |
| `stop` | `type`가 포함된 [SystemStopRequest](payloads/commands/system-stop-request.ko.md) | `Running` | 실행을 중지하고 `Ready`로 돌아갑니다. |
| `results` | 결과 쿼리 조건과 `type` | 모두 | 직접 응답 `type: "results"`를 반환합니다. |

### 송신 이벤트

| 프레임 유형 | 페이로드 | 송신 시점 |
| --- | --- | --- |
| `statusChanged` | `type`가 포함된 [SystemStatus](payloads/system/system-status.ko.md) | 공개 상태가 변경됩니다. |
| `productInfoChanged` | `type`가 포함된 [ProductInfo](payloads/product/product-info.ko.md) | ProductInfo 업데이트가 완료되었습니다. |
| `runStarted` | `type`가 포함된 [SystemStatus](payloads/system/system-status.ko.md) | `Running` 상태로 전환됩니다. |
| `runCompleted` | `type`가 포함된 [SystemStatus](payloads/system/system-status.ko.md) | 실행이 `Running` 상태를 벗어나 `Ready`로 돌아갑니다. |
| `resultCreated` | `type`가 포함된 [ResultSummary](payloads/results/result-summary.ko.md) | 결과 요약이 생성됩니다. |
| `errorChanged` | `type`가 포함된 [ErrorInfo](payloads/system/error-info.ko.md) | 공개 오류 정보가 변경됩니다. |
| `commandRejected` | `type`가 포함된 [CommandResponse](payloads/commands/command-response.ko.md) | 명령이 거부됩니다. |

## 연결 예

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

## initialize 명령

### 목적

TCP를 통해 시스템을 초기화합니다. `InitializationCompleted`가 공개 상태를 `Ready`로 변경하면 명령이 완료됩니다.

### 프레임

```json
{"type":"initialize"}
```

### 페이로드

`type: "initialize"` 외에 필요한 필드는 없습니다.

### 상태 제한

`Uninitialized`에서만 유효합니다.

### 성공 이벤트

서비스는 다음을 보냅니다.

```json
{"type":"statusChanged","state":"Ready"}
```

### 오류 처리

현재 상태가 `Uninitialized`가 아닌 경우 서비스는 `commandRejected`를 보냅니다.

## deinitialize 명령

### 목적

TCP를 통해 시스템을 반초기화합니다. `DeinitializationCompleted`가 공개 상태를 `Uninitialized`로 변경하면 명령이 완료됩니다.

### 프레임

```json
{"type":"deinitialize"}
```

### 페이로드

`type: "deinitialize"` 외에 필요한 필드는 없습니다.

### 상태 제한

`Ready`에서만 유효합니다.

### 성공 이벤트

서비스는 다음을 보냅니다.

```json
{"type":"statusChanged","state":"Uninitialized"}
```

### 오류 처리

현재 상태가 `Ready`가 아닌 경우 서비스는 `commandRejected`를 보냅니다.

## productInfo 명령

### 목적

TCP를 통해 현재 ProductInfo를 업데이트합니다.

### 프레임

```json
{"type":"productInfo","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1"}
```

### 페이로드

`type: "productInfo"`가 포함된 [ProductInfo](payloads/product/product-info.ko.md).

### 상태 제한

`Ready`에서만 유효합니다.

### 성공 이벤트

서비스는 다음을 보냅니다.

```json
{"type":"productInfoChanged","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1"}
```

### 오류 처리

현재 상태가 `Ready`가 아닌 경우 서비스는 `commandRejected`를 보냅니다.

## start 명령

### 목적

TCP를 통해 실행을 시작합니다. 시스템이 `Running` 상태로 전환되면 명령이 수락됩니다. 실행 완료는 나중에 이벤트 및 결과 프레임을 통해 전달됩니다.

### 프레임

```json
{"type":"start","condition":"golden-sample","runMode":"continue"}
```

### 페이로드

`type: "start"`가 포함된 [SystemStartRequest](payloads/commands/system-start-request.ko.md).

### 상태 제한

`Ready`에서만 유효합니다.

### 성공 이벤트

서비스는 `statusChanged`, `runStarted`, 이후 `resultCreated` 및 `runCompleted`를 내보냅니다.

```json
{"type":"runStarted","state":"Running"}
```

### 오류 처리

현재 상태가 `Ready`가 아니거나 `runMode`가 유효하지 않은 경우 서비스는 `commandRejected`를 보냅니다.

## stop 명령

### 목적

TCP를 통한 현재 실행을 중지합니다.

### 프레임

```json
{"type":"stop","reason":"operator-request"}
```

### 페이로드

`type: "stop"`가 포함된 [SystemStopRequest](payloads/commands/system-stop-request.ko.md).

### 상태 제한

`Running`에서만 유효합니다.

### 성공 이벤트

서비스는 다음을 보냅니다.

```json
{"type":"statusChanged","state":"Ready"}
```

### 오류 처리

현재 상태가 `Running`가 아닌 경우 서비스는 `commandRejected`를 보냅니다.

## status 쿼리

### 목적

TCP를 통해 현재 공개 시스템 상태를 읽습니다. 이는 쿼리 프레임이며 수명 주기 명령이 아니므로 모든 상태에서 보낼 수 있습니다.

### 프레임

```json
{"type":"status"}
```

### 페이로드

`type: "status"` 외에는 필드가 필요하지 않습니다.

### 상태 제한

모든 상태에서 호출할 수 있습니다.

### 응답 프레임

서비스는 직접 응답을 보냅니다.

```json
{"type":"status","state":"Ready"}
```

### 참고

쿼리 응답의 `type`은 `status`입니다. 상태 변경 이벤트는 계속 `statusChanged`를 사용합니다.

## error 쿼리

### 목적

TCP를 통해 현재 공개 오류 정보를 읽습니다. 이 쿼리는 RESTful API `GET /api/error` 및 MQTT `commands/error/get`과 동일한 [ErrorInfo](payloads/system/error-info.ko.md) 형태를 반환합니다.

### 프레임

```json
{"type":"error"}
```

### 페이로드

`type: "error"` 외에는 필드가 필요하지 않습니다.

### 상태 제한

모든 상태에서 호출할 수 있습니다.

### 응답 프레임

서비스는 직접 응답을 보냅니다.

```json
{"type":"error","hasError":false,"message":"","state":"Ready"}
```

### 참고

쿼리 응답의 `type`은 `error`입니다. 오류 변경 이벤트는 계속 `errorChanged`를 사용합니다.

## getProductInfo 쿼리

### 목적

시스템 상태를 변경하지 않고 TCP를 통해 현재 ProductInfo를 읽습니다.

### 프레임

```json
{"type":"getProductInfo"}
```

### 페이로드

`type: "getProductInfo"` 외에는 필드가 필요하지 않습니다.

### 상태 제한

모든 상태에서 호출할 수 있습니다.

### 응답 프레임

서비스는 직접 응답을 보냅니다.

```json
{"type":"productInfo","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1"}
```

### 참고

쿼리 응답의 `type`은 `productInfo`입니다. ProductInfo 업데이트 이벤트는 계속 `productInfoChanged`를 사용합니다.

## results 쿼리

### 목적

TCP를 통해 공개 결과 요약을 조회합니다. 결과에는 요약만 포함되며 비공개 검사 세부 정보, 결함 목록, 크롭 목록 또는 이미지 바이너리는 포함되지 않습니다.

### 프레임

```json
{"type":"results","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A"}
```

### 페이로드

| 필드 | 필수 | 설명 |
| --- | --- | --- |
| `type` | Yes | `results`여야 합니다. |
| `lotID` | No | 선택 사항인 Lot ID 필터입니다. |
| `waferID` | No | 선택 사항인 Wafer ID 필터입니다. |
| `recipe` | No | 선택 사항인 Recipe 필터입니다. |

여러 필터는 AND로 결합됩니다.

### 상태 제한

모든 상태에서 호출할 수 있습니다.

### 응답 프레임

서비스는 직접 응답을 보냅니다.

```json
{"type":"results","items":[{"resultId":"RID-1","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","condition":"golden-sample","overallResult":"OK","defectCount":0}],"count":1}
```

## statusChanged 이벤트

### 목적

공개 시스템 상태가 변경되었음을 클라이언트에 알립니다.

### 프레임

```json
{"type":"statusChanged","state":"Ready"}
```

### 페이로드

`type: "statusChanged"`가 포함된 [SystemStatus](payloads/system/system-status.ko.md).

### 참고

이 이벤트를 사용하여 클라이언트 UI와 명령 가용성을 동기화합니다.

## productInfoChanged 이벤트

### 목적

ProductInfo 업데이트가 완료되었음을 클라이언트에 알립니다.

### 프레임

```json
{"type":"productInfoChanged","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1"}
```

### 페이로드

`type: "productInfoChanged"`가 포함된 [ProductInfo](payloads/product/product-info.ko.md).

### 참고

이 이벤트에는 ProductInfo만 포함되어 있습니다.

## runStarted 이벤트

### 목적

실행이 시작되었으며 상태가 `Running`임을 클라이언트에 알립니다.

### 프레임

```json
{"type":"runStarted","state":"Running"}
```

### 페이로드

`type: "runStarted"`가 포함된 [SystemStatus](payloads/system/system-status.ko.md).

### 참고

이 이벤트가 수신될 때 실행이 아직 진행 중입니다.

## runCompleted 이벤트

### 목적

실행 수명 주기가 완료되고 상태가 `Ready`로 반환되었음을 클라이언트에 알립니다.

### 프레임

```json
{"type":"runCompleted","state":"Ready"}
```

### 페이로드

`type: "runCompleted"`가 포함된 [SystemStatus](payloads/system/system-status.ko.md).

### 참고

결과 세부정보는 `resultCreated`를 통해 전달됩니다.

## resultCreated 이벤트

### 목적

공개 결과 요약이 생성되었음을 클라이언트에 알립니다.

### 프레임

```json
{"type":"resultCreated","resultId":"RID-1","timestamp":"2026-06-20T15:30:12+08:00","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1","condition":"golden-sample","overallResult":"OK","defectCount":0,"imageRelativePath":"20260620/LOT-001/20260620_153012_W01.bmp","resultRelativePath":"20260620/LOT-001/20260620_153012_W01.json","imagePath":"/data/virex-results/20260620/LOT-001/20260620_153012_W01.bmp","previewImagePath":"/data/virex-results/20260620/LOT-001/20260620_153012_W01.jpg","resultPath":"/data/virex-results/20260620/LOT-001/20260620_153012_W01.json"}
```

### 페이로드

`type: "resultCreated"`가 포함된 [ResultSummary](payloads/results/result-summary.ko.md).

### 참고

결과에는 `Start`가 수락되었을 때 캡처된 ProductInfo 스냅샷과 `condition`가 포함됩니다. 요약만 제공하며 결함 목록, 자르기 목록, 이미지 바이너리 또는 비공개 검사 내부 내용은 포함하지 않습니다.

## errorChanged 이벤트

### 목적

공개 오류 정보가 변경되었음을 클라이언트에 알립니다.

### 프레임

```json
{"type":"errorChanged","hasError":true,"message":"Camera timeout.","state":"Running"}
```

### 페이로드

`type: "errorChanged"`가 포함된 [ErrorInfo](payloads/system/error-info.ko.md).

### 참고

이는 애플리케이션 계층 이벤트입니다. 소켓 연결 끊김, 시간 초과, 잘못된 JSON 및 불완전한 프레임은 전송 오류입니다.

## commandRejected 이벤트

### 목적

명령이 거부되었음을 클라이언트에 알립니다.

### 프레임

```json
{"type":"commandRejected","accepted":false,"state":"Running","command":"SetProductInfo","errorCode":"invalid_state","message":"SetProductInfo is not valid while state is Running."}
```

### 페이로드

`type: "commandRejected"`가 포함된 [CommandResponse](payloads/commands/command-response.ko.md).

### 참고

클라이언트는 이 이벤트를 전송 방식 오류로 처리해서는 안 됩니다. 이는 명령이 수락되지 않았음을 나타내는 유효한 애플리케이션 계층 응답입니다.

## 오류 처리

잘못된 형식의 JSON, 후행 줄 바꿈 누락, 지원되지 않는 프레임 유형, 소켓 연결 해제 및 읽기 시간 초과는 전송/프로토콜 오류입니다. 잘못된 상태, 잘못된 실행 모드 및 거부된 명령은 `commandRejected`를 통해 보고됩니다.
