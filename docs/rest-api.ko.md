# REST API

REST는 command/query 흐름에 사용됩니다. status 조회, WaferInfo 제출, cycle 제어, result summary 조회를 수행합니다.

Default simulator base URL:

```text
http://127.0.0.1:5088
```

**Start Servers** 후 simulator는 다음도 제공합니다.

```text
Scalar:       http://127.0.0.1:5088/scalar
OpenAPI JSON: http://127.0.0.1:5088/openapi/v1.json
```

Scalar page를 사용하면 `vst-virex.net.wpf` 와 동일한 public REST surface를 수동으로 검증할 수 있습니다.

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

정확한 request / response body shape는 [Transmitted Content / Payloads](payloads.md)를 참조하십시오.

## Status

```json
{
  "initialized": true,
  "processState": "ready",
  "recipe": "Default"
}
```

`processState` 는 다음 중 하나입니다.

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

`GET /api/results` 는 고정 크기의 summary만 반환합니다. defect list, die list, crop list, image binary는 포함하지 않습니다.

Supported query parameters:

```text
lotId
waferId
recipeId
```

여러 parameters는 AND 조건으로 결합됩니다.

REST result summary는 public integration summary입니다. private inspection internals는 공개하지 않습니다.
