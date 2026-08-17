# ImageGrabbedInfo

`ImageGrabbedInfo` は公開 `imageGrabbed` イベントの metadata payload です。画像取得が完了すると送信され、画像または結果のパスは含まれません。関連するパスは後続の `resultCreated` で提供されます。

## JSON

```json
{"captureId":"CAP-1","timestamp":"2026-08-17T10:00:00.000+08:00","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1"}
```

## フィールド

| フィールド | 型 | 必須 | 説明 |
| --- | --- | --- | --- |
| `captureId` | string | はい | 後続の `ResultSummary` と共有する安定した識別子。 |
| `timestamp` | string | はい | 画像取得時刻。 |
| `lotID` | string | はい | Lot 識別子のスナップショット。 |
| `waferID` | string | はい | Wafer 識別子のスナップショット。 |
| `recipe` | string | はい | Recipe 識別子のスナップショット。 |
| `slot` | string | はい | Slot 識別子のスナップショット。 |
| `foupID` | string | はい | FOUP 識別子のスナップショット。 |
| `chamberID` | string | はい | Chamber 識別子のスナップショット。 |
