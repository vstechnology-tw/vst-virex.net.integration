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

オペレーターによる復旧が必要な場合、応答は公開 `Deinitializing` 状態を維持し、構造化された操作を含みます。

```json
{
  "accepted": false,
  "state": "Deinitializing",
  "command": "Stop",
  "errorCode": "requires_deinitialize",
  "recoveryAction": "Deinitialize",
  "recoveryStartedAt": "2026-08-07T10:00:00.000+00:00",
  "recoverySource": "Acquisition",
  "recoveryPhase": "Deinitializing",
  "recoveryDetails": "Camera acquisition failed.",
  "message": "Deinitialize is required before another command can be accepted."
}
```

## フィールド

|フィールド |タイプ |必須 |説明 |
| --- | --- | --- | --- |
| `accepted` |ブール値 |はい |コマンドが受け入れられるかどうか。 |
| `state` |文字列 |はい |コマンド処理後の現在の状態。 |
| `command` |文字列 |はい |公開コマンド名。 |
| `errorCode` |文字列 |いいえ |受け付けられたコマンドの場合は省略されます。 `invalid_state` は、コマンドが現在の状態では無効であることを意味します。 |
| `recoveryAction` |文字列 |いいえ |現在の状態から復旧するために必要なオペレーター操作。現在は `Deinitialize`。操作が不要な場合は省略します。 |
| `recoveryStartedAt` |文字列 (date-time) |いいえ |現在の復旧試行が開始された UTC 時刻。 |
| `recoverySource` |文字列 |いいえ |失敗を報告したサブシステム。 |
| `recoveryPhase` |文字列 |いいえ |現在進行中の復旧フェーズ。 |
| `recoveryDetails` |文字列 |いいえ |人間が読める復旧コンテキスト。 |
| `message` |文字列 |はい |応答メッセージ。 |

## 使用箇所

|インターフェース |使い方 |
| --- | --- |
| RESTful API |システム コマンド ルートと `POST /api/product-info` 応答本文。 |
| TCP |コマンドが拒否された場合の `commandRejected` イベント。 |
| MQTT | `virex/commandRejected`。 |
