# Simulator Manual

Virex.NET Integration Simulator は、顧客側の連携開発で使用するローカルテストツールです。REST、TCP、MQTT エンドポイントを公開し、WaferInfo、状態遷移、結果サマリー、エラーイベントをシミュレートできます。

Repository root から起動します。

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
```

## Purpose

| Purpose | What to verify |
| --- | --- |
| Validate the SDK | `VirexClient` が状態を読み取り、WaferInfo を送信し、サイクルを開始し、結果サマリーを照会できることを確認します。 |
| Validate raw protocols | C# 以外のシステムが REST payload、TCP/NDJSON frame、MQTT topic をテストできるようにします。 |
| Simulate events | Production-compatible service に接続する前に、`status`、`wafer-info`、`result`、`error` イベントをローカルで送出します。 |

## App UI

次のスクリーンショットは、ガイド付きサンプルで使用するシミュレーター画面です。

<figure>
  <div style="position: relative; width: 100%; max-width: 1008px; aspect-ratio: 1008 / 658;">
    <img src="../assets/simulator-main-window.png" alt="Virex.NET Integration Simulator main window" style="display: block; width: 100%; height: 100%; object-fit: contain;">
    <span aria-label="Area 1" style="position: absolute; left: 3%; top: 12%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">1</span>
    <span aria-label="Area 2" style="position: absolute; left: 42%; top: 12%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">2</span>
    <span aria-label="Area 3" style="position: absolute; left: 82%; top: 12%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">3</span>
    <span aria-label="Area 4" style="position: absolute; left: 4%; top: 58%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">4</span>
    <span aria-label="Area 5" style="position: absolute; left: 89%; top: 20%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">5</span>
    <span aria-label="Area 6" style="position: absolute; left: 60%; top: 32%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">6</span>
    <span aria-label="Area 7" style="position: absolute; left: 89%; top: 38%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">7</span>
    <span aria-label="Area 8" style="position: absolute; left: 89%; top: 47%; width: 1.8rem; height: 1.8rem; border-radius: 999px; background: #1976d2; color: #fff; display: grid; place-items: center; font-weight: 700; font-size: 0.9rem; border: 2px solid #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">8</span>
  </div>
  <figcaption>番号マーカーは下の Area 表に対応します。</figcaption>
</figure>

| Area | Name | Purpose |
| --- | --- | --- |
| 1 | Connection Settings | REST prefix、TCP port、MQTT host/port/topic、result prefix。テスト前に確認します。 |
| 2 | WaferInfo | Lot ID、Wafer ID、Recipe ID、Slot、FOUP ID、Chamber ID。 |
| 3 | State | 現在の `initialized`、`processState`、`recipe` と主な操作ボタン。 |
| 4 | Event Log | サービス起動、WaferInfo 更新、サイクルイベント、結果、エラーなどの実行履歴。 |
| 5 | **Start Servers** | 最初に押すボタン。REST/TCP/MQTT サービスはこの後に利用可能です。 |
| 6 | **Apply WaferInfo** | WaferInfo fields 編集後、現在のテスト wafer context を適用します。 |
| 7 | **Start Cycle** | 完全なサイクル、状態遷移、結果サマリーをシミュレートします。 |
| 8 | **Emit Fake Result** / **Emit Error** | クライアント側処理のテスト用に、結果またはエラーイベントを手動送出します。 |

## Standard Operating Procedure

1. Simulator app を起動します。

   ```powershell
   dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
   ```

2. 接続設定を確認します。

   初回はデフォルト値を使用してください。

   | Setting | Default |
   | --- | --- |
   | REST | `http://127.0.0.1:5088/` |
   | TCP | `5089` |
   | MQTT broker | `127.0.0.1:1883` |
   | MQTT topic | `virex` |

3. **Start Servers** を押します。

   起動に成功すると、Event Log に REST listening、TCP listening、MQTT started/connected の記録が表示されます。SDK とサンプルクライアントはこの後に接続できます。

   REST 検証ページはこの時点で利用できます。

   ```text
   Scalar:       http://127.0.0.1:5088/scalar
   OpenAPI JSON: http://127.0.0.1:5088/openapi/v1.json
   ```

   Scalar を使うと、ブラウザーから status、wafer-info、control、results endpoint を呼び出せます。

4. まず `not_initialized` を検証します。

   **Initialize** を押す前に SDK または REST サンプルを実行します。サンプルが start を呼ぶと、期待される戻り値は `HTTP 409 not_initialized` です。これはサンプルが UI 状態を正しく反映していることを示します。

5. WaferInfo を入力して適用します。

   Lot ID、Wafer ID、Recipe ID、Slot、FOUP ID、Chamber ID を入力し、**Apply WaferInfo** を押します。またはサンプルから WaferInfo を送信します。Event Log はすべてのフィールドを 1 行で表示する必要があります。

   ```text
   WaferInfo updated from REST: lotId=LOT-RAW-REST-001, waferId=W01, recipeId=RCP-A, slot=1, foupId=FOUP-A, chamberId=CH-1
   ```

