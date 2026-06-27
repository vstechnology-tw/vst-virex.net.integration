# CommandResponse

`CommandResponse` 回報命令是否被接受，以及命令處理後的狀態。

## 接受時的 JSON

```json
{
  "accepted": true,
  "state": "Ready",
  "command": "Initialize",
  "message": "Initialize accepted."
}
```

## 拒絕時的 JSON

```json
{
  "accepted": false,
  "state": "Running",
  "command": "SetProductInfo",
  "errorCode": "invalid_state",
  "message": "SetProductInfo is not valid while state is Running."
}
```

## 欄位

| 欄位 | 型別 | 必填 | 說明 |
| --- | --- | --- | --- |
| `accepted` | boolean | 是 | 命令是否被接受。 |
| `state` | string | 是 | 命令處理後的目前狀態。 |
| `command` | string | 是 | 公開命令名稱。 |
| `errorCode` | string | 否 | 接受的命令會省略。`invalid_state` 代表命令在目前狀態不合法。 |
| `message` | string | 是 | 公開回應訊息。 |

## 使用位置

| 介面 | 用法 |
| --- | --- |
| REST | 系統命令路由與 `POST /api/product-info` 回應內容。 |
| TCP | 拒絕命令的 `commandRejected` 事件。 |
| MQTT | `virex/commandRejected`。 |
