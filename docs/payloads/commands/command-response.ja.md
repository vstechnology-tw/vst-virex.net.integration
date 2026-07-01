# CommandResponse

`CommandResponse` は、コマンドが受け入れられたかどうか、およびコマンド処理後の状態を報告します。

## 受け入れ時の JSON

```json
{
  "accepted": true,
  "state": "Ready",
  "command": "Initialize",
  "message": "Initialize accepted."
}
```

## 拒否時の JSON

```json
{
  "accepted": false,
  "state": "Running",
  "command": "SetProductInfo",
  "errorCode": "invalid_state",
  "message": "SetProductInfo is not valid while state is Running."
}
```

## フィールド

|フィールド |タイプ |必須 |説明 |
| --- | --- | --- | --- |
| `accepted` |ブール値 |はい |コマンドが受け入れられるかどうか。 |
| `state` |文字列 |はい |コマンド処理後の現在の状態。 |
| `command` |文字列 |はい |公開コマンド名。 |
| `errorCode` |文字列 |いいえ |受け付けられたコマンドの場合は省略されます。 `invalid_state` は、コマンドが現在の状態では無効であることを意味します。 |
| `message` |文字列 |はい |応答メッセージ。 |

## 使用箇所

|インターフェース |使い方 |
| --- | --- |
| RESTful API |システム コマンド ルートと `POST /api/product-info` 応答本文。 |
| TCP |コマンドが拒否された場合の `commandRejected` イベント。 |
| MQTT | `virex/commandRejected`。 |
