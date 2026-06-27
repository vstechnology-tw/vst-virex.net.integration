# ResultSummary

`ResultSummary` is the public summary of a completed run.

It contains the `ProductInfo` snapshot and `condition` captured when `Start` was accepted.

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

## Field

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `resultId` | string | Yes | Result identifier. |
| `timestamp` | string | Yes | Result timestamp. |
| `waferID` | string | Yes | Wafer identifier from the run snapshot. |
| `lotID` | string | Yes | Lot identifier from the run snapshot. |
| `recipe` | string | Yes | Recipe identifier from the run snapshot. |
| `slot` | string | Yes | Slot identifier from the run snapshot. |
| `foupID` | string | Yes | FOUP identifier from the run snapshot. |
| `chamberID` | string | Yes | Chamber identifier from the run snapshot. |
| `condition` | string | Yes | Start condition captured for this run. |
| `overallResult` | string | Yes | Summary result value. |
| `defectCount` | number | Yes | Summary defect count. |
| `imageRelativePath` | string | Yes | Relative image path. |
| `resultRelativePath` | string | Yes | Relative result path. |
| `imagePath` | string | Yes | Image path. |
| `previewImagePath` | string | Yes | Preview image path. |
| `resultPath` | string | Yes | Result path. |

The result summary provides summary data only. It does not include defect lists, die lists, crop lists, image binaries, or private inspection internals.
