# Live and result data sources

The 2.4.0 candidate adds general REST data-source queries to Contracts, Client and Simulator. Each App publishes its own exact source names. The SDK accepts new names without a name enum or whitelist. Production App support needs the matching implementation and separate validation.

| GET route | `VirexClient` / `VirexRestClient` method | Response |
| --- | --- | --- |
| `/api/data-sources` | `GetDataSourcesAsync(token)` | `DataSourceCatalog` |
| `/api/data-sources/value?name=...` | `ReadDataSourceAsync(name, cancellationToken: token)` | Live `DataSourceValue` |
| `/api/data-sources/value?name=...&resultId=...` | `ReadDataSourceAsync(name, resultId, token)` | Committed result `DataSourceValue` |
| `/api/data-sources/current-result` | `GetCurrentResultAsync(token)` | `ResultReference`, containing identity only |

All queries are read-only and support GET only. Catalog/current-reference routes accept no query parameters. The value route requires one nonblank `name`, at most one nonblank `resultId`, and no other parameters. Names and IDs are exact, ordinal, case-sensitive strings; whitespace inside a nonblank name/ID is significant. `RestRoutes.DataSourceValue` URL-encodes them as query values without trimming.

`DataSourceCatalog` contains `appId`, `instanceId`, read `timestamp` and `sources`. Each descriptor has a free-form `name`, `kind` (`live` or `result`), JSON `type`, and `availability`. Supported types are `string`, `boolean`, `integer` (Int64), `number` (finite), `object` and `array`. Availability is `available`, `no_current_result`, `source_unavailable` or `source_value_missing`. Result availability refers to the current work, rather than retention of explicit historical IDs; an available source can still fail if its artifact becomes unreadable.

`DataSourceValue` contains the exact `name`, `kind`, `type`, owned JSON `value`, read `timestamp`, actual App `state`, `appId` and `instanceId`. A live query reports the value and state at the read; it accepts no `resultId`. A result query requires an explicit `resultId` and returns that committed result's `jobId`, `captureId` and optional `requestId`. Null, missing or mismatched JSON values are failures. The returned `JsonElement` remains valid after the response document is disposed. These queries require no caller-managed JSON document; the caller owns any injected `HttpClient`.

The Simulator provides `system.state` (live string), `result.findings` (result array) and `result.detail` (result object). The result sources use the existing public `ResultDetail`, `ResultSummary` and Findings projection. Empty Findings is a real valid result. Host code can configure explicit synthetic live sources with `ConfigureLiveDataSourceAsync`, or custom result names over `detail`, `summary` or `findings` with `ConfigureResultDataSourceAsync`. Configure while Uninitialized/Ready; no live/result name collision is allowed. Configuration copies JSON and rejects a value that does not match its declared type. These are synthetic test producers, without customer IDs, conversion formulas or private result models.

When Start is accepted under the admission gate, current result availability immediately becomes `no_current_result`. This includes CaptureOnly jobs. Rejected Start or a request cancelled before acceptance preserves the preceding current result. Only a committed result of the accepted work becomes current. Stop, cancellation and old timer callbacks cannot resurrect another work. `GetCurrentResultAsync` returns `no_current_result` until a result commits; it never returns the preceding job. CaptureOnly has no committed result. Reading an explicit historical ID does not change the current reference.

The Simulator retains up to 100 committed results and 100 recent capture identities per session. A retained known capture without a committed result returns `result_not_committed`; unknown/expired IDs return `result_not_found`. A new Simulator instance has a new `instanceId` and no current/history index. App identity plus instance/work/capture/result identity is required when correlating Start, TCP `resultCreated`, current reference and a value. Lot/Wafer labels alone do not identify a result.

| HTTP / SDK | `QueryErrorCodes` | Meaning |
| --- | --- | --- |
| 400 / 405 | `InvalidQuery` | Invalid selector/parameters, missing explicit ResultId, live query with ResultId, or non-GET |
| 404 | `UnknownSource` | Exact source name is not published |
| 409 | `NoCurrentResult` | Current accepted work has no committed result |
| 404 | `ResultNotFound` | Result ID is unknown or expired |
| 409 | `ResultNotCommitted` | Known capture has no committed result |
| 409 | `SourceUnavailable` | Producer is unavailable |
| 409 | `SourceValueMissing` | Producer has no value |
| 410 | `ResultDeleted` | A retained committed artifact was deleted |
| 503 | `QueryFailed` | Artifact read, schema or committed identity failed |
| SDK | `ApiFailed` | Transport failure/timeout, malformed API failure, or invalid success response |

New SDK data-source methods throw `VirexQueryException` with typed `Error`, `ErrorCode`, `StatusCode` and `ResponseBody`. StatusCode is zero when there was no usable HTTP query response. Caller cancellation remains `OperationCanceledException`; API timeout is `ApiFailed`. Invalid local name/ID input throws `ArgumentException`. Queries never substitute zero, empty success, an old result or fake OK, and do not change `/api/error` or the machine state.

```csharp
var catalog = await client.GetDataSourcesAsync(cancellationToken);
var live = await client.ReadDataSourceAsync("system.state", cancellationToken: cancellationToken);
var current = await client.GetCurrentResultAsync(cancellationToken); // Identity only.
var detail = await client.ReadDataSourceAsync("result.detail", current.ResultId, cancellationToken);
// Compare AppId/InstanceId/JobId/CaptureId/ResultId with the associated Start/event.
```
