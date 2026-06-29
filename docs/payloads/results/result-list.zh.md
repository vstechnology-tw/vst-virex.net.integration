# ResultList

`ResultList` 是結果查詢的 REST 回應包裝。

## JSON

```json
{
  "items": [],
  "count": 0
}
```

## 欄位

| 欄位 | 型別 | 必填 | 說明 |
| --- | --- | --- | --- |
| `items` | array of [ResultSummary](result-summary.zh.md) | 是 | 符合條件的結果摘要。 |
| `count` | number | 是 | 回傳項目數量。 |

## 使用位置

| 介面 | 用法 |
| --- | --- |
| REST | `GET /api/results` 回應。 |

## 支援的查詢篩選條件

```text
waferID
lotID
recipe
```

多個篩選條件以 AND 組合。
