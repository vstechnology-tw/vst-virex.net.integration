# SystemStartRequest

`SystemStartRequest` 是啟動執行時選用的要求內容。

## JSON

```json
{
  "condition": "golden-sample",
  "runMode": "continue"
}
```

## 欄位

| 欄位 | 型別 | 必填 | 說明 |
| --- | --- | --- | --- |
| `condition` | string | 否 | 公開啟動條件或關聯文字。 |
| `runMode` | string | 否 | 執行模式。省略或空白時預設 `continue`。 |

## 使用位置

| 介面 | 用法 |
| --- | --- |
| REST | `POST /api/system/start` 要求內容。 |
| TCP | 傳入 `type: "start"` 命令資料框。 |

允許的 `runMode` 值請看 [ControlRunModes](control-run-modes.zh-Hant.md)。
