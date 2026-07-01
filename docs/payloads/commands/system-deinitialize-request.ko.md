# SystemDeinitializeRequest

`SystemDeinitializeRequest`는 시스템을 반초기화하는 명령 프레임입니다.

## JSON

```json
{
  "type": "deinitialize"
}
```

## 필드

| 필드 | 유형 | 필수 | 설명 |
| --- | --- | --- | --- |
| `type` | string | 예 | `deinitialize`여야 합니다. |

## 사용 위치

| 인터페이스 | 사용법 |
| --- | --- |
| REST | `POST /api/system/deinitialize`는 요청 본문을 사용하지 않습니다. |
| TCP | 수신 `type: "deinitialize"` 명령 프레임. |
