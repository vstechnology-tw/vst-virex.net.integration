# SystemStartRequest

`SystemStartRequest` 是啟動執行時選用的要求內容。

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

## 欄位

| 欄位 | 型別 | 必填 | 說明 |
| --- | --- | --- | --- |
| `requestId` | string | 否 | 用於關聯受理回覆與工作事件。SDK 在傳送前產生識別字。 |
| `recipeName` | string | 否 | 此工作要求準備的 Recipe 名稱。省略或 null 時沿用目前 Recipe，與 `condition` 分開。 |
| `condition` | string | 否 | 公開啟動條件或關聯文字。 |
| `runMode` | string | 否 | 執行模式。省略或空白時預設 `continue`。 |
| `inspectionMode` | string | 否 | 檢測策略，與執行模式分開。null 時沿用既有 Recipe 行為。請看 [InspectionMode](../../inspection-mode.md)。 |

## 使用位置

| 介面 | 用法 |
| --- | --- |
| RESTful API | `POST /api/system/start` 要求內容。 |
| TCP | 傳入 `type: "start"` 命令資料框。 |

允許的 `runMode` 值請看 [ControlRunModes](control-run-modes.zh-Hant.md)。

REST SDK 傳送這些選項，本身不準備 Recipe。目前 `recipeName` 由 REST 的 `StartAsync(SystemStartRequest)` 傳送；既有 MQTT／TCP SDK 方法尚未傳送此欄位。受理回覆不代表可以掃描；開始掃描前須等待伺服器的關聯可掃描事件。Simulator 目前沿用原有 Recipe 行為；此次欄位新增尚未模擬 Recipe 切換。
