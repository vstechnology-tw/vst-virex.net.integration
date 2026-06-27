# トラブルシューティング

## REST 要求が失敗しました

チェック：

- シミュレーターまたは本番エンドポイントが実行中である。
- REST ベース URL は正しいです。
- ファイアウォールは設定されたポートを許可します。
- ルートは [REST API](rest-api.ja.md) にリストされています。

## コマンドは `invalid_state` を返します

コマンドはサービスに到達しましたが、現在の状態では無効です。

例:

- `Start`は`Ready`でのみ有効です。
- `Stop`は`Running`でのみ有効です。
- `SetProductInfo`は`Ready`でのみ有効です。
- `Initialize`は`Uninitialized`でのみ有効です。
・`Deinitialize`は`Ready`内でのみ有効です。

最初に `GET /api/status` を読み取り、状態が許可したときにコマンドを送信します。

## 結果は返されませんでした

チェック：

- 実行が開始され、完了しました。
- 結果のクエリ フィルターは、`Start` が受け入れられたときにキャプチャされた ProductInfo スナップショットと一致します。
- フィルターは `waferID`、`lotID`、または `recipe` を使用します。

## TCP イベントがありません

チェック：

- TCP ホスト/ポートは正しいです。
- クライアントはソケットを開いたままにします。
- 各受信フレームは `\n` で終了します。
- クライアントは、`statusChanged` や `resultCreated` などの文書化されたイベント名を解析します。

## MQTT イベントがありません

チェック：

- ブローカーのホスト/ポートが正しい。
- サブスクリプションは、`virex/#` などのルートトピックと一致します。
- MQTT は送信イベントにのみ使用されます。
- クライアントは、文書化されたトピック名 (`productInfoChanged` など) をリッスンします。

## ローカル プレビューは GitHub Pages とは異なります

MkDocs をローカルで使用します。

```powershell
python -m mkdocs serve --dev-addr 127.0.0.1:8000
```

GitHub Pages ワークフローは次のことを行います。

```powershell
python -m mkdocs build --strict
```
