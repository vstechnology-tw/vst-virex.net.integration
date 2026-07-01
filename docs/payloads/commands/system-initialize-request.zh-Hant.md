# SystemInitializeRequest

`SystemInitializeRequest` 是初始化系統的命令資料框。

## JSON

```json
{
  "type": "initialize"
}
```

## 欄位

| 欄位 | 型別 | 必填 | 說明 |
| --- | --- | --- | --- |
| `type` | string | 是 | 必須是 `initialize`。 |

## 使用位置

| 介面 | 用法 |
| --- | --- |
| REST | `POST /api/system/initialize` 不使用 request body。 |
| TCP | 傳入 `type: "initialize"` 命令資料框。 |
