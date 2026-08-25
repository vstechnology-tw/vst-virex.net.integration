# バージョン

公開パッケージとプロトコルの変更にはセマンティックバージョニングを使用します。

|バージョン |意味 |
| --- | --- |
| `1.0.0` |最初の公開統合契約。 |
| `1.1.0` |追加的なフィールド、エンドポイント、トピック、またはイベント。 |
| `2.0.0` |データのコンテンツ、ルート、トピック、または動作の重大な変更。 |

## バージョンのルール

- マイナー バージョンでは、公開 JSON フィールドが削除されたり、名前が変更されたりしません。
- マイナー バージョンでは追加フィールドが追加される場合があります。
- 重大な変更にはメジャー バージョンが必要です。
- シミュレーター、SDK、ドキュメント、および契約テストは同期する必要があります。
- [ペイロードリファレンス](payloads.ja.md) は、公開ペイロード モデル、ルート、トピック、イベント構造と一致している必要があります。

## 公開済みリリースノート

以下はお客様向けに公開されたバージョンの一覧です。各見出しは対応する GitHub Release にリンクしています。

### [v2.2.2](https://github.com/vstechnology-tw/vst-virex.net.integration/releases/tag/v2.2.2) — 2026-08-25

- 公開バージョンが 2.1.1 のままだったため、未公開の 2.2.1 候補で準備した復旧機能を 2.2.2 として公開します。
- `statusChanged`、`errorChanged`、`commandRejected` に、再試行可能な Deinitialize ガイダンス、安定したエラーコード、時刻、発生元、フェーズ、サニタイズ済み詳細を含む共通の追加型復旧エンベロープを追加します。
- 同時に発生するシミュレーター復旧遷移を直列化し、`imageGrabbed`、`captureId`、永続化されたシミュレーター成果物を維持します。
- 現行のシミュレーター ZIP、4 言語のドキュメント PDF、Mermaid 描画修正を含みます。
### [v2.1.1](https://github.com/vstechnology-tw/vst-virex.net.integration/releases/tag/v2.1.1) — 2026-08-17

- イベントモデル、TCP、MQTT、C# SDK、シミュレーターに公開 `imageGrabbed` イベントを追加しました。
- `imageGrabbed` と後続の `resultCreated` を関連付ける安定した `captureId` を追加しました。
- シミュレーターは `resultCreated` のパスを通じてテスト用の画像、プレビュー、結果ファイルを提供します。
- シミュレーターの診断情報、トランスポート サンプル、お客様向けドキュメントを更新しました。

### [v2.1.0](https://github.com/vstechnology-tw/vst-virex.net.integration/releases/tag/v2.1.0) — 2026-07-01

- RESTful API、TCP Socket、MQTT のコマンドとクエリの対応を完了しました。
- ステータス、エラー、ProductInfo、ライフサイクルコマンド、結果クエリの MQTT コマンド/レスポンスを追加しました。
- ステータス、エラー、ProductInfo、結果クエリの TCP フレームを追加しました。
- C#、Python、C++ のサンプルを同じ 13 ステップの統合フローに揃えました。
- Contracts、Client パッケージと .NET Framework 4.8、.NET 8、.NET 10 のシミュレーターを公開しました。

### [v2.0.3.1](https://github.com/vstechnology-tw/vst-virex.net.integration/releases/tag/v2.0.3.1) — 2026-06-30

- シミュレーターのみのリリースです。
- TCP の initialize と deinitialize コマンドを文書化し、実装しました。
- シミュレーターの結果画像と公開ドキュメントを `.tiff` から `.bmp` に更新しました。
- インストール ドキュメントと OpenAPI メタデータを更新しました。

### [v2.0.3](https://github.com/vstechnology-tw/vst-virex.net.integration/releases/tag/v2.0.3) — 2026-06-28

- 最新の main からパッケージとシミュレーターを再公開しました。
- 公開内容に ProductInfo コントラクトの更新を含めました。

### [v2.0.2](https://github.com/vstechnology-tw/vst-virex.net.integration/releases/tag/v2.0.2) — 2026-06-27

- 公開 `WaferInfo` コントラクトを `ProductInfo` に変更しました。
- ProductInfo ベースのライフサイクル動作とシミュレーター対応を追加しました。
- RESTful API、TCP、MQTT、サンプル、多言語ドキュメントを更新しました。

### [v2.0.1](https://github.com/vstechnology-tw/vst-virex.net.integration/releases/tag/v2.0.1) — 2026-06-23

- 初期 2.0 公開統合コントラクトとシミュレーター配布パッケージを公開しました。
- ドキュメントを MkDocs Material に移行し、英語、繁体字中国語、日本語、韓国語を提供しました。
- Result Summary と REST 結果クエリ応答の違いを明確にしました。
- 結果パスと payload フィールド、MQTT の既定値、OpenAPI/Scalar 検証、TCP フレーム動作を更新しました。

## GitHub Release ページのない履歴タグ

`v1.0.0` と `v1.0.1` は repository tag ですが、GitHub に公開 Release ページや release note の記録はありません。最初に公開された GitHub Release は `v2.0.1` です。