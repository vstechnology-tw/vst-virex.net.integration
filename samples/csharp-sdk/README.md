# C# SDK Sample

To exercise the four Issue #44 read-only REST queries, use the candidate SDK/Simulator and run:

```powershell
dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj -- http://127.0.0.1:5088 --queries
```

This optional mode reads the recipe catalog, loaded recipe/parameters, and each exact ResultId from the result list. The default 13-step flow remains compatible with older Apps; `--queries` against an unsupported App fails with its actual HTTP status. See [query semantics](../../docs/read-only-queries.md).

Demonstrates the current public `Virex.NET.Client` workflow:

1. Read `GET /api/status`.
2. Send `POST /api/system/initialize`.
3. Update `ProductInfo`.
4. Send `POST /api/system/start`.
5. Observe run completion.
6. Query `GET /api/results`.

Run the simulator first and press **Start Servers**.

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj
```

Expected result:

- Initialize returns `Ready`.
- ProductInfo update returns `Ready`.
- Start returns `Running`.
- Results use the ProductInfo snapshot captured at start.
