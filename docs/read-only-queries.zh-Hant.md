# Recipe 與結果明細唯讀查詢

Issue #44 候選 SDK 與 Simulator 新增四個 REST 查詢。既有命令、事件、結果清單與 ProductInfo 契約保持相容；App 須另實作對應端點。舊 App 回傳 404 時，SDK 會保留失敗，不假成功回傳空資料。完整欄位與範例見 [查詢契約](read-only-queries.md)。

| GET 路徑 | SDK 方法（VirexClient 與 VirexRestClient） | DTO |
| --- | --- | --- |
| `/api/recipes` | `GetRecipesAsync` | `RecipeList`（items、count） |
| `/api/recipes/current` | `GetCurrentRecipeAsync` | `RecipeInfo`（recipe、revision） |
| `/api/recipes/current/parameters` | `GetCurrentRecipeParametersAsync` | `RecipeParameters`（recipe、revision、groups） |
| `/api/results/{resultId}` | `GetResultDetailAsync` | `ResultDetail`（schemaVersion、resultId、summary、findings） |

當前 Recipe 是實際載入的設定，並非 ProductInfo 的請求名稱。參數由 group.key、parameters[].key/type/value 組成；type 支援 string、boolean、integer（Int64）、number，value 為符合型別的 JSON 純量。空參數陣列有效。每次參數查詢都是一致快照；分開查詢時須比對 recipe 與 revision，不同就重新查詢兩者。revision 是設定版本識別。

Simulator 初始化與成功更新 ProductInfo 才提交已載入快照；取消更新保留先前快照，成功反初始化清除快照。初始清單包含 RCP-A、RCP-DEMO；沿用既有任意名稱的模擬行為，成功載入名稱加入清單。模擬參數 simulator/cycleDelayMs=1000 表示既有模擬週期間隔。

結果嚴格按大小寫敏感的完整 ResultId 查詢，同 Lot/Wafer 多筆也不能取最新結果代替。使用 `RestRoutes.ResultDetail(id)` 編碼單一路徑區段；ID 不裁切空白，也不作為檔案路徑。SDK 拒絕空白 ID 與單獨的 . 或 ..。schemaVersion=1 是 Integration 明細外層版本，獨立於 App 公開結果 JSON 版本。resultId 與 summary.resultId 須一致；summary 保留既有摘要；findings 可為零筆。

Finding 使用既有公開 FindingId、Kind、Label、可選 Score、ProductPolygon 與 DiagnosticImageIds。幾何點為 xmm/ymm，表示產品座標系毫米值；不推測缺少的量測。Simulator 產生零 Findings，结果檔保留舊頂層摘要並加入 detail；只查詢本次 session 已提交且仍在索引內的精確結果（最多 100 筆）。

| HTTP | errorCode | 語義 |
| --- | --- | --- |
| 400 | invalid_query | 不支援的 query 參數或空 ID |
| 405 | invalid_query | 只允許 GET，回應 Allow: GET |
| 409 | no_current_recipe | 尚未載入或已卸載 Recipe |
| 409 | query_not_ready | 初始化、更新、反初始化或恢復期間尚未就緒 |
| 404 | result_not_found | 未知、未提交／未發布或超出保留範圍；不宣稱資料存在 |
| 410 | result_deleted | 已知且已提交的結果檔已刪除 |
| 503 | query_failed | 讀取失敗、檔案損毀或識別／版本不一致 |

失敗 JSON 為 `QueryError { errorCode, message }`。四個查詢不接受 query 參數，不改機台狀態、`/api/error` 或發出命令拒絕事件。SDK HTTP 失敗使用 VirexClientException 保留 StatusCode/ResponseBody；成功但格式不完整或版本不支援也拋出例外，不代入空 DTO。所有方法支援 CancellationToken。

執行 [完整 C# SDK 範例](samples.zh-Hant.md) 加上 `--queries` 可驗證四個查詢；預設保留原本 13 步驟流程。
