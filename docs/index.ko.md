# Virex.NET 통합 제품군

Virex.NET Integration Kit는 외부 시스템을 Virex.NET 호환 서비스와 통합하는 데 필요한 공개 계약, 시뮬레이터, 샘플 및 문서를 제공합니다. 이 저장소에는 외부에 공개되는 통합 계약만 포함되며, 운영 제품의 비공개 구현 세부 정보는 포함되지 않습니다.

시뮬레이터와 운영 환경 Virex.NET 엔드포인트는 동일한 `Virex.NET.Contracts`를 구현해야 합니다. 외부 벤더가 `Virex.NET.Simulator.WPF`에 대한 통합을 완료하면 운영 엔드포인트로 전환할 때 일반적으로 엔드포인트 설정만 변경하면 됩니다.

## 통신 인터페이스

Virex.NET은 동일한 비즈니스 통합 기능을 세 가지 통신 인터페이스로 제공합니다. 고객은 하나의 인터페이스를 기본 통합 경로로 선택하고, 해당 프로토콜 문서에 따라 동일한 commands, events, payload 동작을 구현해야 합니다.

| 인터페이스 | 기본 시뮬레이터 엔드포인트 | 사용 시점 | 참조 |
| --- | --- | --- | --- |
| RESTful API | `http://127.0.0.1:5088` | HTTP request/response를 선호하고 상태 또는 결과를 polling할 수 있는 시스템. | [RESTful API](rest-api.ko.md) |
| TCP Socket | `127.0.0.1:5089` | 지속 socket에서 양방향 NDJSON이 필요한 시스템. | [TCP Socket Protocol](tcp-socket.ko.md) |
| MQTT | `127.0.0.1:1883`, root topic `virex` | broker 기반 command, response, event 메시징을 이미 사용하는 시스템. | [MQTT Protocol](mqtt-events.ko.md) |

## 공통 비즈니스 기능

RESTful API, TCP Socket, MQTT는 동일한 공개 비즈니스 기능을 제공합니다. transport 이름은 다르지만 lifecycle rules, state restrictions, command responses, event payloads, result payloads는 정렬되어 있습니다. 명확한 운영 이유가 없다면 고객은 하나의 통신 인터페이스를 선택해 전체 흐름을 구현해야 합니다.

### Commands 및 queries

| 비즈니스 기능 | RESTful API | TCP Socket | MQTT |
| --- | --- | --- | --- |
| Query status | `GET /api/status` | `{"type":"status"}` | `virex/commands/status/get` |
| Query error | `GET /api/error` | `{"type":"error"}` | `virex/commands/error/get` |
| Query ProductInfo | `GET /api/product-info` | `{"type":"getProductInfo"}` | `virex/commands/product-info/get` |
| Initialize | `POST /api/system/initialize` | `{"type":"initialize"}` | `virex/commands/system/initialize` |
| Set ProductInfo | `POST /api/product-info` | `{"type":"productInfo", ...}` | `virex/commands/product-info/set` |
| Start run | `POST /api/system/start` | `{"type":"start", ...}` | `virex/commands/system/start` |
| Stop run | `POST /api/system/stop` | `{"type":"stop", ...}` | `virex/commands/system/stop` |
| Query results | `GET /api/results` | `{"type":"results", ...}` | `virex/commands/results/query` |
| Deinitialize | `POST /api/system/deinitialize` | `{"type":"deinitialize"}` | `virex/commands/system/deinitialize` |

### Events

| Event | 의미 |
| --- | --- |
| `statusChanged` | 공개 lifecycle state가 변경됨. |
| `productInfoChanged` | ProductInfo 업데이트 완료. |
| `runStarted` | run이 `Running`에 진입함. |
| `runCompleted` | run이 `Ready`로 돌아옴. |
| `resultCreated` | 공개 result summary가 생성됨. |
| `errorChanged` | 공개 error information이 변경됨. |
| `commandRejected` | command가 상태 규칙 또는 검증으로 거부됨. |

