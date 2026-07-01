# ControlRunModes

`ControlRunModes` defines the allowed `runMode` values ​​for [SystemStartRequest](system-start-request.md).

## Values

| Value | Meaning |
| --- | --- |
| `continue` | Default run mode. |
| `single` | Single-run mode. |

Omitted or blank `runMode` will be normalized to `continue`.

## Use location

| Interface | Usage |
| --- | --- |
| RESTful API | `POST /api/system/start` request content. |
| TCP | Incoming `type: "start"` command frame. |
