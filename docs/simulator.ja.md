# シミュレーター マニュアル

Virex.NET Integration Simulator は、顧客側の連携開発に使用するローカル テスト ツールです。REST、TCP、MQTT エンドポイントを公開し、WaferInfo、状態遷移、結果要約、エラー イベントをシミュレートできます。

リポジトリ ルートから起動します。

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
```

## 目的

| 目的 | 検証内容 |
| --- | --- |
| SDK を検証する | `VirexClient` が status を読み取り、WaferInfo を送信し、サイクルを開始し、結果要約をクエリできることを確認します。 |
| 生プロトコルを検証する | C# 以外のシステムが REST ペイロード、TCP/NDJSON フレーム、MQTT トピックをテストできるようにします。 |
| イベントをシミュレートする | 本番互換サービスへ接続する前に、status、wafer-info、result、error イベントをローカルで発行します。 |

## アプリ UI

次のスクリーンショットは、ガイド付きサンプルで使用するシミュレーター ウィンドウを示しています。

<figure>
  <div style="position: relative; width: 100%; max-width: 1008px; aspect-ratio: 1008 / 658;">
    <img src="../assets/simulator-main-window.png" alt="Virex.NET Integration Simulator のメイン ウィンドウ" style="display: block; width: 100%; height: 100%; object-fit: contain;">
    <span aria-label="Area 1" style="position: absolute; left: 3%; top: 12%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">1</span>
    <span aria-label="Area 2" style="position: absolute; left: 42%; top: 12%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">2</span>
    <span aria-label="Area 3" style="position: absolute; left: 82%; top: 12%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">3</span>
    <span aria-label="Area 4" style="position: absolute; left: 4%; top: 58%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">4</span>
    <span aria-label="Area 5" style="position: absolute; left: 89%; top: 20%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">5</span>
    <span aria-label="Area 6" style="position: absolute; left: 60%; top: 32%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">6</span>
    <span aria-label="Area 7" style="position: absolute; left: 89%; top: 38%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">7</span>
    <span aria-label="Area 8" style="position: absolute; left: 89%; top: 47%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">8</span>
  </div>
  <figcaption>番号付きマーカーは、下の領域表に対応します。</figcaption>
</figure>

| 領域 | 名前 | 目的 |
| --- | --- | --- |
| 1 | Connection Settings | REST prefix、TCP ポート、MQTT ホスト/ポート/トピック、result prefix。テスト前にこの領域を確認します。 |
| 2 | WaferInfo | Lot ID、Wafer ID、Recipe ID、Slot、FOUP ID、Chamber ID。 |
| 3 | State | 現在の `initialized`、`processState`、`recipe`、主な操作ボタン。 |
| 4 | Event Log | サーバー起動、WaferInfo 更新、サイクル イベント、結果、エラー、その他の活動。 |
| 5 | **Start Servers** | 最初に押すボタンです。この手順の後にだけ REST/TCP/MQTT サービスを利用できます。 |
| 6 | **Apply WaferInfo** | WaferInfo フィールドを編集した後、現在のテスト用ウェーハ コンテキストを適用します。 |
| 7 | **Start Cycle** | 完全なサイクル、状態遷移、結果要約をシミュレートします。 |
| 8 | **Emit Fake Result** / **Emit Error** | クライアント側の処理テスト用に、結果イベントまたはエラー イベントを手動で発行します。 |

## 標準操作手順

1. シミュレーター アプリを起動します。

   ```powershell
   dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
   ```

2. 接続設定を確認します。

   初回利用時は既定値のままにします。

   | 設定 | 既定値 |
   | --- | --- |
   | REST | `http://127.0.0.1:5088/` |
   | TCP | `5089` |
   | MQTT broker | `127.0.0.1:1883` |
   | MQTT topic | `virex` |

3. **Start Servers** を押します。

   起動に成功すると、REST listening、TCP listening、MQTT started/connected の記録が Event Log に書き込まれます。SDK とサンプル クライアントは、この手順の後にだけ接続できます。

   この手順の直後に、REST 検証ページを利用できます。

   ```text
   Scalar:       http://127.0.0.1:5088/scalar
   OpenAPI JSON: http://127.0.0.1:5088/openapi/v1.json
   ```

   Scalar を使用すると、ブラウザーから status、wafer-info、control、results エンドポイントを呼び出せます。

4. 先に `not_initialized` を検証します。

   **Initialize** を押す前に、SDK または REST サンプルを実行します。サンプルが start を呼び出すと、期待される戻り値は `HTTP 409 not_initialized` です。これは、サンプルが UI 状態を正しく反映していることを意味します。

5. WaferInfo を入力して適用します。

   Lot ID、Wafer ID、Recipe ID、Slot、FOUP ID、Chamber ID を入力して **Apply WaferInfo** を押すか、サンプルに WaferInfo を送信させます。Event Log には、すべてのフィールドが 1 行で表示される必要があります。

   ```text
   WaferInfo updated from REST: lotId=LOT-RAW-REST-001, waferId=W01, recipeId=RCP-A, slot=1, foupId=FOUP-A, chamberId=CH-1
   ```

6. **Initialize** と **Start Cycle** を実行します。

   **Initialize** を押して初期化状態にします。その後 **Start Cycle** を押すか、サンプルの続行に任せます。シミュレーターは `capturing`、`inspecting`、`saving` を経て `ready` に戻ります。

