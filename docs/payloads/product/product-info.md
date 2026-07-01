# ProductInfo

`ProductInfo` is the product information used for event correlation and result snapshots.

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

## Field

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `waferID` | string | Yes | Wafer identifier. |
| `lotID` | string | Yes | Lot identifier used for result filtering and event correlation. |
| `recipe` | string | Yes | Recipe identifier. |
| `slot` | string | Yes | Slot identifier. |
| `foupID` | string | Yes | FOUP identifier. |
| `chamberID` | string | Yes | Chamber identifier. |

## Use location

| Interface | Usage |
| --- | --- |
| RESTful API | `GET /api/product-info` response and `POST /api/product-info` request. |
| TCP | Incoming `productInfo` commands and outgoing `productInfoChanged` events. |
| MQTT | `virex/productInfoChanged`. |
| Results | `Start` saves a ProductInfo snapshot; snapshot fields are copied when creating results. |
