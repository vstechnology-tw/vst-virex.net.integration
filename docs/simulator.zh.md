# 模擬器指南

`Virex.NET.Simulator.WPF` 是整合開發用的本機端點。從 REST、TCP、MQTT、資料模型、狀態、事件的角度來看，它應該像一個與正式環境相容的 Virex.NET 服務。

從儲存庫根目錄啟動：

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
```

## 模擬器用途

| 目的 | 驗證項目 |
| --- | --- |
| 合約驗證 | 確認廠商程式使用與正式環境相同的 REST 路由、資料模型、TCP 資料框、MQTT 主題。 |
| 狀態機驗證 | 確認命令順序、可接受狀態、拒絕狀態、執行完成行為。 |
| 事件驗證 | 確認 TCP/MQTT 消費端可處理狀態、ProductInfo、執行、結果、錯誤、拒絕事件。 |

模擬器不是正式檢測引擎，不暴露私有演算法、相機行為、recipe 內部邏輯或儲存內部邏輯。

## 標準操作

1. 啟動模擬器。
2. 確認端點設定。
3. 按 **Start Servers**。
4. 連接範例程式或廠商 Client。
5. 初始化系統。
6. 送出 ProductInfo。
7. 啟動執行。
8. 觀察 `runStarted`、`runCompleted`、`resultCreated`。
9. 查詢 `GET /api/results`。

## 預設端點

| 介面 | 預設值 |
| --- | --- |
| REST | `http://127.0.0.1:5088` |
| REST 瀏覽器 | `http://127.0.0.1:5088/scalar` |
| OpenAPI JSON | `http://127.0.0.1:5088/openapi/v1.json` |
| TCP | `127.0.0.1:5089` |
| MQTT | `127.0.0.1:1883`，根主題 `virex` |

## 按鈕行為

| 按鈕 | 行為 |
| --- | --- |
| **Start Servers** | 啟動 REST、TCP、MQTT 端點。不改變系統狀態。 |
| **Initialize** | 送出初始化命令。只在 `Uninitialized` 合法。 |
| **Deinitialize** | 送出反初始化命令。只在 `Ready` 合法。 |
| **Apply ProductInfo** | 更新目前 ProductInfo。只在 `Ready` 合法。 |
| **Start** | 啟動執行。只在 `Ready` 合法；回應狀態是 `Running`。 |
| **Stop** | 停止執行中的流程。只在 `Running` 合法；回應狀態是 `Ready`。 |

## 可觀察行為

| 動作 | 預期外部觀察結果 |
| --- | --- |
| Initialize | REST 命令回 `Ready`；發佈狀態事件。 |
| ProductInfo 更新 | REST 命令回 `Ready`；發佈 ProductInfo 事件。 |
| Start | REST 命令回 `Running`；發佈執行開始事件。 |
| Run completes | 狀態回到 `Ready`；發佈執行完成與結果建立事件。 |
| 非法命令 | 命令回應包含 `accepted=false` 與 `errorCode=invalid_state`；可能發佈拒絕事件。 |

## 建議的模擬器驗收流程

```powershell
dotnet test Virex.NET.Integration.sln
python -m mkdocs build --strict
```

接著手動使用本機模擬器驗證已產生的文件與 C# SDK 範例。
