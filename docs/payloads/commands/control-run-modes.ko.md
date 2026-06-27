# ControlRunModes

`ControlRunModes`는 [SystemStartRequest](system-start-request.ko.md)에 대해 허용되는 `runMode` 값을 정의합니다.

## 값

| 값 | 의미 |
| --- | --- |
| `continue` | 기본 실행 모드. |
| `single` | 단일 실행 모드. |

생략되거나 공백인 `runMode`는 `continue`로 정규화됩니다.

## 사용 위치

| 인터페이스 | 사용법 |
| --- | --- |
| REST | `POST /api/system/start` 요청 내용입니다. |
| TCP | 수신 `type: "start"` 명령 프레임. |
