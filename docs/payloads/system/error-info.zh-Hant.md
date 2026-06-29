# ErrorInfo

`ErrorInfo` 描述目前作用中的錯誤資訊。

它不是生命週期狀態。`hasError=false` 代表目前沒有作用中的錯誤。

## JSON

```json
{
  "hasError": true,
  "message": "Recipe load failed.",
  "state": "Ready"
}
```

## 欄位

| 欄位 | 型別 | 必填 | 說明 |
| --- | --- | --- | --- |
| `hasError` | boolean | 是 | 目前是否存在作用中的錯誤。 |
| `message` | string | 否 | 公開錯誤訊息。沒有訊息時省略。 |
| `state` | string | 是 | 回報錯誤資訊時的生命週期狀態。 |

## 使用位置

| 介面 | 用法 |
| --- | --- |
| TCP | `errorChanged` 事件。 |
| MQTT | `virex/errorChanged`。 |
