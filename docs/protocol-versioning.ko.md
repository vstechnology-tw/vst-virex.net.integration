# 버전

공개 패키지 및 프로토콜 변경에 시맨틱 버전 관리를 사용합니다.

| 버전 | 의미 |
| --- | --- |
| `1.0.0` | 최초 공개 통합 계약. |
| `1.1.0` | 추가 필드, 엔드포인트, 토픽 또는 이벤트. |
| `2.0.0` | 호환성이 깨지는 데이터 내용, 경로, 토픽 또는 동작 변경. |

## 버전 규칙

- 마이너 버전은 공개 JSON 필드를 제거하거나 이름을 바꾸지 않습니다.
- 마이너 버전에는 추가 필드가 추가될 수 있습니다.
- 호환성이 깨지는 변경에는 메이저 버전이 필요합니다.
- 시뮬레이터, SDK, 문서, 계약 테스트를 동기화해야 합니다.
- [페이로드 참조](payloads.ko.md)는 공개 페이로드 모델, 경로, 토픽 및 이벤트 구조와 정렬되어야 합니다.

## 공개 릴리스 노트

다음은 고객에게 공개된 버전의 목록입니다. 각 제목은 해당 GitHub Release로 연결됩니다.

### [v2.2.2](https://github.com/vstechnology-tw/vst-virex.net.integration/releases/tag/v2.2.2) — 2026-08-25

- 공개 버전이 2.1.1에 머문 뒤, 미공개 2.2.1 후보에서 준비한 복구 기능을 2.2.2로 게시합니다.
- `statusChanged`, `errorChanged`, `commandRejected`에 재시도 가능한 Deinitialize 안내, 안정적인 오류 코드, 시각, 원본, 단계 및 정리된 세부 정보를 포함하는 공통 추가형 복구 엔벌로프를 추가합니다.
- 동시에 발생하는 시뮬레이터 복구 전환을 직렬화하고 `imageGrabbed`, `captureId`, 영구 저장된 시뮬레이터 산출물을 유지합니다.
- 현재 시뮬레이터 ZIP, 4개 언어 문서 PDF 및 Mermaid 렌더링 수정 사항을 포함합니다.
### [v2.1.1](https://github.com/vstechnology-tw/vst-virex.net.integration/releases/tag/v2.1.1) — 2026-08-17

- 이벤트 모델, TCP, MQTT, C# SDK 및 시뮬레이터에 공개 `imageGrabbed` 이벤트를 추가했습니다.
- `imageGrabbed`와 이후 `resultCreated`를 연결하는 안정적인 `captureId`를 추가했습니다.
- 시뮬레이터는 `resultCreated`의 경로를 통해 테스트 이미지, 미리 보기 및 결과 파일을 제공합니다.
- 시뮬레이터 진단 정보, 전송 샘플 및 고객 문서를 업데이트했습니다.

### [v2.1.0](https://github.com/vstechnology-tw/vst-virex.net.integration/releases/tag/v2.1.0) — 2026-07-01

- RESTful API, TCP Socket 및 MQTT의 명령과 쿼리 대응을 완료했습니다.
- 상태, 오류, ProductInfo, 수명 주기 명령 및 결과 쿼리에 대한 MQTT 명령/응답을 추가했습니다.
- 상태, 오류, ProductInfo 및 결과 쿼리에 대한 TCP 프레임을 추가했습니다.
- C#, Python 및 C++ 샘플을 동일한 13단계 통합 흐름으로 맞췄습니다.
- Contracts 및 Client 패키지와 .NET Framework 4.8, .NET 8, .NET 10 시뮬레이터 다운로드를 공개했습니다.

### [v2.0.3.1](https://github.com/vstechnology-tw/vst-virex.net.integration/releases/tag/v2.0.3.1) — 2026-06-30

- 시뮬레이터 전용 릴리스입니다.
- TCP initialize 및 deinitialize 명령을 문서화하고 구현했습니다.
- 시뮬레이터 결과 이미지와 공개 문서를 `.tiff`에서 `.bmp`로 업데이트했습니다.
- 설치 문서와 OpenAPI 메타데이터를 업데이트했습니다.

### [v2.0.3](https://github.com/vstechnology-tw/vst-virex.net.integration/releases/tag/v2.0.3) — 2026-06-28

- 최신 main에서 패키지와 시뮬레이터를 다시 게시했습니다.
- 게시된 내용에 ProductInfo 계약 업데이트를 포함했습니다.

### [v2.0.2](https://github.com/vstechnology-tw/vst-virex.net.integration/releases/tag/v2.0.2) — 2026-06-27

- 공개 `WaferInfo` 계약을 `ProductInfo`로 변경했습니다.
- ProductInfo 기반 수명 주기 동작과 시뮬레이터 지원을 추가했습니다.
- RESTful API, TCP, MQTT, 샘플 및 다국어 문서를 업데이트했습니다.

### [v2.0.1](https://github.com/vstechnology-tw/vst-virex.net.integration/releases/tag/v2.0.1) — 2026-06-23

- 초기 2.0 공개 통합 계약과 시뮬레이터 배포 패키지를 게시했습니다.
- 문서를 MkDocs Material로 이전하고 영어, 번체 중국어, 일본어 및 한국어 버전을 제공했습니다.
- Result Summary와 REST 결과 쿼리 응답의 차이를 명확히 했습니다.
- 결과 경로와 payload 필드, MQTT 기본값, OpenAPI/Scalar 검증 및 TCP 프레임 동작을 업데이트했습니다.

## GitHub Release 페이지가 없는 과거 태그

`v1.0.0` 및 `v1.0.1`은 repository tag이지만 GitHub에 공개 Release 페이지나 release note 기록이 없습니다. 최초의 공개 GitHub Release는 `v2.0.1`입니다.