# SystemDeinitializeRequest

`SystemDeinitializeRequest` は、システムを非初期化するコマンドフレームです。

## JSON

```json
{
  "type": "deinitialize"
}
```

## フィールド

|フィールド |型 |必須 |説明 |
| --- | --- | --- | --- |
| `type` | string |はい | `deinitialize` である必要があります。 |

## 使用場所

|インターフェイス |使用方法 |
| --- | --- |
| RESTful API | `POST /api/system/deinitialize` はリクエストボディを使用しません。 |
| TCP | 受信 `type: "deinitialize"` コマンドフレーム。 |
