# ImageGrabbedInfo

`ImageGrabbedInfo`는 공개 `imageGrabbed` 이벤트의 metadata payload입니다. 이미지 취득 후 시뮬레이터가 artifact를 저장하기 전에 게시되므로 이미지 또는 결과 경로를 포함하지 않습니다.

## JSON

```json
{"captureId":"CAP-1","timestamp":"2026-08-17T10:00:00.000+08:00","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1"}
```

## 필드

| 필드 | 유형 | 필수 | 설명 |
| --- | --- | --- | --- |
| `captureId` | string | 예 | 이후 `ResultSummary`와 공유하는 안정적인 식별자. |
| `timestamp` | string | 예 | 이미지 취득 시간. |
| `lotID` | string | 예 | Lot 식별자 스냅샷. |
| `waferID` | string | 예 | Wafer 식별자 스냅샷. |
| `recipe` | string | 예 | Recipe 식별자 스냅샷. |
| `slot` | string | 예 | Slot 식별자 스냅샷. |
| `foupID` | string | 예 | FOUP 식별자 스냅샷. |
| `chamberID` | string | 예 | Chamber 식별자 스냅샷. |
