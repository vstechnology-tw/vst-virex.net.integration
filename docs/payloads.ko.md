# 페이로드 참조

이 페이지는 공개 Virex.NET 연동 표면에서 전송되는 JSON 내용을 설명합니다. 고객에게 보이는 DTO, 라우트/토픽 매핑, 시뮬레이터 검증 절차를 다룹니다. 비공개 검사 로직이나 런타임 내부 정보는 설명하지 않습니다.

## JSON 규칙

모든 REST, TCP, MQTT 페이로드는 같은 공개 JSON 명명 규칙을 사용합니다.

| 규칙 | 동작 |
| --- | --- |
| 속성 이름 | `camelCase` 로 직렬화됩니다. |
| null 값 | 직렬화된 JSON 에서 생략됩니다. |
| 수신 속성 이름 | 대소문자를 구분하지 않고 읽습니다. |
| 텍스트 인코딩 | UTF-8 JSON. |

예:

```json
{
  "initialized": true,
  "processState": "ready",
  "recipe": "RCP-A"
}
```

`processState` 는 다음 중 하나입니다.

| 값 | 의미 |
| --- | --- |
| `ready` | 시뮬레이터가 유휴 상태이며 다음 작업을 받을 준비가 되었습니다. |
| `capturing` | 모의 캡처 단계가 실행 중입니다. |
| `inspecting` | 모의 검사 단계가 실행 중입니다. |
| `saving` | 모의 저장 단계가 실행 중입니다. |

명령 전이와 거부되는 상태는 [상태 전이](state-machine.md)를 참조하십시오.

## 방향 매트릭스

| 페이로드 | REST | TCP | MQTT |
| --- | --- | --- | --- |
| Status | `GET /api/status` 의 송신 응답 | `type: "status"` 를 가진 송신 이벤트 | `virex/status` 의 송신 이벤트 |
| Control status | 제어 POST 라우트의 송신 응답 | 사용하지 않음 | 사용하지 않음 |
| Error status | `GET /api/error` 의 송신 응답 | `type: "error"` 를 가진 송신 이벤트 | `virex/error` 의 송신 이벤트 |
| WaferInfo | `POST /api/wafer-info` 로 들어오는 수신 요청, `GET /api/wafer-info` 의 송신 응답 | `type: "waferInfo"` 를 가진 수신 명령, `type: "waferInfo"` 를 가진 송신 이벤트 | `virex/wafer-info` 의 송신 이벤트 |
| Result summary | `GET /api/results` 응답의 `items[]` 안에 있는 항목 | `type: "result"` 를 가진 송신 이벤트 | `virex/result` 의 송신 이벤트 |
| REST result query response | `GET /api/results` 가 반환하는 응답 래퍼 | 사용하지 않음 | 사용하지 않음 |
| Start command | 제어 라우트 `POST /api/control/start` | `type: "start"` 를 가진 수신 명령 | 사용하지 않음 |
| Stop command | 제어 라우트 `POST /api/control/stop` | `type: "stop"` 를 가진 수신 명령 | 사용하지 않음 |

MQTT는 송신 전용입니다. MQTT 페이로드는 토픽이 이벤트를 식별하므로 `type` 속성이 필요하지 않습니다.

## WaferInfo

방향:

| 전송 방식 | 방향 |
| --- | --- |
| REST | `POST /api/wafer-info` 에서 수신, `GET /api/wafer-info` 에서 송신. |
| TCP | 수신 명령 및 송신 이벤트. |
| MQTT | 송신 이벤트만. |

전송 시점:

| 시나리오 | 동작 |
| --- | --- |
| 호스트가 웨이퍼 컨텍스트를 업데이트함 | 모의 사이클을 시작하거나 조회하기 전에 REST 또는 TCP 로 WaferInfo 를 보냅니다. |
| 시뮬레이터가 웨이퍼 컨텍스트를 발행함 | WaferInfo 가 변경되면 TCP 와 MQTT 클라이언트가 이벤트를 받습니다. |

JSON 예:

```json
{
  "lotId": "LOT-001",
  "waferId": "W01",
  "recipeId": "RCP-A",
  "slot": "1",
  "foupId": "FOUP-A",
  "chamberId": "CH-1"
}
```

필드 표:

