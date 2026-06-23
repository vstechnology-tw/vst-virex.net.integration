# TCP Socket Protocol

TCP 用於需要雙向 command 與 event traffic 的 direct socket integrations。

TCP 使用單一 port 與 NDJSON framing：

```text
one JSON object per line
each frame ends with \n
UTF-8 encoding
```

C# SDK 讀取 TCP/NDJSON 時會套用 per-frame idle timeout。兩筆完整 frame 之間可以間隔很久；但只要某一筆 frame 已經收到任何 byte，剩餘 bytes 與結尾 newline 必須在 `VirexClientOptions.TcpFrameTimeoutMs` 內抵達，否則 TCP event reader 會 timeout。

Equipment/client 連到 Virex.NET-compatible service。同一條 connection 可送 inbound messages，也可接收 outbound events。

Field-level details 與 shared JSON body shapes 請看 [傳送內容 / Payloads](payloads.md)。

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

Legacy WaferInfo frames 可省略 `type` field。Start/stop 必須包含 `type`。

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

Result event 只提供 summary，不包含 defect lists 或 binary payloads。
