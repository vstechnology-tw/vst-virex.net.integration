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
| REST | `GET /api/status` 응답. |
| TCP | `statusChanged`, `runStarted`, `runCompleted` 이벤트. |
| MQTT | `virex/statusChanged`, `virex/runStarted`, `virex/runCompleted`. |

`status`는 리소스 또는 이벤트 분류입니다. `state`는 이 상태 페이로드가 보고하는 수명 주기 값입니다.
