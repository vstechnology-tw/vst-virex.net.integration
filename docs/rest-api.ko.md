# REST API

REST는 상태 읽기, WaferInfo 제출, 사이클 제어, 결과 요약 조회 같은 명령/조회 흐름에 사용합니다.

기본 시뮬레이터 베이스 URL:

```text
http://127.0.0.1:5088
```

**Start Servers** 후 시뮬레이터는 다음 항목도 제공합니다.

```text
Scalar:       http://127.0.0.1:5088/scalar
OpenAPI JSON: http://127.0.0.1:5088/openapi/v1.json
```

Scalar 페이지를 사용하면 `vst-virex.net.wpf` 가 제공하는 것과 동일한 공개 REST 표면을 수동으로 검증할 수 있습니다.

엔드포인트:

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

정확한 요청 및 응답 본문 형태는 [전송 내용 / 페이로드](payloads.md)를 참조하십시오.

`POST /api/control/start` accepts an optional JSON body:

```json
{
  "condition": "golden-sample",
  "runMode": "continue"
}
```

`runMode` is optional. Omitted or blank values default to `continue`; `single` is also supported.

`POST /api/control/stop` accepts an optional JSON body:

```json
{
  "reason": "operator-request"
}
```

Both bodies are backward compatible. Empty bodies and blank values are valid.

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

`GET /api/results` 는 고정 크기의 요약만 반환합니다. 결함 목록, 다이 목록, 크롭 목록, 이미지 바이너리는 포함하지 않습니다.

지원되는 쿼리 매개변수:

```text
lotId
waferId
recipeId
```

여러 매개변수를 지정하면 AND 로 결합됩니다.

REST 결과 요약은 공개 연동용 요약입니다. 비공개 검사 내부 정보는 노출하지 않습니다.
