# ResultList

`ResultList` is the RESTful API response wrapper for the result query.

## JSON

```json
{
  "items": [],
  "count": 0
}
```

## Field

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `items` | array of [ResultSummary](result-summary.md) | Yes | Matching result summaries. |
| `count` | number | Yes | Number of returned items. |

## Use location

| Interface | Usage |
| --- | --- |
| RESTful API | `GET /api/results` response. |

## Supported Query Filters

```text
waferID
lotID
recipe
```

Multiple filters are combined with AND.