| 필드 | 형식 | 설명 |
| --- | --- | --- |
| `lotId` | string | 결과 필터링과 이벤트 상관에 사용하는 공개 로트 식별자. |
| `waferId` | string | 공개 웨이퍼 식별자. |
| `recipeId` | string | 모의 웨이퍼 컨텍스트와 연결된 공개 레시피 식별자. |
| `slot` | string | 슬롯 식별자. |
| `foupId` | string | FOUP 식별자. |
| `chamberId` | string | 챔버 식별자. |

시뮬레이터/샘플 검증:

1. 시뮬레이터를 시작하고 **Start Servers** 를 클릭합니다.
2. C# SDK, raw REST 샘플, raw TCP 샘플을 통해 WaferInfo 를 보내거나, 시뮬레이터 필드를 편집한 뒤 **Apply WaferInfo** 를 클릭합니다.
3. 시뮬레이터 Event Log 에 다음과 같이 모든 공개 필드가 한 줄로 출력되는지 확인합니다.

```text
WaferInfo updated from REST: lotId=LOT-RAW-REST-001, waferId=W01, recipeId=RCP-A, slot=1, foupId=FOUP-A, chamberId=CH-1
```

## Status

방향:

| 전송 방식 | 방향 |
| --- | --- |
| REST | `GET /api/status` 의 송신 응답. |
| TCP | `type: "status"` 를 가진 송신 이벤트. |
| MQTT | `virex/status` 의 송신 이벤트. |

전송 시점:

| 시나리오 | 동작 |
| --- | --- |
| 클라이언트가 현재 시뮬레이터 상태를 읽음 | REST 가 현재 status 를 반환합니다. |
| 시뮬레이터 상태가 변경됨 | TCP 와 MQTT 클라이언트가 status 이벤트를 받습니다. |

JSON 예:

```json
{
  "initialized": true,
  "processState": "ready",
  "recipe": "RCP-A"
}
```

필드 표:

| 필드 | 형식 | 설명 |
| --- | --- | --- |
| `initialized` | boolean | 모의 시스템이 초기화되었는지 여부. |
| `processState` | string | 현재 공개 처리 상태: `ready`, `capturing`, `inspecting`, `saving`. |
| `recipe` | string | status 가 보고하는 현재 공개 레시피 값. |

시뮬레이터/샘플 검증:

1. 시뮬레이터를 시작하고 **Start Servers** 를 클릭합니다.
2. C# SDK 또는 raw REST 샘플을 실행하고 `GET /api/status` 를 읽는지 확인합니다.
3. **Initialize** 또는 **Start Cycle** 을 클릭하고, TCP 또는 MQTT 샘플이 업데이트된 `initialized` 또는 `processState` 값을 포함한 status 이벤트를 출력하는지 확인합니다.

## Control Status

방향:

| 전송 방식 | 방향 |
| --- | --- |
| REST | 제어 POST 라우트의 송신 응답. |
| TCP | 사용하지 않음. |
| MQTT | 사용하지 않음. |

전송 시점:

| 시나리오 | 동작 |
| --- | --- |
| 클라이언트가 제어 작업을 POST 함 | REST 는 작업 후 상태와 공개 메시지를 반환합니다. |

JSON 예:

```json
{
  "initialized": true,
  "processState": "ready",
  "recipe": "RCP-A",
  "message": "Initialized."
}
```

필드 표:

| 필드 | 형식 | 설명 |
| --- | --- | --- |
| `initialized` | boolean | 제어 작업 후 모의 시스템이 초기화되었는지 여부. |
| `processState` | string | 제어 작업 후 공개 처리 상태. |
| `recipe` | string | 현재 공개 레시피 값. |
| `message` | string | 제어 작업에 대한 공개 응답 메시지. |

시뮬레이터/샘플 검증:

1. 시뮬레이터를 시작하고 **Start Servers** 를 클릭합니다.
2. REST 지원 샘플에서 `POST /api/control/initialize`, `POST /api/control/terminate`, `POST /api/control/start`, `POST /api/control/stop` 을 보냅니다.
3. 샘플이 `initialized`, `processState`, `recipe`, `message` 를 포함한 JSON 응답을 출력하는지 확인합니다.

## Error Status

방향:

| 전송 방식 | 방향 |
| --- | --- |
| REST | `GET /api/error` 의 송신 응답. |
| TCP | `type: "error"` 를 가진 송신 이벤트. |
| MQTT | `virex/error` 의 송신 이벤트. |

전송 시점:

