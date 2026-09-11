# 命令完成與錯誤處理（2.2.3）

REST 的已完成請求都有 HTTP response：JSON／runMode 錯誤為 400、狀態不允許
為 409、操作無法完成為 503。REST SDK 的 `VirexClientException` 保留 status 與 body。

MQTT 以 `correlationId` 對應 `responses/{correlationId}`。即使 payload 的欄位型別
錯誤，只要能讀出 correlationId，就回覆負面結果。JSON 完全損壞、無法辨認 ID 時
會發出拒絕事件，但沒有可靠的對應 response topic。SDK 查詢失敗會拋出
`VirexCommandException`，不會轉成空結果或預設狀態；損壞／缺漏的 response 也會失敗。

TCP 的 status、error、getProductInfo、results 查詢有直接回覆。2.2.3 SDK 會帶
可選的 `requestId`，伺服器在查詢回覆與拒絕事件中回傳它，SDK 不會把其他請求的
拒絕事件誤當成自己的失敗。舊伺服器的成功查詢仍相容；若無法對應拒絕，會由
`TimeoutMs` 限制等待時間。呼叫端取消則保留取消語意。

TCP `Send*Async` 只代表已送出資料，必須接收完成事件或 `commandRejected` 才知道
伺服器結果。新版更新產品命令為 `type: productInfo`；為相容舊 SDK，保留已知的
`productInfoChanged` 別名，其他未知 type 不會修改資料。

`errorChanged` 一定包含 `hasError`，表示目前的 runtime 錯誤。格式／狀態不允許或
操作失敗則透過 `commandRejected` 回報實際狀態與可用的 recovery context；不能把
失敗當成系統已解除初始化，也不能把查詢失敗當成「成功但没有資料」。

ProductInfo 完成後才發出 `productInfoChanged`，更新中會拒絕 Start。Start 接受時
保存產品資料快照，後續結果沿用該快照。Stop 需要 Running；支援來源準備的服務也可
接受 Stop 取消尚在準備中的 Start。Deinitialize 需要 Ready 或進行中／可重試的復原。

`imageGrabbed.captureId` 是取像群組識別碼，供後續 result summary 關聯。多來源事件
可共用 captureId，並用可選的 `sourceId`／`frameId` 區分個別影像。請忽略未知的可選
欄位。尚未取得任何影像即發生的錯誤結果，captureId 可能為空；影像／結果路徑應從
`resultCreated` 或儲存完成後的 results 查詢取得。

ProductInfo 的六個欄位為字串。舊送出端省略欄位時可使用預設值，但 null、object、
array 不等於合法字串；建議送出全部六個欄位。
