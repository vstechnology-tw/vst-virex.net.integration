# C# Raw RESTful API Sample

Demonstrates the current RESTful API without the SDK wrapper.

Run the simulator first and press **Start Servers**.

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
dotnet run --project samples\csharp-raw-rest\CSharpRawRestSample.csproj
```

The sample calls:

- `GET /api/status`
- `POST /api/system/initialize`
- `POST /api/product-info`
- `POST /api/system/start`
- `POST /api/system/stop`
- `GET /api/results`

Expected result: command responses use `accepted`, `state`, `command`, and optional `errorCode`.
