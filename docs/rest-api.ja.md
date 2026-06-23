# REST API

REST は command/query フローに使用します。status の読み取り、WaferInfo の送信、cycle の制御、result summary の照会を行います。

Default simulator base URL:

```text
http://127.0.0.1:5088
```

**Start Servers** 後、simulator は次も公開します。

```text
Scalar:       http://127.0.0.1:5088/scalar
OpenAPI JSON: http://127.0.0.1:5088/openapi/v1.json
```

Scalar page を使うと、`vst-virex.net.wpf` と同じ public REST surface を手動で検証できます。

Endpoints:

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

正確な request / response body shape は [Transmitted Content / Payloads](payloads.md) を参照してください。

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

`GET /api/results` は固定サイズの summary のみを返します。defect list、die list、crop list、image binary は含みません。

Supported query parameters:

```text
lotId
waferId
recipeId
```

複数の parameters は AND 条件で結合されます。

REST result summary は public integration summary です。private inspection internals は公開しません。