| 시나리오 | 동작 |
| --- | --- |
| 클라이언트가 현재 오류 상태를 읽음 | REST 가 현재 error status 를 반환합니다. |
| 시뮬레이터 오류가 변경됨 | TCP 와 MQTT 클라이언트가 error 이벤트를 받습니다. |

JSON 예:

```json
{
  "hasError": true,
  "message": "Simulated error.",
  "initialized": true,
  "processState": "ready",
  "recipe": "RCP-A"
}
```

필드 표:

| 필드 | 형식 | 설명 |
| --- | --- | --- |
| `hasError` | boolean | 현재 오류가 활성 상태인지 여부. |
| `message` | string | 공개 오류 메시지. null 이면 생략됩니다. |
| `initialized` | boolean | error status 시점의 초기화 상태. |
| `processState` | string | error status 시점의 공개 처리 상태. |
| `recipe` | string | 현재 공개 레시피 값. |

시뮬레이터/샘플 검증:

1. 시뮬레이터를 시작하고 **Start Servers** 를 클릭합니다.
2. TCP 또는 MQTT 샘플을 실행하고 연결된 상태로 둡니다.
3. 시뮬레이터에서 **Emit Error** 를 클릭합니다.
4. 샘플이 `hasError`, `message`, `initialized`, `processState`, `recipe` 를 포함한 error 페이로드를 출력하는지 확인합니다.

## Result Summary

`ResultSummaryDto` 는 단일 결과 요약 항목입니다. REST 는 이를 `ResultListDto.items[]` 에 포함하고, TCP 와 MQTT 는 직접 발행합니다.

방향:

| 전송 방식 | 방향 |
| --- | --- |
| REST | `GET /api/results` 응답의 `items[]` 안에 있는 항목. |
| TCP | `type: "result"` 를 가진 송신 이벤트. |
| MQTT | `virex/result` 의 송신 이벤트. |

전송 시점:

| 시나리오 | 동작 |
| --- | --- |
| 시뮬레이터가 결과를 생성함 | TCP 와 MQTT 클라이언트가 결과 요약 이벤트를 받습니다. |
| 클라이언트가 과거 모의 결과를 조회함 | REST 가 `ResultListDto` 를 반환하며, 각 `items[]` 항목은 하나의 `ResultSummaryDto` 입니다. |

JSON 예:

```json
{
  "resultId": "RID-1",
  "timestamp": "2026-06-20T15:30:12+08:00",
  "lotId": "LOT-001",
  "waferId": "W01",
  "recipeId": "RCP-A",
  "slot": "1",
  "foupId": "FOUP-A",
  "chamberId": "CH-1",
  "overallResult": "OK",
  "defectCount": 0,
  "imageRelativePath": "20260620/LOT-001/20260620_153012_W01.tiff",
  "resultRelativePath": "20260620/LOT-001/20260620_153012_W01.json",
  "imagePath": "/data/virex-results/20260620/LOT-001/20260620_153012_W01.tiff",
  "previewImagePath": "/data/virex-results/20260620/LOT-001/20260620_153012_W01.jpg",
  "resultPath": "/data/virex-results/20260620/LOT-001/20260620_153012_W01.json"
}
```

필드 표:

| 필드 | 형식 | 설명 |
| --- | --- | --- |
| `resultId` | string | 공개 결과 식별자. |
| `timestamp` | string | 결과 타임스탬프 문자열. |
| `lotId` | string | 활성 WaferInfo 에서 복사된 로트 식별자. |
| `waferId` | string | 활성 WaferInfo 에서 복사된 웨이퍼 식별자. |
| `recipeId` | string | 결과와 연결된 레시피 식별자. |
| `slot` | string | 활성 WaferInfo 에서 복사된 슬롯 식별자. |
| `foupId` | string | 활성 WaferInfo 에서 복사된 FOUP 식별자. |
| `chamberId` | string | 활성 WaferInfo 에서 복사된 챔버 식별자. |
| `overallResult` | string | 공개 요약 결과 값. |
| `defectCount` | number | 요약에 포함된 모든 결함 범주의 총개수. |
| `imageRelativePath` | string | 관련 이미지 산출물의 상대 경로 문자열. |
| `resultRelativePath` | string | 관련 결과 산출물의 상대 경로 문자열. |
| `imagePath` | string | 시뮬레이터 Result prefix 를 적용한 뒤의 공개 이미지 경로. |
| `previewImagePath` | string | 시뮬레이터 Result prefix 를 적용한 뒤의 공개 미리보기 이미지 경로. |
| `resultPath` | string | 시뮬레이터 Result prefix 를 적용한 뒤의 공개 결과 경로. |

