# 起動順序と状態遷移

このページでは、シミュレーターと連携するクライアントが従うべき起動順序と、外部コマンドによって発生する公開状態の変化を、簡略化した状態図で説明します。この図は、顧客が連携判断に必要とする状態だけを残しており、内部的な可能性をすべて列挙するものではありません。

シミュレーターは 2 つの公開値で状態を報告します。クライアントは内部実装の詳細を知る必要はありません。次のコマンドが有効かどうかを判断するには、これらのフィールドだけで十分です。

| フィールド | 意味 |
| --- | --- |
| `initialized` | シミュレートされたサービスが初期化済みかどうか。 |
| `processState` | 現在の処理状態。`ready` は次のサイクルを受け付けられることを意味します。`capturing`、`inspecting`、`saving` はサイクルの進行状況を示します。 |

`Start Servers` は REST、TCP、MQTT エンドポイントが待ち受けているかどうかを制御します。`initialized` や `processState` は変更しません。

## 状態図

![Virex.NET simulator state transition diagram](assets/state-machine-flow.svg)

図の **external command** は、クライアント、SDK、REST、TCP から送信される制御コマンドを意味します。`capturing`、`inspecting`、`saving` は、サイクル中にシミュレーター内部で進む進行状態です。通常、クライアントは status/result イベントを待つか、キャンセルが必要な場合に **Stop** を送信します。

## クライアント ルール

| ルール | クライアント側の確認 |
| --- | --- |
| **Start Servers** は通信の前提条件です。 | REST/TCP/MQTT エンドポイントが待ち受けてから接続します。 |
| **Initialize** はサイクルの前提条件です。 | `initialized=false` の場合、start は `409 not_initialized` を返します。 |
| **Start Cycle** は初期化済みで ready のときだけ送信してください。 | `initialized=true` かつ `processState=ready` のとき、start が受け付けられます。 |
| 実行中のサイクル状態は進行状況です。 | `capturing`、`inspecting`、`saving` はサイクル実行中を意味します。status/result イベントを待つか、stop を送信します。 |
| シミュレーターは結果イベント後に ready へ戻ります。 | 結果要約の後、`processState=ready` になり、次のサイクルを開始できます。 |

## コマンドと遷移

| コマンドまたは UI 操作 | 必要な状態 | 結果 |
| --- | --- | --- |
| **Initialize** / `POST /api/control/initialize` | `initialized=false`、`processState=ready` | `initialized=true` に設定し、`processState=ready` を維持します。 |
| **Terminate** / `POST /api/control/terminate` | `processState=ready` | `initialized=false` に設定し、`processState=ready` を維持します。 |
| **Start Cycle** / `POST /api/control/start` / TCP `{"type":"start"}` | `initialized=true`、`processState=ready` | `capturing`、`inspecting`、`saving` を経て、結果発行後に `ready` へ戻ります。 |
| **Stop** / `POST /api/control/stop` / TCP `{"type":"stop"}` | アクティブな処理状態: `capturing`、`inspecting`、`saving` | 現在のサイクルをキャンセルし、`ready` へ戻ります。 |
| **Apply WaferInfo** / WaferInfo REST または TCP 更新 | 任意の処理状態 | ウェーハ コンテキストを更新し、wafer-info イベントを発行します。`processState` は変更しません。 |
| **Emit Fake Result** | 任意の処理状態 | 1 件の結果要約イベントを発行します。`processState` は変更しません。 |
| **Emit Error** | 任意の処理状態 | エラー イベントを発行します。`processState` は変更しません。 |

## よくある拒否コマンド

| 条件 | コマンド | 応答 |
| --- | --- | --- |
| `initialized=false` | サイクル開始 | HTTP `409` / `not_initialized`; 状態は `initialized=false`、`processState=ready` のままです。 |
| `processState` が `capturing`、`inspecting`、`saving` のいずれか | サイクル開始 | HTTP `409` / `process_active`; 現在のサイクルは継続します。 |
| `initialized=false` | Stop | HTTP `409` / `not_initialized`; 状態は変わりません。 |
| `initialized=true`、`processState=ready` | Stop | HTTP `409` / `not_running`; 状態は変わりません。 |
| `processState` が `ready` ではない | Terminate | HTTP `409`; 状態は変わりません。 |

## イベントで見える状態

状態変化は次の方法で確認できます。

- REST `GET /api/status`
- TCP `status` イベント
- MQTT `virex/status` イベント
- SDK `GetStatusAsync`

result、wafer-info、error イベントは別のイベント種別です。シミュレーターが `ready` のときに発生する場合もありますが、追加の `processState` 値ではありません。
