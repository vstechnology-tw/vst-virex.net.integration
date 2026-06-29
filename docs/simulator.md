# Simulator Guide

`Virex.NET.Simulator.WPF` is the local endpoint for integration development. From the perspective of REST, TCP, MQTT, data models, state, and events, it should behave like a Virex.NET service compatible with production.

Launch from the repository root:

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
```

The simulator starts as a WPF window:

![Simulator main window with numbered UI regions](assets/simulator-main-window-annotated.png)

## Simulator window walkthrough

| Region | Area | What it is used for |
| --- | --- | --- |
| 1 | **Connection Settings** | Defines the endpoints exposed by the simulator. Use **REST prefix** for the HTTP base address, **TCP port** for the NDJSON socket listener, **MQTT host** and **MQTT port/topic** for the embedded MQTT broker and topic prefix, and **Result prefix** only when result IDs or result paths need a test prefix. |
| 2 | **ProductInfo** | Defines the product context sent into the simulated system. Fill in Lot ID, Wafer ID, Recipe, Slot, Foup ID, and Chamber ID, then press **Apply ProductInfo** after the system is `Ready`. |
| 3 | **State** | Shows the current simulator state and provides the main interaction buttons. **Start Servers** opens the REST, TCP, and MQTT endpoints. **Initialize**, **Deinitialize**, **Start Cycle**, and **Stop** drive the same public state transitions that an external client can call through REST. |
| 4 | **Event Log** | Shows local simulator activity, server start/stop messages, command rejections, and other diagnostic output. Use this area to confirm that button actions and client commands reached the simulator. |

## Simulator purpose

| Purpose | What To Verify |
| --- | --- |
| Contract verification | Confirm that the vendor application uses the same REST routes, data models, TCP frames, and MQTT topics as production. |
| State machine verification | Confirm command order, valid states, rejected states, and run completion behavior. |
| Event verification | Confirm that TCP/MQTT consumers can handle state, ProductInfo, run, result, error, and rejection events. |

The simulator is not a production inspection engine and does not expose private algorithms, camera behavior, recipe internals, or storage internals.

## Standard operation

1. Start the simulator.
2. Confirm the endpoint settings.
3. Press **Start Servers**.
4. Connect a sample or vendor client.
5. Initialize the system.
6. Send ProductInfo.
7. Press **Start Cycle** or send the equivalent client command.
8. Observe `runStarted`, `runCompleted`, `resultCreated`.
9. Query `GET /api/results`.

## Default Endpoints

| Interface | Default |
| --- | --- |
| REST | `http://127.0.0.1:5088` |
| REST Browser | `http://127.0.0.1:5088/scalar` |
| OpenAPI JSON | `http://127.0.0.1:5088/openapi/v1.json` |
| TCP | `127.0.0.1:5089` |
| MQTT | `127.0.0.1:1883`, root topic `virex` |

## Button behavior

| Button | Behavior |
| --- | --- |
| **Start Servers** | Starts REST, TCP, and MQTT endpoints. Does not change system state. |
| **Initialize** | Sends the initialize command. Only valid in `Uninitialized`. |
| **Deinitialize** | Sends the deinitialize command. Only valid in `Ready`. |
| **Apply ProductInfo** | Updates the current ProductInfo. Only valid in `Ready`. |
| **Start Cycle** | Starts a run. Only valid in `Ready`; the response state is `Running`. |
| **Stop** | Stops the active run. Only valid in `Running`; the response state is `Ready`. |

## Observable behavior

| Action | Expected external observations |
| --- | --- |
| Initialize | REST command returns `Ready`; state events are published. |
| ProductInfo update | REST command returns `Ready`; ProductInfo events are published. |
| Start | REST command returns `Running`; run-start events are published. |
| Run completes | State returns to `Ready`; run-completed and result-created events are published. |
| Invalid command | Command response contains `accepted=false` and `errorCode=invalid_state`; a rejection event may be published. |

## Recommended simulator acceptance process

```powershell
dotnet test Virex.NET.Integration.sln
python -m mkdocs build --strict
```

Then manually use the local simulator to verify the generated documentation and C# SDK samples.
