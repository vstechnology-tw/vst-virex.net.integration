# C++ Raw MQTT Sample

Guided demo for observing simulator MQTT events using a small raw MQTT client.

## Simulator Prerequisites

- Start the simulator and keep MQTT at `127.0.0.1:1883`.
- Press **Start Servers**.
- Keep base topic `Virex.NET`.

## UI SOP

1. Press **Start Servers**.
2. Run the sample.
3. Press **Apply WaferInfo** for `Virex.NET/wafer-info`.
4. Press **Initialize** for `Virex.NET/status`.
5. Press **Start Cycle** for status transitions.
6. Press **Emit Fake Result** for `Virex.NET/result`.
7. Press **Emit Error** for `Virex.NET/error`.

## Build and Run

```powershell
cmake -S samples\cpp-raw-mqtt -B samples\cpp-raw-mqtt\build
cmake --build samples\cpp-raw-mqtt\build --config Release
samples\cpp-raw-mqtt\build\Release\cpp-raw-mqtt.exe
```

Optional host, port, topic, and duration:

```powershell
samples\cpp-raw-mqtt\build\Release\cpp-raw-mqtt.exe 127.0.0.1 1883 Virex.NET 30
```

## Expected Output

- The sample subscribes to `Virex.NET/#`.
- Each UI action prints the matching MQTT topic and JSON payload.

## Troubleshooting

- Server not started: press **Start Servers**.
- `not_initialized`: press **Initialize** before **Start Cycle** for normal cycle behavior.
- No result returned: press **Emit Fake Result** or run **Start Cycle** after **Initialize**.
- No MQTT events: verify topic `Virex.NET/#` and listen duration.
