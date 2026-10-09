# SystemStartRequest

`SystemStartRequest`는 실행 시작을 위한 선택적 요청 본문입니다.

## JSON

```json
{
  "requestId": "work-request-1",
  "recipeName": "RCP-A",
  "condition": "golden-sample",
  "runMode": "continue",
  "inspectionMode": "captureOnly"
}
```

## 필드

| 필드 | 유형 | 필수 | 설명 |
| --- | --- | --- | --- |
| `requestId` | 문자열 | 아니요 | 수락 응답과 작업 이벤트를 연결합니다. SDK가 전송 전에 생성합니다. |
| `recipeName` | 문자열 | 아니요 | 이 작업에서 준비할 Recipe의 카탈로그 이름입니다. 생략하거나 null이면 현재 Recipe를 유지합니다. `condition`과 별개입니다. |
| `condition` | 문자열 | 아니요 | 시작 조건 또는 관련 라벨. |
| `runMode` | 문자열 | 아니요 | 실행 모드. 생략되거나 공백인 경우 `continue`로 간주됩니다. |
| `inspectionMode` | 문자열 | 아니요 | 실행 모드와 별개인 검사 정책입니다. null은 기존 Recipe 동작을 사용합니다. [InspectionMode](../../inspection-mode.md)를 참조하세요. |

## 사용 위치

| 인터페이스 | 사용법 |
| --- | --- |
| RESTful API | `POST /api/system/start` 요청 내용입니다. |
| TCP | 수신 `type: "start"` 명령 프레임. |

허용되는 `runMode` 값은 [ControlRunModes](control-run-modes.ko.md)를 참조하세요.

SDK는 옵션을 전송하며 Recipe를 직접 준비하지 않습니다. 수락은 촬영 준비 완료를 뜻하지 않습니다. 스캔 전에 서버의 해당 작업 준비 이벤트를 기다리세요. 시뮬레이터는 현재 기존 Recipe 동작을 유지하며 이번 필드 추가는 Recipe 전환을 시뮬레이션하지 않습니다.
