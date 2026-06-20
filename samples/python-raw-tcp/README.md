# Python Raw TCP Sample

Guided demo for the TCP/NDJSON event socket using Python.

## Simulator Prerequisites

- Start the simulator and keep TCP at `127.0.0.1:5089`.
- Press **Start Servers**.
- **Initialize** is not required for this WaferInfo TCP demo.

## UI SOP

1. Press **Start Servers**.
2. Run the sample.
3. Confirm the initial `status` and `waferInfo` frames.
4. Let the sample send WaferInfo.
5. Check Event Log for the full TCP WaferInfo update.

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

## Troubleshooting

- Server not started: press **Start Servers**.
- `not_initialized`: not required for this sample.
- No result returned: TCP WaferInfo demo does not query results.
- No MQTT events: this sample uses TCP.
