# Alarm 來源與兩段復機

`GET /api/alarms` 回傳 `AlarmCatalog`，包含 App、Instance、讀取時間與來源清單。每個來源有完整字串 `name`、實際 producer 的 `source` 與 `category`。名稱區分大小寫，也是 `GET /api/events` 中可訂閱的事件名稱。SDK 沒有名稱 enum 或白名單。客戶 ID、wire 格式與整機處置由 consumer／Machine 負責。

以既有 `client.TcpEvents.SubscribeAsync` 訂閱所需名稱。callback 的 `EventEnvelope.Payload` 是獨立的 `AlarmNotification` JSON。先核對 App／Instance 與工作身分。payload 時間代表發生時間，envelope 時間代表發布時間。RequestId／JobId／CaptureId／ResultId 只在實際來源知道時提供。結果尚未提交的執行故障沒有 ResultId。訂閱擁有者須呼叫 `CloseAsync`；關閉後不再收到 callback。

| 分類 | 公開行為 |
| --- | --- |
| `fault` | 停止未完成工作，使舊取像 callback 失效，鎖定 `Faulted`。Start／Initialize 不能越過復機。其他 App 繼續。 |
| `defect` | 通知實際一般瑕疵，不單獨鎖成硬體故障。 |
| `criticalDefect` | 分別通知關鍵瑕疵，由 Machine 決定是否停止整機。 |

`POST /api/system/reset`／`client.ResetAsync()` 先停止工作，等待取消的合成 run task，再由同一 session 擁有者沿用 Deinitialize 清理。並發 Reset 共用一次清理；同時到達的 Deinitialize 也加入該清理。成功只回 `Uninitialized`。在此狀態重複 Reset 不再執行清理。清理失敗回 HTTP 409、`accepted=false`、`cleanup_failed` 與 `Faulted`。復機期間的新故障不會被較早 Reset 清除。Reset 不續跑舊工作。

接著必須明確呼叫 `InitializeAsync()`。只有初始化真正完成才回 `Ready`。Running 中 Reset 也會取消原工作。啟用操作權管理時，Local 拒絕 external Reset，回 `operation_not_allowed`，不停止工作，也不清除故障或 Recipe。查詢與模式切換仍可用。

送出前取消沒有 server 副作用。server 受理後的 caller 取消／斷線只停止等待；已受理清理仍完成。SDK 保留 caller cancellation。命令 HTTP 失敗使用 `VirexClientException`；清單、解析與 API 逾時使用 `VirexQueryException`／`api_failed`，不補空清單。`VirexRestClient` 借用 caller 的 HttpClient，caller 負責釋放。SDK 可並發呼叫。

Simulator 使用合成來源。內建實際 producer 是 `simulator.acquisitionFault`（`SimulateAcquisitionFaultAsync`）、`simulator.persistenceFailed`（dummy artifact 寫入失敗）與 `simulator.cleanupFailed`（清理失敗）。它們都列入 catalog 並透過 TCP 通知。`ConfigureAlarmSourceAsync` 在初始化前複製自訂描述並註冊來源；`RaiseAlarmAsync` 明確產生合成通知。任意完整名稱不需更新 SDK。舊 expected JobId、未知／未提交／已被新工作取代的結果及已關閉 host 不通知，不借用上一工作結果。內建名稱不能由此方法手動注入。通用 EmitEvent 也不能繞過 Alarm producer。`EmitError` 只產生診斷 ErrorInfo，不代表完整分類 Alarm。

本機 Simulator 行為與編譯成功不代表真實設備故障、native cleanup、正式客戶配置、硬體或 Machine 已驗收。
