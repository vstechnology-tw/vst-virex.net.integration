# C# Raw REST Sample

Guided demo for the public REST API using `HttpClient` directly.

## Simulator Prerequisites

- Start the simulator and keep REST at `http://127.0.0.1:5088`.
- Press **Start Servers**.
- Leave **Initialize** unpressed for the first negative check.

## UI SOP

1. Press **Start Servers**.
2. Run the sample.
3. Confirm the sample reads `/api/status`.
4. Confirm the sample shows `POST /api/control/start` returning `409 not_initialized`.
5. Press **Initialize** when prompted.
6. Let the sample send WaferInfo, start a cycle with `condition` and `runMode`, stop a second cycle with `reason`, and query results.

## Run

```powershell
dotnet run --project samples\csharp-raw-rest\CSharpRawRestSample.csproj
```

Optional base URL:

```powershell
dotnet run --project samples\csharp-raw-rest\CSharpRawRestSample.csproj -- http://127.0.0.1:5088
```

## Expected Output

- Initial status shows `initialized=False` when starting from a fresh simulator state.
- Start before **Initialize** returns HTTP `409` with `not_initialized`.
- Event Log shows `WaferInfo updated from REST: lotId=LOT-RAW-REST-001, waferId=W01, recipeId=RCP-A, slot=1, foupId=FOUP-A, chamberId=CH-1`.
- After **Initialize**, start with `{"condition":"golden-sample","runMode":"continue"}` returns HTTP `200`, stop with `{"reason":"operator-request"}` returns HTTP `200`, and the result query prints a count.
- Event Log shows `Start condition: golden-sample`, `Start run mode: continue`, and `Stopped. reason=operator-request`.

## Troubleshooting

- Server not started: press **Start Servers** and retry.
- `not_initialized`: expected before **Initialize**; press **Initialize** and continue.
- No result returned: press **Start Cycle** or **Emit Fake Result**, then query with the matching lot ID.
- No MQTT events: REST does not subscribe to MQTT; use a raw MQTT sample.
