# 설치 / 다운로드

이 페이지는 공개 Virex.NET integration 도구를 받는 방법을 설명합니다.

- GitHub Releases에서 미리 빌드된 Windows simulator를 다운로드합니다.
- NuGet에서 C# SDK packages를 설치합니다.
- 이 repository의 sample code를 실행합니다.

## GitHub Releases: 미리 빌드된 Simulator EXE

Simulator는 repository release page에 ZIP asset으로 게시됩니다.

[최신 simulator release 다운로드](https://github.com/vstechnology-tw/vst-virex.net.integration/releases/latest)

각 ZIP에는 `Virex.NET.Simulator.WPF.exe`가 들어 있습니다. 대상 runtime에 맞는 package를 선택하세요.

| Asset | Runtime | 사용 시점 |
| --- | --- | --- |
| `Virex.NET.Simulator-vX.Y.Z-net48-win-x64.zip` | .NET Framework 4.8 | Windows system에 .NET Framework 4.8이 이미 설치된 경우. |
| `Virex.NET.Simulator-vX.Y.Z-net8.0-windows-win-x64-self-contained.zip` | .NET 8 Windows | runtime bundled Windows build를 사용하려는 경우. |
| `Virex.NET.Simulator-vX.Y.Z-net10.0-windows-win-x64-self-contained.zip` | .NET 10 Windows | 최신 지원 Windows target으로 검증해야 하는 경우. |

다운로드 후:

1. ZIP file을 압축 해제합니다.
2. `Virex.NET.Simulator.WPF.exe`를 실행합니다.
3. 테스트 환경에서 다른 port가 필요하지 않다면 default endpoints를 유지합니다.
4. SDK 또는 raw protocol samples를 실행하기 전에 **Start Servers**를 누릅니다.

Default endpoints:

| Interface | Default |
| --- | --- |
| REST | `http://127.0.0.1:5088` |
| TCP | `127.0.0.1:5089` |
| MQTT | `127.0.0.1:1883`, base topic `virex` |

Simulator 버튼 흐름은 [Simulator 사용 설명서](simulator.md)를 참고하세요.

## NuGet Packages

C# integration packages는 NuGet에 게시됩니다.

| Package | Purpose |
| --- | --- |
| [Virex.NET.Contracts](https://www.nuget.org/packages/Virex.NET.Contracts/) | Public DTOs, REST routes, MQTT topic names, TCP/NDJSON formatters, parsers. |
| [Virex.NET.Client](https://www.nuget.org/packages/Virex.NET.Client/) | REST commands/queries, TCP events, MQTT events용 C# SDK wrappers. |

.NET project에 SDK package를 설치합니다.

```powershell
dotnet add package Virex.NET.Client
```

SDK wrapper 없이 public payload types와 protocol helpers만 필요하면 shared contracts만 설치합니다.

```powershell
dotnet add package Virex.NET.Contracts
```

`Virex.NET.Client`와 `Virex.NET.Contracts`는 `netstandard2.0`을 target으로 하므로 `netstandard2.0`을 지원하는 .NET Framework 및 modern .NET applications에서 참조할 수 있습니다.

## Sample Code

Samples는 repository의 `samples/`에 있습니다.

[GitHub에서 sample code 보기](https://github.com/vstechnology-tw/vst-virex.net.integration/tree/main/samples)

처음에는 다음 sample 실행을 권장합니다.

```powershell
dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj
```

다른 samples:

| Sample | Command |
| --- | --- |
| C# raw REST | `dotnet run --project samples\csharp-raw-rest\CSharpRawRestSample.csproj` |
| C# raw TCP | `dotnet run --project samples\csharp-raw-tcp\CSharpRawTcpSample.csproj` |
| C# raw MQTT | `dotnet run --project samples\csharp-raw-mqtt\CSharpRawMqttSample.csproj` |
| Python raw REST | `python samples\python-raw-rest\main.py` |
| Python raw TCP | `python samples\python-raw-tcp\main.py` |
| Python raw MQTT | `python samples\python-raw-mqtt\main.py` |

전체 guided sample workflow는 [샘플](samples.md)을 참고하세요.

## Minimal C# SDK Example

```csharp
using Virex.NET.Client;
using Virex.NET.Contracts;

using var client = new VirexClient(new VirexClientOptions
{
    RestBaseUrl = "http://127.0.0.1:5088",
    TcpHost = "127.0.0.1",
    TcpPort = 5089,
    MqttHost = "127.0.0.1",
    MqttPort = 1883,
    MqttTopic = "virex",
});

await client.SetWaferInfoAsync(new WaferInfo
{
    LotId = "LOT-001",
    WaferId = "W01",
    RecipeId = "RCP-A",
    Slot = "1",
    FoupId = "FOUP-A",
    ChamberId = "CH-1",
});

await client.InitializeAsync();
await client.StartAsync("golden-sample", ControlRunModes.Continue);
```
