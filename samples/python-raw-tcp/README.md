# Python Raw TCP Sample

Guided demo for the TCP/NDJSON event socket using Python.

## Simulator Prerequisites

- Start the simulator and keep TCP at `127.0.0.1:5089`.
- Press **Start Servers**.
- Press **Initialize** before the command demo so `start` can run.

## UI SOP

1. Press **Start Servers** and **Initialize**.
2. Run the sample.
3. Confirm the initial `status` and `waferInfo` frames.
4. Let the sample send WaferInfo.
5. Let the sample send `{"type":"start","condition":"golden-sample","runMode":"continue"}` and `{"type":"stop","reason":"operator-request"}`.
6. Check Event Log for the full TCP WaferInfo update, start condition, and stop reason.

## Run

```powershell
python samples\python-raw-tcp\main.py
```

Optional host and port:

```powershell
python samples\python-raw-tcp\main.py 127.0.0.1 5089
```

## Expected Output

- Initial status frame.
- Initial WaferInfo frame.
- Echoed WaferInfo update event.
- Event Log shows `WaferInfo updated from TCP: lotId=LOT-PY-TCP-001, waferId=W01, recipeId=RCP-A, slot=1, foupId=FOUP-A, chamberId=CH-1`.
- Status transitions after start/stop, and Event Log shows `Start condition: golden-sample`, `Start run mode: continue`, and `Stopped. reason=operator-request`.

## Troubleshooting

- Server not started: press **Start Servers**.
- `not_initialized`: press **Initialize** before the start/stop command step.
- No result returned: this TCP command demo does not query results.
- No MQTT events: this sample uses TCP.
