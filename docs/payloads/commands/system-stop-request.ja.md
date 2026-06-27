# SystemStopRequest

`SystemStopRequest` は、実行を停止するためのオプションの要求本文です。

## JSON

```json
{
  "reason": "operator-request"
}
```

## フィールド

|フィールド |タイプ |必須 |説明 |
| --- | --- | --- | --- |
| `reason` |文字列 |いいえ |停止理由または関連ラベル。 |

## 使用箇所

|インターフェース |使い方 |
| --- | --- |
| REST | `POST /api/system/stop`要求本文。 |
| TCP |受信 `type: "stop"` コマンド フレーム。 |
