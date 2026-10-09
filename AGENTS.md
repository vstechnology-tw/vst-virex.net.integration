# Virex.NET Development Rules

This repository is intended to be public. Do not add private Virex.NET implementation details.

## Public Boundary

Allowed:

- Communication payload schemas and protocol constants.
- REST, MQTT, and TCP/NDJSON payload formatters/parsers.
- C# SDK wrappers for public communication APIs.
- Simulator behavior that mimics externally visible Virex.NET state transitions.
- Samples and documentation.

Not allowed:

- HALCON code or references.
- Camera, recipe internals, inspection algorithms, storage internals, or private UI.
- Customer credentials, internal hostnames, private result examples, or production paths.

## Compatibility

- `Virex.NET.Contracts` and `Virex.NET.Client` target `netstandard2.0`.
- `Virex.NET.Simulator.WPF` targets `.NET Framework 4.8`, `.NET 8 Windows`, and `.NET 10 Windows`.
- Samples can target modern .NET for easy local execution.

## Verification

Before claiming work is complete, run:

```powershell
dotnet test Virex.NET.Integration.slnx
```

If `.NET Framework 4.8` reference assemblies or Windows desktop SDK support are missing, report the exact build failure instead of claiming success.

## Agent skills

### Issue tracker

Issues and specs live in this repository's GitHub Issues; use the `gh` CLI. See `docs/agents/issue-tracker.md`.

### Triage labels

Use the five canonical triage labels. See `docs/agents/triage-labels.md`.

### Domain docs

This is a single-context repository using root `GLOSSARY.md` and `docs/adr/`. See `docs/agents/domain.md`.
