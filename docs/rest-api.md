# REST API

Default simulator base URL:

```text
http://127.0.0.1:5088
```

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

## Status

```json
{
  "initialized": true,
  "processState": "ready",
  "recipe": "Default"
}
```

`processState` is one of:

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

`GET /api/results` returns fixed-size summaries only. It does not include defect lists, die lists, crop lists, or image binaries.

Supported query parameters:

```text
lotId
waferId
recipeId
```

Multiple parameters are combined with AND.
