# Virex.NET Integration Kit

Virex.NET Integration Kit は、Virex.NET-compatible services と連携する customer systems 向けの public SDK、simulator、sample、protocol documentation package です。Public communication contracts と simulator で検証できる behavior のみを文書化します。

Private Virex.NET application はこの repository には含まれません。

## Default Simulator Endpoints

| Interface | Default |
| --- | --- |
| REST | `http://127.0.0.1:5088` |
| TCP | `127.0.0.1:5089` |
| MQTT | `127.0.0.1:1883`, base topic `virex` |
| SDK | `Virex.NET.Client` |

## Product Purpose

Production-compatible service に接続する前に、customer-side integrations を構築し検証するためにこの kit を使用します。

| Area | Purpose |
| --- | --- |
| `Virex.NET.Contracts` | Public DTOs、routes、topic names、TCP/NDJSON formatters、parsers。 |
| `Virex.NET.Client` | REST commands/queries、TCP events、MQTT events 用の C# client wrappers。 |
| `Virex.NET.Simulator.WPF` | REST、TCP、MQTT endpoints を公開する local simulator。 |
| `samples` | C#、Python、C++ clients 向け guided demos。 |
| `docs` | Customer documentation と raw protocol references。 |

## Quick Start

初回利用者は simulator と C# SDK sample を実行してください。Sample は **Start Servers**、**Initialize** 前に期待される `not_initialized` response、そして **Initialize** 後の normal cycle を案内します。

1. Kit を build/test します。

   ```powershell
   dotnet test Virex.NET.Integration.slnx
   ```

2. Simulator を起動します。

   ```powershell
   dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
   ```

3. Simulator window で default endpoints のまま **Start Servers** を押します。

4. 2 つ目の terminal で SDK sample を実行します。

   ```powershell
   dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj
   ```

5. Sample prompts に従います。最初に `HTTP 409 not_initialized` を示し、その後 **Initialize** を押すよう案内し、WaferInfo、cycle start、result query へ進みます。

Expected Event Log examples:

```text
WaferInfo updated from REST: lotId=LOT-RAW-REST-001, waferId=W01, recipeId=RCP-A, slot=1, foupId=FOUP-A, chamberId=CH-1
WaferInfo updated from TCP: lotId=LOT-RAW-TCP-001, waferId=W01, recipeId=RCP-A, slot=1, foupId=FOUP-A, chamberId=CH-1
```

Button-by-button workflow は [Simulator Manual](simulator.md) を参照してください。

## SDK Usage

C# applications は `VirexClient` から開始してください。REST、TCP、MQTT settings を集約し、common integration operations を公開します。

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
    TimeoutMs = 5000,
    TcpFrameTimeoutMs = 5000,
});

var status = await client.GetStatusAsync();

// REST command/query helpers は SDK の default high-level path です。
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

var results = await client.QueryResultsAsync(lotId: "LOT-001");

// TCP は TcpEvents から明示的に選択します。
await client.TcpEvents.SendWaferInfoAsync(new WaferInfo
{
    LotId = "LOT-TCP-001",
    WaferId = "W01",
    RecipeId = "RCP-A",
});
await client.TcpEvents.SendStartAsync();

// TCP and MQTT event listeners are started explicitly.
using var eventCts = new CancellationTokenSource();
client.TcpEvents.EventReceived += (_, value) =>
    Console.WriteLine("TCP event: " + value.Type);
client.MqttEvents.EventReceived += (_, value) =>
    Console.WriteLine("MQTT event: " + value.Type);

_ = client.TcpEvents.RunAsync(eventCts.Token);
_ = client.MqttEvents.RunAsync(eventCts.Token);

