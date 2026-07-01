# ControlRunModes

`ControlRunModes` は、[SystemStartRequest](system-start-request.ja.md) に許可される `runMode` 値を定義します。

## 値

|値 |意味 |
| --- | --- |
| `continue` |既定の実行モード。 |
| `single` |シングルランモード。 |

省略または空白の `runMode` は、`continue` に正規化されます。

## 使用箇所

|インターフェース |使い方 |
| --- | --- |
| RESTful API | `POST /api/system/start`要求本文。 |
| TCP |受信 `type: "start"` コマンド フレーム。 |
