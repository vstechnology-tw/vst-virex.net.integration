# SystemStartRequest

`SystemStartRequest` は、実行を開始するためのオプションの要求本文です。

## JSON

```json
{
  "condition": "golden-sample",
  "runMode": "continue"
}
```

## フィールド

|フィールド |タイプ |必須 |説明 |
| --- | --- | --- | --- |
| `condition` |文字列 |いいえ |開始条件または関連ラベル。 |
| `runMode` |文字列 |いいえ |実行モード。省略または空白の場合は、`continue` が想定されます。 |

## 使用箇所

|インターフェース |使い方 |
| --- | --- |
| REST | `POST /api/system/start`要求本文。 |
| TCP |受信 `type: "start"` コマンド フレーム。 |

許可される `runMode` 値については、[ControlRunModes](control-run-modes.ja.md) を参照してください。
