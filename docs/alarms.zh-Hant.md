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

ResultId 是選用欄位。指定時，RaiseAlarmAsync 要求目前已提交結果。未指定時，实际目前工作的 defect／criticalDefect 通知仍合法；它不等於 ResultCreated／DefectResult，也不借用上一筆結果。

Recovery Stop 僅支援 REST。先查 `SystemStatus.RecoveryStopSupported`；缺省／false 表示不支援，不得 fallback 普通 Stop。`SystemStopRequest` 使用 `RequireReset=true`、目前 catalog 的完整非空 `ExpectedAppId`／`ExpectedInstanceId`，以及最多兩個已知 `CancelStartRequestIds`。ID 採 exact ordinal，不 trim。沒有已知 Start ID 的故障可傳空集合。null array／item、空白、重複或超限回 `invalid_payload`。false 卻帶 ID 或 expected identity 也拒絕。`Reason` 只作診斷。

接收端在副作用前核對 object shape、重複欄位（含大小寫別名）、型別、未知欄位、identity 及操作來源。Recovery Stop 在 Start 共用 gate 內建立持續屏障及撤銷 ID，再停止已受理工作，於 gate 外 join 原有 run cleanup。Ready／Uninitialized 也可建立屏障。只有 accepted、`RecoveryStopApplied=true`、expected App／instance identity 及 `RecoveryAction=Reset` 的回應能證明這些效果。State 必須是 `Ready`、`Uninitialized` 或 `Faulted`；SDK 拒絕 `Running`、`Initializing`、`UpdatingProductInfo` 及 `Deinitializing`，因為工作或清理可能尚未完成。它不代表 Reset 清理或初始化完成。失敗／取消不能降成普通 Stop，也不能重播。取消 client 等待不撤回已受理屏障。DTO array 在非同步受理前複製。

只有成功明確 Reset 才解除屏障。Initial、普通 Stop、診斷訊息及普通 Deinitialize 都不能解除。Reset 失敗或較新 recovery 保留屏障。撤銷 ID 在同 App instance 的 Reset／Initial 後仍保留；明確 Reset→Initial 後可用新 ID。新 App instance 使用新 identity。TCP／MQTT 的 Stop 只支援普通語意；帶 recovery 欄位（包含 false／null）回 typed `invalid_payload`，不能忽略。

`WorkCompletedInfo` 包含 exact 非空 `RequestId`、`JobId` 及實際 `Timestamp`。只有實際 runtime owner 在整個已受理工作的準備、取像、檢測、儲存及其持有的清理都靜止後才能產生。外層 `EventEnvelope` 提供 App／instance scope。事件名稱由配置決定，沒有固定事件名稱。`CaptureCompleted`、`ResultCreated`、舊 `runCompleted` 轉移或 Ready snapshot 都不能證明此事。Simulator 尚未產生這項事實；通用注入事件仍只算合成證據，不能證明實際 owner 清理。
