# TCP Socket Protocol


TCP Socket is a bidirectional integration channel for clients that need to send commands and receive events over the same simple streaming protocol.

## Complete samples

The language tabs in this reference are request fragments. For directly runnable source with all `using`, `import`, and `#include` directives, use the [complete samples](samples.md). The complete C++ TCP sample defines `SendAll`; `SendTcpFrame` is not a library or sample function.

## Basic Information

| Item | Value |
| --- | --- |
| Default host | `127.0.0.1` |
| Default port | `5089` |
| Framing method | NDJSON |
| Encoding | UTF-8 |
| Direction | Client sends command and query frames; service sends direct response and event frames |

Each frame is a JSON object and ends with `\n`.

```text
{"type":"start","condition":"golden-sample","runMode":"continue"}\n
```

When reading TCP/NDJSON, the C# SDK applies an idle timeout per frame. There may be a long wait between complete frames, but once any byte of a frame has arrived, the remaining content and trailing newline must arrive within `VirexClientOptions.TcpFrameTimeoutMs`; otherwise, the TCP event reader reports a timeout.

## Frame Overview

### Incoming commands and queries

| Frame Type | Payload | Valid State | Result |
| --- | --- | --- | --- |
| `status` | `type` only | Any | Returns direct response `type: "status"`. |
| `error` | `type` only | Any | Returns direct response `type: "error"`. |
| `getProductInfo` | `type` only | Any | Returns direct response `type: "productInfo"`. |
| `initialize` | [SystemInitializeRequest](payloads/commands/system-initialize-request.md) with `type` | `Uninitialized` | Enters `Initializing`; completion emits `statusChanged` with `Ready`. |
| `deinitialize` | [SystemDeinitializeRequest](payloads/commands/system-deinitialize-request.md) with `type` | `Ready` | Enters `Deinitializing`; completion emits `statusChanged` with `Uninitialized`. |
| `productInfo` | [ProductInfo](payloads/product/product-info.md) with `type` | `Ready` | Updates ProductInfo and emits `productInfoChanged`. |
| `start` | [SystemStartRequest](payloads/commands/system-start-request.md) with `type` | `Ready` | Enters `Running`; completion is reported by events and results. |
| `stop` | [SystemStopRequest](payloads/commands/system-stop-request.md) with `type` | `Running` | Stops the run and returns to `Ready`. |
| `results` | optional `lotID`, `waferID`, `recipe` filters | Any | Returns direct response `type: "results"`. |

### Outgoing events

| Frame Type | Payload | When Sent |
| --- | --- | --- |
| `statusChanged` | [SystemStatus](payloads/system/system-status.md) with `type` | Public state changes. |
| `productInfoChanged` | [ProductInfo](payloads/product/product-info.md) with `type` | ProductInfo update completes. |
| `imageGrabbed` | [ImageGrabbedInfo](payloads/events/image-grabbed.md) with `type` | Image acquisition completes; paths are supplied later by `resultCreated`. |
| `runStarted` | [SystemStatus](payloads/system/system-status.md) with `type` | State enters `Running`. |
| `runCompleted` | [SystemStatus](payloads/system/system-status.md) with `type` | A run leaves `Running` and returns to `Ready`. |
| `resultCreated` | [ResultSummary](payloads/results/result-summary.md) with `type` | A result summary is created. |
| `errorChanged` | [ErrorInfo](payloads/system/error-info.md) with `type` | Public error information changes. |
| `commandRejected` | [CommandResponse](payloads/commands/command-response.md) with `type` | A command is rejected. |

## Connection example

=== "C# SDK"

    ```csharp
    using System;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;
    using Virex.NET.Client;
    using Virex.NET.Contracts;
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
    await tcp.RunAsync(CancellationToken.None);
    ```

=== "C# Raw"

    ```csharp
    using System;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;
    using Virex.NET.Contracts;
    using var client = new TcpClient();
    await client.ConnectAsync("127.0.0.1", 5089);
    await using var stream = client.GetStream();

    var frame = Encoding.UTF8.GetBytes(
        "{\"type\":\"start\",\"condition\":\"golden-sample\",\"runMode\":\"continue\"}\n");
    await stream.WriteAsync(frame, 0, frame.Length);
    ```

=== "Python"

    ```python
    import json
    import urllib.parse
    import urllib.request
    import socket

    with socket.create_connection(("127.0.0.1", 5089)) as sock:
        frame = b'{"type":"start","condition":"golden-sample","runMode":"continue"}\n'
        sock.sendall(frame)
    ```

