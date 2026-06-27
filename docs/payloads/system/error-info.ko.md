# ErrorInfo

`ErrorInfo`는 현재 유효한 오류 정보를 설명합니다.

수명주기 상태가 아닙니다. `hasError=false`는 현재 활성 오류가 없음을 의미합니다.

## JSON

```json
{
  "hasError": true,
  "message": "Recipe load failed.",
  "state": "Ready"
}
```

## 필드

| 필드 | 유형 | 필수 | 설명 |
| --- | --- | --- | --- |
| `hasError` | 부울 | 예 | 활성 오류가 현재 존재하는지 여부입니다. |
| `message` | 문자열 | 아니요 | 오류 메시지. 메시지가 없으면 생략됩니다. |
| `state` | 문자열 | 예 | 오류 정보가 보고될 때의 수명 주기 상태입니다. |

## 사용 위치

| 인터페이스 | 사용법 |
| --- | --- |
| TCP | `errorChanged` 이벤트. |
| MQTT | `virex/errorChanged`. |
