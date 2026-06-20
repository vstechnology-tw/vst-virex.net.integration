# Python Raw REST Sample

Guided demo for the public REST API using Python standard library HTTP calls.

## Simulator Prerequisites

- Start the simulator and keep REST at `http://127.0.0.1:5088`.
- Press **Start Servers**.
- Leave **Initialize** unpressed for the first negative check.

## UI SOP

1. Press **Start Servers**.
2. Run the sample.
3. Confirm `POST /api/control/start` returns `409 not_initialized`.
4. Press **Initialize** when prompted.
5. Let the sample update WaferInfo, start a cycle, and query results.

## Run

```powershell
python samples\python-raw-rest\main.py
```

Optional base URL:

```powershell
python samples\python-raw-rest\main.py http://127.0.0.1:5088
```

## Expected Output

- Initial status, expected `not_initialized`, WaferInfo update, start result, and result count.
- Event Log shows `WaferInfo updated from REST: lotId=LOT-PY-REST-001, waferId=W01, recipeId=RCP-A, slot=1, foupId=FOUP-A, chamberId=CH-1`.

## Troubleshooting

- Server not started: press **Start Servers**.
- `not_initialized`: expected before **Initialize**.
- No result returned: press **Start Cycle** or **Emit Fake Result** with matching WaferInfo.
- No MQTT events: REST does not subscribe to MQTT.