RESTful API는 commands를 HTTP route로 표현하며 event stream은 제공하지 않습니다. client는 polling으로 상태와 결과를 관찰할 수 있습니다. TCP Socket과 MQTT는 동일한 events를 push message로 제공합니다.

## 기본 패킷 형식

세 가지 통신 인터페이스는 모두 UTF-8 JSON payload를 사용하며 동일한 공개 데이터 모델을 공유합니다. 먼저 다음 참조를 확인하십시오.

| 형식 영역 | 참조 |
| --- | --- |
| JSON model rules 및 공통 payload | [Payload Reference](payloads.ko.md) |
| RESTful API route, HTTP body, response code | [RESTful API](rest-api.ko.md) |
| TCP NDJSON frame format 및 frame type | [TCP Socket Protocol](tcp-socket.ko.md) |
| MQTT topic, command request envelope, correlated response | [MQTT Protocol](mqtt-events.ko.md) |

## 프로젝트 목적

| 영역 | 목적 |
| --- | --- |
| `Virex.NET.Contracts` | 공개 데이터 모델, RESTful API 경로, MQTT topic 이름, TCP/NDJSON 형식 지정 및 구문 분석 도구. |
| `Virex.NET.Client` | RESTful API commands/queries, TCP socket communication, MQTT command/event communication용 C# SDK 래퍼. |
| `Virex.NET.Simulator.Core` | 로컬 시뮬레이터에서 사용하는 시뮬레이터별 상태 머신 및 세션 동작. |
| `Virex.NET.Simulator.WPF` | 공개 RESTful API/TCP/MQTT 계약을 제공하는 로컬 시뮬레이터. |
| `samples` | C#, Python, C++ 통합 예시. |
| `docs` | 공개 프로토콜 및 시뮬레이터 문서. |

## 빠른 시작

1. 빌드 및 테스트:

   ```powershell
   dotnet test Virex.NET.Integration.slnx
   ```

2. 시뮬레이터를 시작합니다:

   ```powershell
   dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
   ```

3. 시뮬레이터에서 **Start Servers**를 눌러 RESTful API/TCP/MQTT 서비스를 시작합니다.

4. 다른 터미널을 열어 SDK 예제를 실행합니다.

   ```powershell
   dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj
   ```

샘플은 동일한 13-step demo flow를 따릅니다: query status, query error, query ProductInfo, initialize, confirm Ready, set ProductInfo, confirm ProductInfo, start run, observe run activity, stop run, query results, deinitialize, confirm Uninitialized.

## 주요 공개 데이터 모델

| 데이터 모델 | 사용법 |
| --- | --- |
| [ProductInfo](payloads/product/product-info.ko.md) | 실행 및 결과와 관련된 제품 정보입니다. |
| [SystemStatus](payloads/system/system-status.ko.md) | 현재 lifecycle state. |
| [CommandResponse](payloads/commands/command-response.ko.md) | 명령에 대한 수락 또는 거부 결과입니다. |
| [ResultSummary](payloads/results/result-summary.ko.md) | 완료된 실행에 대한 공개 요약입니다. |
| [ErrorInfo](payloads/system/error-info.ko.md) | 현재 활성 오류 정보입니다. |

이는 JSON 데이터 모델입니다. 벤더는 `Virex.NET.Contracts`에서 C# 유형을 사용하거나 해당 언어로 동등한 모델을 정의할 수 있습니다.

## 참고문서

- [설치/다운로드](installation.ko.md)
- [시뮬레이터 가이드](simulator.ko.md)
- [시스템 상태 머신](state-machine.ko.md)
- [Payload Reference](payloads.ko.md)
- [RESTful API](rest-api.ko.md)
- [TCP Socket Protocol](tcp-socket.ko.md)
- [MQTT Protocol](mqtt-events.ko.md)
- [샘플](samples.ko.md)
