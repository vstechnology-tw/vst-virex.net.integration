# Python Raw MQTT Sample

Guided demo for observing simulator MQTT events using Python.

## Simulator Prerequisites

- Start the simulator and keep MQTT at `127.0.0.1:1883`.
- Press **Start Servers**.
- Keep base topic `virex`.

## UI SOP

1. Press **Start Servers**.
2. Run the sample.
3. Press **Apply WaferInfo** for `virex/wafer-info`.
4. Press **Initialize** for `virex/status`.
5. Press **Start Cycle** for status transitions.
6. Press **Emit Fake Result** for `virex/result`.
7. Press **Emit Error** for `virex/error`.

## Run

```powershell
python samples\python-raw-mqtt\main.py
```

Optional host, port, topic, and duration:

```powershell
python samples\python-raw-mqtt\main.py 127.0.0.1 1883 virex 30
```

## Expected Output

- The sample subscribes to `virex/#`.
- Each UI action prints the matching MQTT topic and JSON payload.

## Troubleshooting

- Server not started: press **Start Servers**.
- `not_initialized`: press **Initialize** before **Start Cycle** for normal cycle behavior.
- No result returned: press **Emit Fake Result** or run **Start Cycle** after **Initialize**.
- No MQTT events: verify topic `virex/#` and listen duration.
