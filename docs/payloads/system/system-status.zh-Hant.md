# SystemStatus

`SystemStatus` 回報目前公開生命週期狀態。

## JSON

```json
{
  "state": "Ready"
}
```

## 欄位

| 欄位 | 型別 | 必填 | 說明 |
| --- | --- | --- | --- |
| `state` | string | 是 | 目前生命週期狀態。 |

## 狀態值

```text
Uninitialized
Initializing
Ready
UpdatingProductInfo
Running
Deinitializing
```

## 使用位置

| 介面 | 用法 |
| --- | --- |
| RESTful API | `GET /api/status` 回應。 |
| TCP | `statusChanged`、`runStarted`、`runCompleted` 事件。 |
| MQTT | `virex/statusChanged`、`virex/runStarted`、`virex/runCompleted`。 |

`status` 是資源或事件類別。`state` 是這個狀態快照裡的生命週期狀態值。
