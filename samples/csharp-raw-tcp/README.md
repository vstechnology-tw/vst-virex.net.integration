# C# Raw TCP Sample

Guided demo for the TCP/NDJSON event socket.

## Simulator Prerequisites

- Start the simulator and keep TCP at `127.0.0.1:5089`.
- Press **Start Servers**.
- Press **Initialize** before the command demo so `start` can run.

## UI SOP

1. Press **Start Servers** and **Initialize**.
2. Run the sample.
3. Confirm the sample prints the initial status frame and initial WaferInfo frame.
4. Let the sample send a `waferInfo` frame.
5. Let the sample send `{"type":"start","condition":"golden-sample","runMode":"continue"}` and `{"type":"stop","reason":"operator-request"}`.
6. Check Event Log for the full TCP WaferInfo update, start condition, and stop reason.

## Run

```powershell
dotnet run --project samples\csharp-raw-tcp\CSharpRawTcpSample.csproj
```

Optional host and port:

```powershell
dotnet run --project samples\csharp-raw-tcp\CSharpRawTcpSample.csproj -- 127.0.0.1 5089
```

## Expected Output

- The first TCP frame is `status`.
- The second TCP frame is `waferInfo`.
- After the sample sends WaferInfo, Event Log shows `WaferInfo updated from TCP: lotId=LOT-RAW-TCP-001, waferId=W01, recipeId=RCP-A, slot=1, foupId=FOUP-A, chamberId=CH-1`.
- The sample prints the echoed `waferInfo` update event.
- The sample prints status transitions after start/stop, and Event Log shows `Start condition: golden-sample`, `Start run mode: continue`, and `Stopped. reason=operator-request`.

## Troubleshooting

- Server not started: press **Start Servers** and verify port `5089`.
- `not_initialized`: press **Initialize** before the start/stop command step.
- No result returned: this TCP command demo does not query results; use SDK or REST.
- No MQTT events: this sample uses TCP, not MQTT.