결과 요약은 의도적으로 요약만 포함합니다. 결함 목록, 다이 목록, 크롭 목록, 이미지 바이너리, 비공개 검사 내부 정보는 포함하지 않습니다.

시뮬레이터/샘플 검증:

1. 시뮬레이터를 시작하고 **Start Servers** 를 클릭합니다.
2. WaferInfo 를 적용하고 **Initialize** 를 클릭한 뒤 **Start Cycle** 또는 **Emit Fake Result** 를 클릭합니다.
3. TCP 또는 MQTT 샘플이 `result` 이벤트를 출력하는지 확인하거나, REST 지원 샘플로 `GET /api/results` 를 호출합니다.
4. 반환된 요약에 WaferInfo 식별자와 개수 필드가 포함되지만, 결함 목록, 다이 목록, 크롭 목록, 이미지 바이너리, 비공개 검사 내부 정보는 포함되지 않는지 확인합니다.

## REST Result Query Response

`ResultListDto` 는 결과 조회를 위한 REST 응답 래퍼입니다. 각 `items[]` 항목은 하나의 `ResultSummaryDto` 입니다.

방향:

| 전송 방식 | 방향 |
| --- | --- |
| REST | `GET /api/results` 가 반환하는 응답 래퍼. |
| TCP | 사용하지 않음. |
| MQTT | 사용하지 않음. |

이 래퍼는 REST 전용입니다. TCP 와 MQTT 는 단일 Result Summary 를 직접 발행하며 Result List 를 사용하지 않습니다.

전송 시점:

| 시나리오 | 동작 |
| --- | --- |
| 클라이언트가 결과 요약을 조회함 | REST 는 일치하는 요약 항목과 개수를 포함한 목록 래퍼를 반환합니다. |

`GET /api/results` 는 선택 필터를 지원합니다.

- `lotId`
- `waferId`
- `recipeId`

여러 필터를 지정하면 AND 로 결합됩니다.

쿼리 예:

```text
GET /api/results
GET /api/results?lotId=LOT-001
GET /api/results?lotId=LOT-001&waferId=W01
GET /api/results?lotId=LOT-001&waferId=W01&recipeId=RCP-A
```

JSON 예:

```json
{
  "items": [
    {
      "resultId": "RID-1",
      "timestamp": "2026-06-20T15:30:12+08:00",
      "lotId": "LOT-001",
      "waferId": "W01",
      "recipeId": "RCP-A",
      "slot": "1",
      "foupId": "FOUP-A",
      "chamberId": "CH-1",
      "overallResult": "OK",
      "defectCount": 0,
      "imageRelativePath": "20260620/LOT-001/20260620_153012_W01.tiff",
      "resultRelativePath": "20260620/LOT-001/20260620_153012_W01.json",
      "imagePath": "/data/virex-results/20260620/LOT-001/20260620_153012_W01.tiff",
      "previewImagePath": "/data/virex-results/20260620/LOT-001/20260620_153012_W01.jpg",
      "resultPath": "/data/virex-results/20260620/LOT-001/20260620_153012_W01.json"
    }
  ],
  "count": 1
}
```

필드 표:

| 필드 | 형식 | 설명 |
| --- | --- | --- |
| `items` | array | `ResultSummaryDto` 객체 배열. |
| `count` | number | 반환된 결과 요약 항목 수. |

시뮬레이터/샘플 검증:

1. 일치하는 WaferInfo 로 모의 결과를 생성합니다.
2. REST 지원 샘플에서 `GET /api/results` 를 호출합니다.
3. `count` 가 반환된 `items` 수와 일치하는지 확인합니다.

## REST 매핑

