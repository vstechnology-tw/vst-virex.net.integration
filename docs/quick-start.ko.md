# 빠른 시작

이 프로세스에서는 로컬 시뮬레이터를 사용하여 핵심 통합 경로를 검증합니다.

## 1. 시뮬레이터를 시작합니다

저장소 루트에서 실행합니다.

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
```

기본 엔드포인트 설정을 유지하고 **Start Servers**를 클릭하여 REST/TCP/MQTT 서비스를 시작합니다.

| 인터페이스 | 기본값 |
| --- | --- |
| REST | `http://127.0.0.1:5088` |
| TCP | `127.0.0.1:5089` |
| MQTT | `127.0.0.1:1883`, 토픽 접두사 `virex` |

## 2. SDK 샘플 실행

두 번째 터미널을 엽니다.

```powershell
dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj
```

샘플은 표준 흐름을 실행합니다.

1. `GET /api/status`를 읽어보세요.
2. 시스템을 초기화합니다.
3. `ProductInfo`를 보내세요.
4. 실행을 시작합니다.
5. 실행 완료 이벤트를 관찰합니다.
6. 쿼리 결과.

## 3. 예상 상태 확인

| 단계 | 예상 상태 |
| --- | --- |
| 새로 출시된 시뮬레이터 | `Uninitialized` |
| 초기화 완료 | `Ready` |
| ProductInfo 업데이트 완료 | `Ready` |
| start 명령이 허용됨 | `Running` |
| 실행 완료 | `Ready` |

## 4. 결과 스냅샷 확인

결과에는 `Start`가 수락된 ProductInfo가 포함되어야 합니다.

```json
{
  "waferID": "W01",
  "lotID": "LOT-001",
  "recipe": "RCP-A"
}
```

## 5. 프로토콜 확인을 선택하세요

- REST 브라우저: `http://127.0.0.1:5088/scalar`
- MQTT: `virex/#` 구독
- C# SDK를 사용하지 않는 경우 원시 TCP 또는 원시 REST 샘플을 실행하십시오.

## 완료 조건

빠른 시작을 성공적으로 완료하면 다음을 확인한 것입니다.

- REST 상태 쿼리는 유효한 `state`를 반환합니다.
- ProductInfo는 `Ready`에서 업데이트 가능합니다.
- 시작은 `Running`를 반환합니다.
- 실행이 완료된 후 상태는 `Ready`로 돌아갑니다.
- 결과는 쿼리될 수 있으며 ProductInfo 스냅샷을 따릅니다.
