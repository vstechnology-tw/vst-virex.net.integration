# 検証チェックリスト

このチェックリストを使用して、ベンダー統合をシミュレーターから運用互換エンドポイントに移行する準備ができているかどうかを判断します。

## RESTful API

|チェック |期待される結果 |
| --- | --- |
|読み取り状態 | `GET /api/status` は、`SystemStatus` を `state` とともに返します。 |
|初期化 | `POST /api/system/initialize` は `Uninitialized` で受け入れられ、`Ready` を返します。 |
| ProductInfo を更新 | `POST /api/product-info` は `Ready` で受け入れられ、`Ready` を返します。 |
|開始 | `POST /api/system/start` は `Ready` で受け入れられ、`Running` を返します。 |
|停止 | `POST /api/system/stop` は `Running` で受け入れられ、`Ready` を返します。 |
| Deinitialize | `POST /api/system/deinitialize` は `Ready` で受け入れられ、クリーンアップ失敗を注入した後も `Deinitializing` で再試行でき、成功すると `Uninitialized` を返します。 |
| 復旧投影 | 復旧中、`/api/status`、`/api/error`、およびコマンド応答は `state: Deinitializing`、`recoveryAction: Deinitialize`、任意の開始時刻/ソース/フェーズ/サニタイズ済み詳細、該当する安定したエラーコードを公開し、内部の `Faulted` または `RequiresDeinitialize` 状態は公開しません。 |
|無効なコマンド |無効なコマンドは、`accepted=false`、`errorCode=invalid_state`、および現在の `state` を返します。 |
|結果 | `GET /api/results` は、ProductInfo スナップショット フィールドに一致する概要を返します。 |

## TCP

|チェック |期待される結果 |
| --- | --- |
|接続 |クライアントは、構成された TCP ポートに接続できます。 |
|フレーミング |各フレームは、`\n` で終わる 1 つの UTF-8 JSON オブジェクトです。 |
| ProductInfo コマンド | `type: "productInfo"` は、`Ready` の ProductInfo を更新します。 |
|開始/stop コマンド | `type: "start"` および `type: "stop"` は、RESTful API と同じ状態ルールに従います。 |
|イベント解析 |クライアントは、`statusChanged`、`productInfoChanged`、`imageGrabbed`、`runStarted`、`runCompleted`、`resultCreated`、`errorChanged`、および `commandRejected` を処理できます。 |
| 復旧イベントフィールド | `statusChanged`、`errorChanged`、`commandRejected` は Deinitialize を継続して使用できる復旧アクション、任意のコンテキスト、安定したエラーコード、サニタイズ済み詳細を保持します。 |

## MQTT

|チェック |期待される結果 |
| --- | --- |
|サブスクリプション |クライアントは、`virex/#` または構成されたトピックプレフィックスをサブスクライブできます。 |
|状態イベント |クライアントは `statusChanged`、`runStarted`、および `runCompleted` を受信します。 |
| ProductInfoイベント |クライアントは `productInfoChanged` を受信します。 |
|画像イベント |クライアントは `resultCreated` より前に `imageGrabbed` を受信します。取得イベントにはパスがありません。 |
|結果イベント |クライアントは `resultCreated` を受け取ります。 |
|拒否イベント |コマンドが拒否された場合、クライアントは `commandRejected` を受け取ります。 |
| 復旧イベントフィールド | `statusChanged`、`errorChanged`、`commandRejected` は Deinitialize を継続して使用できる復旧アクション、任意のコンテキスト、安定したエラーコード、サニタイズ済み詳細を保持します。 |

## 移植性

本番環境に切り替える前に、次の点を確認してください。

- エンドポイント設定は調整可能です。
- この統合は、シミュレーターの UI ラベルや固定遅延に依存しません。
- 統合では、`Virex.NET.Contracts` モデルまたは同等の JSON 構造を使用します。
- 統合では、MQTT を純粋な送信チャネルとして扱います。
- 統合により、再接続とイベントの重複を処理できます。
