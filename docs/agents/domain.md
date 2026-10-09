# Domain Docs

How the engineering skills should consume this repo's domain documentation when exploring the codebase.

## Before exploring, read these

- `GLOSSARY.md` at the repo root. If `GLOSSARY-MAP.md` exists, read the glossary for each context relevant to the topic.
- Relevant decisions under `docs/adr/`.

If a referenced file does not exist, proceed silently. `/domain-modeling` creates glossary entries and ADRs when terms or decisions are resolved.

## File structure

This is a single-context repository: root `GLOSSARY.md` and `docs/adr/`.

## Use the glossary's vocabulary

Use the glossary's terms for domain concepts in issues, proposals, hypotheses, and tests. If a required concept is absent, reconsider the terminology or record the gap for `/domain-modeling`.

## Flag ADR conflicts

Explicitly identify output that conflicts with an existing ADR instead of silently overriding it.
