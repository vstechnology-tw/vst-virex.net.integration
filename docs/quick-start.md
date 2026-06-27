# Quick Start

This process uses the local simulator to validate the core integration path.

## 1. Start the simulator

Execute from the repository root:

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
```

Keep the default endpoint settings and click **Start Servers** to start the REST/TCP/MQTT service.

| Interface | Default |
| --- | --- |
| REST | `http://127.0.0.1:5088` |
| TCP | `127.0.0.1:5089` |
| MQTT | `127.0.0.1:1883`, root topic `virex` |

## 2. Run the SDK Sample

Open the second terminal:

```powershell
dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj
```

The sample runs the standard flow:

1. Read `GET /api/status`.
2. Initialize the system.
3. Send `ProductInfo`.
4. Start a run.
5. Observe the run completion event.
6. Query results.

## 3. Confirm Expected State

| Step | Expected State |
| --- | --- |
| Newly launched simulator | `Uninitialized` |
| Initialization completed | `Ready` |
| ProductInfo update completed | `Ready` |
| Start command accepted | `Running` |
| Run completed | `Ready` |

## 4. Confirm the result snapshot

The result should contain the ProductInfo at which `Start` was accepted:

```json
{
  "waferID": "W01",
  "lotID": "LOT-001",
  "recipe": "RCP-A"
}
```

## 5. Select a Protocol Check

- REST browser: `http://127.0.0.1:5088/scalar`
- MQTT: Subscribe to `virex/#`
- If you are not using the C# SDK, run the raw TCP or raw REST samples.

## Completion Conditions

A successful quick start confirms that:

- REST state query returns a valid `state`.
- ProductInfo can be updated at `Ready`.
- Start returns `Running`.
- After the run completes, the state returns to `Ready`.
- Results can be queried and conform to the ProductInfo snapshot.
