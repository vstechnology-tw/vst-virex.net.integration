# REST API

REST 用於 command/query flows：讀取 status、送出 WaferInfo、控制 cycle、查詢 result summaries。

Default simulator base URL:

```text
http://127.0.0.1:5088
```

按下 **Start Servers** 後，simulator 也會提供：

```text
Scalar:       http://127.0.0.1:5088/scalar
OpenAPI JSON: http://127.0.0.1:5088/openapi/v1.json
```

可使用 Scalar page 手動驗證與 `vst-virex.net.wpf` 相同的 public REST surface。

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

完整 request/response body shape 請看 [傳送內容 / Payloads](payloads.md)。

`POST /api/control/start` 可接受 optional JSON body：

```json
{
  "condition": "golden-sample",
  "runMode": "continue"
}
```

`runMode` optional。省略或空白時預設為 `continue`；也支援 `single`。

`POST /api/control/stop` 可接受 optional JSON body：

```json
{
  "reason": "operator-request"
}
```

兩個 body 都向下相容；empty body 與 blank value 都有效。

## Status

```json
{
  "initialized": true,
  "processState": "ready",
  "recipe": "Default"
}
```

`processState` 可為：

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

`GET /api/results` 只回傳固定大小的 summaries，不包含 defect lists、die lists、crop lists 或 image binaries。

Supported query parameters:

```text
lotId
waferId
recipeId
```

多個 parameters 以 AND 合併。

REST result summaries 是 public integration summaries，不暴露 private inspection internals。
