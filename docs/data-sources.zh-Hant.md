# 即時與結果資料來源

2.4.0 候選在 Contracts、Client 與 Simulator 加入通用 REST 資料來源查詢。每個 App 公開自己的完整來源名稱。SDK 不使用名稱 enum 或白名單。真實 App 仍須實作對應 API 並另行驗證。

| GET route | `VirexClient`／`VirexRestClient` 方法 | 回應 |
| --- | --- | --- |
| `/api/data-sources` | `GetDataSourcesAsync(token)` | `DataSourceCatalog` |
| `/api/data-sources/value?name=...` | `ReadDataSourceAsync(name, cancellationToken: token)` | 即時 `DataSourceValue` |
| `/api/data-sources/value?name=...&resultId=...` | `ReadDataSourceAsync(name, resultId, token)` | 已提交結果 `DataSourceValue` |
| `/api/data-sources/current-result` | `GetCurrentResultAsync(token)` | 只有結果身分的 `ResultReference` |

查詢只支援 GET，沒有機台或設定副作用。清單及目前結果身分不接受 query parameters。讀值只接受一個非空白 `name` 及最多一個非空白 `resultId`。名稱及 ID 以完整、區分大小寫的字串比對，不 trim。SDK 會正確 URL encode。

清單包含 `appId`、`instanceId`、取值 `timestamp` 及來源。每個來源包含 `name`、`kind`（`live`／`result`）、JSON `type` 及 `availability`。型別為 `string`、`boolean`、`integer`（Int64）、有限 `number`、`object` 或 `array`。可用性為 `available`、`no_current_result`、`source_unavailable` 或 `source_value_missing`。結果來源的可用性指目前工作；明確指定的歷史 ID 可另行讀取。已標為可用的來源仍可能因 artifact 損壞而讀取失敗。

讀值包含完整來源名稱、種類、型別、實際 App `state`、取值時間及 App 身分。即時值不接受 `resultId`。結果值必須指定 `resultId`，並回傳相同已提交結果的 `jobId`、`captureId` 及選用 `requestId`。`JsonElement` 是獨立快照，原始 JSON document 釋放後仍可用。缺值、null、型別不符或身分不符都不能變成成功。注入的 `HttpClient` 由呼叫者持有。

Simulator 預設公開即時字串 `system.state`、結果陣列 `result.findings` 及結果物件 `result.detail`。結果沿用既有公開 `ResultDetail`／`ResultSummary`／Findings。零 Findings 是有效結果。Host 可用 `ConfigureLiveDataSourceAsync` 設定明確合成值，或用 `ConfigureResultDataSourceAsync` 為 `detail`、`summary`、`findings` 投影指定自訂名稱。設定只在 Uninitialized／Ready 執行；即時及結果名稱不能衝突。設定會 clone JSON，並驗證宣告型別。這些是合成測試來源，不含客戶 ID、公式或私有結果模型。

Start 受理後，目前結果來源立即變成 `no_current_result`。拒絕 Start 或受理前取消不使舊目前結果失效。只有受理工作真正提交的結果可成為目前結果。Stop、取消及舊 timer callback 不能恢復舊工作。CaptureOnly 不提交結果。讀取明確歷史 ID 不會更改目前結果身分。

Simulator 每 session 保留最多 100 個已提交結果及 100 個近期取像身分。已知但未提交回 `result_not_committed`；未知或過期 ID 回 `result_not_found`。新 instance 使用新 `instanceId`，沒有目前結果或歷史索引。關聯 Start、TCP `resultCreated` 與讀值時須核對 App／instance／工作／取像／結果 ID，不以 Lot／Wafer 名稱推定。

型別化錯誤分為 `unknown_source`、`no_current_result`、`result_not_found`、`result_not_committed`、`source_unavailable`、`source_value_missing`、`result_deleted`、`query_failed`、`invalid_query`。SDK 的 transport、API timeout 或非法回應使用 `api_failed`。資料來源方法拋出 `VirexQueryException`，可讀 `ErrorCode`、`Error`、`StatusCode`、`ResponseBody`；沒有可用 HTTP 回應時 StatusCode 為零。呼叫者取消仍是 `OperationCanceledException`。本機非法名稱／ID 是 `ArgumentException`。完整 HTTP 對照與範例見 [英文參考](data-sources.md)。
