# 시뮬레이터 가이드

`Virex.NET.Simulator.WPF`는 통합 개발을 위한 로컬 엔드포인트입니다. RESTful API, TCP, MQTT, 데이터 모델, 상태 및 이벤트의 관점에서 볼 때 운영 환경과 호환되는 Virex.NET 서비스처럼 작동해야 합니다.

저장소 루트에서 시작합니다.

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
```

시뮬레이터를 시작하면 다음 WPF 창이 표시됩니다.

![번호가 표시된 시뮬레이터 메인 창](assets/simulator-main-window-annotated.png)

## 시뮬레이터 창 안내

| 영역 | 화면 영역 | 용도 |
| --- | --- | --- |
| 1 | **Connection Settings** | 시뮬레이터가 외부로 제공하는 엔드포인트를 설정합니다. **RESTful API prefix**는 HTTP 기본 주소, **TCP port**는 NDJSON socket 리스너 포트, **MQTT host**와 **MQTT port/topic**은 내장 MQTT broker와 토픽 접두사, **Result prefix**는 결과 ID 또는 결과 경로에 테스트용 접두사가 필요할 때만 사용합니다. |
| 2 | **ProductInfo** | 시뮬레이션 시스템에 보낼 제품 컨텍스트를 설정합니다. Lot ID, Wafer ID, Recipe, Slot, Foup ID, Chamber ID를 입력한 뒤 시스템이 `Ready` 상태가 되면 **Apply ProductInfo**를 누릅니다. |
| 3 | **State** | 현재 시뮬레이터 상태를 표시하고 주요 상호작용 버튼을 제공합니다. **Start Servers**는 RESTful API, TCP, MQTT 엔드포인트를 엽니다. **Initialize**, **Deinitialize**, **Start Single**, **Start Continue**, **Stop**은 외부 RESTful API 클라이언트가 호출할 수 있는 것과 동일한 공개 상태 전이를 실행합니다. |
| 4 | **Event Log** | 로컬 시뮬레이터 활동, 서버 시작/중지 메시지, 명령 거부, 생성된 결과 및 기타 진단 출력을 표시합니다. 버튼 동작이나 클라이언트 명령이 시뮬레이터에 도달했는지 확인할 때 사용합니다. |
| 5 | **State Machine** | 실시간 상태 그래프를 표시합니다. 강조된 블록은 현재 시뮬레이터 상태를 따라갑니다. `ƒ` 라벨은 command, `⚡` 라벨은 event입니다. `Initializing`, `UpdatingProductInfo`, `Deinitializing` 같은 중간 상태는 잠시 표시되어 전이 경로를 확인하기 쉽습니다. |

## 시뮬레이터 목적

| 목적 | 확인할 사항 |
| --- | --- |
| 계약 확인 | 벤더 애플리케이션이 운영 환경과 동일한 RESTful API 경로, 데이터 모델, TCP 프레임 및 MQTT 토픽을 사용하는지 확인합니다. |
| 상태 머신 검증 | 명령 순서, 유효한 상태, 거부된 상태 및 실행 완료 동작을 확인합니다. |
| 이벤트 확인 | TCP/MQTT 소비자가 상태, ProductInfo, 실행, 결과, 오류 및 거부 이벤트를 처리할 수 있는지 확인합니다. |

시뮬레이터는 운영 검사 엔진이 아니며 비공개 알고리즘, 카메라 동작, 레시피 내부 또는 저장소 내부를 노출하지 않습니다.

## 표준 작동

1. 시뮬레이터를 시작합니다.
2. 엔드포인트 설정을 확인합니다.
3. **Start Servers**를 누르세요.
4. 샘플 또는 벤더 클라이언트를 연결합니다.
5. 시스템을 초기화합니다.
6. 제품 정보를 보냅니다.
7. **Start Single**을 눌러 한 번만 자동 실행하거나, **Start Continue**를 눌러 **Stop**을 누를 때까지 결과를 계속 생성합니다.
8. **State Machine**의 강조 표시가 현재 상태에 맞춰 이동하는지 확인합니다.
9. run mode에 따라 `runStarted`, `runCompleted`, `resultCreated` 또는 반복되는 `resultCreated` 이벤트를 관찰합니다.
10. `GET /api/results`를 쿼리합니다.

## 기본 엔드포인트

| 인터페이스 | 기본값 |
| --- | --- |
| RESTful API | `http://127.0.0.1:5088` |
| RESTful API 브라우저 | `http://127.0.0.1:5088/scalar` |
| OpenAPI JSON | `http://127.0.0.1:5088/openapi/v1.json` |
| TCP | `127.0.0.1:5089` |
| MQTT | `127.0.0.1:1883`, 토픽 접두사 `virex` |

## 버튼 동작

| 버튼 | 동작 |
| --- | --- |
| **Start Servers** | RESTful API, TCP 및 MQTT 엔드포인트를 시작합니다. 시스템 상태를 변경하지 않습니다. |
| **Initialize** | 초기화 명령을 보냅니다. `Uninitialized`에서만 유효합니다. |
| **Deinitialize** | 초기화 해제 명령을 보냅니다. `Ready`에서만 유효합니다. |
| **Apply ProductInfo** | 현재 ProductInfo를 업데이트합니다. `Ready`에서만 유효합니다. |
| **Start Single** | `runMode=single`로 단일 실행을 시작합니다. `Ready`에서만 유효합니다. 응답 상태는 `Running`입니다. 시뮬레이터는 결과를 생성하고 run-completed event 이후 `Ready`로 돌아갑니다. |
| **Start Continue** | `runMode=continue`로 연속 실행을 시작합니다. `Ready`에서만 유효합니다. 응답 상태는 `Running`입니다. **Stop**을 누를 때까지 결과를 계속 생성합니다. |
| **Stop** | 활성 실행을 중지합니다. `Running`에서만 유효합니다. 응답 상태는 `Ready`입니다. |

## 관찰 가능한 동작

| 액션 | 예상되는 외부 관찰 |
| --- | --- |
| Initialize | RESTful API 명령은 `Ready`를 반환합니다. 상태 이벤트가 게시됩니다. |
| ProductInfo 업데이트 | RESTful API 명령은 `Ready`를 반환합니다. ProductInfo 이벤트가 게시됩니다. |
| Start single | RESTful API 명령은 `Running`을 반환합니다. 상태는 `Running`으로 바뀝니다. 결과 생성 이벤트가 게시되고 run-completed로 상태가 `Ready`로 돌아갑니다. |
| Start continue | RESTful API 명령은 `Running`을 반환합니다. 상태는 `Running`으로 유지됩니다. stop 명령이 수락될 때까지 결과 생성 이벤트가 계속 게시됩니다. |
| Stop | 상태는 `Ready`로 돌아갑니다. continue mode에서는 추가 자동 run-completed event가 필요하지 않습니다. |
| 잘못된 명령 | 명령 응답에는 `accepted=false` 및 `errorCode=invalid_state`가 포함됩니다. 거부 이벤트가 게시될 수 있습니다. |

## 권장 시뮬레이터 검수 절차

```powershell
dotnet test Virex.NET.Integration.slnx
python -m mkdocs build --strict
```

그런 다음 로컬 시뮬레이터를 수동으로 사용하여 생성된 문서와 C# SDK 샘플을 확인합니다.
