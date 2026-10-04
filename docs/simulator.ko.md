# 시뮬레이터 가이드

공개 REST, TCP, MQTT 계약을 위한 로컬 시뮬레이터입니다. 이미지와 검사 결과는 모의 데이터입니다. 이벤트는 실제 App 또는 카메라의 준비 완료를 증명하지 않습니다.

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj --framework net10.0-windows
# 소스 정책을 명시적으로 활성화
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj --framework net10.0-windows -- --manage-operation-mode
```

## 창 조작

- **Connection Settings**: REST prefix, TCP port, MQTT host／port／topic을 설정합니다. **Start Servers** 또는 **Stop Servers**로 이 창이 소유한 엔드포인트를 시작하거나 중지합니다.
- **ProductInfo**: Lot ID, Wafer ID, Recipe, Slot, Foup ID, Chamber ID를 설정합니다. Ready에서 **Apply ProductInfo**를 눌러 적용합니다.
- **State**: **Initialize**, **Deinitialize**, **Start Single**, **Start Continue**, **Stop**을 제공합니다. Start에는 Ready, Stop에는 Running이 필요합니다. Single이 완료되면 자동으로 Ready로 돌아갑니다.

## 이미지 캡처 및 검사 모드

**Start Single** 또는 **Start Continue** 전에 **Omitted (legacy)**, **captureOnly**, **captureAndInspect**를 선택합니다. Run mode와 inspection mode는 독립적입니다. 생략하면 recipe 기본값을 유지합니다. 이 시뮬레이터에서 captureOnly는 모의 이미지와 capture 진단 파일을 저장하지만 검사 결과나 resultCreated 이벤트는 생성하지 않습니다. captureAndInspect는 모의 검사 결과도 생성합니다.

## 조회 및 조작 모드

recipe 목록, 현재 recipe, 공개 매개 변수 또는 result detail을 조회합니다. **Get result detail** 전에 대소문자를 구분하는 정확한 ResultId를 입력합니다. 현재 recipe가 없거나 ID를 찾을 수 없으면 공개 조회 오류를 표시하며 최신 결과로 대체하지 않습니다.

**Get operation mode**는 실제 적용된 local/remote 및 managementEnabled를 표시합니다. 모드를 선택하고 **Apply mode**를 누르면 수락 후 표시가 바뀝니다. 관리는 기본적으로 비활성화됩니다. 시작 인수 **--manage-operation-mode**를 사용하면 Local은 로컬 조작을, Remote는 외부 라이프사이클 조작을 허용합니다. 모드 변경은 라이프사이클 상태를 변경하지 않습니다.

## 캡처 이벤트 및 상태 머신

**Capture Events** 탭은 최신 captureReady 및 captureCompleted의 JobId, CaptureId, SourcesCount, timestamp를 표시합니다. captureCompleted와 이후 resultCreated는 별도 이벤트입니다. Event Log에서 순서를 확인합니다. **State Machine** 탭은 실제 session 상태를 표시합니다.

## 취소 및 종료

조회는 창을 차단하지 않습니다. **Cancel query**는 현재 조회를 취소합니다. 이전 조회는 새로운 결과를 덮어쓰지 않습니다. 창을 닫으면 모의 실행을 취소하고 완료를 기다린 다음 소유한 엔드포인트를 중지합니다. 호스트 종료는 외부 Stop을 가장하거나 operation mode를 변경하지 않습니다.

## 기본 엔드포인트

| 인터페이스 | 기본값 |
| --- | --- |
| REST | `http://127.0.0.1:5088` |
| API 브라우저 | `http://127.0.0.1:5088/scalar` |
| OpenAPI | `http://127.0.0.1:5088/openapi/v1.json` |
| TCP | `127.0.0.1:5089` |
| MQTT | `127.0.0.1:1883`, topic `virex` |

## 검증

```powershell
dotnet test Virex.NET.Integration.slnx
python -m mkdocs build --strict
```
