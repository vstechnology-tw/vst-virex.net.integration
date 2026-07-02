# MQTT Protocol

MQTT is a bidirectional integration channel. The service publishes events to `virex/{eventName}`. Clients can publish RESTful API equivalent commands and queries to `virex/commands/...` and receive correlated responses on `virex/responses/{correlationId}`.

## Basic Information

| Item | Value |
| --- | --- |
| Default broker | `127.0.0.1:1883` |
| Default root topic | `virex` |
| Topic format | Events: `virex/{eventName}`; commands: `virex/commands/...`; responses: `virex/responses/{correlationId}` |
| Data format | JSON |
| Direction | Service publishes events; clients publish command/query requests |

The simulator starts the embedded MQTT broker after **Start Servers** is pressed. A local client does not need an external broker.

## Event Topic Overview

| Topic | Payload | When Published |
| --- | --- | --- |
| `virex/statusChanged` | [SystemStatus](payloads/system/system-status.md) | Public state changes. |
| `virex/productInfoChanged` | [ProductInfo](payloads/product/product-info.md) | ProductInfo update completes. |
| `virex/runStarted` | [SystemStatus](payloads/system/system-status.md) | State enters `Running`. |
| `virex/runCompleted` | [SystemStatus](payloads/system/system-status.md) | A run leaves `Running` and returns to `Ready`. |
| `virex/resultCreated` | [ResultSummary](payloads/results/result-summary.md) | A result summary is created. |
| `virex/errorChanged` | [ErrorInfo](payloads/system/error-info.md) | Public error information changes. |
| `virex/commandRejected` | [CommandResponse](payloads/commands/command-response.md) | A command is rejected by state rules or validation. |

## Command Topic Overview

Each command or query payload should include `correlationId`. The response is published to `virex/responses/{correlationId}`. If `correlationId` is omitted, the service generates one, but clients should normally provide it so they can subscribe to and correlate the response deterministically.

| RESTful API equivalent | MQTT command topic | Response payload field |
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

## Command request envelope

All MQTT command topics accept a UTF-8 JSON object. The common fields are:

| Field | Used by | Description |
| --- | --- | --- |
| `correlationId` | All command topics | Client-provided request id. The response is published to `virex/responses/{correlationId}`. |
| `productInfo` | `commands/product-info/set` | Optional nested [ProductInfo](payloads/product/product-info.md). The service also accepts a flat ProductInfo payload for compatibility. |
| `condition` | `commands/system/start` | Optional run condition copied into result summaries. |
| `runMode` | `commands/system/start` | Optional run mode. Supported values are documented in [ControlRunModes](payloads/commands/control-run-modes.md). |
| `reason` | `commands/system/stop` | Optional stop reason. |
| `lotID` | `commands/results/query` | Optional result query filter. |
| `waferID` | `commands/results/query` | Optional result query filter. |
| `recipe` | `commands/results/query` | Optional result query filter. |

Example request:

```json
{"correlationId":"start-1","condition":"golden-sample","runMode":"continue"}
```

## Command response envelope

All command response payloads use the same envelope:

| Field | Description |
| --- | --- |
| `correlationId` | The request id used to build the response topic. |
| `topic` | The command topic below the base topic, for example `commands/status/get`. |
| `accepted` | `true` when the command/query was accepted. For lifecycle commands, this mirrors `commandResponse.accepted`. |
| `errorCode` | Present for MQTT-level failures such as `unknown_topic`. |
| `message` | Optional MQTT-level message. |
| `status` | Present for `commands/status/get`. |
| `error` | Present for `commands/error/get`. |
| `productInfo` | Present for `commands/product-info/get`. |
| `commandResponse` | Present for state-changing commands such as initialize, set ProductInfo, start, stop, and deinitialize. |
| `results` | Present for `commands/results/query`. |

Example response topic:

```text
virex/responses/start-1
```

Example response payload:

```json
{"correlationId":"start-1","topic":"commands/system/start","accepted":true,"commandResponse":{"accepted":true,"state":"Running","command":"Start","message":"Started."}}
```

## Command topic details

### Query status

Publish to:

```text
virex/commands/status/get
```

Request payload:

```json
{"correlationId":"status-1"}
```

Response:

```text
virex/responses/status-1
```

```json
{"correlationId":"status-1","topic":"commands/status/get","accepted":true,"status":{"state":"Ready"}}
```

### Query error

Publish to:

```text
virex/commands/error/get
```

Request payload:

```json
{"correlationId":"error-1"}
```

Response payload field: `error`.

```json
{"correlationId":"error-1","topic":"commands/error/get","accepted":true,"error":{"hasError":false,"state":"Ready"}}
```

### Query ProductInfo

Publish to:

```text
virex/commands/product-info/get
```

Request payload:

```json
{"correlationId":"product-get-1"}
```

Response payload field: `productInfo`.

