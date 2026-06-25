# インストール / ダウンロード

このページでは、公開されている Virex.NET integration tools の入手方法を説明します。

- GitHub Releases からビルド済み Windows simulator をダウンロードする。
- NuGet から C# SDK packages をインストールする。
- この repository の sample code を実行する。

## GitHub Releases: ビルド済み Simulator EXE

Simulator は repository の release page で ZIP asset として公開されます。

[最新 simulator release をダウンロード](https://github.com/vstechnology-tw/vst-virex.net.integration/releases/latest)

各 ZIP には `Virex.NET.Simulator.WPF.exe` が含まれます。対象 runtime に合わせて選択してください。

| Asset | Runtime | 使用タイミング |
| --- | --- | --- |
| `Virex.NET.Simulator-vX.Y.Z-net48-win-x64.zip` | .NET Framework 4.8 | Windows machine に .NET Framework 4.8 がインストール済みの場合。 |
| `Virex.NET.Simulator-vX.Y.Z-net8.0-windows-win-x64-self-contained.zip` | .NET 8 Windows | runtime bundled の Windows build を使う場合。 |
| `Virex.NET.Simulator-vX.Y.Z-net10.0-windows-win-x64-self-contained.zip` | .NET 10 Windows | 最新サポート Windows target で検証する場合。 |

ダウンロード後:

1. ZIP を展開します。
2. `Virex.NET.Simulator.WPF.exe` を実行します。
3. テスト環境で別 port が必要な場合を除き、default endpoints のままにします。
4. SDK または raw protocol samples を実行する前に **Start Servers** を押します。

Default endpoints:

| Interface | Default |
| --- | --- |
| REST | `http://127.0.0.1:5088` |
| TCP | `127.0.0.1:5089` |
| MQTT | `127.0.0.1:1883`, base topic `virex` |

Simulator のボタン操作については [Simulator 操作マニュアル](simulator.md) を参照してください。

## NuGet Packages

C# integration packages は NuGet に公開されます。

| Package | Purpose |
| --- | --- |
| [Virex.NET.Contracts](https://www.nuget.org/packages/Virex.NET.Contracts/) | Public DTOs、REST routes、MQTT topic names、TCP/NDJSON formatters、parsers。 |
| [Virex.NET.Client](https://www.nuget.org/packages/Virex.NET.Client/) | REST commands/queries、TCP events、MQTT events 用 C# SDK wrappers。 |

.NET project に SDK package を追加します。

```powershell
dotnet add package Virex.NET.Client
```

SDK wrapper が不要で、公開 payload types と protocol helpers のみ必要な場合:

```powershell
dotnet add package Virex.NET.Contracts
```

`Virex.NET.Client` と `Virex.NET.Contracts` は `netstandard2.0` を target にしているため、`netstandard2.0` 対応の .NET Framework および modern .NET applications から参照できます。

## Sample Code

Samples は repository の `samples/` にあります。

[GitHub で sample code を見る](https://github.com/vstechnology-tw/vst-virex.net.integration/tree/main/samples)

最初は次の sample を推奨します。

```powershell
dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj
```

その他の samples:

| Sample | Command |
| --- | --- |
| C# raw REST | `dotnet run --project samples\csharp-raw-rest\CSharpRawRestSample.csproj` |
| C# raw TCP | `dotnet run --project samples\csharp-raw-tcp\CSharpRawTcpSample.csproj` |
| C# raw MQTT | `dotnet run --project samples\csharp-raw-mqtt\CSharpRawMqttSample.csproj` |
| Python raw REST | `python samples\python-raw-rest\main.py` |
| Python raw TCP | `python samples\python-raw-tcp\main.py` |
| Python raw MQTT | `python samples\python-raw-mqtt\main.py` |

Guided sample workflow 全体は [サンプル](samples.md) を参照してください。

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
await client.StartAsync();
```
