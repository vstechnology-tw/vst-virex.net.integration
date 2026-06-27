# ProductInfo

`ProductInfo`는 이벤트 상관관계 및 결과 스냅샷에 사용되는 제품 정보입니다.

## JSON

```json
{
  "waferID": "W01",
  "lotID": "LOT-001",
  "recipe": "RCP-A",
  "slot": "1",
  "foupID": "FOUP-A",
  "chamberID": "CH-1"
}
```

## 필드

| 필드 | 유형 | 필수 | 설명 |
| --- | --- | --- | --- |
| `waferID` | 문자열 | 예 | 웨이퍼 식별자. |
| `lotID` | 문자열 | 예 | 결과 필터링 및 이벤트 상관관계에 사용되는 로트 식별자입니다. |
| `recipe` | 문자열 | 예 | 레시피 식별자. |
| `slot` | 문자열 | 예 | 슬롯 식별자. |
| `foupID` | 문자열 | 예 | FOUP 식별자. |
| `chamberID` | 문자열 | 예 | 챔버 식별자. |

## 사용 위치

| 인터페이스 | 사용법 |
| --- | --- |
| REST | `GET /api/product-info` 응답 및 `POST /api/product-info` 요청. |
| TCP | 수신 `productInfo` 명령 및 송신 `productInfoChanged` 이벤트. |
| MQTT | `virex/productInfoChanged`. |
| 결과 | `Start`는 ProductInfo 스냅샷을 저장합니다. 결과를 생성할 때 스냅샷 필드가 복사됩니다. |
