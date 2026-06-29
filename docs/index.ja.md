# Virex.NET 統合スイート

Virex.NET Integration Kit は、外部システムを Virex.NET 互換サービスと統合するために必要な公開契約、C# SDK、シミュレーター、サンプル、およびドキュメントを提供します。このリポジトリには、Virex.NET 製品の非公開実装の詳細は含まれていません。

シミュレーターと本番 Virex.NET エンドポイントは、同じ `Virex.NET.Contracts` を実装する必要があります。外部ベンダーが `Virex.NET.Simulator.WPF` に対する統合を完了した場合、本番エンドポイントに切り替えるには、REST/TCP/MQTT エンドポイント設定を変更するだけで済みます。

## プロジェクトの目的

|エリア |目的 |
| --- | --- |
| `Virex.NET.Contracts` |公開 データモデル、REST ルート、MQTT トピック名、TCP/NDJSON フォーマットおよび解析ツール。 |
| `Virex.NET.Client` | C# REST コマンド/クエリ、TCP イベント、および MQTT イベントの SDK ラッパー。 |
| `Virex.NET.Simulator.Core` |ローカル シミュレーターによって使用されるシミュレーター固有のステート マシンとセッションの動作。 |
| `Virex.NET.Simulator.WPF` |公開 REST/TCP/MQTT コントラクトを提供するローカル シミュレーター。 |
| `samples` | C#、Python、C++ の統合例。 |
| `docs` |公開プロトコルとシミュレーターのドキュメント。 |

## 既定のシミュレーターエンドポイント

|インターフェース |既定 |
| --- | --- |
| REST | `http://127.0.0.1:5088` |
| TCP | `127.0.0.1:5089` |
| MQTT | `127.0.0.1:1883`、トピックプレフィックス `virex` |

## クイックスタート

1. ビルドとテスト:

   ```powershell
   dotnet test Virex.NET.Integration.sln
   ```

2. シミュレーターを起動します。

   ```powershell
   dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
   ```

3. シミュレーターで **Start Servers** を押して、REST/TCP/MQTT サービスを開始します。

4. 別の端末を開いて、SDK の例を実行します。

   ```powershell
   dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj
   ```

この例では、システムを初期化し、ProductInfo を更新し、実行を開始し、実行が完了するのを待機して、結果をクエリします。

## 主な SDK メソッド

- `GetStatusAsync`
- `GetProductInfoAsync`
- `SetProductInfoAsync`
- `InitializeAsync`
- `DeinitializeAsync`
- `StartAsync`
- `StopAsync`
- `QueryResultsAsync`

## 主な公開データモデル

|データモデル |使い方 |
| --- | --- |
| [ProductInfo](payloads/product/product-info.ja.md) |実行とその結果に関連付けられた製品情報。 |
| [SystemStatus](payloads/system/system-status.ja.md) |現在のライフサイクル状態。 |
| [CommandResponse](payloads/commands/command-response.ja.md) |コマンドの受け入れまたは拒否の結果。 |
| [ResultSummary](payloads/results/result-summary.ja.md) |完了した実行の公開サマリー。 |
| [ErrorInfo](payloads/system/error-info.ja.md) |現在アクティブなエラー情報。 |

これらは JSON データモデルです。ベンダーは、`Virex.NET.Contracts` で C# タイプを使用することも、独自の言語で同等のモデルを定義することもできます。

## 参考資料

- [インストール/ダウンロード](installation.ja.md)
- [シミュレーターガイド](simulator.ja.md)
- [システムステートマシン](state-machine.ja.md)
- [ペイロードリファレンス](payloads.ja.md)
- [REST API](rest-api.ja.md)
- [TCP ソケット プロトコル](tcp-socket.ja.md)
- [MQTT イベント](mqtt-events.ja.md)
- [サンプル](samples.ja.md)
