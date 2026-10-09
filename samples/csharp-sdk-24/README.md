# C# SDK 2.4 candidate sample

This ordinary NuGet consumer uses `Virex.NET.Client` and its transitive Contracts package. It has no source ProjectReference. It demonstrates REST commands/queries, exact-name TCP subscriptions, accepted-work correlation, live values, explicit committed ResultId reads, current-result invalidation, and Reset followed by explicit Initialize.

Use an owned synthetic Simulator with explicit REST/TCP endpoints. The sample initializes, starts, stops, resets and deinitializes that session. It does not inject faults or establish real App/equipment readiness. `captureReady`, `captureCompleted`, `resultCreated`, `system.state`, and `result.findings` are this example's Simulator bindings, not SDK name restrictions.

Read the exact `packageVersion` and feed location from the candidate manifest. Restore and build with that version, then run the built consumer. The 2.4.0 candidate is local and unpublished; a default 2.4.0 PackageReference does not mean that a release is available.

```powershell
$version = '<manifest packageVersion>'
$feed = '<candidate local feed>'
New-Item -ItemType Directory -Force ./artifacts | Out-Null
$nugetConfig = Join-Path $PWD 'artifacts/candidate.NuGet.Config'
$feedXml = [Security.SecurityElement]::Escape($feed)
$configXml = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration><packageSources><clear/>
  <add key="candidate" value="$feedXml" />
  <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
</packageSources></configuration>
"@
[IO.File]::WriteAllText($nugetConfig, $configXml, [Text.UTF8Encoding]::new($true))
dotnet restore samples/csharp-sdk-24/CSharpSdk24Sample.csproj -p:VirexPackageVersion=$version --configfile $nugetConfig --packages ./artifacts/consumer-cache
dotnet build samples/csharp-sdk-24/CSharpSdk24Sample.csproj --no-restore -p:VirexPackageVersion=$version
dotnet samples/csharp-sdk-24/bin/Debug/net8.0/CSharpSdk24Sample.dll http://127.0.0.1:<owned-rest-port>/ 127.0.0.1 <owned-tcp-port> ./artifacts/consumer-report.json
```

The report records the target framework separately from the actual execution host and loaded Client/Contracts/sample SHA256. Inspect `obj/project.assets.json`, the packages' nuspec files, and those loaded hashes against the candidate manifest. Keep the configured roll-forward policy visible. A net8 target running on a .NET 10 host is not native .NET 8 validation.

This explicit config applies only to this restore. It does not change global/repository NuGet sources or audit policy. It also avoids SDK source-argument URL normalization differences. Preserve a failed restore with its exact command and diagnostics; do not treat it as a package-consumer PASS.

The subscription is installed before Start. Events can arrive before its REST response, so the sample buffers the matching events before comparing the accepted App/Instance/Request/Job/Capture identity. Acceptance is not capture readiness. The sample reads the result by the event's ResultId, even after another accepted work invalidates current result availability. Historical reads do not change the current result. Typed errors and cancellation are failures, never zero or a previous value. Awaited CloseAsync joins the owned connection; the connected Simulator's owner is responsible for shutdown and cleanup after any sample failure.