| 메서드와 라우트 | 방향 | 페이로드 내용 | 참고 |
| --- | --- | --- | --- |
| `GET /api/status` | 송신 응답 | `StatusDto` | 현재 공개 상태를 읽습니다. |
| `GET /api/error` | 송신 응답 | `ErrorStatusDto` | 현재 공개 오류 상태를 읽습니다. |
| `GET /api/wafer-info` | 송신 응답 | `WaferInfo` | 현재 공개 웨이퍼 컨텍스트를 읽습니다. |
| `POST /api/wafer-info` | 수신 요청 | `WaferInfo` | 현재 공개 웨이퍼 컨텍스트를 업데이트합니다. |
| `POST /api/control/initialize` | 송신 응답 | `ControlStatusDto` | 시뮬레이터 상태를 초기화합니다. |
| `POST /api/control/terminate` | 송신 응답 | `ControlStatusDto` | 시뮬레이터 상태를 종료합니다. |
| `POST /api/control/start` | 송신 응답 | `ControlStatusDto` | 모의 사이클을 시작합니다. |
| `POST /api/control/stop` | 송신 응답 | `ControlStatusDto` | 모의 사이클을 중지합니다. |
| `GET /api/results` | 송신 응답 | `ResultListDto` | 선택 필터: `lotId`, `waferId`, `recipeId`. 여러 필터는 AND 로 결합됩니다. |

REST 검증:

1. 시뮬레이터를 시작하고 **Start Servers** 를 클릭합니다.
2. C# SDK, C# raw REST, C++ raw REST 샘플을 실행합니다.
3. 샘플이 status 를 읽고, WaferInfo 를 POST 하고, 제어 작업을 실행하고, 위의 공개 페이로드를 사용해 결과 요약을 조회하는지 확인합니다.

## TCP 매핑

TCP 메시지는 JSON 객체입니다. TCP 송신 이벤트에는 `type` 속성이 포함됩니다. TCP 수신 명령은 다음 형태를 사용합니다.

| 수신 명령 | 방향 | JSON 예 | 참고 |
| --- | --- | --- | --- |
| WaferInfo | 시뮬레이터로 수신 | `{"type":"waferInfo","lotId":"LOT-001","waferId":"W01","recipeId":"RCP-A","slot":"1","foupId":"FOUP-A","chamberId":"CH-1"}` | 기존 WaferInfo 프레임은 `type` 을 생략할 수 있습니다. 파서는 WaferInfo JSON 파서로 대체 처리합니다. |
| Start | 시뮬레이터로 수신 | `{"type":"start"}` | 모의 사이클을 시작합니다. |
| Stop | 시뮬레이터로 수신 | `{"type":"stop"}` | 모의 사이클을 중지합니다. |

TCP 송신 이벤트:

| 이벤트 type | 방향 | 페이로드 내용 |
| --- | --- | --- |
| `status` | 시뮬레이터에서 송신 | `StatusDto` 와 `type`. |
| `waferInfo` | 시뮬레이터에서 송신 | `WaferInfo` 와 `type`. |
| `result` | 시뮬레이터에서 송신 | `ResultSummaryDto` 와 `type`. |
| `error` | 시뮬레이터에서 송신 | `ErrorStatusDto` 와 `type`. |

TCP 검증:

1. 시뮬레이터를 시작하고 **Start Servers** 를 클릭합니다.
2. raw TCP 샘플을 실행합니다.
3. 초기 status 및 WaferInfo 프레임이 출력되는지 확인합니다.
4. 샘플이 WaferInfo 프레임을 보내도록 하고, 시뮬레이터 Event Log 에 TCP WaferInfo 업데이트가 표시되는지 확인합니다.

## MQTT 매핑

MQTT는 송신 전용입니다. 모든 시뮬레이터 이벤트를 관측하려면 베이스 토픽 와일드카드를 구독합니다.

```text
virex/#
```

| 토픽 | 방향 | 페이로드 내용 | 참고 |
| --- | --- | --- | --- |
| `virex/status` | 시뮬레이터에서 송신 | `StatusDto` | 페이로드에 `type` 이 필요하지 않습니다. |
| `virex/wafer-info` | 시뮬레이터에서 송신 | `WaferInfo` | 페이로드에 `type` 이 필요하지 않습니다. |
| `virex/result` | 시뮬레이터에서 송신 | `ResultSummaryDto` | 페이로드에 `type` 이 필요하지 않습니다. |
| `virex/error` | 시뮬레이터에서 송신 | `ErrorStatusDto` | 페이로드에 `type` 이 필요하지 않습니다. |

MQTT 검증:

1. 시뮬레이터를 시작하고 **Start Servers** 를 클릭합니다.
2. raw MQTT 샘플을 실행하고 구독 상태를 유지합니다.
3. **Apply WaferInfo**, **Initialize**, **Start Cycle**, **Emit Fake Result**, **Emit Error** 를 클릭합니다.
4. 샘플이 `wafer-info`, `status`, `result`, `error` 에 대응하는 토픽과 JSON 페이로드를 출력하는지 확인합니다.
