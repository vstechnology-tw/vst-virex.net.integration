# ResultList

`ResultList`는 결과 쿼리에 대한 RESTful API 응답 래퍼입니다.

## JSON

```json
{
  "items": [],
  "count": 0
}
```

## 필드

| 필드 | 유형 | 필수 | 설명 |
| --- | --- | --- | --- |
| `items` | [ResultSummary](result-summary.ko.md) 배열 | 예 | 일치 결과 요약. |
| `count` | 번호 | 예 | 반품된 항목의 수입니다. |

## 사용 위치

| 인터페이스 | 사용법 |
| --- | --- |
| RESTful API | `GET /api/results` 응답. |

## 지원되는 쿼리 필터

```text
waferID
lotID
recipe
```

여러 필터는 AND로 결합됩니다.
