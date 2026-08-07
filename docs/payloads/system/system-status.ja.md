# SystemStatus

`SystemStatus` は、現在の公開 ライフサイクル状態を報告します。

## JSON

```json
{
  "state": "Deinitializing",
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
| `state` |文字列 |はい |現在のライフサイクル状態。 |
| `recoveryAction` |文字列 |いいえ |復旧が必要な場合のオペレーター操作。現在は `Deinitialize`。操作が不要な場合は省略します。 |
| `recoveryStartedAt` |文字列 (date-time) |いいえ |現在の復旧試行が開始された UTC 時刻。 |
| `recoverySource` |文字列 |いいえ |失敗を報告したサブシステム。例: `Acquisition`。 |
| `recoveryPhase` |文字列 |いいえ |現在進行中の復旧フェーズ。例: `Deinitializing`。 |
| `recoveryDetails` |文字列 |いいえ |人間が読める復旧コンテキスト。 |

## 状態値

```text
Uninitialized
Initializing
Ready
UpdatingProductInfo
Running
Deinitializing
```

## 使用箇所

|インターフェース |使い方 |
| --- | --- |
| RESTful API | `GET /api/status` 応答。 |
| TCP | `statusChanged`、`runStarted`、`runCompleted` イベント。 |
| MQTT | `virex/statusChanged`、`virex/runStarted`、`virex/runCompleted`。 |

`status` は、リソースまたはイベントのカテゴリです。 `state` は、このステータス ペイロードによって報告されるライフサイクル値です。
