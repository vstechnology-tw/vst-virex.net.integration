# TCP Socket Protocol

TCP Socket is a bidirectional integration channel for clients that need to send commands and receive events over the same simple streaming protocol.

## Basic Information

| Item | Value |
| --- | --- |
| Default host | `127.0.0.1` |
| Default port | `5089` |
| Framing method | NDJSON |
| Encoding | UTF-8 |
| Direction | Client sends command frames; service sends event frames |

Each frame is a JSON object and ends with `\n`.

```text
{"type":"start","condition":"golden-sample","runMode":"continue"}\n
```

When reading TCP/NDJSON, the C# SDK applies an idle timeout per frame. There may be a long wait between complete frames, but once any byte of a frame has arrived, the remaining content and trailing newline must arrive within `VirexClientOptions.TcpFrameTimeoutMs`; otherwise, the TCP event reader reports a timeout.

## Frame Overview

### Incoming Commands

| Frame Type | Payload | Valid State | Result |
| --- | --- | --- | --- |
| `productInfo` | [ProductInfo](payloads/product/product-info.md) with `type` | `Ready` | Updates ProductInfo and emits `productInfoChanged`. |
| `start` | [SystemStartRequest](payloads/commands/system-start-request.md) with `type` | `Ready` | Enters `Running`; completion is reported by events and results. |
| `stop` | [SystemStopRequest](payloads/commands/system-stop-request.md) with `type` | `Running` | Stops the run and returns to `Ready`. |

### Outgoing events

| Frame Type | Payload | When Sent |
| --- | --- | --- |
| `statusChanged` | [SystemStatus](payloads/system/system-status.md) with `type` | Public state changes. |
| `productInfoChanged` | [ProductInfo](payloads/product/product-info.md) with `type` | ProductInfo update completes. |
| `runStarted` | [SystemStatus](payloads/system/system-status.md) with `type` | State enters `Running`. |
| `runCompleted` | [SystemStatus](payloads/system/system-status.md) with `type` | A run leaves `Running` and returns to `Ready`. |
| `resultCreated` | [ResultSummary](payloads/results/result-summary.md) with `type` | A result summary is created. |
| `errorChanged` | [ErrorInfo](payloads/system/error-info.md) with `type` | Public error information changes. |
| `commandRejected` | [CommandResponse](payloads/commands/command-response.md) with `type` | A command is rejected. |

## Connection example

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

## productInfo command

### Purpose

Update the current ProductInfo via TCP.

### Frame

```json
{"type":"productInfo","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1"}
```

### Payload

[ProductInfo](payloads/product/product-info.md) with `type: "productInfo"`.

### State Restrictions

Only valid in `Ready`.

### Success event

The service sends:

```json
{"type":"productInfoChanged","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1"}
```

### Error handling

If the current state is not `Ready`, the service sends `commandRejected`.

## start command

### Purpose

Starts a run over TCP. The command is accepted when the system enters `Running`; run completion is delivered later through event and result frames.

### Frame

```json
{"type":"start","condition":"golden-sample","runMode":"continue"}
```

### Payload

[SystemStartRequest](payloads/commands/system-start-request.md) with `type: "start"`.

### State Restrictions

Only valid in `Ready`.

### Success event

The service emits `statusChanged`, `runStarted`, and later `resultCreated` and `runCompleted`.

```json
{"type":"runStarted","state":"Running"}
```

### Error handling

If the current state is not `Ready`, or `runMode` is invalid, the service sends `commandRejected`.

## stop command

### Purpose

Stops the current run over TCP.

### Frame

```json
{"type":"stop","reason":"operator-request"}
```

### Payload

[SystemStopRequest](payloads/commands/system-stop-request.md) with `type: "stop"`.

### State Restrictions

Only valid in `Running`.

### Success event

The service sends:

```json
{"type":"statusChanged","state":"Ready"}
```

### Error handling

If the current state is not `Running`, the service sends `commandRejected`.

## statusChanged event

### Purpose

Notifies the client that the public system state has changed.

### Frame

```json
{"type":"statusChanged","state":"Ready"}
```

### Payload

[SystemStatus](payloads/system/system-status.md) with `type: "statusChanged"`.

### Notes

Use this event to synchronize client UI and command availability.

## productInfoChanged event

### Purpose

Notifies the client that the ProductInfo update is complete.

### Frame

```json
{"type":"productInfoChanged","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1"}
```

### Payload

[ProductInfo](payloads/product/product-info.md) with `type: "productInfoChanged"`.

### Notes

This event contains ProductInfo only.

## runStarted event

### Purpose

Notifies the client that a run has started and the state is `Running`.

### Frame

```json
{"type":"runStarted","state":"Running"}
```

### Payload

[SystemStatus](payloads/system/system-status.md) with `type: "runStarted"`.

### Notes

The run is still in progress when this event is received.

## runCompleted event

### Purpose

Notifies the client that the run lifecycle is complete and the state has returned to `Ready`.

### Frame

```json
{"type":"runCompleted","state":"Ready"}
```

### Payload

[SystemStatus](payloads/system/system-status.md) with `type: "runCompleted"`.

### Notes

Result details are passed by `resultCreated`.

## resultCreated event

### Purpose

Notifies the client that a public result summary has been created.

### Frame

```json
{"type":"resultCreated","resultId":"RID-1","timestamp":"2026-06-20T15:30:12+08:00","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1","condition":"golden-sample","overallResult":"OK","defectCount":0,"imageRelativePath":"20260620/LOT-001/20260620_153012_W01.tiff","resultRelativePath":"20260620/LOT-001/20260620_153012_W01.json","imagePath":"/data/virex-results/20260620/LOT-001/20260620_153012_W01.tiff","previewImagePath":"/data/virex-results/20260620/LOT-001/20260620_153012_W01.jpg","resultPath":"/data/virex-results/20260620/LOT-001/20260620_153012_W01.json"}
```

### Payload

[ResultSummary](payloads/results/result-summary.md) with `type: "resultCreated"`.

### Notes

The result contains the ProductInfo snapshot and `condition` captured when `Start` was accepted. It provides a summary only, and does not include defect lists, crop lists, image binaries, or private inspection internals.

## errorChanged event

### Purpose

Notifies the client that public error information has changed.

### Frame

```json
{"type":"errorChanged","hasError":true,"message":"Camera timeout.","state":"Running"}
```

### Payload

[ErrorInfo](payloads/system/error-info.md) with `type: "errorChanged"`.

### Notes

This is an application-layer event. Socket disconnections, timeouts, malformed JSON, and incomplete frames are transport errors.

## commandRejected event

### Purpose

Notifies the client that a command was rejected.

### Frame

```json
{"type":"commandRejected","accepted":false,"state":"Running","command":"SetProductInfo","errorCode":"invalid_state","message":"SetProductInfo is not valid while state is Running."}
```

### Payload

[CommandResponse](payloads/commands/command-response.md) with `type: "commandRejected"`.

### Notes

The client should not treat this event as a transport failure. It is a valid application-layer response indicating that the command was not accepted.

## Error handling

Malformed JSON, a missing trailing newline, unsupported frame types, socket disconnections, and read timeouts are transport/protocol failures. Invalid state, invalid run mode, and rejected commands are reported through `commandRejected`.
