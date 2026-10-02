# 取像準備與整次完成

`captureReady` 表示外部機台可開始此 job／capture 的掃描：**sourceIds 中全部來源已成功完成準備，並已 armed、可接受這次取像**。它與 SystemStatus Ready、runStarted 分開。準備失敗須拒絕 Start（capture_preparation_failed，REST503），不得發出允許掃描事件。無法證明此保證的來源不能發 captureReady。

`captureCompleted` 表示 ready 集合中**所有來源取像都明確結束、影像已交給應用程式**，同一 capture 只發一次。它不等待檢測、結果或檔案保存，也不保證結果成功。單張 imageGrabbed、累計張數、第一個來源完成，都不能推估整次完成；runCompleted 亦不能替代這兩種事件。取像完成後才發生的檢測／保存故障不會否定已完成取像。

| 欄位 | captureReady | captureCompleted |
| --- | --- | --- |
| jobId | 一次成功 Start 的主機產生識別碼，必填 | 同一工作 |
| captureId | 整次取像群組，必填 | 同一群組 |
| timestamp | 就緒成立的 ISO8601 時間 | 全部來源取像結束時間 |
| sourceIds | 已準備來源，非空且不重複 | 相同完整來源集合 |
| imageCount | 無 | 個別影像總數，每個來源至少一張 |

REST／MQTT Start 回覆新增 jobId、第一個 captureId；imageGrabbed 與 resultCreated 新增可選 jobId，原 captureId 關聯仍不變。連續模式同一 Start 使用同一 jobId、每次 captureId 不同且逐次宣告準備；下次 Start 產生新 jobId。舊事件沒有 jobId 仍能解析，舊 SDK 簽名、runMode／inspectionMode 保持相容。

例如：準備（來源A/B）→A影像1→A影像2→B影像1→全部來源明確結束、整次完成→稍後結果。captureOnly 有準備／影像／完成，但無 resultCreated。來源結束必須有明確callback，不能靠張數推估；重複影像／結束、未知來源、舊 job／capture、取消／來源故障不會提前或重複宣告完成。

TCP NDJSON type 為 captureReady／captureCompleted，欄位平鋪；MQTT 使用同名子topic。SDK 回傳 VirexEvent.CaptureReady／CaptureCompleted；Simulator MQTT 依發出順序排隊。新事件缺少關聯ID、非法時間、缺少／重複來源或非法張數會拒絕解析。

事件即時發送、沒有重播或保留掃描許可。必須先訂閱再 Start，依回覆 job／capture 配對；消費端以此去重，忽略已取消或舊工作事件，Stop／來源故障／斷線／失去就緒時撤銷許可，不允許晚到事件重新啟用已結束工作。連線狀態不確定時先停止／核對，再開始新工作，不能沿用舊許可。

Simulator 預設 synthetic source `simulator`、一張影像；閒置時可用 ConfigureCaptureSimulationAsync 設多來源、多張或準備失敗。callback追蹤要求每來源明確結束，拒絕重複／晚到／取消。BMP／JPEG／結果仍維持原路徑，檔案是代表性的模擬輸出；多來源影像事件不代表真實來源取像。

**App #283 接續注意**：必須證明支援來源完成設定、已armed才發準備；來源失去就緒／故障須撤銷工作；全部來源真正取像結束才發完成，時點與檢測分離。Simulator PASS 不代表相機、外部觸發、掃描時序或客戶設備就緒。不加入客戶協定名稱或私有取像實作。