7. テスト イベントを発行します。

   結果処理をテストするには **Emit Fake Result** を使用します。エラー処理をテストするには **Emit Error** を使用します。MQTT サンプルの実行中にこれらのボタンを押すと、`virex/result` と `virex/error` が生成されます。

8. テストを終了します。

   **Stop Servers** を押すか、シミュレーター ウィンドウを閉じます。

## シナリオ別テスト フロー

| シナリオ | 条件 | UI 手順 | 期待される結果 |
| --- | --- | --- | --- |
| 通信サービスを開始する | シミュレーター アプリが開いており、サーバーは未開始。 | REST/TCP/MQTT 設定を確認してから **Start Servers** を押す。 | Event Log に REST listening、TCP listening、MQTT connected/started が表示され、サンプルが接続できる。 |
| `not_initialized` | **Start Servers** は押したが、**Initialize** は押していない。 | **Initialize** は押さない。C# SDK または REST サンプルの start 手順を実行する。 | コンソールに HTTP `409` / `not_initialized` が表示される。これは想定された状態動作であり、接続失敗ではない。 |
| 通常の初期化とサイクル | **Start Servers** が押され、状態は `ready`。 | **Initialize** を押し、Status が `initialized=True` であることを確認してから、サンプルの start を続行する。 | Status が `capturing`、`inspecting`、`saving`、`ready` と表示され、Event Log に結果発行が表示される。 |
| WaferInfo 更新検証 | **Start Servers** が押されている。 | UI で **Apply WaferInfo** を押す、または SDK/REST/TCP サンプルに WaferInfo を送信させる。 | Event Log に `lotId`、`waferId`、`recipeId`、`slot`、`foupId`、`chamberId` が 1 行で表示される。 |
| MQTT イベント観測 | MQTT サンプルが `virex/#` を購読している。 | **Apply WaferInfo**、**Initialize**、**Start Cycle**、**Emit Fake Result**、**Emit Error** を押す。 | コンソールに `wafer-info`、`status`、`result`、`error` トピックが表示される。 |
| 結果クエリ | **Start Cycle** が完了した、または **Emit Fake Result** が押された。 | SDK/REST サンプルを使い、現在の WaferInfo `lotId` でクエリする。 | コンソールに結果件数が表示される。0 の場合は、クエリ フィルターが WaferInfo と一致しているか確認する。 |

## ガイド付き検証シナリオ

### シナリオ A: 初回 SDK 接続確認

1. 既定の接続設定を維持します。
2. **Start Servers** を押します。
3. `samples/csharp-sdk` を実行します。
4. Event Log に WaferInfo、start、result の活動が表示されることを確認します。

### シナリオ B: 手動 WaferInfo テスト

1. テスト用ウェーハ コンテキストを入力します。
2. **Apply WaferInfo** を押します。
3. **Initialize** を押します。
4. **Start Cycle** を押し、結果要約を待ちます。

### シナリオ C: 結果イベント処理

1. **Start Servers** を押します。
2. TCP または MQTT イベント リスナーを開始します。
3. **Emit Fake Result** を押します。
4. クライアントが結果要約を受信し、Event Log にイベントが記録されることを確認します。

### シナリオ D: エラー処理

1. **Start Servers** を押します。
2. TCP または MQTT イベント リスナーを開始します。
3. **Emit Error** を押します。
4. クライアントがエラー メッセージを表示または記録することを確認します。

## クライアント検証

| クライアント パス | コマンド | 検証 |
| --- | --- | --- |
| SDK サンプル | `dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj` | 標準のガイド付きデモです。先に `not_initialized` を確認し、その後 **Initialize** を押してサイクルと結果クエリを完了します。 |
| Raw REST サンプル | `dotnet run --project samples\csharp-raw-rest\CSharpRawRestSample.csproj` | REST status、WaferInfo、start、結果クエリを示します。 |
| Raw TCP サンプル | `dotnet run --project samples\csharp-raw-tcp\CSharpRawTcpSample.csproj` | 初期フレームと WaferInfo 更新イベントを確認します。 |
| Raw MQTT サンプル | `dotnet run --project samples\csharp-raw-mqtt\CSharpRawMqttSample.csproj` | UI 操作からの status、wafer-info、result、error トピックを確認します。 |

## トラブルシューティング

| 症状 | 解決方法 |
| --- | --- |
| **Start Servers** が失敗する | REST prefix または TCP ポートが使用中でないか、MQTT ブローカーを開始できるか確認します。 |
| SDK が接続できない | **Start Servers** が押されていること、SDK の `RestBaseUrl`、TCP ホスト/ポート、MQTT ホスト/ポート/トピックが UI と一致していることを確認します。 |
| `not_initialized` が表示される | **Initialize** を押していない場合は想定どおりです。**Initialize** を押し、Status を確認してからサンプルを続行します。 |
| イベントを受信しない | まず Event Log に発行イベントがあるか確認し、その後クライアントが TCP または MQTT で購読/接続していることを確認します。 |
| 結果が返らない | 先に **Start Cycle** または **Emit Fake Result** を実行し、クエリ フィルターが現在の WaferInfo と一致していることを確認します。 |
