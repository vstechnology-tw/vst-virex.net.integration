# Recipe and result detail queries

These additive REST queries are available in the version 2.3.0 SDK and Simulator. Existing status, ProductInfo, result lists, commands and events keep their contracts. No new MQTT/TCP commands are introduced. App support requires the corresponding App implementation; an old App returning HTTP 404 is a failure, never an empty successful query.

| GET route | C# method (VirexRestClient and VirexClient) | 200 body |
| --- | --- | --- |
| `/api/recipes` | `GetRecipesAsync()` | `RecipeList`: `items` of `RecipeInfo`, `count` |
| `/api/recipes/current` | `GetCurrentRecipeAsync()` | `RecipeInfo`: `recipe`, `revision` |
| `/api/recipes/current/parameters` | `GetCurrentRecipeParametersAsync()` | `RecipeParameters`: `recipe`, `revision`, `groups` |
| `/api/results/{resultId}` | `GetResultDetailAsync(resultId)` | `ResultDetail`: `schemaVersion`, `resultId`, `summary`, `findings` |

All four endpoints are read-only: no machine state changes, command rejection or error events. They accept no query parameters and no writes (`405`, `Allow: GET`). SDK methods accept a cancellation token and propagate cancellation and HTTP errors. The caller owns an injected HttpClient. Empty, malformed, incomplete or mismatched success bodies are failures, not substitute DTOs. A valid empty recipe list or zero Findings is allowed.

## Loaded recipe and parameters

`current` means actually loaded, rather than requested `ProductInfo.recipe`. `revision` is an opaque identifier for that recipe configuration. Each parameters response is one internally consistent snapshot. Separate calls are not a transaction: compare both `recipe` and `revision`, and retry both queries if they differ.

Each group has a nonempty `key` and `parameters`. Each parameter has a nonempty `key`, `type` and a JSON scalar `value`. Supported types are `string`, `boolean`, `integer` (Int64), and `number`; value must match type. Group/key names are generic, with no AML or customer token mapping. Empty groups/parameter arrays are valid when the loaded recipe has no public parameters.

```json
{"recipe":"RCP-A","revision":"simulator-v1","groups":[{"key":"simulator","parameters":[{"key":"cycleDelayMs","type":"integer","value":1000}]}]}
```

Simulator initially lists `RCP-A` and `RCP-DEMO`; it preserves existing acceptance of arbitrary Recipe names and adds successfully loaded names to the catalog. Initialize and successful ProductInfo update commit a loaded snapshot; cancellation preserves the previous snapshot. Successful deinitialization clears it. Transitional states return `query_not_ready`. The sample parameter describes the Simulator's 1000 ms cycle interval, not camera or inspection settings.

## Exact result detail

Use the precise case-sensitive ResultId from `ResultSummary` or `resultCreated`. URL-encode it as one path segment (`RestRoutes.ResultDetail(id)` does this). Whitespace within a nonempty ID is significant; SDK does not trim it. Empty or dot-only (`.`/`..`) IDs are rejected locally. The ID never becomes a filesystem path. Repeated Lot/Wafer results remain distinct; querying one ID never returns another or selects the newest result.

`schemaVersion: 1` versions the Integration envelope, independently of the App public JSON file schema. `resultId` must match both the requested ID and `summary.resultId`. `summary` retains existing [ResultSummary](payloads/results/result-summary.md) metadata. Required `findings` may be empty for a valid result. Each Finding projects existing public fields:

| Field | Meaning |
| --- | --- |
| `findingId`, `kind`, `label` | Public finding identity, category and label |
| `score` | Optional numeric score; absent/null means unavailable |
| `productPolygon` | Ordered points `{ "xmm": 1.5, "ymm": 2.5 }` in the public product frame, in millimeters |
| `diagnosticImageIds` | Public diagnostic image identifiers |

No additional measurements are inferred. Simulator generates valid zero-Findings results. Saved Simulator JSON retains all original top-level summary fields and adds a `detail` envelope. Reads use the exact committed artifact for the retained result index (up to 100 results per session). No query falls back to a cached summary if the detail artifact is unreadable or deleted.

SDK responses and Simulator detail artifacts use the shared `QueryPayloadJson` reader to check required nested fields, JSON types and null elements. Missing `parameters`, Finding fields or point coordinates fail; they never become empty arrays or invented zero coordinates. Explicit empty arrays, actual `(0,0)` points and absent/null optional score remain valid.

## Query failures

Non-200 responses use `QueryError`, e.g. `{"errorCode":"no_current_recipe","message":"No recipe is currently loaded."}`. Read failures do not change `/api/error` or system state.

| HTTP | `errorCode` | Meaning |
| --- | --- | --- |
| 400 | `invalid_query` | Unsupported query parameters or empty identifier |
| 405 | `invalid_query` | GET only |
| 409 | `no_current_recipe` | No recipe loaded, or unloaded |
| 409 | `query_not_ready` | Snapshot unavailable during loading, update or deinitialization/recovery |
| 404 | `result_not_found` | Unknown ID, not committed/published, or outside retained history; no existence claim |
| 410 | `result_deleted` | Indexed committed result artifact deleted |
| 503 | `query_failed` | Snapshot read failed, corrupt or inconsistent artifact |

SDK HTTP failures throw `VirexClientException` with actual `StatusCode` and `ResponseBody`, including old Apps' 404 and plain-text bodies. Invalid successful payloads or unsupported detail schema versions throw JSON/InvalidOperation exceptions. Connection failures remain HttpRequestException; cancellation remains OperationCanceledException.

## C# example

```csharp
var recipes = await client.GetRecipesAsync();
var current = await client.GetCurrentRecipeAsync();
var parameters = await client.GetCurrentRecipeParametersAsync();
if (current.Recipe != parameters.Recipe || current.Revision != parameters.Revision)
    throw new InvalidOperationException("Recipe changed; retry both snapshot queries.");
foreach (var summary in (await client.QueryResultsAsync(lotID: "LOT-001")).Items)
{
    var detail = await client.GetResultDetailAsync(summary.ResultId);
    Console.WriteLine($"{detail.ResultId}: {detail.Findings.Length} findings");
}
```

Run the [complete SDK sample](samples.md) with explicit `--queries` to exercise all four APIs. Default execution retains the old-App-compatible 13-step flow.
