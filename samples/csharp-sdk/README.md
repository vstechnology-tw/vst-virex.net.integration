# C# SDK Sample

Guided demo for `Virex.NET.Client`. This is the canonical sample for learning how simulator UI state controls API behavior.

## Simulator Prerequisites

- Start the simulator:

  ```powershell
  dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
  ```

- Keep the default endpoints.
- Press **Start Servers**.
- Do not press **Initialize** before the first guided check. The sample intentionally shows `not_initialized`.

## UI SOP

1. Press **Start Servers**.
2. Run the sample and follow the console prompts.
3. When prompted, press **Initialize** and confirm `initialized=True, processState=ready`.
4. Let the sample send WaferInfo through REST.
5. Watch Event Log for the full WaferInfo line.
6. Let the sample start the cycle and query results.

## Run

```powershell
dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj
```

Optional REST base URL:

```powershell
dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj -- http://127.0.0.1:5088
```

## Expected Output

- Before **Initialize**, the sample reports `HTTP 409 not_initialized`.
- After **Initialize**, the sample updates WaferInfo, starts a cycle, and prints the result count.
- Event Log shows `WaferInfo updated from REST: lotId=LOT-SDK-001, waferId=W01, recipeId=RCP-A, slot=1, foupId=FOUP-A, chamberId=CH-1`.

## Troubleshooting

- Server not started: press **Start Servers** and verify `http://127.0.0.1:5088`.
- `not_initialized`: expected before **Initialize**; press **Initialize** when prompted.
- No result returned: run the cycle again or press **Emit Fake Result** and verify the lot ID.
- No MQTT events: this sample does not require MQTT observation; use the raw MQTT samples for event-only validation.
