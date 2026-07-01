# SystemInitializeRequest

`SystemInitializeRequest` は、システムを初期化するコマンドフレームです。

## JSON

```json
{
  "type": "initialize"
}
```

## フィールド

|フィールド |型 |必須 |説明 |
| --- | --- | --- | --- |
| `type` | string |はい | `initialize` である必要があります。 |

## 使用場所

|インターフェイス |使用方法 |
| --- | --- |
| REST | `POST /api/system/initialize` はリクエストボディを使用しません。 |
| TCP | 受信 `type: "initialize"` コマンドフレーム。 |
