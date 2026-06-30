# 설치/다운로드

이 페이지에서는 공개 Virex.NET 통합 도구를 얻는 방법을 설명합니다.

- GitHub 릴리스에서 Windows 시뮬레이터를 다운로드합니다.
- NuGet에서 C# SDK 패키지를 설치합니다.
- 인증 절차의 다음 단계를 선택하세요.

## GitHub Releases: 사전 구축된 시뮬레이터 EXE

현재 시뮬레이터 릴리스는 `v2.0.3.1`입니다.

[Virex.NET Simulator v2.0.3.1 다운로드](https://github.com/vstechnology-tw/vst-virex.net.integration/releases/tag/v2.0.3.1)

각 우편번호에는 `Virex.NET.Simulator.WPF.exe`가 포함되어 있습니다. 대상 환경에 따라 패키지를 선택하십시오:

| 파일 | 런타임 | 사용 시기 |
| --- | --- | --- |
| `Virex.NET.Simulator-v2.0.3.1-net48-win-x64.zip` | .NET Framework 4.8 | 대상 Windows 시스템에 이미 .NET Framework 4.8이 설치된 경우에 사용합니다. |
| `Virex.NET.Simulator-v2.0.3.1-net8.0-windows-win-x64-self-contained.zip` | .NET 8 윈도우 | 런타임을 포함하는 현재 Windows 빌드가 필요할 때 사용합니다. |
| `Virex.NET.Simulator-v2.0.3.1-net10.0-windows-win-x64-self-contained.zip` | .NET 10 윈도우 | 지원되는 최신 Windows 대상의 유효성을 검사해야 할 때 사용합니다. |

다운로드 후:

1. ZIP 압축을 푼다.
2. `Virex.NET.Simulator.WPF.exe`를 실행합니다.
3. 테스트 환경에 다른 포트가 필요한 경우가 아니면 기본 엔드포인트를 유지합니다.
4. SDK 또는 원래 프로토콜 예제를 실행하기 전에 **Start Servers**를 누르십시오.

시뮬레이터 버튼, 기본 끝점 및 작동 절차는 [시뮬레이터 가이드](simulator.ko.md)를 참조하세요.

## NuGet 패키지

| 패키지 | 목적 |
| --- | --- |
| [Virex.NET.Contracts](https://www.nuget.org/packages/Virex.NET.Contracts/) | 공개 C# 데이터 모델, REST 경로, MQTT 토픽 이름, TCP/NDJSON 형식 지정 및 구문 분석 도구를 제공합니다. |
| [Virex.NET.Client](https://www.nuget.org/packages/Virex.NET.Client/) | REST, TCP 및 MQTT용 C# SDK 래퍼입니다. |

SDK를 설치합니다.

```powershell
dotnet add package Virex.NET.Client --version 2.0.3
```

공유 계약 패키지만 설치하십시오.

```powershell
dotnet add package Virex.NET.Contracts --version 2.0.3
```

`Virex.NET.Client` 및 `Virex.NET.Contracts`는 `netstandard2.0`를 대상으로 합니다.

## 다음 단계

- 첫 번째 통합 확인 흐름: [빠른 시작](quick-start.ko.md)를 참조하세요.
- C# SDK 사용법을 완료하세요. [C# SDK 가이드](csharp-sdk.ko.md)를 참조하세요.
- 다른 언어 또는 원시 프로토콜 예: [샘플](samples.ko.md)를 참조하세요.
