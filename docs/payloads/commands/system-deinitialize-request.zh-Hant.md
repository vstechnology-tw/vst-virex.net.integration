# SystemDeinitializeRequest

`SystemDeinitializeRequest` 是反初始化系統的命令資料框。

## JSON

```json
{
  "type": "deinitialize"
}
```

## 欄位

| 欄位 | 型別 | 必填 | 說明 |
| --- | --- | --- | --- |
| `type` | string | 是 | 必須是 `deinitialize`。 |

## 使用位置

| 介面 | 用法 |
| --- | --- |
| RESTful API | `POST /api/system/deinitialize` 不使用 request body。 |
| TCP | 傳入 `type: "deinitialize"` 命令資料框。 |
