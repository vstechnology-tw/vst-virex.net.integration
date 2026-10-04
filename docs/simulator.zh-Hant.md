# 模擬器指南

本機模擬器提供公開 REST、TCP 與 MQTT 契約。取像與檢測資料都是模擬資料；事件不能證明正式 App 或相機已就緒。

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj --framework net10.0-windows
# 選擇啟用來源授權
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj --framework net10.0-windows -- --manage-operation-mode
```

## 視窗操作

- **Connection Settings**：設定 REST prefix、TCP port、MQTT host／port／topic。按 **Start Servers** 或 **Stop Servers** 啟動或停止本視窗擁有的端點。
- **ProductInfo**：設定 Lot ID、Wafer ID、Recipe、Slot、Foup ID、Chamber ID。在 Ready 時按 **Apply ProductInfo** 套用。
- **State**：提供 **Initialize**、**Deinitialize**、**Start Single**、**Start Continue** 與 **Stop**。Start 需要 Ready；Stop 需要 Running。Single 完成後會自動回到 Ready。

## 取像與檢測模式

按 **Start Single** 或 **Start Continue** 前，選擇 **Legacy (omitted)**、**captureOnly** 或 **captureAndInspect**。Run mode 與 inspection mode 各自獨立。省略 inspectionMode 時保留 recipe 預設。在本模擬器中，captureOnly 產生取像事件，但不產生 resultCreated，也不保存影像或結果檔案；captureAndInspect 另產生模擬檢測結果。

## 查詢與操作模式

查詢 recipe 清單、目前 recipe、目前公開參數或 result detail。先輸入完整且區分大小寫的 ResultId，再按 **Get result detail**。沒有目前 recipe 或找不到 ID 時，畫面顯示公開查詢錯誤；不改查最新結果。

按 **Get operation mode** 讀取實際套用的 local/remote 與 managementEnabled。選擇模式後按 **Apply mode**；接受成功後才更新顯示。預設不啟用模式管理。啟動時加上 **--manage-operation-mode** 才執行來源授權：Local 允許本機生命週期按鈕；Remote 允許外部生命週期客戶端。切換模式不改變生命週期狀態。

## 取像事件與狀態機

**Capture Events** 頁籤顯示最新 captureReady 與 captureCompleted payload，包含 JobId、CaptureId、SourcesCount 與時間戳記。相同 capture 的 captureCompleted 與稍後 resultCreated 分開觀察。Event Log 保留公開事件順序。**State Machine** 頁籤跟隨實際 session 狀態。

## 取消與關閉

查詢不阻塞視窗。**Cancel query** 取消目前查詢；已被取代的查詢不覆寫新結果。關閉視窗會取消並等待模擬執行完成，並停止自己擁有的端點。宿主關閉不冒充外部 Stop，也不改變 operation mode。

## 預設端點

| 介面 | 預設值 |
| --- | --- |
| REST | `http://127.0.0.1:5088` |
| API 瀏覽器 | `http://127.0.0.1:5088/scalar` |
| OpenAPI | `http://127.0.0.1:5088/openapi/v1.json` |
| TCP | `127.0.0.1:5089` |
| MQTT | `127.0.0.1:1883`, topic `virex` |

## 驗證

```powershell
dotnet test Virex.NET.Integration.slnx
python -m mkdocs build --strict
```
