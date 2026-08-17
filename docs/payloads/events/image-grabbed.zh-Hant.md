# ImageGrabbedInfo

`ImageGrabbedInfo` 是公開 `imageGrabbed` 事件的 metadata payload。它在取像完成、模擬器儲存 artifact 前發布，因此不包含影像或結果路徑。

## JSON

```json
{"captureId":"CAP-1","timestamp":"2026-08-17T10:00:00.000+08:00","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1"}
```

## 欄位

| 欄位 | 類型 | 必填 | 說明 |
| --- | --- | --- | --- |
| `captureId` | string | 是 | 與後續 `ResultSummary` 共用的穩定識別碼。 |
| `timestamp` | string | 是 | 取像時間。 |
| `lotID` | string | 是 | Lot 識別碼快照。 |
| `waferID` | string | 是 | Wafer 識別碼快照。 |
| `recipe` | string | 是 | Recipe 識別碼快照。 |
| `slot` | string | 是 | Slot 識別碼快照。 |
| `foupID` | string | 是 | FOUP 識別碼快照。 |
| `chamberID` | string | 是 | Chamber 識別碼快照。 |
