# TCP Socket Protocol

TCP는 bidirectional command 및 event traffic이 필요한 direct socket integration에 사용됩니다.

TCP는 single port와 NDJSON framing을 사용합니다.

```text
one JSON object per line
each frame ends with \n
UTF-8 encoding
```

C# SDK는 TCP/NDJSON을 읽는 동안 per-frame idle timeout을 적용합니다. 두 complete frames 사이의 긴 gap은 유효합니다. Frame의 첫 byte가 도착한 뒤에는 나머지 bytes와 종료 newline이 `VirexClientOptions.TcpFrameTimeoutMs` 이내에 도착해야 하며, 그렇지 않으면 TCP event reader가 timeout으로 실패합니다.

Equipment/client는 Virex.NET-compatible service에 연결합니다. 같은 connection으로 inbound messages를 보내고 outbound events를 받을 수 있습니다.

Field-level details 및 shared JSON body shapes는 [Transmitted Content / Payloads](payloads.md)를 참조하십시오.

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

`type` field는 legacy WaferInfo frames에서는 optional입니다. Start/stop에는 `type`이 필요합니다.

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

Result event는 summary-only이며 defect lists 또는 binaries를 포함하지 않습니다.