6. **Initialize** と **Start Cycle** を実行します。

   **Initialize** を押して initialized 状態にします。その後 **Start Cycle** を押すか、サンプルを続行します。Simulator は `capturing`、`inspecting`、`saving` を経由して `ready` に戻ります。

7. テストイベントを送出します。

   **Emit Fake Result** で結果処理をテストします。**Emit Error** でエラー処理をテストします。MQTT サンプル実行中は、これらのボタンが `virex/result` と `virex/error` を生成します。

8. テストを終了します。

   **Stop Servers** を押すか、シミュレーター画面を閉じます。

## シナリオ別テストフロー

| シナリオ | 条件 | UI SOP | 期待結果 |
| --- | --- | --- | --- |
| 通信サービスの起動 | Simulator app が開いており、サービスがまだ起動していない。 | REST/TCP/MQTT 設定を確認し、**Start Servers** を押します。 | Event Log に REST listening、TCP listening、MQTT connected/started が表示され、サンプルが接続できます。 |
| `not_initialized` | **Start Servers** は押したが、**Initialize** はまだ押していない。 | **Initialize** は押さず、C# SDK または REST サンプルの start 手順を実行します。 | コンソールに HTTP `409` / `not_initialized` が表示されます。これは接続失敗ではなく、期待される状態動作です。 |
| 通常の initialize と cycle | **Start Servers** を押し、状態が `ready`。 | **Initialize** を押し、Status が `initialized=True` であることを確認してから、サンプルの start を続行します。 | Status が `capturing`、`inspecting`、`saving`、`ready` の順に表示され、Event Log に結果送出が記録されます。 |
| WaferInfo 更新確認 | **Start Servers** を押している。 | UI の **Apply WaferInfo** を押すか、SDK/REST/TCP サンプルから WaferInfo を送信します。 | Event Log に `lotId`、`waferId`、`recipeId`、`slot`、`foupId`、`chamberId` が 1 行で表示されます。 |
| MQTT イベント観察 | MQTT サンプルが `virex/#` を購読している。 | **Apply WaferInfo**、**Initialize**、**Start Cycle**、**Emit Fake Result**、**Emit Error** を押します。 | コンソールに `wafer-info`、`status`、`result`、`error` topic が表示されます。 |
| 結果照会 | **Start Cycle** が完了している、または **Emit Fake Result** を押している。 | 現在の WaferInfo の `lotId` で SDK/REST サンプルから照会します。 | コンソールに result count が表示されます。0 の場合は query filter が WaferInfo と一致しているか確認します。 |

## Guided Validation Scenarios

### Scenario A: First SDK Connection Check

1. デフォルトの接続設定を維持します。
2. **Start Servers** を押します。
3. `samples/csharp-sdk` を実行します。
4. Event Log に WaferInfo、start、result の実行履歴が表示されることを確認します。

### Scenario B: 手動 WaferInfo テスト

1. テスト用 wafer context を入力します。
2. **Apply WaferInfo** を押します。
3. **Initialize** を押します。
4. **Start Cycle** を押し、結果サマリーを待ちます。

### Scenario C: 結果イベント処理

1. **Start Servers** を押します。
2. TCP または MQTT イベントリスナーを起動します。
3. **Emit Fake Result** を押します。
4. クライアントが結果サマリーを受信し、Event Log にイベントが記録されることを確認します。

### Scenario D: エラー処理

1. **Start Servers** を押します。
2. TCP または MQTT イベントリスナーを起動します。
3. **Emit Error** を押します。
4. クライアントがエラーメッセージを表示または記録することを確認します。

## クライアント検証

| クライアント | コマンド | 検証内容 |
| --- | --- | --- |
| SDK サンプル | `dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj` | 標準のガイド付きデモです。最初に `not_initialized` を確認し、**Initialize** を押して cycle と result query を完了します。 |
| Raw REST サンプル | `dotnet run --project samples\csharp-raw-rest\CSharpRawRestSample.csproj` | REST status、WaferInfo、start、result query を示します。 |
| Raw TCP サンプル | `dotnet run --project samples\csharp-raw-tcp\CSharpRawTcpSample.csproj` | 初期 frame と WaferInfo update event を確認します。 |
| Raw MQTT サンプル | `dotnet run --project samples\csharp-raw-mqtt\CSharpRawMqttSample.csproj` | UI 操作から `status`、`wafer-info`、`result`、`error` topic を確認します。 |

## Troubleshooting

| 症状 | 対処 |
| --- | --- |
| **Start Servers** が失敗する | REST prefix または TCP port が使用中か、MQTT broker が起動できるかを確認します。 |
| SDK が接続できない | **Start Servers** が押されていること、SDK `RestBaseUrl`、TCP host/port、MQTT host/port/topic が UI と一致することを確認します。 |
| `not_initialized` が表示される | **Initialize** がまだ押されていない場合は期待される結果です。**Initialize** を押し、Status を確認してサンプルを続行します。 |
| イベントを受信できない | まず Event Log に送出済みイベントがあるか確認し、クライアントが TCP または MQTT 経由で購読していることを確認します。 |
| 結果が返らない | 先に **Start Cycle** または **Emit Fake Result** を実行し、query filter が現在の WaferInfo と一致することを確認します。 |
