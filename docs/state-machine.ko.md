# 시작 순서와 상태 전이

이 페이지는 시뮬레이터와 연동하는 클라이언트가 따라야 하는 시작 순서와, 외부 명령으로 발생하는 공개 상태 변화를 단순화한 상태도로 설명합니다. 이 도표는 고객이 연동 판단에 필요한 상태만 남긴 것이며, 모든 내부 가능성을 나열하지 않습니다.

시뮬레이터는 두 개의 공개 값으로 상태를 보고합니다. 클라이언트는 내부 구현 세부 정보를 알 필요가 없습니다. 다음 명령이 유효한지 판단하는 데는 이 필드만 필요합니다.

| 필드 | 의미 |
| --- | --- |
| `initialized` | 시뮬레이션된 서비스가 초기화되었는지 여부. |
| `processState` | 현재 처리 상태. `ready` 는 다음 사이클을 받을 수 있음을 의미합니다. `capturing`, `inspecting`, `saving` 은 사이클 진행 상태입니다. |

`Start Servers` 는 REST, TCP, MQTT 엔드포인트가 수신 중인지 제어합니다. `initialized` 또는 `processState` 는 변경하지 않습니다.

## 상태도

![Virex.NET simulator state transition diagram](assets/state-machine-flow.svg)

도표에서 **external command** 는 클라이언트, SDK, REST, TCP 가 보내는 제어 명령을 의미합니다. `capturing`, `inspecting`, `saving` 은 사이클 중 시뮬레이터 내부에서 진행되는 진행 상태입니다. 클라이언트는 일반적으로 status/result 이벤트를 기다리거나, 취소가 필요하면 **Stop** 을 보냅니다.

## 클라이언트 규칙

| 규칙 | 클라이언트 측 확인 |
| --- | --- |
| **Start Servers** 는 통신 전제 조건입니다. | REST/TCP/MQTT 엔드포인트가 수신 중인 뒤에 연결합니다. |
| **Initialize** 는 사이클 전제 조건입니다. | `initialized=false` 이면 start 는 `409 not_initialized` 를 반환합니다. |
| **Start Cycle** 은 초기화되었고 ready 일 때만 보내야 합니다. | `initialized=true` 이고 `processState=ready` 일 때 start 가 수락됩니다. |
| 실행 중인 사이클 상태는 진행 상황입니다. | `capturing`, `inspecting`, `saving` 은 사이클이 실행 중임을 의미합니다. status/result 이벤트를 기다리거나 stop 을 보냅니다. |
| 시뮬레이터는 결과 이벤트 후 ready 로 돌아갑니다. | 결과 요약 후 `processState=ready` 가 되고 다음 사이클을 시작할 수 있습니다. |

## 명령과 전이

| 명령 또는 UI 작업 | 필요한 상태 | 결과 |
| --- | --- | --- |
| **Initialize** / `POST /api/control/initialize` | `initialized=false`, `processState=ready` | `initialized=true` 로 설정하고 `processState=ready` 를 유지합니다. |
| **Terminate** / `POST /api/control/terminate` | `processState=ready` | `initialized=false` 로 설정하고 `processState=ready` 를 유지합니다. |
| **Start Cycle** / `POST /api/control/start` / TCP `{"type":"start"}` | `initialized=true`, `processState=ready` | `capturing`, `inspecting`, `saving` 을 거쳐 결과 발행 후 `ready` 로 돌아갑니다. |
| **Stop** / `POST /api/control/stop` / TCP `{"type":"stop"}` | 활성 처리 상태: `capturing`, `inspecting`, `saving` | 현재 사이클을 취소하고 `ready` 로 돌아갑니다. |
| **Apply WaferInfo** / WaferInfo REST 또는 TCP 업데이트 | 모든 처리 상태 | 웨이퍼 컨텍스트를 업데이트하고 wafer-info 이벤트를 발행합니다. `processState` 는 변경하지 않습니다. |
| **Emit Fake Result** | 모든 처리 상태 | 단일 결과 요약 이벤트를 발행합니다. `processState` 는 변경하지 않습니다. |
| **Emit Error** | 모든 처리 상태 | 오류 이벤트를 발행합니다. `processState` 는 변경하지 않습니다. |

## 자주 거부되는 명령

| 조건 | 명령 | 응답 |
| --- | --- | --- |
| `initialized=false` | 사이클 시작 | HTTP `409` / `not_initialized`; 상태는 `initialized=false`, `processState=ready` 로 유지됩니다. |
| `processState` 가 `capturing`, `inspecting`, `saving` 중 하나 | 사이클 시작 | HTTP `409` / `process_active`; 현재 사이클은 계속됩니다. |
| `initialized=false` | Stop | HTTP `409` / `not_initialized`; 상태는 변경되지 않습니다. |
| `initialized=true`, `processState=ready` | Stop | HTTP `409` / `not_running`; 상태는 변경되지 않습니다. |
| `processState` 가 `ready` 가 아님 | Terminate | HTTP `409`; 상태는 변경되지 않습니다. |

## 이벤트로 확인되는 상태

상태 변화는 다음 방법으로 확인할 수 있습니다.

- REST `GET /api/status`
- TCP `status` 이벤트
- MQTT `virex/status` 이벤트
- SDK `GetStatusAsync`

result, wafer-info, error 이벤트는 별도의 이벤트 유형입니다. 시뮬레이터가 `ready` 일 때 발생할 수도 있지만, 추가 `processState` 값은 아닙니다.
