# ImageGrabbedInfo

`ImageGrabbedInfo` is the metadata payload for the public `imageGrabbed` event. It is emitted after image acquisition and before simulator artifact persistence, so it does not contain image or result paths.

## JSON

```json
{"captureId":"CAP-1","timestamp":"2026-08-17T10:00:00.000+08:00","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1"}
```

## Fields

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `captureId` | string | Yes | Stable identity shared with the later `ResultSummary`. |
| `timestamp` | string | Yes | Image acquisition timestamp. |
| `lotID` | string | Yes | Lot identifier snapshot. |
| `waferID` | string | Yes | Wafer identifier snapshot. |
| `recipe` | string | Yes | Recipe identifier snapshot. |
| `slot` | string | Yes | Slot identifier snapshot. |
| `foupID` | string | Yes | FOUP identifier snapshot. |
| `chamberID` | string | Yes | Chamber identifier snapshot. |
