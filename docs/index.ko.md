# Virex.NET 통합 제품군

Virex.NET Integration Kit는 외부 시스템을 Virex.NET 호환 서비스와 통합하는 데 필요한 공개 계약, C# SDK, 시뮬레이터, 샘플 및 문서를 제공합니다. 이 저장소에는 운영 환경 Virex.NET 제품의 비공개 구현 세부 정보가 포함되어 있지 않습니다.

시뮬레이터와 운영 환경 Virex.NET 엔드포인트는 동일한 `Virex.NET.Contracts`를 구현해야 합니다. 외부 벤더가 `Virex.NET.Simulator.WPF`에 대한 통합을 완료한 경우 운영 엔드포인트로 전환하려면 REST/TCP/MQTT 엔드포인트 설정만 변경하면 됩니다.

## 프로젝트 목적

| 면적 | 목적 |
| --- | --- |
| `Virex.NET.Contracts` | 공개 데이터 모델, REST 경로, MQTT 토픽 이름, TCP/NDJSON 형식 지정 및 구문 분석 도구. |
| `Virex.NET.Client` | C# REST 명령/쿼리, TCP 이벤트 및 MQTT 이벤트용 SDK 래퍼입니다. |
| `Virex.NET.Simulator.Core` | 로컬 시뮬레이터에서 사용하는 시뮬레이터별 상태 머신 및 세션 동작입니다. |
| `Virex.NET.Simulator.WPF` | 공개 REST/TCP/MQTT 계약을 제공하는 로컬 시뮬레이터입니다. |
| `samples` | C#, Python, C++ 통합 예시. |
| `docs` | 공개 프로토콜 및 시뮬레이터 문서. |

## 기본 시뮬레이터 끝점

| 인터페이스 | 기본값 |
| --- | --- |
| REST | `http://127.0.0.1:5088` |
| TCP | `127.0.0.1:5089` |
| MQTT | `127.0.0.1:1883`, 루트 토픽 `virex` |

## 빠른 시작

1. 빌드 및 테스트:

   ```powershell
   dotnet test Virex.NET.Integration.sln
   ```

2. 시뮬레이터를 시작합니다:

   ```powershell
   dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
   ```

3. 시뮬레이터에서 **Start Servers**를 눌러 REST/TCP/MQTT 서비스를 시작합니다.

4. 다른 터미널을 열어 SDK 예제를 실행합니다.

   ```powershell
   dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj
   ```

이 예에서는 시스템을 초기화하고, ProductInfo를 업데이트하고, 실행을 시작하고, 실행이 완료될 때까지 기다리고, 결과를 쿼리합니다.

## 주요 SDK 메서드

- `GetStatusAsync`
- `GetProductInfoAsync`
- `SetProductInfoAsync`
- `InitializeAsync`
-`DeinitializeAsync`
- `StartAsync`
- `StopAsync`
- `QueryResultsAsync`

## 주요 공개 데이터 모델

| 데이터 모델 | 사용법 |
| --- | --- |
| [ProductInfo](payloads/product/product-info.ko.md) | 실행 및 결과와 관련된 제품 정보입니다. |
| [SystemStatus](payloads/system/system-status.ko.md) | 현재 수명주기 상태. |
| [CommandResponse](payloads/commands/command-response.ko.md) | 명령에 대한 수락 또는 거부 결과입니다. |
| [ResultSummary](payloads/results/result-summary.ko.md) | 완료된 실행에 대한 공개 요약입니다. |
| [ErrorInfo](payloads/system/error-info.ko.md) | 현재 활성 오류 정보입니다. |

이는 JSON 데이터 모델입니다. 벤더는 `Virex.NET.Contracts`에서 C# 유형을 사용하거나 해당 언어로 동등한 모델을 정의할 수 있습니다.

## 참고문서

- [설치/다운로드](installation.ko.md)
- [시뮬레이터 가이드](simulator.ko.md)
- [시스템 상태 머신](state-machine.ko.md)
- [페이로드 참조](payloads.ko.md)
- [REST API](rest-api.ko.md)
- [TCP 소켓 프로토콜](tcp-socket.ko.md)
- [MQTT 이벤트](mqtt-events.ko.md)
- [샘플](samples.ko.md)
