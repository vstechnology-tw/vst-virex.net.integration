# ペイロード リファレンス

このページでは、公開 Virex.NET 連携サーフェスで送受信される JSON 内容を説明します。顧客から見える DTO、ルート/トピックの対応、シミュレーターでの検証手順を扱います。非公開の検査ロジックや実行時内部情報は説明しません。

## JSON ルール

すべての REST、TCP、MQTT ペイロードは、同じ公開 JSON 命名規則を使用します。

| ルール | 動作 |
| --- | --- |
| プロパティ名 | `camelCase` としてシリアライズされます。 |
| null 値 | シリアライズされた JSON から省略されます。 |
| 受信プロパティ名 | 大文字小文字を区別せずに読み取ります。 |
| テキスト エンコード | UTF-8 JSON。 |

例:

```json
{
  "initialized": true,
  "processState": "ready",
  "recipe": "RCP-A"
}
```

`processState` は次のいずれかです。

| 値 | 意味 |
| --- | --- |
| `ready` | シミュレーターはアイドル状態で、次の操作を受け付けられます。 |
| `capturing` | 模擬キャプチャ手順が実行中です。 |
| `inspecting` | 模擬検査手順が実行中です。 |
| `saving` | 模擬保存手順が実行中です。 |

コマンド遷移と拒否される状態については、[状態遷移](state-machine.md)を参照してください。

## 方向マトリクス

| ペイロード | REST | TCP | MQTT |
| --- | --- | --- | --- |
| Status | `GET /api/status` からの送信応答 | `type: "status"` を持つ送信イベント | `virex/status` 上の送信イベント |
| Control status | 制御 POST ルートからの送信応答 | 使用しません | 使用しません |
| Error status | `GET /api/error` からの送信応答 | `type: "error"` を持つ送信イベント | `virex/error` 上の送信イベント |
| WaferInfo | `POST /api/wafer-info` への受信リクエスト、`GET /api/wafer-info` からの送信応答 | `type: "waferInfo"` を持つ受信コマンド、`type: "waferInfo"` を持つ送信イベント | `virex/wafer-info` 上の送信イベント |
| Result summary | `GET /api/results` 応答の `items[]` 内の項目 | `type: "result"` を持つ送信イベント | `virex/result` 上の送信イベント |
| REST result query response | `GET /api/results` が返す応答ラッパー | 使用しません | 使用しません |
| Start command | 制御ルート `POST /api/control/start` | `type: "start"` を持つ受信コマンド | 使用しません |
| Stop command | 制御ルート `POST /api/control/stop` | `type: "stop"` を持つ受信コマンド | 使用しません |

MQTT は送信専用です。MQTT ペイロードでは、トピックがイベントを識別するため `type` プロパティは不要です。

## WaferInfo

方向:

| トランスポート | 方向 |
| --- | --- |
| REST | `POST /api/wafer-info` で受信、`GET /api/wafer-info` で送信。 |
| TCP | 受信コマンドおよび送信イベント。 |
| MQTT | 送信イベントのみ。 |

送信されるタイミング:

| シナリオ | 動作 |
| --- | --- |
| ホストがウェーハ コンテキストを更新する | 模擬サイクルの開始またはクエリ前に、REST または TCP で WaferInfo を送信します。 |
| シミュレーターがウェーハ コンテキストを発行する | WaferInfo が変更されると、TCP と MQTT クライアントはイベントを受信します。 |

JSON 例:

```json
{
  "lotId": "LOT-001",
  "waferId": "W01",
  "recipeId": "RCP-A",
  "slot": "1",
  "foupId": "FOUP-A",
  "chamberId": "CH-1"
}
```

フィールド表:

| フィールド | 型 | 説明 |
| --- | --- | --- |
| `lotId` | string | 結果フィルタリングとイベント相関に使用する公開ロット識別子。 |
| `waferId` | string | 公開ウェーハ識別子。 |
| `recipeId` | string | 模擬ウェーハ コンテキストに関連付けられた公開レシピ識別子。 |
| `slot` | string | スロット識別子。 |
| `foupId` | string | FOUP 識別子。 |
| `chamberId` | string | チャンバー識別子。 |

