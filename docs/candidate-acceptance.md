# 2.4.0 public candidate acceptance

The 2.4.0 public contract includes [exact-name event discovery/subscriptions](event-subscriptions.md), [live and explicitly selected result sources](data-sources.md), and [Alarm discovery and two-stage recovery](alarms.md). Commands and queries use REST; these event subscriptions use TCP/NDJSON. Old samples remain transport demonstrations. Their polling delays do not prove capture readiness, and old SDK/wire compatibility is not a required 2.4.0 gate.

This is an unpublished local candidate. Contracts/Client remain netstandard2.0. Simulator targets remain net48, net8.0-windows and net10.0-windows. A build proves compilation for its target; record RuntimeInformation.FrameworkDescription, Environment.Version and loaded binary hashes for every runtime check. Missing desktop SDK, reference assemblies or native runtime remains an exact recorded limitation. Do not install a runtime or hide an environment failure to turn it into PASS.

Use one clean source commit for the packages, Simulator and documentation. Pass an external source-derived VersionSuffix such as `spec2.g<short-commit>` during build/pack; do not insert that commit into its own source. Both packages must use the same exact `2.4.0-<suffix>` version, and Client's nuspec dependency must select that Contracts version. Create a separate local candidate feed; do not overwrite another active feed or publish it.

Run `dotnet test Virex.NET.Integration.slnx`. Build both netstandard2.0 libraries and all three Simulator targets. Execute applicable SDK/REST/TCP checks against each built Simulator artifact. Preserve commands, exit codes, initial failures, TRX and actual outcomes. Include illegal input, unknown/unavailable/missing values, cancellation, failure and late-work checks; a hash or build is not a behavior test.

The `samples/csharp-sdk-24` is intentionally outside the source solution. Its restore needs the exact candidate version/feed. This keeps the required solution gate reproducible before packaging and makes the package-consumer check a separate gate. No global NuGet configuration change or Client/Contracts ProjectReference is required.

The external acceptance manifest records source commit/tree, package version, package/nuspec/restore provenance, Simulator targets, documentation/source/binary SHA256 and per-check runtime/outcomes. Its own hash belongs in a separate handoff receipt. This avoids a manifest hashing itself. Pin that manifest and receipt as the public part of a later integration combination.

Result selectors are explicit. Alarm ResultId is optional: when supplied it must select the current committed result; without it an actual current-work defect/criticalDefect notification is allowed and is not a ResultCreated/DefectResult. Reset performs stop/cleanup and returns Uninitialized; only subsequent explicit Initialize returns Ready. The existing standalone Deinitialize failure can remain Faulted while a later Shutdown succeeds; do not claim all cleanup failures propagate through Shutdown. Keep this alternate boundary visible in host evidence.

Simulator acceptance covers synthetic public behavior only. It does not accept a real App, native cleanup, hardware, customer configuration, machine wire format, deployment, CI, installer or release. Publishing, tagging, deployment and closing tracker items require their own authorization.
