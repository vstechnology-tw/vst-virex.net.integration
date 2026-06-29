# SystemStopRequest

`SystemStopRequest` 是停止執行時選用的要求內容。

## JSON

```json
{
  "reason": "operator-request"
}
```

## 欄位

| 欄位 | 型別 | 必填 | 說明 |
| --- | --- | --- | --- |
| `reason` | string | 否 | 公開停止原因或關聯文字。 |

## 使用位置

| 介面 | 用法 |
| --- | --- |
| REST | `POST /api/system/stop` 要求內容。 |
| TCP | 傳入 `type: "stop"` 命令資料框。 |
