# MQTT 이벤트

MQTT는 Virex.NET 호환 서비스에서 통합 클라이언트로 송신 이벤트 채널입니다. 명령이나 쿼리에는 사용되지 않습니다.

## 기본 정보

| 항목 | 값 |
| --- | --- |
| 기본 브로커 | `127.0.0.1:1883` |
| 기본 토픽 접두사 | `virex` |
| 토픽 형식 | `virex/{eventName}` |
| 데이터 형식 | JSON |
| 방향 | 서비스가 게시하고 클라이언트가 구독합니다 |

시뮬레이터는 **Start Servers**를 누른 후 내장된 MQTT 브로커를 시작합니다. 로컬 클라이언트에는 외부 브로커가 필요하지 않습니다.

## 토픽 개요

| 토픽 | 페이로드 | 게시 시점 |
| --- | --- | --- |
| `virex/statusChanged` | [SystemStatus](payloads/system/system-status.ko.md) | 공개 상태가 변경됩니다. |
| `virex/productInfoChanged` | [ProductInfo](payloads/product/product-info.ko.md) | ProductInfo 업데이트가 완료되었습니다. |
| `virex/runStarted` | [SystemStatus](payloads/system/system-status.ko.md) | `Running` 상태로 전환됩니다. |
| `virex/runCompleted` | [SystemStatus](payloads/system/system-status.ko.md) | 실행이 `Running` 상태를 벗어나 `Ready`로 돌아갑니다. |
| `virex/resultCreated` | [ResultSummary](payloads/results/result-summary.ko.md) | 결과 요약이 생성됩니다. |
| `virex/errorChanged` | [ErrorInfo](payloads/system/error-info.ko.md) | 공개 오류 정보가 변경됩니다. |
| `virex/commandRejected` | [CommandResponse](payloads/commands/command-response.ko.md) | 상태 규칙이나 유효성 검사에 의해 명령이 거부됩니다. |

## 구독 예시

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

### 목적

공개 시스템 상태가 변경되었음을 클라이언트에 알립니다.

### 토픽

```text
virex/statusChanged
```

### 페이로드

[SystemStatus](payloads/system/system-status.ko.md)

### 예

```json
{"state":"Ready"}
```

### 참고

클라이언트는 이 이벤트를 사용하여 UI 상태를 업데이트하고 다음 명령이 유효한지 여부를 결정할 수 있습니다.

## productInfoChanged

### 목적

`POST /api/product-info` 또는 TCP ProductInfo 명령이 완료되었음을 클라이언트에 알립니다.

### 토픽

```text
virex/productInfoChanged
```

### 페이로드

[ProductInfo](payloads/product/product-info.ko.md)

### 예

```json
{"lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1"}
```

### 참고

이 이벤트에는 ProductInfo만 포함되어 있습니다. 결과 데이터는 포함되지 않습니다.

## runStarted

### 목적

`Start` 명령이 수락되었고 시스템이 `Running`를 입력했음을 클라이언트에 알립니다.

### 토픽

```text
virex/runStarted
```

### 페이로드

[SystemStatus](payloads/system/system-status.ko.md)

### 예

```json
{"state":"Running"}
```

### 참고

`Start`는 실행이 완료되기 전에 응답합니다. 클라이언트는 `resultCreated`, `runCompleted`를 기다리거나 [GET /api/results](rest-api.ko.md#get-apiresults)를 쿼리해야 합니다.

## runCompleted

### 목적

현재 실행이 종료되었으며 공개 상태가 `Ready`로 반환되었음을 클라이언트에 알립니다.

### 토픽

```text
virex/runCompleted
```

### 페이로드

[SystemStatus](payloads/system/system-status.ko.md)

### 예

```json
{"state":"Ready"}
```

### 참고

이는 실행 수명 주기 완료를 나타냅니다. 결과 세부정보는 `resultCreated`를 통해 전달됩니다.

## resultCreated

### 목적

공개 결과 요약이 생성되었음을 클라이언트에 알립니다.

### 토픽

```text
virex/resultCreated
```

### 페이로드

[ResultSummary](payloads/results/result-summary.ko.md)

### 예

```json
{"resultId":"RID-1","timestamp":"2026-06-20T15:30:12+08:00","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1","condition":"golden-sample","overallResult":"OK","defectCount":0}
```

### 참고

결과에는 `Start`가 수락되었을 때 캡처된 ProductInfo 스냅샷과 `condition`가 포함됩니다. 요약만 제공하며 결함 목록, 자르기 목록, 이미지 바이너리 또는 비공개 검사 내부 내용은 포함하지 않습니다.

## errorChanged

### 목적

공개 오류 정보가 변경되었음을 클라이언트에 알립니다.

### 토픽

```text
virex/errorChanged
```

### 페이로드

[ErrorInfo](payloads/system/error-info.ko.md)

### 예

```json
{"hasError":true,"message":"Camera timeout.","state":"Running"}
```

### 참고

이 이벤트는 공개 오류 정보만 보고합니다. 연결 중단, 브로커 사용 불가능 및 구독 실패는 MQTT 클라이언트 연결 수준 오류입니다.

## commandRejected

### 목적

상태 규칙이나 유효성 검사에 의해 명령이 거부되었음을 클라이언트에 알립니다.

### 토픽

```text
virex/commandRejected
```

### 페이로드

[CommandResponse](payloads/commands/command-response.ko.md)

### 예

```json
{"accepted":false,"state":"Running","command":"SetProductInfo","errorCode":"invalid_state","message":"SetProductInfo is not valid while state is Running."}
```

### 참고

이 이벤트를 사용하여 거부된 RESTful API, TCP 또는 UI 명령을 연관시키십시오. 모든 전송은 동일한 주 규칙을 사용합니다.

## 오류 처리

MQTT 이벤트에는 HTTP 상태 코드가 없습니다. 잘못된 JSON, 알 수 없는 토픽, 브로커 연결 끊김 및 구독 실패는 전송 계층 오류로 처리되어야 합니다. `commandRejected`는 Virex.NET 호환 서비스에서 보고한 애플리케이션 계층 거부입니다.
