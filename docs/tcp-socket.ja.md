# TCP Socket Protocol

TCP は bidirectional command と event traffic が必要な direct socket integration に使用します。

TCP は single port と NDJSON framing を使用します。

```text
one JSON object per line
each frame ends with \n
UTF-8 encoding
```

C# SDK は TCP/NDJSON の読み取り中に per-frame idle timeout を適用します。Complete frames の間に長い gap があっても有効です。Frame の最初の byte を受信した後は、残りの bytes と終端 newline が `VirexClientOptions.TcpFrameTimeoutMs` 以内に到着しない場合、TCP event reader は timeout で失敗します。

Equipment/client は Virex.NET-compatible service に接続します。同じ connection で inbound messages を送信し、outbound events を受信できます。

Field-level details と shared JSON body shapes は [Transmitted Content / Payloads](payloads.md) を参照してください。

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

`type` field は legacy WaferInfo frames では optional です。Start/stop には `type` が必要です。

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

Result event は summary-only であり、defect lists や binaries は含みません。
