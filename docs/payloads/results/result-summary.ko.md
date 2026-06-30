# ResultSummary

`ResultSummary`는 완료된 실행의 공개 요약입니다.

여기에는 `Start`가 수락되었을 때 캡처된 `ProductInfo` 스냅샷과 `condition`가 포함되어 있습니다.

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

## 필드

| 필드 | 유형 | 필수 | 설명 |
| --- | --- | --- | --- |
| `resultId` | 문자열 | 예 | 결과 식별자. |
| `timestamp` | 문자열 | 예 | 결과 타임스탬프. |
| `waferID` | 문자열 | 예 | 실행 스냅샷의 웨이퍼 식별자입니다. |
| `lotID` | 문자열 | 예 | 실행 스냅샷의 로트 식별자입니다. |
| `recipe` | 문자열 | 예 | 실행 스냅샷의 레시피 식별자입니다. |
| `slot` | 문자열 | 예 | 실행 스냅샷의 슬롯 식별자입니다. |
| `foupID` | 문자열 | 예 | 실행 스냅샷의 FOUP 식별자입니다. |
| `chamberID` | 문자열 | 예 | 실행 스냅샷의 챔버 식별자입니다. |
| `condition` | 문자열 | 예 | 이 실행에 대해 캡처된 시작 조건입니다. |
| `overallResult` | 문자열 | 예 | 요약 결과 값입니다. |
| `defectCount` | 번호 | 예 | 요약 결함 수. |
| `imageRelativePath` | 문자열 | 예 | 상대 이미지 경로. |
| `resultRelativePath` | 문자열 | 예 | 상대 결과 경로. |
| `imagePath` | 문자열 | 예 | 이미지 경로. |
| `previewImagePath` | 문자열 | 예 | 미리보기 이미지 경로. |
| `resultPath` | 문자열 | 예 | 결과 경로. |

결과 요약은 요약 데이터만 제공합니다. 결함 목록, 다이 목록, 자르기 목록, 이미지 바이너리 또는 비공개 검사 내부는 포함되지 않습니다.
