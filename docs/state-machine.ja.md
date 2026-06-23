# 起動順序と状態遷移

このページでは、client が simulator と統合するときに従うべき起動順序と、external commands によって公開状態がどのように変わるかを、簡略化した state diagram で説明します。図には integration 判断に必要な状態だけを含め、すべての内部可能性を列挙しません。

Simulator は 2 つの public values で状態を報告します。Clients は内部実装の詳細を知る必要はなく、次の command が有効かどうかをこれらの fields で判断します。

| Field | Meaning |
| --- | --- |
| `initialized` | simulated service が initialize されたかどうか。 |
| `processState` | 現在の process state。`ready` は次の cycle を受け付け可能であることを示します。`capturing`、`inspecting`、`saving` は cycle progress updates です。 |

`Start Servers` は REST、TCP、MQTT endpoints が listening するかどうかだけを制御します。`initialized` や `processState` は変更しません。

## State Diagram

![Virex.NET simulator state transition diagram](assets/state-machine-flow.svg)

図中の **external command** は、client、SDK、REST、または TCP から送信される control command を意味します。`capturing`、`inspecting`、`saving` は cycle 中に simulator が内部的に進める progress states です。Clients は通常、status/result events を待つか、cancel が必要な場合に **Stop** を送ります。

## Client Rules

| Rule | Client-side check |
| --- | --- |
| **Start Servers** は communication prerequisite です。 | REST/TCP/MQTT endpoints が listening してから接続します。 |
| **Initialize** は cycle prerequisite です。 | `initialized=false` の場合、start は `409 not_initialized` を返します。 |
| **Start Cycle** は initialized かつ ready のときだけ送信してください。 | `initialized=true` かつ `processState=ready` のとき start が受け付けられます。 |
| Active cycle states は progress updates です。 | `capturing`、`inspecting`、`saving` は cycle が実行中であることを示します。status/result events を待つか、stop を送ります。 |
| Simulator は result event 後に ready に戻ります。 | Result summary の後、`processState=ready` になり、次の cycle を開始できます。 |

## Commands And Transitions

| Command or UI action | Required state | Result |
| --- | --- | --- |
| **Initialize** / `POST /api/control/initialize` | `initialized=false`, `processState=ready` | `initialized=true` に設定し、`processState=ready` を維持します。 |
| **Terminate** / `POST /api/control/terminate` | `processState=ready` | `initialized=false` に設定し、`processState=ready` を維持します。 |
| **Start Cycle** / `POST /api/control/start` / TCP `{"type":"start"}` | `initialized=true`, `processState=ready` | `capturing`、`inspecting`、`saving` の順に遷移し、result emission 後に `ready` へ戻ります。 |
| **Stop** / `POST /api/control/stop` / TCP `{"type":"stop"}` | Active process state: `capturing`、`inspecting`、`saving` | 現在の cycle を cancel し、`ready` に戻ります。 |
| **Apply WaferInfo** / WaferInfo REST or TCP update | Any process state | wafer context を更新し、wafer-info events を送信します。`processState` は変更しません。 |
| **Emit Fake Result** | Any process state | 単一の result summary event を送信します。`processState` は変更しません。 |
| **Emit Error** | Any process state | error event を送信します。`processState` は変更しません。 |

## Common Rejected Commands

| Condition | Command | Response |
| --- | --- | --- |
| `initialized=false` | Start cycle | HTTP `409` / `not_initialized`; state は `initialized=false`, `processState=ready` のままです。 |
| `processState` is `capturing`, `inspecting`, or `saving` | Start cycle | HTTP `409` / `process_active`; 現在の cycle は継続します。 |
| `initialized=false` | Stop | HTTP `409` / `not_initialized`; state は変更されません。 |
| `initialized=true`, `processState=ready` | Stop | HTTP `409` / `not_running`; state は変更されません。 |
| `processState` is not `ready` | Terminate | HTTP `409`; state は変更されません。 |

## Event Visibility

State changes は次の方法で確認できます。

- REST `GET /api/status`
- TCP `status` events
- MQTT `virex/status` events
- SDK `GetStatusAsync`

Result、wafer-info、error events は別の event types です。Simulator が `ready` のときに発生することがありますが、追加の `processState` values ではありません。
