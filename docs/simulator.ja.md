# シミュレーターガイド

`Virex.NET.Simulator.WPF` は、統合開発のローカル エンドポイントです。 REST、TCP、MQTT、データモデル、状態、イベントの観点から見ると、本番環境と互換性のある Virex.NET サービスのように動作する必要があります。

リポジトリのルートから起動します。

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
```

## シミュレーターの目的

|目的 |何を確認するか |
| --- | --- |
|契約の確認 |ベンダーアプリケーションが本番環境と同じ REST ルート、データモデル、TCP フレーム、および MQTT トピックを使用していることを確認します。 |
|ステートマシンの検証 |コマンドの順序、有効な状態、拒否された状態、およびrunCompletedの動作を確認します。 |
|イベント検証 | TCP/MQTT コンシューマーが状態、ProductInfo、実行、結果、エラー、および拒否イベントを処理できることを確認します。 |

シミュレーターは本番検査エンジンではなく、非公開アルゴリズム、カメラの動作、レシピの内部、またはストレージの内部を公開しません。

## 標準動作

1. シミュレーターを起動します。
2. エンドポイントの設定を確認します。
3. **Start Servers** を押します。
4. サンプルまたはベンダーのクライアントを接続します。
5. システムを初期化します。
6. 製品情報を送信します。
7. 実行を開始します。
8. `runStarted`、`runCompleted`、`resultCreated` を観察します。
9. `GET /api/results` をクエリします。

## 既定のエンドポイント

|インターフェース |既定 |
| --- | --- |
| REST | `http://127.0.0.1:5088` |
| REST ブラウザ | `http://127.0.0.1:5088/scalar` |
| OpenAPI JSON | `http://127.0.0.1:5088/openapi/v1.json` |
| TCP | `127.0.0.1:5089` |
| MQTT | `127.0.0.1:1883`、ルートトピック `virex` |

## ボタンの動作

|ボタン |動作 |
| --- | --- |
| **Start Servers** | REST、TCP、および MQTT エンドポイントを開始します。システム状態を変更しません。 |
| **Initialize** |初期化コマンドを送信します。 `Uninitialized` でのみ有効です。 |
| **Deinitialize** |初期化解除コマンドを送信します。 `Ready` でのみ有効です。 |
| **Apply ProductInfo** |現在の ProductInfo を更新します。 `Ready` でのみ有効です。 |
| **Start** |実行を開始します。 `Ready` でのみ有効です。応答状態は `Running` です。 |
| **Stop** |アクティブな実行を停止します。 `Running` でのみ有効です。応答状態は `Ready` です。 |

## 観察可能な動作

|アクション |予想される外部観測 |
| --- | --- |
| Initialize | REST コマンドは `Ready` を返します。状態イベントが公開されます。 |
| ProductInfo アップデート | REST コマンドは `Ready` を返します。 ProductInfo イベントが公開されます。 |
|開始 | REST コマンドは `Running` を返します。実行開始イベントが公開されます。 |
|実行が完了しました |状態は `Ready` に戻ります。runCompletedイベントと結果作成イベントが公開されます。 |
|無効なコマンド |コマンド応答には `accepted=false` および `errorCode=invalid_state` が含まれます。拒否イベントが公開される場合があります。 |

## 推奨されるシミュレーターの受け入れプロセス

```powershell
dotnet test Virex.NET.Integration.sln
python -m mkdocs build --strict
```

次に、ローカル シミュレーターを手動で使用して、生成されたドキュメントと C# SDK サンプルを確認します。
