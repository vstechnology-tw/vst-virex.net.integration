# シミュレーターガイド

公開 REST、TCP、MQTT 契約のローカルシミュレーターです。取得画像と検査結果は模擬データです。イベントは実際の App やカメラの準備完了を証明しません。

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj --framework net10.0-windows
# ソースポリシーを明示的に有効化
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj --framework net10.0-windows -- --manage-operation-mode
```

## ウィンドウ操作

- **Connection Settings**：REST prefix、TCP port、MQTT host／port／topic を設定します。**Start Servers** または **Stop Servers** で、このウィンドウが所有するエンドポイントを起動または停止します。
- **ProductInfo**：Lot ID、Wafer ID、Recipe、Slot、Foup ID、Chamber ID を設定します。Ready で **Apply ProductInfo** を押して適用します。
- **State**：**Initialize**、**Deinitialize**、**Start Single**、**Start Continue**、**Stop** を提供します。Start には Ready、Stop には Running が必要です。Single 完了後は自動で Ready に戻ります。

## 画像取得と検査モード

**Start Single** または **Start Continue** の前に **Legacy (omitted)**、**captureOnly**、**captureAndInspect** を選択します。Run mode と inspection mode は独立しています。省略時は recipe の既定値を維持します。このシミュレーターでは、captureOnly は取得イベントを生成しますが、resultCreated は生成せず、画像や結果ファイルも保存しません。captureAndInspect は模擬検査結果も生成します。

## クエリと操作モード

recipe 一覧、現在の recipe、公開パラメーター、result detail を照会します。**Get result detail** の前に、大文字小文字を区別する正確な ResultId を入力します。現在の recipe がない場合や ID が不明な場合は公開クエリエラーを表示し、最新結果に置き換えません。

**Get operation mode** は適用済みの local/remote と managementEnabled を表示します。モードを選び **Apply mode** を押すと、受理後に表示が更新されます。管理は既定で無効です。起動引数 **--manage-operation-mode** で有効にすると、Local はローカル操作を、Remote は外部ライフサイクル操作を許可します。モード変更はライフサイクル状態を変更しません。

## 取得イベントと状態機械

**Capture Events** タブには最新の captureReady と captureCompleted の JobId、CaptureId、SourcesCount、timestamp が表示されます。captureCompleted と後続の resultCreated は別のイベントです。Event Log で順序を確認します。**State Machine** タブは実際の session 状態を表示します。

## キャンセルと終了

照会はウィンドウをブロックしません。**Cancel query** は現在の照会を取り消します。古い照会は新しい結果を上書きしません。ウィンドウを閉じると模擬実行を取り消して完了を待ち、所有するエンドポイントを停止します。ホスト終了は外部 Stop を偽装せず operation mode を変更しません。

## 既定のエンドポイント

| インターフェース | 既定値 |
| --- | --- |
| REST | `http://127.0.0.1:5088` |
| API ブラウザー | `http://127.0.0.1:5088/scalar` |
| OpenAPI | `http://127.0.0.1:5088/openapi/v1.json` |
| TCP | `127.0.0.1:5089` |
| MQTT | `127.0.0.1:1883`, topic `virex` |

## 検証

```powershell
dotnet test Virex.NET.Integration.slnx
python -m mkdocs build --strict
```
