# Simulator Guide

Local simulator for the public REST, TCP and MQTT contract. Capture and inspection data are synthetic; these events do not prove production App or camera readiness.

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj --framework net10.0-windows
# opt-in source policy
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj --framework net10.0-windows -- --manage-operation-mode
```

## Window controls

- **Connection Settings**: REST prefix, TCP port, MQTT host/port/topic. **Start Servers** / **Stop Servers** own the endpoints.
- **ProductInfo**: Lot ID, Wafer ID, Recipe, Slot, Foup ID, Chamber ID; **Apply ProductInfo** in Ready.
- **State**: **Initialize** / **Deinitialize**, **Start Single** / **Start Continue**, **Stop**. Start requires Ready; Stop requires Running. Single completion returns to Ready automatically.

## Inspection mode

Choose **Legacy (omitted)**, **captureOnly**, or **captureAndInspect** before **Start Single** or **Start Continue**. Run mode and inspection mode are independent. Omission preserves recipe defaults. In this Simulator, captureOnly emits capture events without resultCreated or persisted image/result artifacts; captureAndInspect also produces a synthetic inspection result.

## Queries / Operation Mode

Read the recipe list, current recipe, current public parameters, or result detail. Enter the exact, case-sensitive ResultId before selecting **Get result detail**. Missing current recipes and unknown IDs show the public query error; the query never substitutes the latest result.

Use **Get operation mode** to read the applied local/remote mode and managementEnabled. Select a mode and press **Apply mode**; the displayed mode changes only after acceptance. Mode management is disabled by default. Launch with **--manage-operation-mode** to enforce source policy: Local permits local lifecycle buttons; Remote permits external lifecycle clients. Switching mode does not change lifecycle state.

## Capture Events / State Machine

The **Capture Events** tab shows the latest captureReady and captureCompleted payloads, including JobId, CaptureId, SourcesCount and timestamp. A matching captureCompleted is distinct from a later resultCreated. The Event Log retains the public sequence. The **State Machine** tab follows actual session state.

## Cancellation / shutdown

Queries run without blocking the window. **Cancel query** cancels the current query; a replaced query cannot overwrite a newer result. Closing the window cancels and joins its simulated runs and stops its owned endpoints. Host shutdown does not impersonate an external Stop command or change operation mode.

## Default endpoints

| Interface | Default |
| --- | --- |
| REST | `http://127.0.0.1:5088` |
| API browser | `http://127.0.0.1:5088/scalar` |
| OpenAPI | `http://127.0.0.1:5088/openapi/v1.json` |
| TCP | `127.0.0.1:5089` |
| MQTT | `127.0.0.1:1883`, topic `virex` |

## Verification

```powershell
dotnet test Virex.NET.Integration.slnx
python -m mkdocs build --strict
```
