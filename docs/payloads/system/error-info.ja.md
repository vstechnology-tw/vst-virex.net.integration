# ErrorInfo

`ErrorInfo` は、現在有効なエラー情報を示します。

これはライフサイクル状態ではありません。 `hasError=false` は、現在アクティブなエラーがないことを意味します。

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

## フィールド

|フィールド |タイプ |必須 |説明 |
| --- | --- | --- | --- |
| `hasError` |ブール値 |はい |アクティブなエラーが現在存在するかどうか。 |
| `message` |文字列 |いいえ |エラーメッセージ。メッセージがない場合は省略します。 |
| `state` |文字列 |はい |エラー情報が報告されたときのライフサイクル状態。 |
| `errorCode` |文字列 |いいえ |機械可読なエラー分類。例: `requires_deinitialize`。 |
| `recoveryAction` |文字列 |いいえ |エラーから復旧するために必要なオペレーター操作。現在は `Deinitialize`。操作が不要な場合は省略します。 |
| `recoveryStartedAt` |文字列 (date-time) |いいえ |現在の復旧試行が開始された UTC 時刻。 |
| `recoverySource` |文字列 |いいえ |失敗を報告したサブシステム。 |
| `recoveryPhase` |文字列 |いいえ |現在進行中の復旧フェーズ。 |
| `recoveryDetails` |文字列 |いいえ |人間が読める復旧コンテキスト。 |

## 使用箇所

|インターフェース |使い方 |
| --- | --- |
| TCP | `errorChanged` イベント。 |
| MQTT | `virex/errorChanged`。 |