シミュレーター/サンプル検証:

1. シミュレーターを起動し、**Start Servers** をクリックします。
2. C# SDK、raw REST サンプル、raw TCP サンプルで WaferInfo を送信するか、シミュレーターのフィールドを編集して **Apply WaferInfo** をクリックします。
3. シミュレーターの Event Log に、次のようにすべての公開フィールドが 1 行で出力されることを確認します。

```text
WaferInfo updated from REST: lotId=LOT-RAW-REST-001, waferId=W01, recipeId=RCP-A, slot=1, foupId=FOUP-A, chamberId=CH-1
```

## Status

方向:

| トランスポート | 方向 |
| --- | --- |
| REST | `GET /api/status` からの送信応答。 |
| TCP | `type: "status"` を持つ送信イベント。 |
| MQTT | `virex/status` 上の送信イベント。 |

送信されるタイミング:

| シナリオ | 動作 |
| --- | --- |
| クライアントが現在のシミュレーター状態を読む | REST が現在の status を返します。 |
| シミュレーター状態が変わる | TCP と MQTT クライアントが status イベントを受信します。 |

JSON 例:

```json
{
  "initialized": true,
  "processState": "ready",
  "recipe": "RCP-A"
}
```

フィールド表:

| フィールド | 型 | 説明 |
| --- | --- | --- |
| `initialized` | boolean | 模擬システムが初期化済みかどうか。 |
| `processState` | string | 現在の公開処理状態: `ready`、`capturing`、`inspecting`、`saving`。 |
| `recipe` | string | status が報告する現在の公開レシピ値。 |

シミュレーター/サンプル検証:

1. シミュレーターを起動し、**Start Servers** をクリックします。
2. C# SDK または raw REST サンプルを実行し、`GET /api/status` を読み取ることを確認します。
3. **Initialize** または **Start Cycle** をクリックし、TCP または MQTT サンプルが更新後の `initialized` または `processState` 値を持つ status イベントを出力することを確認します。

## Control Status

方向:

| トランスポート | 方向 |
| --- | --- |
| REST | 制御 POST ルートからの送信応答。 |
| TCP | 使用しません。 |
| MQTT | 使用しません。 |

送信されるタイミング:

| シナリオ | 動作 |
| --- | --- |
| クライアントが制御アクションを POST する | REST はアクション後の状態と公開メッセージを返します。 |

JSON 例:

```json
{
  "initialized": true,
  "processState": "ready",
  "recipe": "RCP-A",
  "message": "Initialized."
}
```

フィールド表:

| フィールド | 型 | 説明 |
| --- | --- | --- |
| `initialized` | boolean | 制御アクション後に模擬システムが初期化済みかどうか。 |
| `processState` | string | 制御アクション後の公開処理状態。 |
| `recipe` | string | 現在の公開レシピ値。 |
| `message` | string | 制御アクションの公開応答メッセージ。 |

シミュレーター/サンプル検証:

1. シミュレーターを起動し、**Start Servers** をクリックします。
2. REST 対応サンプルから `POST /api/control/initialize`、`POST /api/control/terminate`、`POST /api/control/start`、`POST /api/control/stop` を送信します。
3. サンプルが `initialized`、`processState`、`recipe`、`message` を含む JSON 応答を出力することを確認します。

## Error Status

方向:

| トランスポート | 方向 |
| --- | --- |
| REST | `GET /api/error` からの送信応答。 |
| TCP | `type: "error"` を持つ送信イベント。 |
| MQTT | `virex/error` 上の送信イベント。 |

送信されるタイミング:

| シナリオ | 動作 |
| --- | --- |
| クライアントが現在のエラー状態を読む | REST が現在の error status を返します。 |
| シミュレーターのエラーが変わる | TCP と MQTT クライアントが error イベントを受信します。 |

JSON 例:

