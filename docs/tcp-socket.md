# TCP Socket Protocol

TCP uses a single port and NDJSON framing:

```text
one JSON object per line
each frame ends with \n
UTF-8 encoding
```

The equipment/client connects to the Virex.NET-compatible service. The same connection can send inbound messages and receive outbound events.

## Inbound

```json
{"type":"waferInfo","lotId":"LOT-001","waferId":"W01","recipeId":"RCP-A","slot":"1","foupId":"FOUP-A","chamberId":"CH-1"}
```

```json
{"type":"start"}
```

```json
{"type":"stop"}
```

The `type` field is optional for legacy WaferInfo frames. Start/stop require `type`.

## Outbound

```json
{"type":"status","initialized":true,"processState":"ready","recipe":"Default"}
```

```json
{"type":"waferInfo","lotId":"LOT-001","waferId":"W01","recipeId":"RCP-A","slot":"1","foupId":"FOUP-A","chamberId":"CH-1"}
```

```json
{"type":"result","resultId":"RID-1","timestamp":"2026-06-20T00:00:00+08:00","lotId":"LOT-001","waferId":"W01","recipeId":"RCP-A","slot":"1","foupId":"FOUP-A","chamberId":"CH-1","overallResult":"OK","defectCount":0,"dieCount":100,"imageRelativePath":"20260620/LOT-001/image.tiff","resultRelativePath":"20260620/LOT-001/result.json","imagePath":"20260620/LOT-001/image.tiff","resultPath":"20260620/LOT-001/result.json"}
```

```json
{"type":"error","message":"Recipe load failed.","initialized":true,"processState":"ready","recipe":"Default","timestamp":"2026-06-20T00:00:00+08:00"}
```

The result event is summary-only and does not include defect lists or binaries.
