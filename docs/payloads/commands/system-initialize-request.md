# SystemInitializeRequest

`SystemInitializeRequest` is the command frame for initializing the system.

## JSON

```json
{
  "type": "initialize"
}
```

## Field

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `type` | string | Yes | Must be `initialize`. |

## Use location

| Interface | Usage |
| --- | --- |
| RESTful API | `POST /api/system/initialize` uses no request body. |
| TCP | Incoming `type: "initialize"` command frame. |
