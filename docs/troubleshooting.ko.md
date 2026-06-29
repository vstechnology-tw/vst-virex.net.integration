# 문제 해결

## REST 요청 실패

확인하다:

- 시뮬레이터 또는 운영 엔드포인트가 실행 중입니다.
- REST 기본 URL이 정확합니다.
- 방화벽은 구성된 포트를 허용합니다.
- 경로는 [REST API](rest-api.ko.md)에 나열되어 있습니다.

## 명령은 `invalid_state`를 반환합니다.

명령이 서비스에 도달했지만 현재 상태에서는 유효하지 않습니다.

예:

- `Start`는 `Ready`에서만 유효합니다.
- `Stop`는 `Running`에서만 유효합니다.
- `SetProductInfo`는 `Ready`에서만 유효합니다.
- `Initialize`는 `Uninitialized`에서만 유효합니다.
- `Deinitialize`는 `Ready`에서만 유효합니다.

먼저 `GET /api/status`를 읽은 후 상태가 허용할 때 명령을 보냅니다.

## 결과가 반환되지 않았습니다.

확인하다:

- 실행이 시작되고 완료되었습니다.
- 결과 쿼리 필터는 `Start`가 수락되었을 때 캡처된 ProductInfo 스냅샷과 일치합니다.
- 필터는 `waferID`, `lotID` 또는 `recipe`를 사용합니다.

## TCP 이벤트 누락

확인하다:

- TCP 호스트/포트가 정확합니다.
- 클라이언트는 소켓을 열린 상태로 유지합니다.
- 각 수신 프레임은 `\n`로 끝납니다.
- 클라이언트는 `statusChanged` 및 `resultCreated`와 같은 문서화된 이벤트 이름을 구문 분석합니다.

## MQTT 이벤트 누락

확인하다:

- 브로커 호스트/포트가 정확합니다.
- 구독이 `virex/#`와 같은 토픽 접두사와 일치합니다.
- MQTT는 송신 이벤트에만 사용됩니다.
- 클라이언트는 `productInfoChanged`와 같은 문서화된 토픽 이름을 수신합니다.

## 로컬 미리보기는 GitHub Pages와 다릅니다.

로컬에서 MkDocs를 사용하세요.

```powershell
python -m mkdocs serve --dev-addr 127.0.0.1:8000
```

GitHub Pages 워크플로우는 다음을 수행합니다.

```powershell
python -m mkdocs build --strict
```
