# 疑難排解

## REST 要求失敗

檢查：

- 模擬器或正式端點已啟動。
- REST 基底 URL 正確。
- 防火牆允許設定的連接埠。
- 路由列在 [REST API](rest-api.zh-Hant.md)。

## 命令回傳 `invalid_state`

命令已到達服務，但在目前狀態不合法。

範例：

- `Start` 只在 `Ready` 合法。
- `Stop` 只在 `Running` 合法。
- `SetProductInfo` 只在 `Ready` 合法。
- `Initialize` 只在 `Uninitialized` 合法。
- `Deinitialize` 只在 `Ready` 合法。

先讀 `GET /api/status`，等狀態允許後再送命令。

## 沒有回傳結果

檢查：

- 執行已啟動並完成。
- 結果查詢篩選條件符合啟動當下的 ProductInfo 快照。
- 篩選條件使用 `waferID`、`lotID`、`recipe`。

## TCP 事件遺失

檢查：

- TCP host/port 正確。
- 用戶端保持 socket 開啟。
- 每個傳入資料框以 `\n` 結尾。
- 用戶端正在解析目前事件名稱，例如 `statusChanged` 與 `resultCreated`。

## MQTT 事件遺失

檢查：

- Broker host/port 正確。
- 訂閱符合 topic 前綴，例如 `virex/#`。
- MQTT 只用於傳出事件。
- 用戶端監聽文件定義的 topic 名稱，例如 `productInfoChanged`。

## 文件預覽與 GitHub Pages 不一致

本機使用 MkDocs：

```powershell
python -m mkdocs serve --dev-addr 127.0.0.1:8000
```

GitHub Pages workflow 會執行：

```powershell
python -m mkdocs build --strict
```