// MQTT は event-only で、command/control には使用しません。
// Listening を終了するときは eventCts.Cancel() を呼び出します。
```

Main SDK methods:

- `GetStatusAsync`
- `SetWaferInfoAsync`
- `InitializeAsync`
- `TerminateAsync`
- `StartAsync`
- `StopAsync`
- `QueryResultsAsync`

Non-success REST responses は HTTP status code と response body を含む `VirexClientException` を throw します。Guided samples では、**Initialize** 前の `HTTP 409 not_initialized` は expected simulator state であり、connection failure ではありません。

`TcpFrameTimeoutMs` は partial frame data が到着した後の TCP/NDJSON read を保護します。Complete frames の間の長い idle gap は許可されますが、client が frame の最初の byte を受信した後は、その frame の残りと `\n` が configured timeout 内に到着する必要があります。

## Interface Selection

| Interface | Best fit | Direction | Typical use |
| --- | --- | --- | --- |
| REST | Commands and queries | Client to service | Read status、send WaferInfo、control cycles、query results。 |
| TCP / NDJSON | Direct socket integration | Bidirectional | Command frames を送信し、status、wafer-info、result、error events を受信します。 |
| MQTT | Event subscription | Outbound events only | Broker 経由で status、wafer-info、result、error を subscribe します。MQTT は command/control には使用しません。 |

正確な JSON bodies、fields、topics、frames は [Transmitted Content / Payloads](payloads.md) を参照してください。

## Typical Use Cases

各 use case は simulator UI state と sample output の両方で検証します。まず condition を確認し、指定された UI button を押し、console output と Event Log を比較してください。

| Use case | Required state | UI action | Sample | Expected output |
| --- | --- | --- | --- | --- |
| Start communication services | Simulator is open | Press **Start Servers** | Any sample | Event Log shows REST/TCP/MQTT listening; samples can connect. |
| `not_initialized` | **Start Servers** pressed, **Initialize** not pressed | Do not press **Initialize** yet | C# SDK, raw REST | Start returns HTTP `409` / `not_initialized`. |
| Normal cycle | `processState=ready` | Press **Initialize**, then continue the sample | C# SDK, raw REST | State transitions through `capturing`, `inspecting`, `saving`, and `ready`; a result is created. |
| WaferInfo update verification | **Start Servers** pressed | Press **Apply WaferInfo** or send from sample | SDK, REST, TCP | Event Log lists `lotId`, `waferId`, `recipeId`, `slot`, `foupId`, and `chamberId` in one line. |
| MQTT event observation | MQTT sample subscribed to `virex/#` | Press **Apply WaferInfo**, **Initialize**, **Start Cycle**, **Emit Fake Result**, **Emit Error** | Raw MQTT | Console shows `wafer-info`, `status`, `result`, and `error` topics. |
| Result query | Cycle completed or **Emit Fake Result** pressed | Run result query from SDK/REST sample | SDK, REST | Console prints result count filtered by matching wafer context. |

## Main Data Contracts

| Contract | Public fields |
| --- | --- |
| WaferInfo | `lotId`, `waferId`, `recipeId`, `slot`, `foupId`, `chamberId` |
| Status | `initialized`, `processState`, `recipe` |
| Result summary | `resultId`, `timestamp`, wafer context, `overallResult`, total `defectCount`, result/image paths |
| Error | `hasError`, `message`, `initialized`, `processState`, `recipe` |

Result responses と result events は summaries のみを提供します。Defect lists、die lists、crop lists、image binaries、private inspection details は含みません。

## Troubleshooting

| Symptom | Check |
| --- | --- |
| REST request fails | Simulator が running であること、`RestBaseUrl` が正しいこと、firewall access が許可されていることを確認し、`VirexClientException` response body を確認します。 |
| TCP events are missing | Host/port を確認し、各 NDJSON frame が newline で終わること、event loop が継続していることを確認します。 |
| MQTT events are missing | Broker connectivity、port `1883`、base topic `virex`、subscription topic tree を確認します。 |
| Result query is empty | Simulator が result を emit 済みであり、`lotId`、`waferId`、`recipeId` filters が submitted WaferInfo と一致することを確認します。 |

## References

- [Simulator Manual](simulator.md)
- [State Machine](state-machine.md)
- [Transmitted Content / Payloads](payloads.md)
- [Samples](samples.md)
- [REST API](rest-api.md)
- [TCP Socket Protocol](tcp-socket.md)
- [MQTT Events](mqtt-events.md)
- [Protocol Versioning](protocol-versioning.md)
