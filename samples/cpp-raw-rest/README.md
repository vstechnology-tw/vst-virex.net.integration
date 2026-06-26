# C++ Raw REST Sample

Guided demo for the public REST API using WinHTTP.

## Simulator Prerequisites

- Start the simulator and keep REST at `http://127.0.0.1:5088`.
- Press **Start Servers**.
- Leave **Initialize** unpressed for the first negative check.

## UI SOP

1. Press **Start Servers**.
2. Run the sample.
3. Confirm `POST /api/control/start` returns `409 not_initialized`.
4. Press **Initialize** when prompted.
5. Let the sample update WaferInfo, start a cycle with `condition` and `runMode`, stop a second cycle with `reason`, and query results.

## Build and Run

```powershell
cmake -S samples\cpp-raw-rest -B samples\cpp-raw-rest\build
cmake --build samples\cpp-raw-rest\build --config Release
samples\cpp-raw-rest\build\Release\cpp-raw-rest.exe
```

Optional base URL:

```powershell
samples\cpp-raw-rest\build\Release\cpp-raw-rest.exe http://127.0.0.1:5088
```

## Expected Output

- Initial status, expected `not_initialized`, WaferInfo update, start result, stop result, and result query body.
- Event Log shows `WaferInfo updated from REST: lotId=LOT-CPP-REST-001, waferId=W01, recipeId=RCP-A, slot=1, foupId=FOUP-A, chamberId=CH-1`.
- Event Log shows `Start condition: golden-sample`, `Start run mode: continue`, and `Stopped. reason=operator-request`.

## Troubleshooting

- Server not started: press **Start Servers**.
- `not_initialized`: expected before **Initialize**.
- No result returned: press **Start Cycle** or **Emit Fake Result** with matching WaferInfo.
- No MQTT events: REST does not subscribe to MQTT.