```json
{
  "hasError": true,
  "message": "Simulated error.",
  "initialized": true,
  "processState": "ready",
  "recipe": "RCP-A"
}
```

フィールド表:

| フィールド | 型 | 説明 |
| --- | --- | --- |
| `hasError` | boolean | 現在エラーが有効かどうか。 |
| `message` | string | 公開エラー メッセージ。null の場合は省略されます。 |
| `initialized` | boolean | error status 時点の初期化状態。 |
| `processState` | string | error status 時点の公開処理状態。 |
| `recipe` | string | 現在の公開レシピ値。 |

シミュレーター/サンプル検証:

1. シミュレーターを起動し、**Start Servers** をクリックします。
2. TCP または MQTT サンプルを実行し、接続したままにします。
3. シミュレーターで **Emit Error** をクリックします。
4. サンプルが `hasError`、`message`、`initialized`、`processState`、`recipe` を含む error ペイロードを出力することを確認します。

## Result Summary

`ResultSummaryDto` は単一の結果要約項目です。REST はこれを `ResultListDto.items[]` に埋め込み、TCP と MQTT は直接発行します。

方向:

| トランスポート | 方向 |
| --- | --- |
| REST | `GET /api/results` 応答の `items[]` 内の項目。 |
| TCP | `type: "result"` を持つ送信イベント。 |
| MQTT | `virex/result` 上の送信イベント。 |

送信されるタイミング:

| シナリオ | 動作 |
| --- | --- |
| シミュレーターが結果を作成する | TCP と MQTT クライアントが結果要約イベントを受信します。 |
| クライアントが履歴の模擬結果をクエリする | REST が `ResultListDto` を返し、各 `items[]` 要素が 1 つの `ResultSummaryDto` になります。 |

JSON 例:

```json
{
  "resultId": "RID-1",
  "timestamp": "2026-06-20T15:30:12+08:00",
  "lotId": "LOT-001",
  "waferId": "W01",
  "recipeId": "RCP-A",
  "slot": "1",
  "foupId": "FOUP-A",
  "chamberId": "CH-1",
  "overallResult": "OK",
  "defectCount": 0,
  "imageRelativePath": "20260620/LOT-001/20260620_153012_W01.tiff",
  "resultRelativePath": "20260620/LOT-001/20260620_153012_W01.json",
  "imagePath": "/data/virex-results/20260620/LOT-001/20260620_153012_W01.tiff",
  "previewImagePath": "/data/virex-results/20260620/LOT-001/20260620_153012_W01.jpg",
  "resultPath": "/data/virex-results/20260620/LOT-001/20260620_153012_W01.json"
}
```

フィールド表:

| フィールド | 型 | 説明 |
| --- | --- | --- |
| `resultId` | string | 公開結果識別子。 |
| `timestamp` | string | 結果タイムスタンプ文字列。 |
| `lotId` | string | アクティブな WaferInfo からコピーされたロット識別子。 |
| `waferId` | string | アクティブな WaferInfo からコピーされたウェーハ識別子。 |
| `recipeId` | string | 結果に関連付けられたレシピ識別子。 |
| `slot` | string | アクティブな WaferInfo からコピーされたスロット識別子。 |
| `foupId` | string | アクティブな WaferInfo からコピーされた FOUP 識別子。 |
| `chamberId` | string | アクティブな WaferInfo からコピーされたチャンバー識別子。 |
| `overallResult` | string | 公開要約結果値。 |
| `defectCount` | number | 要約内のすべての欠陥カテゴリの合計数。 |
| `imageRelativePath` | string | 関連画像成果物の相対パス文字列。 |
| `resultRelativePath` | string | 関連結果成果物の相対パス文字列。 |
| `imagePath` | string | シミュレーターの Result prefix を適用した後の公開画像パス。 |
| `previewImagePath` | string | シミュレーターの Result prefix を適用した後の公開プレビュー画像パス。 |
| `resultPath` | string | シミュレーターの Result prefix を適用した後の公開結果パス。 |

