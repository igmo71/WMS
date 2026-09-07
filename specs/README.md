# Specification registry

This registry selects the specification that governs current work. It is not a
catalog of every historical directory under `specs/`.

## Statuses

- **Active** — the single issue currently being implemented.
- **Queued** — accepted future work that is not active yet.
- **Frozen reference** — accepted decision history; do not update it for a new
  issue.
- **Superseded** — replaced by a newer decision.
- **Removed** — lasting rules were distilled into current documentation and the
  historical body was removed from the working tree.

Only one specification is normally active. Read an active specification, one
explicitly named by the user, or a frozen reference directly required to
establish an otherwise undocumented rule.

## Current selection

**Active:** none.

## Recent governing references

| Created | Status | Problem | Path |
| --- | --- | --- | --- |
| 2026-09-07 | Frozen reference | Confirmed safe cleanup | [`2026-09-07-safe-cleanup/spec.md`](2026-09-07-safe-cleanup/spec.md) |
| 2026-09-07 | Frozen reference | Mobile maintainability pilot | [`2026-09-07-mobile-maintainability-pilot/spec.md`](2026-09-07-mobile-maintainability-pilot/spec.md) |
| 2026-09-07 | Frozen reference | Application and 1C boundary | [`2026-09-07-application-onec-boundary/spec.md`](2026-09-07-application-onec-boundary/spec.md) |
| 2026-09-07 | Frozen reference | Receiving command consistency | [`2026-09-07-receiving-command-consistency/spec.md`](2026-09-07-receiving-command-consistency/spec.md) |
| 2026-09-07 | Frozen reference | Mobile receipt replay boundary | [`2026-09-07-mobile-receipt-replay-boundary/spec.md`](2026-09-07-mobile-receipt-replay-boundary/spec.md) |
| 2026-09-07 | Frozen reference | Operational topology integrity | [`2026-09-07-operational-topology-integrity/spec.md`](2026-09-07-operational-topology-integrity/spec.md) |
| 2026-09-07 | Frozen reference | Immediate client-visible correctness fixes | [`2026-09-07-immediate-correctness-fixes/spec.md`](2026-09-07-immediate-correctness-fixes/spec.md) |
| 2026-09-04 | Frozen reference | Architecture and process-boundary review | [`2026-09-04-architecture-process-boundary-review/spec.md`](2026-09-04-architecture-process-boundary-review/spec.md) |
| 2026-09-03 | Frozen reference | Standalone staging | [`2026-09-03-standalone-staging/spec.md`](2026-09-03-standalone-staging/spec.md) |
| 2026-09-03 | Frozen reference | Initial order plan synchronization | [`2026-09-03-initial-order-plan-synchronization/spec.md`](2026-09-03-initial-order-plan-synchronization/spec.md) |
| 2026-09-02 | Frozen reference | Order synchronization state cleanup | [`2026-09-02-order-synchronization-state-cleanup/spec.md`](2026-09-02-order-synchronization-state-cleanup/spec.md) |
| 2026-09-02 | Frozen reference | Order synchronization simplification | [`2026-09-02-order-synchronization-simplification/spec.md`](2026-09-02-order-synchronization-simplification/spec.md) |
| 2026-09-02 | Frozen reference | Order synchronization decisions | [`2026-09-02-order-synchronization-decisions/spec.md`](2026-09-02-order-synchronization-decisions/spec.md) |
| 2026-09-02 | Frozen reference | Decimal warehouse quantities | [`2026-09-02-decimal-warehouse-quantities/spec.md`](2026-09-02-decimal-warehouse-quantities/spec.md) |
| 2026-09-01 | Frozen reference | Operational location display and mobile operator UX | [`2026-09-01-operator-location-mobile-ux/spec.md`](2026-09-01-operator-location-mobile-ux/spec.md) |
| 2026-09-01 | Frozen reference | Mobile WMS stabilization | [`2026-09-01-mobile-wms-stabilization/spec.md`](2026-09-01-mobile-wms-stabilization/spec.md) |
| 2026-08-31 | Frozen reference | Mobile picking and shipping | [`2026-08-31-mobile-picking-shipping/spec.md`](2026-08-31-mobile-picking-shipping/spec.md) |
| 2026-08-29 | Frozen reference | Mobile receiving and putaway | [`2026-08-29-mobile-receiving-putaway/spec.md`](2026-08-29-mobile-receiving-putaway/spec.md) |
| 2026-08-27 | Frozen reference | Location-based inventory counting | [`2026-08-27-location-inventory-count/spec.md`](2026-08-27-location-inventory-count/spec.md) |
| 2026-08-27 | Frozen reference | Storage-location locking | [`2026-08-27-storage-location-locking/spec.md`](2026-08-27-storage-location-locking/spec.md) |
| 2026-08-25 | Frozen reference | Mobile command idempotency | [`2026-08-25-mobile-command-idempotency/spec.md`](2026-08-25-mobile-command-idempotency/spec.md) |
| 2026-08-21 | Frozen reference | Inventory-transfer concurrency | [`2026-08-21-inventory-transfer-concurrency/spec.md`](2026-08-21-inventory-transfer-concurrency/spec.md) |
| 2026-08-19 | Frozen reference | Mobile WMS foundation | [`mobile-wms/spec.md`](mobile-wms/spec.md) |

Older frozen and superseded specifications remain available in their existing
directories and Git history. They are intentionally omitted here so that
historical volume does not look like current scope.

## Creating and closing work

Create a substantial issue under `specs/YYYY-MM-DD-<problem-slug>/`. Keep it
focused on one business outcome, scope, acceptance criteria, and open questions.

When accepted:

1. move lasting product and architecture rules into current documentation;
2. move unfinished follow-up work into `docs/ROADMAP.md`;
3. mark the specification **Frozen reference**;
4. remove its temporary `implementation-plan.md`.
