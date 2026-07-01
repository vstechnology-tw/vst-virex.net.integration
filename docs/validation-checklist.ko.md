# 검증 체크리스트

이 체크리스트를 사용하여 벤더 통합이 시뮬레이터에서 운영 환경 호환 엔드포인트로 이동할 준비가 되었는지 확인하세요.

## RESTful API

| 확인 | 예상 결과 |
| --- | --- |
| 상태 읽기 | `GET /api/status`는 `state`와 함께 `SystemStatus`를 반환합니다. |
| 초기화 | `POST /api/system/initialize`는 `Uninitialized`에서 허용되며 `Ready`를 반환합니다. |
| ProductInfo 업데이트 | `POST /api/product-info`는 `Ready`에서 허용되며 `Ready`를 반환합니다. |
| 시작 | `POST /api/system/start`는 `Ready`에서 허용되며 `Running`를 반환합니다. |
| 중지 | `POST /api/system/stop`는 `Running`에서 허용되며 `Ready`를 반환합니다. |
| Deinitialize | `POST /api/system/deinitialize`는 `Ready`에서 허용되며 `Uninitialized`를 반환합니다. |
| 잘못된 명령 | 잘못된 명령은 `accepted=false`, `errorCode=invalid_state` 및 현재 `state`를 반환합니다. |
| 결과 | `GET /api/results`는 ProductInfo 스냅샷 필드와 일치하는 요약을 반환합니다. |

## TCP

| 확인 | 예상결과 |
| --- | --- |
| 연결 | 클라이언트는 구성된 TCP 포트에 연결할 수 있습니다. |
| 프레이밍 | 각 프레임은 `\n`로 끝나는 하나의 UTF-8 JSON 객체입니다. |
| ProductInfo 명령 | `type: "productInfo"`는 `Ready`의 ProductInfo를 업데이트합니다. |
| 시작/stop 명령 | `type: "start"` 및 `type: "stop"`는 REST와 동일한 상태 규칙을 따릅니다. |
| 이벤트 분석 | 클라이언트는 `statusChanged`, `productInfoChanged`, `runStarted`, `runCompleted`, `resultCreated`, `errorChanged` 및 `commandRejected`를 처리할 수 있습니다. |

## MQTT

| 확인 | 예상결과 |
| --- | --- |
| 구독 | 클라이언트는 `virex/#` 또는 구성된 토픽 접두사를 구독할 수 있습니다. |
| 상태 이벤트 | 클라이언트는 `statusChanged`, `runStarted` 및 `runCompleted`를 받습니다. |
| ProductInfo 이벤트 | 클라이언트는 `productInfoChanged`를 받습니다. |
| 결과 이벤트 | 클라이언트는 `resultCreated`를 받습니다. |
| 거절 이벤트 | 명령이 거부되면 클라이언트는 `commandRejected`를 수신합니다. |

## 이식성

운영 환경으로 전환하기 전에 다음 사항을 확인하세요.

- 끝점 설정은 조정 가능합니다.
- 통합은 시뮬레이터 UI 라벨이나 고정 지연에 의존하지 않습니다.
- 통합에서는 `Virex.NET.Contracts` 모델 또는 동등한 JSON 구조를 사용합니다.
- 통합에서는 MQTT를 순수 발신 채널로 처리합니다.
- 통합을 통해 재접속 및 중복 이벤트를 처리할 수 있습니다.
