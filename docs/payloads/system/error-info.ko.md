# ErrorInfo

`ErrorInfo`는 현재 유효한 오류 정보를 설명합니다.

수명주기 상태가 아닙니다. `hasError=false`는 현재 활성 오류가 없음을 의미합니다.

## JSON

```json
{
  "hasError": true,
  "message": "Recipe load failed.",
  "state": "Deinitializing",
  "errorCode": "requires_deinitialize",
  "recoveryAction": "Deinitialize",
  "recoveryStartedAt": "2026-08-07T10:00:00.000+00:00",
  "recoverySource": "Acquisition",
  "recoveryPhase": "Deinitializing",
  "recoveryDetails": "Camera acquisition failed."
}
```

## 필드

| 필드 | 유형 | 필수 | 설명 |
| --- | --- | --- | --- |
| `hasError` | 부울 | 예 | 활성 오류가 현재 존재하는지 여부입니다. |
| `message` | 문자열 | 아니요 | 오류 메시지. 메시지가 없으면 생략됩니다. |
| `state` | 문자열 | 예 | 오류 정보가 보고될 때의 수명 주기 상태입니다. |
| `errorCode` | 문자열 | 아니요 | 기계가 읽을 수 있는 오류 분류입니다. 예: `requires_deinitialize`. |
| `recoveryAction` | 문자열 | 아니요 | 오류에서 복구하기 위해 필요한 운영자 작업입니다. 현재 `Deinitialize`가 정의되어 있으며 작업이 없으면 생략됩니다. |
| `recoveryStartedAt` | 문자열 (date-time) | 아니요 | 현재 복구 시도가 시작된 UTC 시각입니다. |
| `recoverySource` | 문자열 | 아니요 | 실패를 보고한 하위 시스템입니다. |
| `recoveryPhase` | 문자열 | 아니요 | 현재 진행 중인 복구 단계입니다. |
| `recoveryDetails` | 문자열 | 아니요 | 사람이 읽을 수 있는 복구 컨텍스트입니다. |

## 사용 위치

| 인터페이스 | 사용법 |
| --- | --- |
| TCP | `errorChanged` 이벤트. |
| MQTT | `virex/errorChanged`. |
