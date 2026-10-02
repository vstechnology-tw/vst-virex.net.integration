# Per-start inspection mode

`SystemStartRequest.inspectionMode` is optional and independent of `runMode` (`single` / `continue`).

| Value | Behavior |
| --- | --- |
| omitted or `null` | Preserve the existing recipe behavior, including configured inspection bypass. |
| `captureOnly` | Acquire and save the usual images/diagnostics without executing inspection or publishing a fabricated OK/Final result. |
| `captureAndInspect` | Acquire and execute inspection for this run, overriding recipe inspection bypass for this run only. |

Values are case-sensitive. Empty, whitespace, unknown strings and non-string JSON values are invalid. Unknown/empty strings are retained through formatters/parsers and rejected as `invalid_inspection_mode`; invalid JSON types are rejected as `invalid_payload`. REST returns HTTP 400. No AML setting names or ECIDs are part of this contract.

```json
{"condition":"sample","runMode":"single","inspectionMode":"captureOnly"}
```

REST SDK: `client.StartAsync(new SystemStartRequest { RunMode = ControlRunModes.SingleRun, InspectionMode = InspectionModes.CaptureOnly })`.

MQTT SDK: `client.MqttCommands.StartWithOptionsAsync(new SystemStartRequest { InspectionMode = InspectionModes.CaptureAndInspect })`. The named method preserves the existing `StartAsync(condition, runMode, cancellationToken)` signature and source compatibility for null/default arguments.

TCP formatter: `TcpSocketEventFormatter.FormatStartCommand(condition, runMode, inspectionMode)`; the existing two-argument overload is unchanged. The start frame contains `type: "start"` and the same `inspectionMode` field. MQTT `commands/system/start` uses that field in `MqttCommandRequest` with its usual correlation ID.

The Simulator snapshots each accepted selection. `captureOnly` emits `imageGrabbed`, saves BMP/JPEG and a `.capture.json` diagnostic, returns to Ready after `single`, and continues capturing until Stop for `continue`. It does not emit `resultCreated`, add result history, or write inspection ResultDetail JSON. `captureAndInspect` and omission preserve the Simulator's existing normal result flow. A later start cannot inherit an earlier selection. Simulator artifacts are synthetic; this does not verify real camera or private App inspection behavior.

This change is stacked on the approved Integration #44 revision `e4982b367c7576674d7d250161a3ca1510230fe5`. Candidate packages include those read-only query APIs. App #281 consumes the new candidate and verifies its actual execution context separately.
