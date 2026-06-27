# 驗證清單

使用這份清單判斷廠商整合是否可以從模擬器切到正式相容端點。

## REST

| 檢查項目 | 預期結果 |
| --- | --- |
| 讀取狀態 | `GET /api/status` 回傳含 `state` 的 `SystemStatus`。 |
| 初始化 | `POST /api/system/initialize` 在 `Uninitialized` 被接受，回 `Ready`。 |
| 更新 ProductInfo | `POST /api/product-info` 在 `Ready` 被接受，回 `Ready`。 |
| 啟動 | `POST /api/system/start` 在 `Ready` 被接受，回 `Running`。 |
| 停止 | `POST /api/system/stop` 在 `Running` 被接受，回 `Ready`。 |
| 反初始化 | `POST /api/system/deinitialize` 在 `Ready` 被接受，回 `Uninitialized`。 |
| 非法命令 | 非法命令回 `accepted=false`、`errorCode=invalid_state` 與目前 `state`。 |
| 結果 | `GET /api/results` 回傳符合 ProductInfo 快照欄位的摘要。 |

## TCP

| 檢查項目 | 預期結果 |
| --- | --- |
| 連線 | Client 可以連上設定的 TCP 連接埠。 |
| 分幀 | 每個資料框是一個 UTF-8 JSON 物件，並以 `\n` 結尾。 |
| ProductInfo 命令 | `type: "productInfo"` 在 `Ready` 更新 ProductInfo。 |
| 啟動/停止命令 | `type: "start"` 與 `type: "stop"` 遵守 REST 相同狀態規則。 |
| 事件解析 | Client 可處理 `statusChanged`、`productInfoChanged`、`runStarted`、`runCompleted`、`resultCreated`、`errorChanged`、`commandRejected`。 |

## MQTT

| 檢查項目 | 預期結果 |
| --- | --- |
| 訂閱 | Client 可訂閱 `virex/#` 或設定的根主題。 |
| 狀態事件 | Client 收到 `statusChanged`、`runStarted`、`runCompleted`。 |
| ProductInfo 事件 | Client 收到 `productInfoChanged`。 |
| 結果事件 | Client 收到 `resultCreated`。 |
| 拒絕事件 | 發佈拒絕命令事件時，Client 收到 `commandRejected`。 |

## 可攜性

切到正式環境前確認：

- 端點設定可調整。
- 整合程式不依賴模擬器 UI 標籤或固定延遲。
- 整合程式使用 `Virex.NET.Contracts` 資料模型或等價 JSON 結構。
- 整合程式將 MQTT 視為純傳出通道。
- 整合程式能處理重新連線與重複事件。
