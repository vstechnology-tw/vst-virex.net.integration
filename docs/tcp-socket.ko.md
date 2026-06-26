# TCP 소켓 프로토콜

TCP는 양방향 명령 및 이벤트 트래픽이 필요한 직접 소켓 연동에 사용합니다.

TCP는 단일 포트와 NDJSON 프레이밍을 사용합니다.

```text
한 줄에 하나의 JSON 객체
각 프레임은 \n 으로 끝남
UTF-8 인코딩
```

C# SDK는 TCP/NDJSON을 읽을 때 프레임별 유휴 제한 시간을 적용합니다. 완전한 프레임 사이의 긴 공백은 유효합니다. 그러나 프레임의 어떤 바이트든 도착한 뒤에는 나머지 바이트와 종료 개행 문자가 `VirexClientOptions.TcpFrameTimeoutMs` 안에 도착해야 하며, 그렇지 않으면 TCP 이벤트 리더가 시간 초과로 실패합니다.

장비 또는 클라이언트는 Virex.NET 호환 서비스에 연결합니다. 같은 연결에서 수신 메시지를 보내고 송신 이벤트를 받을 수 있습니다.

필드 수준 세부 정보와 공통 JSON 본문 형태는 [전송 내용 / 페이로드](payloads.md)를 참조하십시오.

## 수신

```json
{"type":"waferInfo","lotId":"LOT-001","waferId":"W01","recipeId":"RCP-A","slot":"1","foupId":"FOUP-A","chamberId":"CH-1"}
```

```json
{"type":"start","condition":"golden-sample","runMode":"continue"}
```

```json
{"type":"stop","reason":"operator-request"}
```

기존 WaferInfo 프레임에서는 `type` 필드를 생략할 수 있습니다. start/stop 에는 `type` 이 필요합니다. Start `condition`, start `runMode`, and stop `reason` are optional. Start `runMode` defaults to `continue`; `single` is also supported. Legacy `{"type":"start"}` and `{"type":"stop"}` remain valid.

## 송신

```json
{"type":"status","initialized":true,"processState":"ready","recipe":"Default"}
```

```json
{"type":"waferInfo","lotId":"LOT-001","waferId":"W01","recipeId":"RCP-A","slot":"1","foupId":"FOUP-A","chamberId":"CH-1"}
```

```json
{"type":"result","resultId":"RID-1","timestamp":"2026-06-20T15:30:12+08:00","lotId":"LOT-001","waferId":"W01","recipeId":"RCP-A","slot":"1","foupId":"FOUP-A","chamberId":"CH-1","overallResult":"OK","defectCount":0,"imageRelativePath":"20260620/LOT-001/20260620_153012_W01.tiff","resultRelativePath":"20260620/LOT-001/20260620_153012_W01.json","imagePath":"/data/virex-results/20260620/LOT-001/20260620_153012_W01.tiff","previewImagePath":"/data/virex-results/20260620/LOT-001/20260620_153012_W01.jpg","resultPath":"/data/virex-results/20260620/LOT-001/20260620_153012_W01.json"}
```

```json
{"type":"error","message":"Recipe load failed.","initialized":true,"processState":"ready","recipe":"Default","timestamp":"2026-06-20T00:00:00+08:00"}
```

result 이벤트는 요약만 포함하며 결함 목록이나 바이너리는 포함하지 않습니다.
