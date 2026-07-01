# ProductInfo

`ProductInfo` 是公開產品資訊，用於執行關聯與結果快照。

## JSON

```json
{
  "waferID": "W01",
  "lotID": "LOT-001",
  "recipe": "RCP-A",
  "slot": "1",
  "foupID": "FOUP-A",
  "chamberID": "CH-1"
}
```

## 欄位

| 欄位 | 型別 | 必填 | 說明 |
| --- | --- | --- | --- |
| `waferID` | string | 是 | 公開 Wafer 識別碼。 |
| `lotID` | string | 是 | 公開 Lot 識別碼，用於結果篩選與事件關聯。 |
| `recipe` | string | 是 | 公開 Recipe 識別碼。 |
| `slot` | string | 是 | Slot 識別碼。 |
| `foupID` | string | 是 | FOUP 識別碼。 |
| `chamberID` | string | 是 | Chamber 識別碼。 |

## 使用位置

| 介面 | 用法 |
| --- | --- |
| RESTful API | `GET /api/product-info` 回應與 `POST /api/product-info` 要求。 |
| TCP | 傳入 `productInfo` 命令與傳出 `productInfoChanged` 事件。 |
| MQTT | `virex/productInfoChanged`。 |
| Results | `Start` 會保存 ProductInfo 快照；建立結果時會複製快照欄位。 |
