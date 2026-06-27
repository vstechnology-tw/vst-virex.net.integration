# ProductInfo

`ProductInfo` は、イベント相関および結果スナップショットに使用される製品情報です。

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

## フィールド

|フィールド |タイプ |必須 |説明 |
| --- | --- | --- | --- |
| `waferID` |文字列 |はい |ウェーハの識別子。 |
| `lotID` |文字列 |はい |結果のフィルタリングとイベントの関連付けに使用されるロット識別子。 |
| `recipe` |文字列 |はい |レシピの識別子。 |
| `slot` |文字列 |はい |スロット識別子。 |
| `foupID` |文字列 |はい | FOUPの識別子。 |
| `chamberID` |文字列 |はい |チャンバーの識別子。 |

## 使用箇所

|インターフェース |使い方 |
| --- | --- |
| REST | `GET /api/product-info` 応答と `POST /api/product-info` 要求。 |
| TCP |受信 `productInfo` コマンドと送信 `productInfoChanged` イベント。 |
| MQTT | `virex/productInfoChanged`。 |
|結果 | `Start` は ProductInfo スナップショットを保存します。スナップショット フィールドは結果の作成時にコピーされます。 |
