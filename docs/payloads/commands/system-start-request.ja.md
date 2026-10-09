# SystemStartRequest

`SystemStartRequest` は、実行を開始するためのオプションの要求本文です。

## JSON

```json
{
  "requestId": "work-request-1",
  "recipeName": "RCP-A",
  "condition": "golden-sample",
  "runMode": "continue",
  "inspectionMode": "captureOnly"
}
```

## フィールド

|フィールド |タイプ |必須 |説明 |
| --- | --- | --- | --- |
| `requestId` |文字列 |いいえ |受理応答と作業イベントを関連付けます。SDK が送信前に生成します。 |
| `recipeName` |文字列 |いいえ |この作業で準備する Recipe のカタログ名です。省略または null は現在の Recipe を維持します。`condition` とは別の項目です。 |
| `condition` |文字列 |いいえ |開始条件または関連ラベル。 |
| `runMode` |文字列 |いいえ |実行モード。省略または空白の場合は、`continue` が想定されます。 |
| `inspectionMode` |文字列 |いいえ |実行モードとは別の検査方針です。null は既存の Recipe 動作を使用します。[InspectionMode](../../inspection-mode.md) を参照してください。 |

## 使用箇所

|インターフェース |使い方 |
| --- | --- |
| RESTful API | `POST /api/system/start`要求本文。 |
| TCP |受信 `type: "start"` コマンド フレーム。 |

許可される `runMode` 値については、[ControlRunModes](control-run-modes.ja.md) を参照してください。

REST SDK は選択項目を送信し、Recipe 自体を準備しません。現在 `recipeName` は REST の `StartAsync(SystemStartRequest)` で送信されます。既存の MQTT/TCP SDK メソッドはこのフィールドを送信しません。受理応答は撮影準備完了を示しません。スキャン前にサーバーの該当作業の準備イベントを待ってください。シミュレーターは現在、既存の Recipe 動作を維持し、このフィールド追加では Recipe 切り替えを模擬しません。
