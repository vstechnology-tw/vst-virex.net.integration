# 検証チェックリスト

このチェックリストを使用して、ベンダー統合をシミュレーターから運用互換エンドポイントに移行する準備ができているかどうかを判断します。

## REST

|チェック |期待される結果 |
| --- | --- |
|読み取り状態 | `GET /api/status` は、`SystemStatus` を `state` とともに返します。 |
|初期化 | `POST /api/system/initialize` は `Uninitialized` で受け入れられ、`Ready` を返します。 |
| ProductInfo を更新 | `POST /api/product-info` は `Ready` で受け入れられ、`Ready` を返します。 |
|開始 | `POST /api/system/start` は `Ready` で受け入れられ、`Running` を返します。 |
|停止 | `POST /api/system/stop` は `Running` で受け入れられ、`Ready` を返します。 |
| Deinitialize | `POST /api/system/deinitialize` は `Ready` で受け入れられ、`Uninitialized` を返します。 |
|無効なコマンド |無効なコマンドは、`accepted=false`、`errorCode=invalid_state`、および現在の `state` を返します。 |
|結果 | `GET /api/results` は、ProductInfo スナップショット フィールドに一致する概要を返します。 |

## TCP

|チェック |期待される結果 |
| --- | --- |
|接続 |クライアントは、構成された TCP ポートに接続できます。 |
|フレーミング |各フレームは、`\n` で終わる 1 つの UTF-8 JSON オブジェクトです。 |
| ProductInfo コマンド | `type: "productInfo"` は、`Ready` の ProductInfo を更新します。 |
|開始/stop コマンド | `type: "start"` および `type: "stop"` は、REST と同じ状態ルールに従います。 |
|イベント解析 |クライアントは、`statusChanged`、`productInfoChanged`、`runStarted`、`runCompleted`、`resultCreated`、`errorChanged`、および `commandRejected` を処理できます。 |

## MQTT

|チェック |期待される結果 |
| --- | --- |
|サブスクリプション |クライアントは、`virex/#` または構成されたトピックプレフィックスをサブスクライブできます。 |
|状態イベント |クライアントは `statusChanged`、`runStarted`、および `runCompleted` を受信します。 |
| ProductInfoイベント |クライアントは `productInfoChanged` を受信します。 |
|結果イベント |クライアントは `resultCreated` を受け取ります。 |
|拒否イベント |コマンドが拒否された場合、クライアントは `commandRejected` を受け取ります。 |

## 移植性

本番環境に切り替える前に、次の点を確認してください。

- エンドポイント設定は調整可能です。
- この統合は、シミュレーターの UI ラベルや固定遅延に依存しません。
- 統合では、`Virex.NET.Contracts` モデルまたは同等の JSON 構造を使用します。
- 統合では、MQTT を純粋な送信チャネルとして扱います。
- 統合により、再接続とイベントの重複を処理できます。
