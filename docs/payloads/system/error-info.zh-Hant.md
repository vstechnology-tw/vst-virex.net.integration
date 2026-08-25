# ErrorInfo

`ErrorInfo` 描述目前作用中的錯誤資訊。

它不是生命週期狀態。`hasError=false` 代表目前沒有作用中的錯誤。

## JSON

```json
{
  "hasError": true,
  "message": "Recipe load failed.",
  "state": "Deinitializing",
  "errorCode": "requires_deinitialize",
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
| `hasError` | boolean | 是 | 目前是否存在作用中的錯誤。 |
| `message` | string | 否 | 公開錯誤訊息。沒有訊息時省略。 |
| `state` | string | 是 | 回報錯誤資訊時的生命週期狀態。 |
| `errorCode` | string | 否 | 機器可讀的錯誤分類，例如 `requires_deinitialize`。 |
| `recoveryAction` | string | 否 | 錯誤需要操作人員採取的復原動作；無動作時省略。 |
| `recoveryStartedAt` | string (date-time) | 否 | 目前復原嘗試開始的 UTC 時間。 |
| `recoverySource` | string | 否 | 回報失敗的子系統。 |
| `recoveryPhase` | string | 否 | 目前進行中的復原階段。 |
| `recoveryDetails` | string | 否 | 提供給操作人員的人類可讀復原資訊。 |

## 使用位置

| 介面 | 用法 |
| --- | --- |
| TCP | `errorChanged` 事件。 |
| MQTT | `virex/errorChanged`。 |
