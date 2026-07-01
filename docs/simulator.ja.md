# シミュレーターガイド

`Virex.NET.Simulator.WPF` は、統合開発のローカル エンドポイントです。 RESTful API、TCP、MQTT、データモデル、状態、イベントの観点から見ると、本番環境と互換性のある Virex.NET サービスのように動作する必要があります。

リポジトリのルートから起動します。

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
```

シミュレーターを起動すると、次の WPF ウィンドウが表示されます。

![番号付きのシミュレーター メイン ウィンドウ](assets/simulator-main-window-annotated.png)

## シミュレーター ウィンドウの見方

| 領域 | 画面領域 | 用途 |
| --- | --- | --- |
| 1 | **Connection Settings** | シミュレーターが公開するエンドポイントを設定します。**RESTful API prefix** は HTTP のベース アドレス、**TCP port** は NDJSON socket の待ち受けポート、**MQTT host** と **MQTT port/topic** は組み込み MQTT broker とトピック プレフィックス、**Result prefix** は結果 ID または結果パスにテスト用プレフィックスが必要な場合だけ使用します。 |
| 2 | **ProductInfo** | 模擬システムへ送る製品コンテキストを設定します。Lot ID、Wafer ID、Recipe、Slot、Foup ID、Chamber ID を入力し、システムが `Ready` になってから **Apply ProductInfo** を押します。 |
| 3 | **State** | 現在のシミュレーター状態を表示し、主要な操作ボタンを提供します。**Start Servers** は RESTful API、TCP、MQTT エンドポイントを開きます。**Initialize**、**Deinitialize**、**Start Single**、**Start Continue**、**Stop** は、外部 RESTful API クライアントから呼び出せるものと同じ公開状態遷移を実行します。 |
| 4 | **Event Log** | ローカル シミュレーターの動作、サーバーの起動/停止メッセージ、コマンド拒否、生成された結果、その他の診断出力を表示します。ボタン操作やクライアント コマンドがシミュレーターに届いたことを確認するために使用します。 |
| 5 | **State Machine** | ライブ状態図を表示します。強調表示されたブロックは現在のシミュレーター状態に追従します。`ƒ` ラベルは command、`⚡` ラベルは event です。`Initializing`、`UpdatingProductInfo`、`Deinitializing` などの中間状態は短時間表示されるため、遷移経路を確認しやすくなります。 |

## シミュレーターの目的

|目的 |何を確認するか |
| --- | --- |
|契約の確認 |ベンダーアプリケーションが本番環境と同じ RESTful API ルート、データモデル、TCP フレーム、および MQTT トピックを使用していることを確認します。 |
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
7. **Start Single** を押して 1 回だけ自動実行するか、**Start Continue** を押して **Stop** まで結果を継続生成します。
8. **State Machine** の強調表示が現在状態に合わせて移動することを確認します。
9. run mode に応じて `runStarted`、`runCompleted`、`resultCreated`、または連続した `resultCreated` イベントを観察します。
10. `GET /api/results` をクエリします。

## 既定のエンドポイント

|インターフェース |既定 |
| --- | --- |
| RESTful API | `http://127.0.0.1:5088` |
| RESTful API ブラウザ | `http://127.0.0.1:5088/scalar` |
| OpenAPI JSON | `http://127.0.0.1:5088/openapi/v1.json` |
| TCP | `127.0.0.1:5089` |
| MQTT | `127.0.0.1:1883`、トピックプレフィックス `virex` |

## ボタンの動作

|ボタン |動作 |
| --- | --- |
| **Start Servers** | RESTful API、TCP、および MQTT エンドポイントを開始します。システム状態を変更しません。 |
| **Initialize** |初期化コマンドを送信します。 `Uninitialized` でのみ有効です。 |
| **Deinitialize** |初期化解除コマンドを送信します。 `Ready` でのみ有効です。 |
| **Apply ProductInfo** |現在の ProductInfo を更新します。 `Ready` でのみ有効です。 |
| **Start Single** | `runMode=single` で 1 回の実行を開始します。`Ready` でのみ有効です。応答状態は `Running` です。シミュレーターは結果を生成し、run-completed event の後で `Ready` に戻ります。 |
| **Start Continue** | `runMode=continue` で継続実行を開始します。`Ready` でのみ有効です。応答状態は `Running` です。**Stop** を押すまで結果を継続生成します。 |
| **Stop** |アクティブな実行を停止します。 `Running` でのみ有効です。応答状態は `Ready` です。 |

## 観察可能な動作

|アクション |予想される外部観測 |
| --- | --- |
| Initialize | RESTful API コマンドは `Ready` を返します。状態イベントが公開されます。 |
| ProductInfo アップデート | RESTful API コマンドは `Ready` を返します。 ProductInfo イベントが公開されます。 |
| Start single | RESTful API コマンドは `Running` を返します。状態は `Running` に変わります。結果作成イベントが公開され、run-completed によって状態は `Ready` に戻ります。 |
| Start continue | RESTful API コマンドは `Running` を返します。状態は `Running` のままです。stop コマンドが受け入れられるまで結果作成イベントが継続します。 |
| Stop | 状態は `Ready` に戻ります。continue mode では追加の自動 run-completed event は不要です。 |
|無効なコマンド |コマンド応答には `accepted=false` および `errorCode=invalid_state` が含まれます。拒否イベントが公開される場合があります。 |

## 推奨されるシミュレーターの受け入れプロセス

```powershell
dotnet test Virex.NET.Integration.slnx
python -m mkdocs build --strict
```

次に、ローカル シミュレーターを手動で使用して、生成されたドキュメントと C# SDK サンプルを確認します。
