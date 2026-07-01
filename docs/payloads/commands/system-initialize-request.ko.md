# SystemInitializeRequest

`SystemInitializeRequest`는 시스템을 초기화하는 명령 프레임입니다.

## JSON

```json
{
  "type": "initialize"
}
```

## 필드

| 필드 | 유형 | 필수 | 설명 |
| --- | --- | --- | --- |
| `type` | string | 예 | `initialize`여야 합니다. |

## 사용 위치

| 인터페이스 | 사용법 |
| --- | --- |
| RESTful API | `POST /api/system/initialize`는 요청 본문을 사용하지 않습니다. |
| TCP | 수신 `type: "initialize"` 명령 프레임. |