結果要約は意図的に要約のみです。欠陥リスト、ダイ リスト、クロップ リスト、画像バイナリ、非公開の検査内部情報は含まれません。

シミュレーター/サンプル検証:

1. シミュレーターを起動し、**Start Servers** をクリックします。
2. WaferInfo を適用し、**Initialize** をクリックしてから、**Start Cycle** または **Emit Fake Result** をクリックします。
3. TCP または MQTT サンプルが `result` イベントを出力すること、または REST 対応サンプルで `GET /api/results` を呼び出せることを確認します。
4. 返された要約に WaferInfo 識別子と件数フィールドが含まれ、欠陥リスト、ダイ リスト、クロップ リスト、画像バイナリ、非公開の検査内部情報が含まれないことを確認します。

## REST Result Query Response

`ResultListDto` は結果クエリ用の REST 応答ラッパーです。各 `items[]` 要素は 1 つの `ResultSummaryDto` です。

方向:

| トランスポート | 方向 |
| --- | --- |
| REST | `GET /api/results` が返す応答ラッパー。 |
| TCP | 使用しません。 |
| MQTT | 使用しません。 |

このラッパーは REST 専用です。TCP と MQTT は単一の Result Summary を直接発行し、Result List は使用しません。

送信されるタイミング:

| シナリオ | 動作 |
| --- | --- |
| クライアントが結果要約をクエリする | REST は一致する要約項目と件数を含むリスト ラッパーを返します。 |

`GET /api/results` は任意のフィルターに対応します。

- `lotId`
- `waferId`
- `recipeId`

複数のフィルターを指定した場合は AND で結合されます。

クエリ例:

```text
GET /api/results
GET /api/results?lotId=LOT-001
GET /api/results?lotId=LOT-001&waferId=W01
GET /api/results?lotId=LOT-001&waferId=W01&recipeId=RCP-A
```

JSON 例:

```json
{
  "items": [
    {
      "resultId": "RID-1",
      "timestamp": "2026-06-20T15:30:12+08:00",
      "lotId": "LOT-001",
      "waferId": "W01",
      "recipeId": "RCP-A",
      "slot": "1",
      "foupId": "FOUP-A",
      "chamberId": "CH-1",
      "overallResult": "OK",
      "defectCount": 0,
      "imageRelativePath": "20260620/LOT-001/20260620_153012_W01.tiff",
      "resultRelativePath": "20260620/LOT-001/20260620_153012_W01.json",
      "imagePath": "/data/virex-results/20260620/LOT-001/20260620_153012_W01.tiff",
      "previewImagePath": "/data/virex-results/20260620/LOT-001/20260620_153012_W01.jpg",
      "resultPath": "/data/virex-results/20260620/LOT-001/20260620_153012_W01.json"
    }
  ],
  "count": 1
}
```

フィールド表:

| フィールド | 型 | 説明 |
| --- | --- | --- |
| `items` | array | `ResultSummaryDto` オブジェクトの配列。 |
| `count` | number | 返された結果要約項目の数。 |

シミュレーター/サンプル検証:

1. 一致する WaferInfo で模擬結果を作成します。
2. REST 対応サンプルから `GET /api/results` を呼び出します。
3. `count` が返された `items` の数と一致することを確認します。

## REST マッピング

| メソッドとルート | 方向 | ペイロード内容 | 備考 |
| --- | --- | --- | --- |
| `GET /api/status` | 送信応答 | `StatusDto` | 現在の公開状態を読み取ります。 |
| `GET /api/error` | 送信応答 | `ErrorStatusDto` | 現在の公開エラー状態を読み取ります。 |
| `GET /api/wafer-info` | 送信応答 | `WaferInfo` | 現在の公開ウェーハ コンテキストを読み取ります。 |
| `POST /api/wafer-info` | 受信リクエスト | `WaferInfo` | 現在の公開ウェーハ コンテキストを更新します。 |
| `POST /api/control/initialize` | 送信応答 | `ControlStatusDto` | シミュレーター状態を初期化します。 |
| `POST /api/control/terminate` | 送信応答 | `ControlStatusDto` | シミュレーター状態を終了します。 |
| `POST /api/control/start` | Optional inbound `ControlStartRequest`; outbound response `ControlStatusDto` | `condition` is optional. `runMode` is optional and defaults to `continue`; supported values are `continue` and `single`. |
| `POST /api/control/stop` | Optional inbound `ControlStopRequest`; outbound response `ControlStatusDto` | `reason` is optional and may be omitted or blank. |
| `GET /api/results` | 送信応答 | `ResultListDto` | 任意フィルター: `lotId`、`waferId`、`recipeId`。複数フィルターは AND で結合されます。 |

