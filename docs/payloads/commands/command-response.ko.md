# CommandResponse

`CommandResponse`는 명령이 수락되었는지 여부와 명령 처리 후 상태를 보고합니다.

## JSON 수락 시

```json
{
  "accepted": true,
  "state": "Ready",
  "command": "Initialize",
  "message": "Initialize accepted."
}
```

## 거부 시 JSON

```json
{
  "accepted": false,
  "state": "Running",
  "command": "SetProductInfo",
  "errorCode": "invalid_state",
  "message": "SetProductInfo is not valid while state is Running."
}
```

복구 작업이 필요한 경우 응답은 공개 `Deinitializing` 상태를 유지하고 구조화된 작업을 포함합니다:

```json
{
  "accepted": false,
  "state": "Deinitializing",
  "command": "Stop",
  "errorCode": "requires_deinitialize",
  "recoveryAction": "Deinitialize",
  "recoveryStartedAt": "2026-08-07T10:00:00.000+00:00",
  "recoverySource": "Acquisition",
  "recoveryPhase": "Deinitializing",
  "recoveryDetails": "Camera acquisition failed.",
  "message": "Deinitialize is required before another command can be accepted."
}
```

## 필드

| 필드 | 유형 | 필수 | 설명 |
| --- | --- | --- | --- |
| `accepted` | 부울 | 예 | 명령이 수락되는지 여부입니다. |
| `state` | 문자열 | 예 | 명령 처리 후 현재 상태입니다. |
| `command` | 문자열 | 예 | 공개 명령 이름입니다. |
| `errorCode` | 문자열 | 아니요 | 수락된 명령의 경우 생략됩니다. `invalid_state`는 현재 상태에서 명령이 유효하지 않음을 의미합니다. |
| `recoveryAction` | 문자열 | 아니요 | 현재 상태에서 복구하는 데 필요한 운영자 작업입니다. 현재 `Deinitialize`가 정의되어 있으며 작업이 없으면 생략됩니다. |
| `recoveryStartedAt` | 문자열 (date-time) | 아니요 | 현재 복구 시도가 시작된 UTC 시각입니다. |
| `recoverySource` | 문자열 | 아니요 | 실패를 보고한 하위 시스템입니다. |
| `recoveryPhase` | 문자열 | 아니요 | 현재 진행 중인 복구 단계입니다. |
| `recoveryDetails` | 문자열 | 아니요 | 사람이 읽을 수 있는 복구 컨텍스트입니다. |
| `message` | 문자열 | 예 | 응답 메시지. |

## 사용 위치

| 인터페이스 | 사용법 |
| --- | --- |
| RESTful API | 시스템 명령 경로 및 `POST /api/product-info` 응답 본문. |
| TCP | 명령이 거부되면 `commandRejected` 이벤트입니다. |
| MQTT | `virex/commandRejected`. |
