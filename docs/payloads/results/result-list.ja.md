# ResultList

`ResultList` は、結果クエリの REST 応答ラッパーです。

## JSON

```json
{
  "items": [],
  "count": 0
}
```

## フィールド

|フィールド |タイプ |必須 |説明 |
| --- | --- | --- | --- |
| `items` | [ResultSummary](result-summary.ja.md) の配列 |はい |マッチング結果の概要。 |
| `count` |番号 |はい |返品された商品の数。 |

## 使用箇所

|インターフェース |使い方 |
| --- | --- |
| REST | `GET /api/results` 応答。 |

## サポートされているクエリ フィルタ

```text
waferID
lotID
recipe
```

複数のフィルターを AND で組み合わせます。
