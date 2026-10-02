# Operation mode

Operation mode is independent of the SystemStatus lifecycle. Wire values are exactly `local` and `remote`. A mode does not initialize, start, stop or recover the system.

Management is a host opt-in, disabled by default. Queries return `{ "mode": "local", "managementEnabled": false }` initially. With management disabled, existing Initialize, Deinitialize, ProductInfo, Start and Stop behavior remains unchanged regardless of the displayed mode. Enabling management is a trusted deployment choice, unavailable through network commands.

When enabled, Local allows those commands from a trusted local entry point and rejects external commands with `operation_not_allowed`; Remote allows external commands and rejects local commands. Queries and mode switching remain available externally in either mode, including Local → Remote. An accepted switch returns the applied `operationMode` snapshot; a repeated switch succeeds without another changed event. Switching does not cancel an active run. Safety/fault recovery is internal and bypasses operator authority.

REST, TCP and MQTT connections are always external, including loopback. Request fields `source` or `operationSource` are rejected as `invalid_payload`. Mode control is operational arbitration, not authentication: hosts must separately restrict access to their network services.

| Transport | Query | Apply |
| --- | --- | --- |
| REST | GET /api/operation-mode | POST /api/operation-mode, `{"mode":"remote"}` |
| TCP NDJSON | `{"type":"operationMode"}` | `{"type":"setOperationMode","mode":"remote"}` |
| MQTT | commands/operation-mode/get | commands/operation-mode/set, `{"correlationId":"id","mode":"remote"}` |

REST apply returns CommandResponse (400 invalid mode/payload, 409 source/state refusal); TCP apply returns `commandResponse` with requestId, including rejection; MQTT uses the existing correlated response envelope. Null, omitted, blank, unknown or differently cased modes are invalid. Changed mode is observable as `operationModeChanged` on TCP and the MQTT child topic of that name. Events are live notifications; reconnecting clients query applied state.

SDK: VirexClient / VirexRestClient, VirexTcpEventClient and VirexMqttCommandClient expose GetOperationModeAsync and SetOperationModeAsync. Existing method signatures are retained. Contracts and Client remain netstandard2.0.

Simulator Core: construct `new SimulatorSession(root, operationManagementEnabled: true)` to opt in. Trusted host code uses the *FromSourceAsync methods with OperationSource.Local; invalid source enum values are refused. The existing constructor remains unmanaged. Simulator executable accepts `--manage-operation-mode`; existing buttons use the local entry point. It has no new mode UI. App UI/actual permission integration belongs to App #282 and must preserve these semantics. No AML acknowledgement, strings, EC or SV mappings are defined here.
