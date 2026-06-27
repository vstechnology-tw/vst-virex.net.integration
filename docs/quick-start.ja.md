# クイックスタート

このプロセスでは、ローカル シミュレーターを使用してコア統合パスを検証します。

## 1. シミュレーターを起動します

リポジトリのルートから実行します。

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
```

既定のエンドポイント設定を保持し、**Start Servers** をクリックして、REST/TCP/MQTT サービスを開始します。

|インターフェース |既定 |
| --- | --- |
| REST | `http://127.0.0.1:5088` |
| TCP | `127.0.0.1:5089` |
| MQTT | `127.0.0.1:1883`、ルートトピック `virex` |

## 2. SDK サンプルを実行します。

2 番目のターミナルを開きます。

```powershell
dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj
```

サンプルは標準フローを実行します。

1. `GET /api/status` を読み取ります。
2. システムを初期化します。
3. `ProductInfo` を送信します。
4. 実行を開始します。
5. runCompletedイベントを観察します。
6. 結果をクエリします。

## 3. 期待される状態を確認する

|ステップ |期待される状態 |
| --- | --- |
|新しくリリースされたシミュレーター | `Uninitialized` |
|初期化が完了しました | `Ready` |
| ProductInfo アップデートが完了しました | `Ready` |
|開始コマンドが受け入れられました | `Running` |
|実行が完了しました | `Ready` |

## 4. 結果のスナップショットを確認する

結果には、`Start` が受け入れられた ProductInfo が含まれている必要があります。

```json
{
  "waferID": "W01",
  "lotID": "LOT-001",
  "recipe": "RCP-A"
}
```

## 5. プロトコルチェックを選択します

- REST ブラウザ: `http://127.0.0.1:5088/scalar`
- MQTT: `virex/#` を購読する
- C# SDK を使用していない場合は、Raw TCP またはRaw REST サンプルを実行します。

## 完了条件

クイック スタートが成功すると、次のことが確認されます。

- REST 状態クエリは有効な `state` を返します。
- ProductInfo は `Ready` で更新できます。
- Start は `Running` を返します。
- 実行が完了すると、状態は `Ready` に戻ります。
- 結果はクエリ可能で、ProductInfo スナップショットに準拠します。
