# システムステートマシン

ステート マシンは、各システム ライフサイクル状態でどのコマンドが有効であるかを定義します。クライアントはコマンドを送信する前に、現在の状態を使用してそのコマンドが許可されるかどうかを判断する必要があります。

## 状態

|状態 |意味 |
| --- | --- |
| `Uninitialized` |初期化されていません。初期化コマンドのみ有効です。 |
| `Initializing` | Initialize は受け入れられ、システムは完了イベントを待っています。 |
| `Ready` |アイドル。 ProductInfo 更新、開始、および初期化解除コマンドは有効です。 |
| `UpdatingProductInfo` | ProductInfo 更新が受け入れられ、システムは完了イベントを待っています。 |
| `Running` |実行がアクティブです。内部実行フェーズは公開状態ではありません。 |
| `Deinitializing` | Deinitialize の実行中、または再試行可能な復旧状態です。クリーンアップが完了するまで Deinitialize を使用可能に保ちます。 |

## コマンドとイベント

コマンドは、クライアントまたはオペレーターからの要求です。システムは、現在の状態で有効なコマンドのみを受け入れます。

イベントはシステムによって生成された結果です。これらは中間状態を完了するか、長時間実行されたアクションが終了したことを示します。

## 状態遷移

![システムステートマシン](assets/system-state-machine.svg)

実線のトランジションはコマンドを表します。破線の遷移はイベントを表します。

## コマンドの有効性

|コマンド |有効な状態 |受け入れ後の結果 |
| --- | --- | --- |
| `Initialize` | `Uninitialized` | `Initializing` に遷移します。 `InitializationCompleted` は状態を `Ready` に変更します。 |
| `SetProductInfo` | `Ready` | `UpdatingProductInfo` に遷移します。 `ProductInfoUpdateCompleted` は状態を `Ready` に戻します。 |
| `Start` | `Ready` |現在の ProductInfo スナップショットをキャプチャし、`Running` に遷移します。 |
| `Stop` | `Running` |アクティブな実行を停止し、状態を `Ready` に戻します。 |
| `Deinitialize` | `Ready` または `Deinitializing`（復旧再試行） | `Deinitializing` に遷移または再試行します。成功すると状態を `Uninitialized` に変更します。 |

## 拒否されたコマンド

上の表に記載されていない状態から送信されたコマンドは無効です。無効なコマンドは一貫して拒否する必要があり、現在の状態を変更してはなりません。

たとえば、状態が `Running` であるときに `SetProductInfo` を送信することは、状態が `Ready` であるときにのみ ProductInfo を変更できるため、無効です。

## 復旧契約

内部の acquisition fault により現在のソースを信頼できなくなった場合でも、`Faulted` をクライアントに公開しません。公開状態は `Deinitializing` となり、`CommandResponse`、`SystemStatus`、または `ErrorInfo` に `recoveryAction: "Deinitialize"` が含まれます。自動クリーンアップが失敗しても Deinitialize は使用可能なまま再試行できます。再試行でも完了できない場合のみ、アプリの再起動を最後の手段とします。

復旧ペイロードには、任意の `recoveryStartedAt`、`recoverySource`、`recoveryPhase`、サニタイズ済みの `recoveryDetails`、およびエラー/コマンド用の安定した `errorCode` も含めることができます。これらは追加フィールドであり、クライアントは未知のフィールドを無視します。

## 結果のスナップショット

`Start` は、現在の `ProductInfo` をすぐにキャプチャします。生成された結果はそのスナップショットを使用します。将来の実装で実行中に製品情報を変更できるようになったとしても、その変更はすでに開始されている実行の結果に影響を与えてはなりません。

現在のステート マシンでは、`SetProductInfo` は `Ready` でのみ有効であるため、`Running` 中に ProductInfo を変更することはできません。
