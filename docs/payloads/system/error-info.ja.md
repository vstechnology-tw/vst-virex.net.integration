# ErrorInfo

`ErrorInfo` は、現在有効なエラー情報を示します。

これはライフサイクル状態ではありません。 `hasError=false` は、現在アクティブなエラーがないことを意味します。

## JSON

```json
{
  "hasError": true,
  "message": "Recipe load failed.",
  "state": "Ready"
}
```

## フィールド

|フィールド |タイプ |必須 |説明 |
| --- | --- | --- | --- |
| `hasError` |ブール値 |はい |アクティブなエラーが現在存在するかどうか。 |
| `message` |文字列 |いいえ |エラーメッセージ。メッセージがない場合は省略します。 |
| `state` |文字列 |はい |エラー情報が報告されたときのライフサイクル状態。 |

## 使用箇所

|インターフェース |使い方 |
| --- | --- |
| TCP | `errorChanged` イベント。 |
| MQTT | `virex/errorChanged`。 |
