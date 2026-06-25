# Virex.NET Integration Kit

Virex.NET Integration Kit は、Virex.NET 互換サービスと連携する顧客側システム向けの公開 SDK、シミュレーター、サンプル、プロトコル文書をまとめたパッケージです。このリポジトリでは、公開通信契約とシミュレーターで検証できる動作のみを説明します。

非公開の Virex.NET アプリケーションは、このリポジトリには含まれていません。

## 既定のシミュレーター エンドポイント

| インターフェイス | 既定値 |
| --- | --- |
| REST | `http://127.0.0.1:5088` |
| TCP | `127.0.0.1:5089` |
| MQTT | `127.0.0.1:1883`、ベース トピック `virex` |
| SDK | `Virex.NET.Client` |

## 製品の目的

本番互換サービスへ接続する前に、顧客側連携を構築して検証するためにこのキットを使用します。

| 領域 | 目的 |
| --- | --- |
| `Virex.NET.Contracts` | 公開 DTO、ルート、トピック名、TCP/NDJSON フォーマッター、パーサー。 |
| `Virex.NET.Client` | REST コマンドとクエリ、TCP イベント、MQTT イベントのための C# クライアント ラッパー。 |
| `Virex.NET.Simulator.WPF` | REST、TCP、MQTT エンドポイントを公開するローカル シミュレーター。 |
| `samples` | C#、Python、C++ クライアント向けのガイド付きデモ。 |
| `docs` | 顧客向け文書と生プロトコルのリファレンス。 |

## クイック スタート

初回利用時は、シミュレーターと C# SDK サンプルを実行してください。サンプルは、**Start Servers**、**Initialize** 前に期待される `not_initialized` 応答、そして **Initialize** 後の通常サイクルを順番に案内します。

1. キットをビルドしてテストします。

   ```powershell
   dotnet test Virex.NET.Integration.slnx
   ```

2. シミュレーターを起動します。

   ```powershell
   dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
   ```

3. シミュレーター ウィンドウで既定のエンドポイントをそのまま使用し、**Start Servers** を押します。

4. 2 つ目のターミナルで SDK サンプルを実行します。

   ```powershell
   dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj
   ```

5. サンプルの案内に従います。最初に `HTTP 409 not_initialized` を確認し、その後 **Initialize** を押して、WaferInfo、サイクル開始、結果クエリへ進みます。

Event Log の想定例:

```text
WaferInfo updated from REST: lotId=LOT-RAW-REST-001, waferId=W01, recipeId=RCP-A, slot=1, foupId=FOUP-A, chamberId=CH-1
WaferInfo updated from TCP: lotId=LOT-RAW-TCP-001, waferId=W01, recipeId=RCP-A, slot=1, foupId=FOUP-A, chamberId=CH-1
```

ボタンごとの完全な手順は、[シミュレーター マニュアル](simulator.md)を参照してください。

## SDK の使い方

C# アプリケーションでは `VirexClient` から開始してください。`VirexClient` は REST、TCP、MQTT の設定を一元化し、一般的な連携操作を提供します。

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

// REST のコマンド/クエリ ヘルパーは、SDK の既定の高レベル経路です。
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

// TCP と MQTT のイベント リスナーは明示的に開始します。
using var eventCts = new CancellationTokenSource();
client.TcpEvents.EventReceived += (_, value) =>
    Console.WriteLine("TCP event: " + value.Type);
client.MqttEvents.EventReceived += (_, value) =>
    Console.WriteLine("MQTT event: " + value.Type);

_ = client.TcpEvents.RunAsync(eventCts.Token);
_ = client.MqttEvents.RunAsync(eventCts.Token);

