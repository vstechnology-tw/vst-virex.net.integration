# 2.4.0 公開候選驗收

2.4.0 公開契約包含 [完整名稱事件清單與訂閱](event-subscriptions.md)、[即時值與明確結果來源](data-sources.zh-Hant.md)，以及 [Alarm 清單與兩段復機](alarms.zh-Hant.md)。命令／查詢使用 REST；這些事件訂閱使用 TCP/NDJSON。舊 samples 保留為 transport 示範。固定等待時間不能證明可取像，舊 SDK／wire 相容性也不是本輪必要 gate。

本版是尚未發布的本機候選。Contracts／Client 保持 netstandard2.0；Simulator 保持 net48、net8.0-windows、net10.0-windows。Build 只證明目標可以編譯。每項 runtime check 分別記錄 target framework、RuntimeInformation.FrameworkDescription、Environment.Version 與实际載入 binary SHA256。缺 reference assemblies、desktop SDK 或原生 runtime 時保留精確限制，不能補成 PASS，也不安裝 runtime 或隱藏環境失敗。

套件、Simulator 與文件使用同一乾淨 source commit。Build／pack 由外部傳入 `spec2.g<short-commit>` 等 VersionSuffix，避免 source 自我包含自己的 commit hash。兩個套件使用完全相同的 `2.4.0-<suffix>`；Client 的 nuspec dependency 必須選到該 Contracts 版本。另建本機 feed，不覆蓋其他正在使用的候選 feed，也不發布。

執行 `dotnet test Virex.NET.Integration.slnx`，建置兩個 netstandard2.0 library 與三個 Simulator targets，並對各目標產物執行適用 SDK／REST／TCP 檢查。保留 commands、exit codes、原始失敗、TRX 與实际 outcomes。非法值、未知／不可用／缺值來源、取消、失敗及晚到工作都需要行為證據，hash 或 build 不能代替測試。

`samples/csharp-sdk-24` 刻意不加入 source solution。它使用完全固定的候選版本與 feed 還原，再獨立驗證 consumer。這使完整 solution gate 可在打包前重現；不需更改全域 NuGet 設定，也不使用 Client／Contracts ProjectReference。

外部 acceptance manifest 固定 source commit／tree、package version、nuspec／restore／實際載入證據、Simulator targets、文件／source／binary SHA256，以及逐項實際 host、outcomes 與未執行範圍。Manifest 自己的 hash 放在另一份 handoff receipt，避免循環。後續組合驗收固定這兩份公開候選證據。

結果來源使用明確 ResultId。Alarm 的 ResultId 是選用欄位：指定時須對應目前已提交結果；未指定時，实际目前工作的 defect／criticalDefect 通知仍合法，且不等於 ResultCreated／DefectResult。Reset 先停止並清理，成功回 Uninitialized；其後明確 Initialize 才回 Ready。既有單獨 Deinitialize 清理失敗可能保持 Faulted，但後續 Shutdown 成功；不能宣稱所有清理失敗都會由 Shutdown 上拋。Host 證據須保留此邊界。

Simulator 驗收只涵蓋合成公開行為。它不代表真 App、native cleanup、設備、正式客戶配置、機台 wire、部署、CI、installer 或 release 接受。發布、tag、部署與關卡須有各自授權。
