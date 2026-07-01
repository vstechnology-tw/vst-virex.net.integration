# Virex.NET 統合スイート

Virex.NET Integration Kit は、外部システムを Virex.NET 互換サービスと統合するために必要な公開契約、シミュレーター、サンプル、ドキュメントを提供します。このリポジトリには、外部から見える統合契約のみが含まれ、製品内部の非公開実装詳細は含まれません。

シミュレーターと本番 Virex.NET エンドポイントは、同じ `Virex.NET.Contracts` を実装する必要があります。外部ベンダーが `Virex.NET.Simulator.WPF` に対する統合を完了した場合、本番エンドポイントへの切り替えは通常、エンドポイント設定の変更だけで済みます。

## 通信インターフェース

Virex.NET は、同じビジネス統合機能を 3 種類の通信インターフェースで提供します。顧客は 1 つのインターフェースを主要な統合方式として選択し、そのプロトコル文書に従って同じ commands、events、payload の動作を実装してください。

| インターフェース | 既定のシミュレーターエンドポイント | 適している用途 | 参照 |
| --- | --- | --- | --- |
| RESTful API | `http://127.0.0.1:5088` | HTTP request/response を使用し、状態や結果を polling できるシステム。 | [RESTful API](rest-api.ja.md) |
| TCP Socket | `127.0.0.1:5089` | 永続 socket 上で双方向 NDJSON を扱うシステム。 | [TCP Socket Protocol](tcp-socket.ja.md) |
| MQTT | `127.0.0.1:1883`、root topic `virex` | broker ベースの command、response、event メッセージングを使用するシステム。 | [MQTT Protocol](mqtt-events.ja.md) |

## 共通ビジネス機能

RESTful API、TCP Socket、MQTT は同じ公開ビジネス機能を提供します。transport 名は異なりますが、ライフサイクル規則、状態制約、command response、event payload、result payload は揃っています。明確な運用上の理由がない限り、顧客は 1 つの通信インターフェースを選び、その中でフロー全体を実装してください。

### Commands と queries

| ビジネス機能 | RESTful API | TCP Socket | MQTT |
| --- | --- | --- | --- |
| Query status | `GET /api/status` | `{"type":"status"}` | `virex/commands/status/get` |
| Query error | `GET /api/error` | `{"type":"error"}` | `virex/commands/error/get` |
| Query ProductInfo | `GET /api/product-info` | `{"type":"getProductInfo"}` | `virex/commands/product-info/get` |
| Initialize | `POST /api/system/initialize` | `{"type":"initialize"}` | `virex/commands/system/initialize` |
| Set ProductInfo | `POST /api/product-info` | `{"type":"productInfo", ...}` | `virex/commands/product-info/set` |
| Start run | `POST /api/system/start` | `{"type":"start", ...}` | `virex/commands/system/start` |
| Stop run | `POST /api/system/stop` | `{"type":"stop", ...}` | `virex/commands/system/stop` |
| Query results | `GET /api/results` | `{"type":"results", ...}` | `virex/commands/results/query` |
| Deinitialize | `POST /api/system/deinitialize` | `{"type":"deinitialize"}` | `virex/commands/system/deinitialize` |

### Events

| Event | 意味 |
| --- | --- |
| `statusChanged` | 公開ライフサイクル状態が変化した。 |
| `productInfoChanged` | ProductInfo 更新が完了した。 |
| `runStarted` | run が `Running` に入った。 |
| `runCompleted` | run が `Ready` に戻った。 |
| `resultCreated` | 公開 result summary が作成された。 |
| `errorChanged` | 公開エラー情報が変化した。 |
| `commandRejected` | command が状態規則または検証で拒否された。 |

RESTful API は commands を HTTP route として表現し、event stream は提供しません。client は polling によって状態と結果を観察できます。TCP Socket と MQTT は同じ events を push message として提供します。

## 基本パケット形式

3 つの通信インターフェースはすべて UTF-8 JSON payload を使用し、同じ公開データモデルを共有します。まず次の参照を確認してください。

| 形式領域 | 参照 |
| --- | --- |
| JSON model rules と共通 payload | [Payload Reference](payloads.ja.md) |
| RESTful API route、HTTP body、response code | [RESTful API](rest-api.ja.md) |
| TCP NDJSON frame format と frame type | [TCP Socket Protocol](tcp-socket.ja.md) |
| MQTT topic、command request envelope、correlated response | [MQTT Protocol](mqtt-events.ja.md) |

## プロジェクトの目的

| エリア | 目的 |
| --- | --- |
| `Virex.NET.Contracts` | 公開データモデル、RESTful API ルート、MQTT topic 名、TCP/NDJSON フォーマットおよび解析ツール。 |
| `Virex.NET.Client` | RESTful API commands/queries、TCP socket communication、MQTT command/event communication の C# SDK ラッパー。 |
| `Virex.NET.Simulator.Core` | ローカルシミュレーターで使用されるシミュレーター固有のステートマシンとセッション動作。 |
| `Virex.NET.Simulator.WPF` | 公開 RESTful API/TCP/MQTT コントラクトを提供するローカルシミュレーター。 |
| `samples` | C#、Python、C++ の統合例。 |
| `docs` | 公開プロトコルとシミュレーターのドキュメント。 |

## クイックスタート

1. ビルドとテスト:

   ```powershell
   dotnet test Virex.NET.Integration.slnx
   ```

2. シミュレーターを起動します。

   ```powershell
   dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
   ```

3. シミュレーターで **Start Servers** を押して、RESTful API/TCP/MQTT サービスを開始します。

4. 別の端末を開いて SDK の例を実行します。

   ```powershell
   dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj
   ```

サンプルは同じ 13-step demo flow に従います: query status、query error、query ProductInfo、initialize、confirm Ready、set ProductInfo、confirm ProductInfo、start run、observe run activity、stop run、query results、deinitialize、confirm Uninitialized。

## 主な公開データモデル

| データモデル | 使い方 |
| --- | --- |
| [ProductInfo](payloads/product/product-info.ja.md) | 実行とその結果に関連付けられた製品情報。 |
| [SystemStatus](payloads/system/system-status.ja.md) | 現在のライフサイクル状態。 |
| [CommandResponse](payloads/commands/command-response.ja.md) | コマンドの受け入れまたは拒否の結果。 |
| [ResultSummary](payloads/results/result-summary.ja.md) | 完了した実行の公開サマリー。 |
| [ErrorInfo](payloads/system/error-info.ja.md) | 現在アクティブなエラー情報。 |

これらは JSON データモデルです。ベンダーは `Virex.NET.Contracts` の C# 型を使用することも、独自の言語で同等のモデルを定義することもできます。

## 参考資料

- [インストール/ダウンロード](installation.ja.md)
- [シミュレーターガイド](simulator.ja.md)
- [システムステートマシン](state-machine.ja.md)
- [Payload Reference](payloads.ja.md)
- [RESTful API](rest-api.ja.md)
- [TCP Socket Protocol](tcp-socket.ja.md)
- [MQTT Protocol](mqtt-events.ja.md)
- [サンプル](samples.ja.md)
