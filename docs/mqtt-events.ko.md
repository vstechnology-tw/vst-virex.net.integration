# MQTT 이벤트

MQTT는 Virex.NET 호환 서비스에서 송신 전용으로 사용됩니다.

**Start Servers** 를 클릭하면 시뮬레이터가 내장 MQTT 브로커를 시작합니다. 로컬 클라이언트는 기본 브로커 `127.0.0.1:1883` 을 구독할 수 있습니다. 시뮬레이터 테스트에는 외부 브로커가 필요하지 않습니다.

기본 베이스 토픽:

```text
virex
```

발행되는 토픽:

```text
virex/status
virex/wafer-info
virex/result
virex/error
```

페이로드는 REST 및 TCP 소켓 이벤트와 동일한 JSON 형태를 사용합니다. 다만 MQTT 페이로드는 하위 토픽이 이벤트를 식별하므로 `type` 필드가 필요하지 않습니다.

MQTT는 명령/제어에 사용하지 않습니다.

필드 수준 페이로드 세부 정보와 시뮬레이터 검증 절차는 [전송 내용 / 페이로드](payloads.md)를 참조하십시오.
