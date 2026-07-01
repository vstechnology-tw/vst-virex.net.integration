# SystemDeinitializeRequest

`SystemDeinitializeRequest` is the command frame for deinitializing the system.

## JSON

```json
{
  "type": "deinitialize"
}
```

## Field

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `type` | string | Yes | Must be `deinitialize`. |

## Use location

| Interface | Usage |
| --- | --- |
| RESTful API | `POST /api/system/deinitialize` uses no request body. |
| TCP | Incoming `type: "deinitialize"` command frame. |
