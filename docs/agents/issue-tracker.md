# Issue tracker: Beads

Issues and specs for this repo live in Beads (`bd`), served from the maintainers' shared Dolt server (not stored in this repo). Issue IDs look like `vxi-a3f2`. GitHub Issues stay open for external reports; maintainers move accepted work into Beads.

## Conventions

- **Create an issue**: `bd create "<title>" -d "<body>" [-l <label>] [-p <0-4>]`.
- **Create a spec (parent)**: `bd create "<title>" -t epic -d "<body>"`.
- **Make an issue a child of a parent**: `bd create "<title>" --parent <epic-id> ...`.
- **Read an issue**: `bd show <id>` (`--json` for structured output).
- **List issues**: `bd list --status open [--label <label>] [--parent <epic-id>] --json`.
- **Comment on an issue**: `bd comments add <id> "<text>"`.
- **Apply / remove labels**: `bd update <id> --add-label <label>` / `--remove-label <label>`.
- **Blocking**: `bd dep add <blocked> <blocker>`; `bd ready` only lists beads with no open blockers.
- **Labels skills apply** (`ready-for-agent`, `needs-info`, ...): add them as bead labels. Every implementation bead also gets one of `tier:basic`, `tier:standard`, `tier:complex`.
- **Close**: `bd close <id> --reason "<SHA; verification command → result>"` (the coordinator closes after review and integration).

## Pull requests as a triage surface

**PRs as a request surface: no.**

## When a skill says "publish to the issue tracker"

Create a bead (an epic for a spec, a child bead for a ticket).

## When a skill says "fetch the relevant ticket"

`bd show <id>`.

## Wayfinding operations

- **Map**: an epic labelled `wayfinder:map`.
- **Child ticket**: a bead with `--parent <map-id>` and a label `wayfinder:<type>` (`research`/`prototype`/`grilling`/`task`).
- **Blocking**: `bd dep add <blocked> <blocker>`. A ticket is unblocked when every blocker is closed.
- **Frontier query**: `bd ready --parent <map-id>`.
- **Claim**: `bd update <id> --claim`, the session's first write.
- **Resolve**: `bd comments add <id> "<answer>"`, then `bd close <id>`, then add a one-line pointer to the map's notes (`bd update <map-id> --append-notes "..."`).
