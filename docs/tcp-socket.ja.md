# TCP ソケット プロトコル

TCP は、双方向のコマンドおよびイベント通信が必要な直接ソケット連携に使用します。

TCP は単一ポートと NDJSON フレーミングを使用します。

```text
1 行につき 1 つの JSON オブジェクト
各フレームは \n で終わります
UTF-8 エンコード
```

C# SDK は、TCP/NDJSON を読み取る際にフレームごとのアイドル タイムアウトを適用します。完全なフレーム同士の間に長い空白時間があっても問題ありません。ただし、フレームのいずれかのバイトが到着した後は、残りのバイトと終端の改行が `VirexClientOptions.TcpFrameTimeoutMs` 以内に到着しなければ、TCP イベント リーダーはタイムアウトで失敗します。

装置またはクライアントは Virex.NET 互換サービスへ接続します。同じ接続で受信メッセージを送信し、送信イベントを受け取ることができます。

フィールド単位の詳細と共通 JSON 本文の形状は、[送信内容 / ペイロード](payloads.md)を参照してください。

## 受信

```json
{"type":"waferInfo","lotId":"LOT-001","waferId":"W01","recipeId":"RCP-A","slot":"1","foupId":"FOUP-A","chamberId":"CH-1"}
```

```json
{"type":"start"}
```

```json
{"type":"stop"}
```

従来の WaferInfo フレームでは `type` フィールドを省略できます。start/stop には `type` が必要です。

## 送信

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

result イベントは要約のみであり、欠陥リストやバイナリは含まれません。
