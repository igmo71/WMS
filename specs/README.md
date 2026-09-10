# Specification registry

Specifications are temporary issue scopes and frozen decision history. Current
product and engineering truth belongs in `docs/`; do not read or update frozen
specifications unless a current task explicitly needs their rationale.

## Active

None.

## Frozen

- [Unified shipping transitions](2026-09-10-unified-shipping-commands/spec.md)
  — Shared start-picking, complete-picking, and ship implemented on 2026-09-10.
- [Unified receiving commands](2026-09-10-unified-receiving-commands/spec.md)
  — Receiving start/completion pilot accepted on 2026-09-10.

## Lifecycle

Create substantial work under `specs/YYYY-MM-DD-<problem-slug>/` with one
outcome, scope, acceptance criteria, and genuine open questions. Normally only
one specification is active.

When accepted:

1. move lasting rules to the appropriate current document;
2. keep unfinished accepted work in `docs/ROADMAP.md`;
3. mark the specification frozen;
4. remove temporary implementation plans.