```json
{"correlationId":"product-get-1","topic":"commands/product-info/get","accepted":true,"productInfo":{"lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1"}}
```

### Set ProductInfo

Publish to:

```text
virex/commands/product-info/set
```

Request payload:

```json
{"correlationId":"product-set-1","productInfo":{"lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1"}}
```

Response payload field: `commandResponse`.

```json
{"correlationId":"product-set-1","topic":"commands/product-info/set","accepted":true,"commandResponse":{"accepted":true,"state":"Ready","command":"SetProductInfo","message":"ProductInfo updated."}}
```

### Initialize

Publish to:

```text
virex/commands/system/initialize
```

Request payload:

```json
{"correlationId":"initialize-1"}
```

Response payload field: `commandResponse`.

### Start run

Publish to:

```text
virex/commands/system/start
```

Request payload:

```json
{"correlationId":"start-1","condition":"golden-sample","runMode":"continue"}
```

Response payload field: `commandResponse`. After the command is accepted, the service also publishes `statusChanged`, `runStarted`, and later `resultCreated` / `runCompleted` events.

### Stop run

Publish to:

```text
virex/commands/system/stop
```

Request payload:

```json
{"correlationId":"stop-1","reason":"operator-request"}
```

Response payload field: `commandResponse`.

### Query results

Publish to:

```text
virex/commands/results/query
```

Request payload:

```json
{"correlationId":"results-1","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A"}
```

Response payload field: `results`.

```json
{"correlationId":"results-1","topic":"commands/results/query","accepted":true,"results":{"items":[],"count":0}}
```

### Deinitialize

Publish to:

```text
virex/commands/system/deinitialize
```

Request payload:

```json
{"correlationId":"deinitialize-1"}
```

Response payload field: `commandResponse`.

## Command example

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

## Subscription example

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

### Purpose

Notifies the client that the public system state has changed.

### Topic

```text
virex/statusChanged
```

### Payload

[SystemStatus](payloads/system/system-status.md)

### Example

```json
{"state":"Ready"}
```

### Notes

The client can use this event to update UI state and decide whether the next command is valid.

## productInfoChanged

### Purpose

Notifies the client that `POST /api/product-info` or the TCP ProductInfo command has completed.

### Topic

```text
virex/productInfoChanged
```

### Payload

[ProductInfo](payloads/product/product-info.md)

### Example

```json
{"lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1"}
```

### Notes

This event contains ProductInfo only. It does not contain result data.

## runStarted

### Purpose

Notifies the client that the `Start` command has been accepted and the system has entered `Running`.

### Topic

```text
virex/runStarted
```

### Payload

[SystemStatus](payloads/system/system-status.md)

### Example

```json
{"state":"Running"}
```

### Notes

`Start` responds before the run completes. The client should wait for `resultCreated`, `runCompleted`, or query [GET /api/results](rest-api.md#get-apiresults).

## runCompleted

### Purpose

Notifies the client that the current run has ended and the public state has returned to `Ready`.

### Topic

```text
virex/runCompleted
```

### Payload

[SystemStatus](payloads/system/system-status.md)

### Example

```json
{"state":"Ready"}
```

### Notes

This represents run lifecycle completion. Result details are delivered by `resultCreated`.

## resultCreated

### Purpose

Notifies the client that a public result summary has been created.

### Topic

```text
virex/resultCreated
```

### Payload

[ResultSummary](payloads/results/result-summary.md)

### Example

```json
{"resultId":"RID-1","timestamp":"2026-06-20T15:30:12+08:00","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1","condition":"golden-sample","overallResult":"OK","defectCount":0}
```

### Notes

The result contains the ProductInfo snapshot and `condition` captured when `Start` was accepted. It provides a summary only, and does not include defect lists, crop lists, image binaries, or private inspection internals.

## errorChanged

### Purpose

Notifies the client that public error information has changed.

### Topic

```text
virex/errorChanged
```

### Payload

[ErrorInfo](payloads/system/error-info.md)

### Example

```json
{"hasError":true,"message":"Camera timeout.","state":"Running"}
```

### Notes

This event reports public error information only. Connection interruptions, broker unavailability, and subscription failures are MQTT client connection-level errors.

## commandRejected

### Purpose

Notifies the client that a command was rejected by state rules or validation.

### Topic

```text
virex/commandRejected
```

### Payload

[CommandResponse](payloads/commands/command-response.md)

### Example

```json
{"accepted":false,"state":"Running","command":"SetProductInfo","errorCode":"invalid_state","message":"SetProductInfo is not valid while state is Running."}
```

### Notes

Use this event to correlate RESTful API, TCP, or UI commands that were rejected. All transports use the same state rules.

## Error handling

MQTT events do not have HTTP status codes. Malformed JSON, unknown topics, broker disconnections, and subscription failures should be treated as transport-layer errors. `commandRejected` is an application-layer rejection reported by the Virex.NET compatible service.
