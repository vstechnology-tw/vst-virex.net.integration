# C# SDK ガイド

2.4.0 開発版の `GetSoftwareVersionAsync()`（`GET /api/software-version`）は REST のみを使用し、対象 App の実際のバージョンとビルド接尾辞を読み取ります。API／応答形式の失敗は `VirexQueryException`、呼び出し元のキャンセルは維持されます。Simulator は実行中の server アセンブリのバージョンを返します。

現在の 2.4.0 ソースは未公開の候補です。[候補の検証（英語）](candidate-acceptance.md) と `samples/csharp-sdk-24` は REST、完全一致 TCP イベント、明示的 ResultId、Alarm と二段階復旧を使用します。旧 SDK／wire の互換性は 2.4.0 の必須検証ではありません。

バージョン 2.3.0 では VirexClient と VirexRestClient に `GetRecipesAsync`、`GetCurrentRecipeAsync`、`GetCurrentRecipeParametersAsync`、`GetResultDetailAsync` を追加します。[DTO とエラーの契約（英語）](read-only-queries.md)を参照してください。


`Virex.NET.Client` は、公開 RESTful API、TCP、および MQTT 統合 API の型付きラッパーを提供します。

## 完全なサンプル

すべての `using` と project reference を含む実行可能な C# プログラムは[C# SDK サンプル](samples.ja.md)を使用してください。以下のコードブロックは完全なファイルから抜粋した操作例です。

## インストール

```powershell
dotnet add package Virex.NET.Client
```

公開 データモデルとプロトコル ヘルパーのみが必要な場合:

```powershell
dotnet add package Virex.NET.Contracts
```

## クライアントの作成

```csharp
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Virex.NET.Contracts;
using Virex.NET.Client;

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
```

## RESTful API コマンド/クエリの流れ

```csharp
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
var status = await client.GetStatusAsync();

var initialize = await client.InitializeAsync();
if (!initialize.Accepted)
    throw new InvalidOperationException(initialize.Message);

await client.SetProductInfoAsync(new ProductInfo
{
    WaferID = "W01",
    LotID = "LOT-001",
    Recipe = "RCP-A",
    Slot = "1",
    FoupID = "FOUP-A",
    ChamberID = "CH-1",
});

var start = await client.StartAsync("golden-sample", ControlRunModes.Continue);
Console.WriteLine(start.State); // Running

var results = await client.QueryResultsAsync(lotID: "LOT-001");
```

## 復旧と Deinitialize の再試行

2.4.0 の分類済み障害は公開状態 `Faulted` と `recoveryAction: "Reset"` を返します。Reset は停止と必要なクリーンアップを完了し、成功すると `Uninitialized` になります。`cleanup_failed` は `Faulted` を維持します。その後の明示的 Initialize だけが `Ready` に戻し、旧作業は再開しません。実際の state、errorCode と復旧情報を確認してください。`RequiresDeinitialize` は公開状態ではありません。

```csharp
var status = await client.GetStatusAsync();
if (status.RecoveryAction == RecoveryActions.Reset)
{
    var cleanup = await client.ResetAsync();
    if (cleanup.Accepted) await client.InitializeAsync();
    Console.WriteLine($"Recovery: accepted={cleanup.Accepted}, phase={cleanup.RecoveryPhase}, details={cleanup.RecoveryDetails}");
}
```

2.4.0 の分類済み障害は公開状態 `Faulted` と `recoveryAction: "Reset"` を返します。Reset は停止と必要なクリーンアップを完了し、成功すると `Uninitialized` になります。`cleanup_failed` は `Faulted` を維持します。その後の明示的 Initialize だけが `Ready` に戻し、旧作業は再開しません。実際の state、errorCode と復旧情報を確認してください。`RequiresDeinitialize` は公開状態ではありません。

## TCP イベント

```csharp
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
client.TcpEvents.EventReceived += (_, value) =>
{
    Console.WriteLine($"TCP {value.Type}");
};

using var cts = new CancellationTokenSource();
var tcpTask = client.TcpEvents.RunAsync(cts.Token);

await client.TcpEvents.SendProductInfoAsync(new ProductInfo
{
    WaferID = "W01",
    LotID = "LOT-TCP-001",
    Recipe = "RCP-A",
});

await client.TcpEvents.SendStartAsync("tcp-check", ControlRunModes.Continue);
```

`value.Type` が `imageGrabbed` の場合は `value.ImageGrabbed` を読み取ります。`captureId` は後続の `resultCreated` と一致し、artifact のパスはそこで取得できます。

## MQTT イベント

```csharp
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
client.MqttEvents.EventReceived += (_, value) =>
{
    Console.WriteLine($"MQTT {value.Type}");
};

using var cts = new CancellationTokenSource();
await client.MqttEvents.RunAsync(cts.Token);
```

MQTT はイベントにのみ使用されます。コマンドにはRESTまたはTCPを使用してください。

`value.Type` が `imageGrabbed` の場合は `value.ImageGrabbed` を読み取ります。`captureId` は後続の `resultCreated` と一致し、artifact のパスはそこで取得できます。

## エラー処理

RESTful API 通信の失敗と成功しない HTTP 応答は `VirexClientException` をスローします。プロトコルレベルのコマンド拒否は、`CommandResponse` で表されます。

```csharp
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
var response = await client.StartAsync();
if (!response.Accepted && response.ErrorCode == CommandErrorCodes.InvalidState)
{
    Console.WriteLine($"Start rejected in state {response.State}");
}
```

`invalid_state` は通常のコマンド検証動作であり、トランスポートの失敗ではありません。
