# SystemStatus

`SystemStatus`는 현재 공개 수명 주기 상태를 보고합니다.

## JSON

```json
{
  "state": "Ready"
}
```

## 필드

| 필드 | 유형 | 필수 | 설명 |
| --- | --- | --- | --- |
| `state` | 문자열 | 예 | 현재 수명주기 상태. |
| `recoveryAction` | 문자열 | 아니요 | 복구에 필요한 운영자 작업입니다. 현재 `Deinitialize`가 정의되어 있으며 작업이 없으면 생략됩니다. |

## 상태 값

```text
Uninitialized
Initializing
Ready
UpdatingProductInfo
Running
Deinitializing
```

## 사용 위치

| 인터페이스 | 사용법 |
| --- | --- |
| RESTful API | `GET /api/status` 응답. |
| TCP | `statusChanged`, `runStarted`, `runCompleted` 이벤트. |
| MQTT | `virex/statusChanged`, `virex/runStarted`, `virex/runCompleted`. |

`status`는 리소스 또는 이벤트 분류입니다. `state`는 이 상태 페이로드가 보고하는 수명 주기 값입니다.
