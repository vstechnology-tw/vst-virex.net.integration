# 快速開始

這個流程使用本機模擬器驗證核心整合路徑。

## 1. 啟動模擬器

從儲存庫根目錄執行：

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
```

保留預設端點設定，按 **Start Servers** 啟動 REST/TCP/MQTT 服務。

| 介面 | 預設值 |
| --- | --- |
| REST | `http://127.0.0.1:5088` |
| TCP | `127.0.0.1:5089` |
| MQTT | `127.0.0.1:1883`，topic 前綴 `virex` |

## 2. 執行 SDK 範例

開啟第二個終端機：

```powershell
dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj
```

範例會執行標準流程：

1. 讀取 `GET /api/status`。
2. 初始化系統。
3. 送出 `ProductInfo`。
4. 啟動執行。
5. 觀察執行完成事件。
6. 查詢結果。

## 3. 確認預期狀態

| 步驟 | 預期狀態 |
| --- | --- |
| 新啟動的模擬器 | `Uninitialized` |
| 初始化完成 | `Ready` |
| ProductInfo 更新完成 | `Ready` |
| 啟動命令已接受 | `Running` |
| 執行完成 | `Ready` |

## 4. 確認結果快照

結果應該包含 `Start` 被接受當下的 ProductInfo：

```json
{
  "waferID": "W01",
  "lotID": "LOT-001",
  "recipe": "RCP-A"
}
```

## 5. 選用通訊協定檢查

- REST 瀏覽器：`http://127.0.0.1:5088/scalar`
- MQTT：訂閱 `virex/#`
- 如果不用 C# SDK，可以執行原始 TCP 或原始 REST 範例。

## 完成條件

快速開始完成時應該符合：

- REST 狀態查詢回傳有效 `state`。
- ProductInfo 可以在 `Ready` 更新。
- Start 回傳 `Running`。
- 執行完成後狀態回到 `Ready`。
- 結果可以查詢，且符合 ProductInfo 快照。
