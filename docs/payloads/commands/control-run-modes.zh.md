# ControlRunModes

`ControlRunModes` 定義 [SystemStartRequest](system-start-request.zh.md) 允許的 `runMode` 值。

## 值

| 值 | 意義 |
| --- | --- |
| `continue` | 預設執行模式。 |
| `single` | 單次執行模式。 |

省略或空白的 `runMode` 會被正規化成 `continue`。

## 使用位置

| 介面 | 用法 |
| --- | --- |
| REST | `POST /api/system/start` 要求內容。 |
| TCP | 傳入 `type: "start"` 命令資料框。 |
