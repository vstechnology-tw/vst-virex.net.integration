# C# Raw MQTT Sample

Guided demo for observing simulator MQTT events.

## Simulator Prerequisites

- Start the simulator and keep MQTT at `127.0.0.1:1883`.
- Press **Start Servers**.
- Keep base topic `virex`.

## UI SOP

1. Press **Start Servers**.
2. Run the sample.
3. Press **Apply WaferInfo** and expect `virex/wafer-info`.
4. Press **Initialize** and expect `virex/status`.
5. Press **Start Cycle** and expect status transitions.
6. Press **Emit Fake Result** and expect `virex/result`.
7. Press **Emit Error** and expect `virex/error`.

## Run

```powershell
dotnet run --project samples\csharp-raw-mqtt\CSharpRawMqttSample.csproj
```

Optional host, port, topic, and duration:

```powershell
dotnet run --project samples\csharp-raw-mqtt\CSharpRawMqttSample.csproj -- 127.0.0.1 1883 virex 30
```

## Expected Output

- The sample subscribes to `virex/#`.
- Each UI action prints the matching MQTT topic and JSON payload.

## Troubleshooting

- Server not started: press **Start Servers** and verify MQTT `127.0.0.1:1883`.
- `not_initialized`: press **Initialize** before **Start Cycle** if you want normal cycle transitions.
- No result returned: press **Emit Fake Result** or run **Start Cycle** after **Initialize**.
- No MQTT events: verify the base topic is `virex` and the sample is still within its listen duration.
