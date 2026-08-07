# SystemStatus

`SystemStatus` 回報目前公開生命週期狀態。

## JSON

```json
{
  "state": "Deinitializing",
  "recoveryAction": "Deinitialize",
  "recoveryStartedAt": "2026-08-07T10:00:00.000+00:00",
  "recoverySource": "Acquisition",
  "recoveryPhase": "Deinitializing",
  "recoveryDetails": "Camera acquisition failed."
}
```

## 欄位

| 欄位 | 型別 | 必填 | 說明 |
| --- | --- | --- | --- |
| `state` | string | 是 | 目前生命週期狀態。 |
| `recoveryAction` | string | 否 | 狀態需要操作人員採取的復原動作；復原中通常為 `Deinitialize`。無動作時省略。 |
| `recoveryStartedAt` | string (date-time) | 否 | 目前復原嘗試開始的 UTC 時間。 |
| `recoverySource` | string | 否 | 回報失敗的子系統，例如 `Acquisition`。 |
| `recoveryPhase` | string | 否 | 目前進行中的復原階段，例如 `Deinitializing`。 |
| `recoveryDetails` | string | 否 | 提供給操作人員的人類可讀復原資訊。 |

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