REST 検証:

1. シミュレーターを起動し、**Start Servers** をクリックします。
2. C# SDK、C# raw REST、または C++ raw REST サンプルを実行します。
3. サンプルが status を読み取り、WaferInfo を POST し、制御アクションを実行し、上記の公開ペイロードで結果要約をクエリすることを確認します。

## TCP マッピング

TCP メッセージは JSON オブジェクトです。TCP 送信イベントには `type` プロパティが含まれます。TCP 受信コマンドは次の形状を使用します。

| 受信コマンド | 方向 | JSON 例 | 備考 |
| --- | --- | --- | --- |
| WaferInfo | シミュレーターへの受信 | `{"type":"waferInfo","lotId":"LOT-001","waferId":"W01","recipeId":"RCP-A","slot":"1","foupId":"FOUP-A","chamberId":"CH-1"}` | 従来の WaferInfo フレームでは `type` を省略できます。パーサーは WaferInfo JSON パーサーへフォールバックします。 |
| Start | シミュレーターへの受信 | `{"type":"start","condition":"golden-sample","runMode":"continue"}` | `condition` is optional. `runMode` defaults to `continue`; legacy `{"type":"start"}` remains valid. |
| Stop | シミュレーターへの受信 | `{"type":"stop","reason":"operator-request"}` | `reason` is optional; legacy `{"type":"stop"}` remains valid. |

TCP 送信イベント:

| イベント type | 方向 | ペイロード内容 |
| --- | --- | --- |
| `status` | シミュレーターから送信 | `StatusDto` と `type`。 |
| `waferInfo` | シミュレーターから送信 | `WaferInfo` と `type`。 |
| `result` | シミュレーターから送信 | `ResultSummaryDto` と `type`。 |
| `error` | シミュレーターから送信 | `ErrorStatusDto` と `type`。 |

TCP 検証:

1. シミュレーターを起動し、**Start Servers** をクリックします。
2. raw TCP サンプルを実行します。
3. 初期 status フレームと WaferInfo フレームが出力されることを確認します。
4. サンプルに WaferInfo フレームを送信させ、シミュレーターの Event Log に TCP WaferInfo 更新が表示されることを確認します。

## MQTT マッピング

MQTT は送信専用です。すべてのシミュレーター イベントを観測するには、ベース トピックのワイルドカードを購読します。

```text
virex/#
```

| トピック | 方向 | ペイロード内容 | 備考 |
| --- | --- | --- | --- |
| `virex/status` | シミュレーターから送信 | `StatusDto` | ペイロードに `type` は不要です。 |
| `virex/wafer-info` | シミュレーターから送信 | `WaferInfo` | ペイロードに `type` は不要です。 |
| `virex/result` | シミュレーターから送信 | `ResultSummaryDto` | ペイロードに `type` は不要です。 |
| `virex/error` | シミュレーターから送信 | `ErrorStatusDto` | ペイロードに `type` は不要です。 |

MQTT 検証:

1. シミュレーターを起動し、**Start Servers** をクリックします。
2. raw MQTT サンプルを実行し、購読したままにします。
3. **Apply WaferInfo**、**Initialize**、**Start Cycle**、**Emit Fake Result**、**Emit Error** をクリックします。
4. サンプルが `wafer-info`、`status`、`result`、`error` に対応するトピックと JSON ペイロードを出力することを確認します。
