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

## 필드

| 필드 | 유형 | 필수 | 설명 |
| --- | --- | --- | --- |
| `accepted` | 부울 | 예 | 명령이 수락되는지 여부입니다. |
| `state` | 문자열 | 예 | 명령 처리 후 현재 상태입니다. |
| `command` | 문자열 | 예 | 공개 명령 이름입니다. |
| `errorCode` | 문자열 | 아니요 | 수락된 명령의 경우 생략됩니다. `invalid_state`는 현재 상태에서 명령이 유효하지 않음을 의미합니다. |
| `message` | 문자열 | 예 | 응답 메시지. |

## 사용 위치

| 인터페이스 | 사용법 |
| --- | --- |
| REST | 시스템 명령 경로 및 `POST /api/product-info` 응답 본문. |
| TCP | 명령이 거부되면 `commandRejected` 이벤트입니다. |
| MQTT | `virex/commandRejected`. |
