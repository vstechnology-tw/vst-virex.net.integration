# ResultSummary

`ResultSummary` 是已完成執行的公開摘要資料。

它包含 `Start` 被接受當下保存的 `ProductInfo` 快照與 `condition`。

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
  "imageRelativePath": "20260620/LOT-001/20260620_153012_W01.tiff",
  "resultRelativePath": "20260620/LOT-001/20260620_153012_W01.json",
  "imagePath": "/data/virex-results/20260620/LOT-001/20260620_153012_W01.tiff",
  "previewImagePath": "/data/virex-results/20260620/LOT-001/20260620_153012_W01.jpg",
  "resultPath": "/data/virex-results/20260620/LOT-001/20260620_153012_W01.json"
}
```

## 欄位

| 欄位 | 型別 | 必填 | 說明 |
| --- | --- | --- | --- |
| `resultId` | string | 是 | 公開結果識別碼。 |
| `timestamp` | string | 是 | 結果時間戳記。 |
| `waferID` | string | 是 | 執行快照裡的 Wafer 識別碼。 |
| `lotID` | string | 是 | 執行快照裡的 Lot 識別碼。 |
| `recipe` | string | 是 | 執行快照裡的 Recipe 識別碼。 |
| `slot` | string | 是 | 執行快照裡的 Slot 識別碼。 |
| `foupID` | string | 是 | 執行快照裡的 FOUP 識別碼。 |
| `chamberID` | string | 是 | 執行快照裡的 Chamber 識別碼。 |
| `condition` | string | 是 | 這次執行保存的開始條件。 |
| `overallResult` | string | 是 | 公開摘要結果值。 |
| `defectCount` | number | 是 | 摘要瑕疵數量。 |
| `imageRelativePath` | string | 是 | 影像成品的相對路徑。 |
| `resultRelativePath` | string | 是 | 結果成品的相對路徑。 |
| `imagePath` | string | 是 | 公開影像路徑。 |
| `previewImagePath` | string | 是 | 公開預覽影像路徑。 |
| `resultPath` | string | 是 | 公開結果路徑。 |

結果摘要只提供摘要，不包含瑕疵清單、die 清單、裁切清單、影像二進位資料或私有檢測內部資料。
