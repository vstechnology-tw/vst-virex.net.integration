# REST API

REST は、状態の読み取り、WaferInfo の送信、サイクル制御、結果要約のクエリなど、コマンド/クエリの流れに使用します。

既定のシミュレーター ベース URL:

```text
http://127.0.0.1:5088
```

**Start Servers** の後、シミュレーターは次も公開します。

```text
Scalar:       http://127.0.0.1:5088/scalar
OpenAPI JSON: http://127.0.0.1:5088/openapi/v1.json
```

Scalar ページを使用すると、`vst-virex.net.wpf` が公開するものと同じ公開 REST サーフェスを手動で検証できます。

エンドポイント:

```text
GET  /api/status
GET  /api/error
GET  /api/wafer-info
POST /api/wafer-info
POST /api/control/initialize
POST /api/control/terminate
POST /api/control/start
POST /api/control/stop
GET  /api/results
```

正確なリクエスト本文とレスポンス本文の形状は、[送信内容 / ペイロード](payloads.md)を参照してください。

## Status

```json
{
  "initialized": true,
  "processState": "ready",
  "recipe": "Default"
}
```

`processState` は次のいずれかです。

```text
ready
capturing
inspecting
saving
```

## WaferInfo

```json
{
  "lotId": "LOT-001",
  "waferId": "W01",
  "recipeId": "RCP-A",
  "slot": "1",
  "foupId": "FOUP-A",
  "chamberId": "CH-1"
}
```

## Results

`GET /api/results` は固定サイズの要約のみを返します。欠陥リスト、ダイ リスト、クロップ リスト、画像バイナリは含まれません。

対応するクエリ パラメーター:

```text
lotId
waferId
recipeId
```

複数のパラメーターを指定した場合は AND で結合されます。

REST の結果要約は公開連携用の要約です。非公開の検査内部情報は公開しません。