=== "C++"

    ```cpp
    #define WIN32_LEAN_AND_MEAN
    #include <winsock2.h>
    #include <ws2tcpip.h>
    #include <cstddef>
    #include <iostream>
    #include <stdexcept>
    #include <string>

    #pragma comment(lib, "ws2_32.lib")

    void SendAll(SOCKET socket, const std::string& value)
    {
        std::size_t sent = 0;
        while (sent < value.size())
        {
            const int chunk = send(socket, value.data() + sent, static_cast<int>(value.size() - sent), 0);
            if (chunk <= 0)
            {
                throw std::runtime_error("send failed.");
            }

            sent += static_cast<std::size_t>(chunk);
        }
    }

    int main()
    {
        WSADATA wsaData{};
        if (WSAStartup(MAKEWORD(2, 2), &wsaData) != 0)
        {
            return 1;
        }

        SOCKET client = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
        if (client == INVALID_SOCKET)
        {
            WSACleanup();
            return 1;
        }

        sockaddr_in address{};
        address.sin_family = AF_INET;
        address.sin_port = htons(5089);
        if (InetPtonA(AF_INET, "127.0.0.1", &address.sin_addr) != 1 ||
            connect(client, reinterpret_cast<const sockaddr*>(&address), sizeof(address)) == SOCKET_ERROR)
        {
            closesocket(client);
            WSACleanup();
            return 1;
        }

        const std::string frame =
            R"({"type":"start","condition":"golden-sample","runMode":"continue"})"
            "\n";
        SendAll(client, frame);

        closesocket(client);
        WSACleanup();
        return 0;
    }
    ```

## initialize command

### Purpose

Initialize the system over TCP. The command completes when `InitializationCompleted` moves the public state to `Ready`.

### Frame

```json
{"type":"initialize"}
```

### Payload

No body fields are required beyond `type: "initialize"`.

### State Restrictions

Only valid in `Uninitialized`.

### Success event

The service sends:

```json
{"type":"statusChanged","state":"Ready"}
```

### Error handling

If the current state is not `Uninitialized`, the service sends `commandRejected`.

## deinitialize command

### Purpose

Deinitialize the system over TCP. The command completes when `DeinitializationCompleted` moves the public state to `Uninitialized`.

### Frame

```json
{"type":"deinitialize"}
```

### Payload

No body fields are required beyond `type: "deinitialize"`.

### State Restrictions

Only valid in `Ready`.

### Success event

The service sends:

```json
{"type":"statusChanged","state":"Uninitialized"}
```

### Error handling

If the current state is not `Ready`, the service sends `commandRejected`.

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

## status query

### Purpose

Read the current public system state over TCP. This is a query frame, not a lifecycle command, and can be sent in any state.

### Frame

```json
{"type":"status"}
```

### Payload

No body fields are required beyond `type: "status"`.

### State Restrictions

Can be called in any state.

### Response frame

The service sends a direct response frame:

```json
{"type":"status","state":"Ready"}
```

### Notes

The response type is `status`. State-change events still use `statusChanged`.

## error query

### Purpose

Read the current public error information over TCP. This query returns the same [ErrorInfo](payloads/system/error-info.md) shape used by RESTful API `GET /api/error` and MQTT `commands/error/get`.

### Frame

```json
{"type":"error"}
```

### Payload

No body fields are required beyond `type: "error"`.

### State Restrictions

Can be called in any state.

### Response frame

The service sends a direct response frame:

```json
{"type":"error","hasError":false,"message":"","state":"Ready"}
```

### Notes

The response type is `error`. Error-change events still use `errorChanged`.

## getProductInfo query

### Purpose

Read the current ProductInfo over TCP without changing state.

### Frame

```json
{"type":"getProductInfo"}
```

### Payload

No body fields are required beyond `type: "getProductInfo"`.

### State Restrictions

Can be called in any state.

### Response frame

The service sends a direct response frame:

```json
{"type":"productInfo","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1"}
```

### Notes

The response type is `productInfo`. ProductInfo update events still use `productInfoChanged`.

## results query

### Purpose

Query public result summaries over TCP. Results include summaries only; they do not include private inspection internals, defect lists, crop lists, or image binaries.

### Frame

```json
{"type":"results","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A"}
```

### Payload

| Field | Required | Description |
| --- | --- | --- |
| `type` | Yes | Must be `results`. |
| `lotID` | No | Optional Lot ID filter. |
| `waferID` | No | Optional Wafer ID filter. |
| `recipe` | No | Optional Recipe filter. |

Multiple filters are combined with AND.

### State Restrictions

Can be called in any state.

### Response frame

The service sends a direct response frame:

```json
{"type":"results","items":[{"resultId":"RID-1","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","condition":"golden-sample","overallResult":"OK","defectCount":0}],"count":1}
```

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

## imageGrabbed event

### Purpose

Notifies the client that one image acquisition completed. The frame contains capture metadata only; related image and result paths are provided later by `resultCreated`.

### Frame

```json
{"type":"imageGrabbed","captureId":"CAP-1","timestamp":"2026-08-17T10:00:00.000+08:00","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1"}
```

### Notes

The later `resultCreated` frame uses the same `captureId` and includes the persisted image and result paths.
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
{"type":"resultCreated","resultId":"RID-1","timestamp":"2026-06-20T15:30:12+08:00","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1","condition":"golden-sample","overallResult":"OK","defectCount":0,"imageRelativePath":"20260620/LOT-001/20260620_153012_W01.bmp","resultRelativePath":"20260620/LOT-001/20260620_153012_W01.json","imagePath":"/data/virex-results/20260620/LOT-001/20260620_153012_W01.bmp","previewImagePath":"/data/virex-results/20260620/LOT-001/20260620_153012_W01.jpg","resultPath":"/data/virex-results/20260620/LOT-001/20260620_153012_W01.json"}
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
