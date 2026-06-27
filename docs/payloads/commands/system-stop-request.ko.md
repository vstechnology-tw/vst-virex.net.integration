# SystemStopRequest

`SystemStopRequest`는 실행 중지를 위한 선택적 요청 본문입니다.

## JSON

```json
{
  "reason": "operator-request"
}
```

## 필드

| 필드 | 유형 | 필수 | 설명 |
| --- | --- | --- | --- |
| `reason` | 문자열 | 아니요 | 중지 이유 또는 관련 라벨. |

## 사용 위치

| 인터페이스 | 사용법 |
| --- | --- |
| REST | `POST /api/system/stop` 요청 내용입니다. |
| TCP | 수신 `type: "stop"` 명령 프레임. |
