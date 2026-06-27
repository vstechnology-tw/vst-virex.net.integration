# 시뮬레이터 가이드

`Virex.NET.Simulator.WPF`는 통합 개발을 위한 로컬 엔드포인트입니다. REST, TCP, MQTT, 데이터 모델, 상태 및 이벤트의 관점에서 볼 때 운영 환경과 호환되는 Virex.NET 서비스처럼 작동해야 합니다.

저장소 루트에서 시작합니다.

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
```

## 시뮬레이터 목적

| 목적 | 확인할 사항 |
| --- | --- |
| 계약 확인 | 벤더 애플리케이션이 운영 환경과 동일한 REST 경로, 데이터 모델, TCP 프레임 및 MQTT 토픽을 사용하는지 확인합니다. |
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
7. 실행을 시작합니다.
8. `runStarted`, `runCompleted`, `resultCreated`를 관찰합니다.
9. `GET /api/results`를 쿼리합니다.

## 기본 엔드포인트

| 인터페이스 | 기본값 |
| --- | --- |
| REST | `http://127.0.0.1:5088` |
| REST 브라우저 | `http://127.0.0.1:5088/scalar` |
| OpenAPI JSON | `http://127.0.0.1:5088/openapi/v1.json` |
| TCP | `127.0.0.1:5089` |
| MQTT | `127.0.0.1:1883`, 루트 토픽 `virex` |

## 버튼 동작

| 버튼 | 동작 |
| --- | --- |
| **Start Servers** | REST, TCP 및 MQTT 엔드포인트를 시작합니다. 시스템 상태를 변경하지 않습니다. |
| **Initialize** | 초기화 명령을 보냅니다. `Uninitialized`에서만 유효합니다. |
| **Deinitialize** | 초기화 해제 명령을 보냅니다. `Ready`에서만 유효합니다. |
| **Apply ProductInfo** | 현재 ProductInfo를 업데이트합니다. `Ready`에서만 유효합니다. |
| **Start** | 실행을 시작합니다. `Ready`에서만 유효합니다. 응답 상태는 `Running`입니다. |
| **Stop** | 활성 실행을 중지합니다. `Running`에서만 유효합니다. 응답 상태는 `Ready`입니다. |

## 관찰 가능한 동작

| 액션 | 예상되는 외부 관찰 |
| --- | --- |
| Initialize | REST 명령은 `Ready`를 반환합니다. 상태 이벤트가 게시됩니다. |
| ProductInfo 업데이트 | REST 명령은 `Ready`를 반환합니다. ProductInfo 이벤트가 게시됩니다. |
| 시작 | REST 명령은 `Running`를 반환합니다. 실행 시작 이벤트가 게시됩니다. |
| 실행 완료 | 상태는 `Ready`로 돌아갑니다. 실행 완료 및 결과 생성 이벤트가 게시됩니다. |
| 잘못된 명령 | 명령 응답에는 `accepted=false` 및 `errorCode=invalid_state`가 포함됩니다. 거부 이벤트가 게시될 수 있습니다. |

## 권장 시뮬레이터 검수 절차

```powershell
dotnet test Virex.NET.Integration.sln
python -m mkdocs build --strict
```

그런 다음 로컬 시뮬레이터를 수동으로 사용하여 생성된 문서와 C# SDK 샘플을 확인합니다.
