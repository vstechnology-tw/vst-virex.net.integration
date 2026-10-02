# 操作模式

操作模式獨立於 SystemStatus 生命週期，wire 值嚴格使用 `local`／`remote`。切換模式不會初始化、啟動、停止或復原系統。

主機須明確 opt-in 啟用管理；預設停用。未啟用時，既有 Initialize／Deinitialize／ProductInfo／Start／Stop 行為不變。查詢回傳 mode 與 managementEnabled；啟用設定不能由網路命令變更。

啟用後，Local 允許可信本地入口、拒絕外部控制命令；Remote 允許外部入口、拒絕本地控制命令，錯誤為 `operation_not_allowed`。查詢與模式切換在兩種模式都允許外部呼叫，包含 Local→Remote。切換成功代表實際套用，回傳 operationMode 快照；重複切換成功但不重發 changed 事件。切換不取消已開始工作；內部安全／故障復原不受操作權阻擋。

REST／TCP／MQTT 一律視為外部來源，loopback 亦同；payload 提供 source 或 operationSource 回傳 invalid_payload。操作模式是操作權仲裁，網路服務的身分驗證與存取限制由主機另行處理。

| 傳輸 | 查詢 | 切換 |
| --- | --- | --- |
| REST | GET /api/operation-mode | POST /api/operation-mode，`{"mode":"remote"}` |
| TCP NDJSON | `{"type":"operationMode"}` | `{"type":"setOperationMode","mode":"remote"}` |
| MQTT | commands/operation-mode/get | commands/operation-mode/set，附 correlationId 與 mode |

REST 非法模式／payload 為400、來源／狀態拒絕為409；TCP 切換回覆 commandResponse 含 requestId 與套用快照；MQTT 沿用 correlationId 回覆。模式省略、null、空白、未知、大小寫不同皆拒絕。TCP／MQTT 新增 operationModeChanged；事件為即時通知，重連後須查詢目前模式。

VirexClient／VirexRestClient／VirexTcpEventClient／VirexMqttCommandClient 新增 GetOperationModeAsync、SetOperationModeAsync，保留原簽名與 netstandard2.0。

Simulator Core 以 `new SimulatorSession(root, operationManagementEnabled: true)` 啟用；可信主機用 *FromSourceAsync 與 OperationSource.Local，非法來源 enum 拒絕。原 constructor 維持停用。Simulator 執行檔使用 `--manage-operation-mode` 啟用，既有按鈕透過本地入口；不新增模式 UI。真實 App 操作权／UI 接續 #282；本卡不定義 AML Ack、字串、EC 或 SV。
