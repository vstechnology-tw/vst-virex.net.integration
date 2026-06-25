# TCP Socket Protocol

TCP is used for direct socket integrations that need bidirectional command and event traffic.

TCP uses a single port and NDJSON framing:

```text
one JSON object per line
each frame ends with \n
UTF-8 encoding
```

The C# SDK applies a per-frame idle timeout while reading TCP/NDJSON. A long gap between two complete frames is valid. After any byte of a frame has arrived, the remaining bytes and the terminating newline must arrive within `VirexClientOptions.TcpFrameTimeoutMs` or the TCP event reader fails with a timeout.

The equipment/client connects to the Virex.NET-compatible service. The same connection can send inbound messages and receive outbound events.

For field-level details and shared JSON body shapes, see [Transmitted Content / Payloads](payloads.md).

## Inbound

```json
{"type":"waferInfo","lotId":"LOT-001","waferId":"W01","recipeId":"RCP-A","slot":"1","foupId":"FOUP-A","chamberId":"CH-1"}
```

```json
{"type":"start","condition":"golden-sample","runMode":"continue"}
```

```json
{"type":"stop","reason":"operator-request"}
```

The `type` field is optional for legacy WaferInfo frames. Start/stop require `type`. Start `condition`, start `runMode`, and stop `reason` are optional. Start `runMode` defaults to `continue`; `single` is also supported. Legacy `{"type":"start"}` and `{"type":"stop"}` remain valid.

## Outbound

```json
{"type":"status","initialized":true,"processState":"ready","recipe":"Default"}
```

```json
{"type":"waferInfo","lotId":"LOT-001","waferId":"W01","recipeId":"RCP-A","slot":"1","foupId":"FOUP-A","chamberId":"CH-1"}
```

```json
{"type":"result","resultId":"RID-1","timestamp":"2026-06-20T15:30:12+08:00","lotId":"LOT-001","waferId":"W01","recipeId":"RCP-A","slot":"1","foupId":"FOUP-A","chamberId":"CH-1","overallResult":"OK","defectCount":0,"imageRelativePath":"20260620/LOT-001/20260620_153012_W01.tiff","resultRelativePath":"20260620/LOT-001/20260620_153012_W01.json","imagePath":"/data/virex-results/20260620/LOT-001/20260620_153012_W01.tiff","previewImagePath":"/data/virex-results/20260620/LOT-001/20260620_153012_W01.jpg","resultPath":"/data/virex-results/20260620/LOT-001/20260620_153012_W01.json"}
```

```json
{"type":"error","message":"Recipe load failed.","initialized":true,"processState":"ready","recipe":"Default","timestamp":"2026-06-20T00:00:00+08:00"}
```

The result event is summary-only and does not include defect lists or binaries.