// MQTT はイベント専用で、コマンド/制御には使用しません。
// アプリケーションの待ち受けを終了するときは eventCts.Cancel() を呼び出します。
```

主な SDK メソッド:

- `GetStatusAsync`
- `SetWaferInfoAsync`
- `InitializeAsync`
- `TerminateAsync`
- `StartAsync`
- `StopAsync`
- `QueryResultsAsync`

成功以外の REST 応答では、HTTP ステータス コードと応答本文を含む `VirexClientException` がスローされます。ガイド付きサンプルでは、**Initialize** 前の `HTTP 409 not_initialized` は想定されたシミュレーター状態であり、接続失敗ではありません。

`TcpFrameTimeoutMs` は、TCP/NDJSON の読み取り中にフレームの一部を受信した後の待ち時間を保護します。完全なフレーム同士の間に長いアイドル時間があっても問題ありませんが、フレームの最初のバイトを受信した後は、残りのバイトと終端の `\n` が設定されたタイムアウト内に到着する必要があります。

## インターフェイスの選択

| インターフェイス | 適した用途 | 方向 | 代表的な使い方 |
| --- | --- | --- | --- |
| REST | コマンドとクエリ | クライアントからサービスへ | 状態の読み取り、WaferInfo の送信、サイクル制御、結果クエリ。 |
| TCP / NDJSON | 直接ソケット連携 | 双方向 | コマンド フレームを送信し、status、wafer-info、result、error イベントを受信します。 |
| MQTT | イベント購読 | 送信イベントのみ | ブローカー経由で status、wafer-info、result、error を購読します。MQTT はコマンド/制御には使用しません。 |

正確な JSON 本文、フィールド、トピック、フレームは、[送信内容 / ペイロード](payloads.md)を参照してください。

## 代表的なユースケース

各ユースケースは、シミュレーター UI の状態とサンプル出力の両方で検証します。先に条件を確認し、指定された UI ボタンを押してから、コンソール出力と Event Log を比較してください。

| ユースケース | 必要な状態 | UI 操作 | サンプル | 想定出力 |
| --- | --- | --- | --- | --- |
| 通信サービスを開始する | シミュレーターが開いている | **Start Servers** を押す | 任意のサンプル | Event Log に REST/TCP/MQTT の待ち受けが表示され、サンプルが接続できる。 |
| `not_initialized` | **Start Servers** は押したが、**Initialize** は押していない | まだ **Initialize** を押さない | C# SDK、raw REST | Start が HTTP `409` / `not_initialized` を返す。 |
| 通常サイクル | `processState=ready` | **Initialize** を押し、その後サンプルを続行する | C# SDK、raw REST | 状態が `capturing`、`inspecting`、`saving`、`ready` の順に遷移し、結果が作成される。 |
| WaferInfo 更新の検証 | **Start Servers** が押されている | UI で **Apply WaferInfo** を押す、またはサンプルから送信する | SDK、REST、TCP | Event Log に `lotId`、`waferId`、`recipeId`、`slot`、`foupId`、`chamberId` が 1 行で表示される。 |
| MQTT イベントの確認 | MQTT サンプルが `virex/#` を購読している | **Apply WaferInfo**、**Initialize**、**Start Cycle**、**Emit Fake Result**、**Emit Error** を押す | Raw MQTT | コンソールに `wafer-info`、`status`、`result`、`error` トピックが表示される。 |
| 結果クエリ | サイクルが完了した、または **Emit Fake Result** が押されている | SDK/REST サンプルから結果クエリを実行する | SDK、REST | 一致するウェーハ コンテキストで絞り込まれた結果件数がコンソールに表示される。 |

## 主なデータ契約

| 契約 | 公開フィールド |
| --- | --- |
| WaferInfo | `lotId`、`waferId`、`recipeId`、`slot`、`foupId`、`chamberId` |
| Status | `initialized`、`processState`、`recipe` |
| Result summary | `resultId`、`timestamp`、ウェーハ コンテキスト、`overallResult`、合計 `defectCount`、結果/画像パス |
| Error | `hasError`、`message`、`initialized`、`processState`、`recipe` |

結果応答と結果イベントは要約のみを提供します。欠陥リスト、ダイ リスト、クロップ リスト、画像バイナリ、非公開の検査詳細は含まれません。

## トラブルシューティング

| 症状 | 確認事項 |
| --- | --- |
| REST リクエストが失敗する | シミュレーターが実行中であること、`RestBaseUrl` が正しいこと、ファイアウォールでアクセスが許可されていることを確認し、`VirexClientException` の応答本文を確認します。 |
| TCP イベントが届かない | ホスト/ポートを確認し、各 NDJSON フレームが改行で終わっていること、イベント ループが実行中であることを確認します。 |
| MQTT イベントが届かない | ブローカー接続、ポート `1883`、ベース トピック `virex`、購読トピック ツリーを確認します。 |
| 結果クエリが空になる | シミュレーターが結果を発行済みであり、`lotId`、`waferId`、`recipeId` フィルターが送信した WaferInfo と一致していることを確認します。 |

## 参照

- [シミュレーター マニュアル](simulator.md)
- [状態遷移](state-machine.md)
- [送信内容 / ペイロード](payloads.md)
- [サンプル](samples.md)
- [REST API](rest-api.md)
- [TCP ソケット プロトコル](tcp-socket.md)
- [MQTT イベント](mqtt-events.md)
- [プロトコル バージョニング](protocol-versioning.md)
