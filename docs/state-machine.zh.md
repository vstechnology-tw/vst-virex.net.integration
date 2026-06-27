# 系統狀態機

狀態機定義系統生命週期中，哪些命令在目前狀態下是合法的。Client 送出命令前，應該以目前狀態作為判斷依據。

## 狀態

| 狀態 | 意義 |
| --- | --- |
| `Uninitialized` | 尚未初始化，只能送出初始化命令。 |
| `Initializing` | 初始化命令已接受，等待完成事件。 |
| `Ready` | 閒置中，可接受 ProductInfo、啟動、反初始化命令。 |
| `UpdatingProductInfo` | ProductInfo 更新已接受，等待完成事件。 |
| `Running` | 執行中。內部執行階段不對外公開。 |
| `Deinitializing` | 反初始化命令已接受，等待完成事件。 |

## 命令與事件

命令是 Client 或操作人員提出的要求。只有命令符合目前狀態規則時，系統才會接受。

事件是系統產生的結果，用來完成中間狀態，或表示長時間動作已結束。

## 狀態轉移

![系統狀態機](assets/system-state-machine.svg)

實線轉移代表命令。虛線轉移代表事件。

## 命令合法性

| 命令 | 合法狀態 | 接受後結果 |
| --- | --- | --- |
| `Initialize` | `Uninitialized` | 進入 `Initializing`；`InitializationCompleted` 讓狀態變成 `Ready`。 |
| `SetProductInfo` | `Ready` | 進入 `UpdatingProductInfo`；`ProductInfoUpdateCompleted` 讓狀態回到 `Ready`。 |
| `Start` | `Ready` | 保存目前 ProductInfo 快照，並進入 `Running`。 |
| `Stop` | `Running` | 停止目前執行，並讓狀態回到 `Ready`。 |
| `Deinitialize` | `Ready` | 進入 `Deinitializing`；`DeinitializationCompleted` 讓狀態變成 `Uninitialized`。 |

## 拒絕的命令

任何不在上表合法狀態內送出的命令都屬於非法命令。非法命令必須被一致地拒絕，而且不能改變目前狀態。

例如，`Running` 狀態下送出 `SetProductInfo` 是非法的，因為 ProductInfo 只能在 `Ready` 狀態改變。

## 結果快照

`Start` 會立即保存目前的 `ProductInfo`。後續產生的結果會使用這份快照，即使未來實作允許執行中改變產品資料，也不應影響已啟動執行的結果。

在目前狀態機中，`SetProductInfo` 只在 `Ready` 合法，因此 `Running` 期間不能改變 ProductInfo。
