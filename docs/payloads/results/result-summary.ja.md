# ResultSummary

`ResultSummary` は、完了した実行の公開サマリーです。

これには、`ProductInfo` スナップショットと、`Start` が受け入れられたときにキャプチャされた `condition` が含まれています。

## JSON

```json
{
  "resultId": "RID-1",
  "timestamp": "2026-06-20T15:30:12+08:00",
  "waferID": "W01",
  "lotID": "LOT-001",
  "recipe": "RCP-A",
  "slot": "1",
  "foupID": "FOUP-A",
  "chamberID": "CH-1",
  "condition": "golden-sample",
  "overallResult": "OK",
  "defectCount": 0,
  "imageRelativePath": "20260620/LOT-001/20260620_153012_W01.bmp",
  "resultRelativePath": "20260620/LOT-001/20260620_153012_W01.json",
  "imagePath": "/data/virex-results/20260620/LOT-001/20260620_153012_W01.bmp",
  "previewImagePath": "/data/virex-results/20260620/LOT-001/20260620_153012_W01.jpg",
  "resultPath": "/data/virex-results/20260620/LOT-001/20260620_153012_W01.json"
}
```

## フィールド

|フィールド |タイプ |必須 |説明 |
| --- | --- | --- | --- |
| `resultId` |文字列 |はい |結果の識別子。 |
| `timestamp` |文字列 |はい |結果のタイムスタンプ。 |
| `waferID` |文字列 |はい |実行スナップショットからのウェーハ識別子。 |
| `lotID` |文字列 |はい |実行スナップショットからのロット識別子。 |
| `recipe` |文字列 |はい |実行スナップショットからのレシピ識別子。 |
| `slot` |文字列 |はい |実行スナップショットからのスロット識別子。 |
| `foupID` |文字列 |はい |実行スナップショットからの FOUP 識別子。 |
| `chamberID` |文字列 |はい |実行スナップショットからのチャンバー識別子。 |
| `condition` |文字列 |はい |この実行のためにキャプチャされた開始条件。 |
| `overallResult` |文字列 |はい |要約結果の値。 |
| `defectCount` |番号 |はい |欠陥数の概要。 |
| `imageRelativePath` |文字列 |はい |相対画像パス。 |
| `resultRelativePath` |文字列 |はい |相対的な結果パス。 |
| `imagePath` |文字列 |はい |画像のパス。 |
| `previewImagePath` |文字列 |はい |プレビュー画像のパス。 |
| `resultPath` |文字列 |はい |結果のパス。 |

結果の概要は概要データのみを提供します。これには、欠陥リスト、ダイ リスト、クロップ リスト、イメージ バイナリ、または非公開検査内部は含まれません。
