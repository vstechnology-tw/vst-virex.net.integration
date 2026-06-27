# SystemStartRequest

`SystemStartRequest`는 실행 시작을 위한 선택적 요청 본문입니다.

## JSON

```json
{
  "condition": "golden-sample",
  "runMode": "continue"
}
```

## 필드

| 필드 | 유형 | 필수 | 설명 |
| --- | --- | --- | --- |
| `condition` | 문자열 | 아니요 | 시작 조건 또는 관련 라벨. |
| `runMode` | 문자열 | 아니요 | 실행 모드. 생략되거나 공백인 경우 `continue`로 간주됩니다. |

## 사용 위치

| 인터페이스 | 사용법 |
| --- | --- |
| REST | `POST /api/system/start` 요청 내용입니다. |
| TCP | 수신 `type: "start"` 명령 프레임. |

허용되는 `runMode` 값은 [ControlRunModes](control-run-modes.ko.md)를 참조하세요.
