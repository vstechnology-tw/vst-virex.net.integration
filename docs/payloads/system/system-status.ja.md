# SystemStatus

`SystemStatus` は、現在の公開 ライフサイクル状態を報告します。

## JSON

```json
{
  "state": "Ready"
}
```

## フィールド

|フィールド |タイプ |必須 |説明 |
| --- | --- | --- | --- |
| `state` |文字列 |はい |現在のライフサイクル状態。 |

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
| REST | `GET /api/status` 応答。 |
| TCP | `statusChanged`、`runStarted`、`runCompleted` イベント。 |
| MQTT | `virex/statusChanged`、`virex/runStarted`、`virex/runCompleted`。 |

`status` は、リソースまたはイベントのカテゴリです。 `state` は、このステータス ペイロードによって報告されるライフサイクル値です。
