# Scoped control

`POST /api/control/scoped` is additive. Existing routes retain their existing behavior.
Use scoped control when a command must address the exact process and Workspace activation
previously observed in an event or EventCatalog. DataSourceCatalog does not contain the full
Workspace scope. A catalog read alone cannot protect a
subsequent legacy mutation from a Workspace switch.

```json
{
  "expectedTarget": {
    "appId": "runtime",
    "instanceId": "observed-process-instance",
    "workspaceScope": {
      "workspaceId": "11111111-1111-1111-1111-111111111111",
      "registrationId": "22222222-2222-2222-2222-222222222222",
      "activationId": "33333333-3333-3333-3333-333333333333"
    }
  },
  "command": "Start",
  "payload": { "requestId": "caller-created-request", "runMode": "single" }
}
```

AppId and InstanceId are exact, case-sensitive strings. All three Workspace GUIDs must
be nonempty. A new activation is required even when selecting A, then B, then A again.
The receiver must validate all five identity components and admit the action under the
same authority gate/lease that excludes Workspace changes. Validate before allocating a
job, changing parameters, starting acquisition, stopping work or beginning recovery.
An admitted action and its cleanup retain their original owner; cancellation of the
caller does not undo admitted work. Target identity is not authentication: licensing,
authorization, operation mode and existing state guards still apply.

| Command | Payload |
| --- | --- |
| Initialize | omitted, null or `{}` |
| Deinitialize | omitted, null or `{}` |
| Reset | omitted, null or `{}` |
| Start | existing `SystemStartRequest`; nonempty `requestId` required |
| Stop | existing `SystemStopRequest` |
| SetOperationMode | existing `SetOperationModeRequest`; `mode` is `local` or `remote` |
| SetProductInfo | existing `ProductInfo` |

Malformed/missing targets, unknown commands, duplicate/unknown properties and payloads
for another command return HTTP 400 `invalid_payload` without business effects. A valid
target that is no longer current returns HTTP 409 `target_mismatch`, also without
business effects. Other command failures retain the existing `CommandResponse` and
error behavior. Diagnostics may record rejected commands.

`ScopedControlJson.ReadRequest(string)` is the strict ingress parser.
`ScopedControlJson.ReadPayload<T>(ScopedControlRequest)` reads the original business DTO
after validation. The parser does not perform authorization or acquire an authority lease.

`VirexRestClient` exposes `InitializeScopedAsync`, `StartScopedAsync`, `StopScopedAsync`,
`ResetScopedAsync`, `DeinitializeScopedAsync`, `SetOperationModeScopedAsync` and
`SetProductInfoScopedAsync`. Each takes a `ControlTarget`, its original payload where
applicable, and an optional final `CancellationToken`. Each invocation snapshots its
target and payload, supports concurrent requests, and never alters shared default headers.
HTTP errors throw `VirexClientException`; cancellation propagates to the caller.

HTTP 404 or 405 means the addressed server does not support this route. The SDK does
**not** fall back to legacy control. Consumers must not refresh the target and silently
retry a mutation against a newly selected station. Legacy endpoints and optional headers
on old servers cannot provide this isolation guarantee.

The Simulator uses its existing session admission gate for scoped validation and
command execution. This is synthetic behavior evidence; it does not establish Runtime
wiring, hardware arming, external-trigger operation or full AML acceptance.
