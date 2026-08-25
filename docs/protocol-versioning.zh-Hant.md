# 版本

公開套件與通訊協定變更使用語意化版本。

```text
1.0.0 初始公開整合合約。
1.1.0 新增累加式欄位、端點、主題或事件。
2.0.0 破壞性的資料內容、路由、主題或行為變更。
```

## 版本規則

- Minor version 不移除或重新命名公開 JSON 欄位。
- Minor version 可以新增累加式欄位。
- 破壞性變更需要 major version。
- 模擬器、SDK、文件、合約測試必須同步。
- [資料模型參考](payloads.zh-Hant.md) 必須與公開資料模型、路由、主題、事件結構對齊。

## 已發布版本說明

以下整理提供給客戶的已發布版本。每個標題都連到對應的 GitHub Release。

### [v2.2.2](https://github.com/vstechnology-tw/vst-virex.net.integration/releases/tag/v2.2.2) — 2026-08-25

- 公開版本仍停在 2.1.1 後，將未發布的 2.2.1 候選版復原功能正式發布為 2.2.2。
- 為 `statusChanged`、`errorChanged` 與 `commandRejected` 新增一致且可加性的復原封套，包含可重試 Deinitialize 指引、穩定錯誤代碼、時間戳記、來源、階段與已清理細節。
- 將同時發生的模擬器復原轉換序列化，並保留 `imageGrabbed`、`captureId` 與已持久化的模擬器產物。
- 包含目前的模擬器 ZIP、四種語系文件 PDF 與 Mermaid 顯示修正。
### [v2.1.1](https://github.com/vstechnology-tw/vst-virex.net.integration/releases/tag/v2.1.1) — 2026-08-17

- 新增公開 `imageGrabbed` 事件，涵蓋事件模型、TCP、MQTT、C# SDK 與模擬器。
- 新增穩定的 `captureId`，可將 `imageGrabbed` 與後續的 `resultCreated` 關聯。
- 模擬器會透過 `resultCreated` 的路徑提供測試用影像、預覽圖與結果檔案。
- 更新模擬器診斷資訊、傳輸範例與對外文件。

### [v2.1.0](https://github.com/vstechnology-tw/vst-virex.net.integration/releases/tag/v2.1.0) — 2026-07-01

- 完成 RESTful API、TCP Socket 與 MQTT 的命令及查詢對齊。
- 新增 MQTT 狀態、錯誤、ProductInfo、生命週期命令與結果查詢的命令/回應。
- 新增 TCP 狀態、錯誤、ProductInfo 與結果查詢資料框。
- 讓 C#、Python、C++ 範例使用相同的 13 步驟整合流程。
- 發布 Contracts、Client 套件，以及 .NET Framework 4.8、.NET 8、.NET 10 模擬器下載包。

### [v2.0.3.1](https://github.com/vstechnology-tw/vst-virex.net.integration/releases/tag/v2.0.3.1) — 2026-06-30

- 僅模擬器版本發布。
- 補充並實作 TCP initialize 與 deinitialize 命令。
- 將模擬器結果影像與公開文件從 `.tiff` 更新為 `.bmp`。
- 更新安裝文件與 OpenAPI metadata。

### [v2.0.3](https://github.com/vstechnology-tw/vst-virex.net.integration/releases/tag/v2.0.3) — 2026-06-28

- 從最新 main 重新發布套件與模擬器下載包。
- 已發布內容包含 ProductInfo 合約更新。

### [v2.0.2](https://github.com/vstechnology-tw/vst-virex.net.integration/releases/tag/v2.0.2) — 2026-06-27

- 將公開 `WaferInfo` 合約改為 `ProductInfo`。
- 新增以 ProductInfo 為基礎的生命週期行為與模擬器支援。
- 更新 RESTful API、TCP、MQTT、範例與多語言文件。

### [v2.0.1](https://github.com/vstechnology-tw/vst-virex.net.integration/releases/tag/v2.0.1) — 2026-06-23

- 發布初始 2.0 公開整合合約與模擬器下載包。
- 將文件遷移到 MkDocs Material，提供英文、繁體中文、日文與韓文版本。
- 說明 Result Summary 與 REST 結果查詢回應的差異。
- 更新結果路徑與 payload 欄位、MQTT 預設值、OpenAPI/Scalar 驗證與 TCP framing 行為。

## 沒有 GitHub Release 頁面的歷史標籤

`v1.0.0` 與 `v1.0.1` 是 repository tags，但 GitHub 沒有對應的公開 Release 頁面或 release note 紀錄。第一個已發布的 GitHub Release 是 `v2.0.1`。