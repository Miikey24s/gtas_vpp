# ADR-003 — Per-user request revisions and whole-company basket

- Status: Accepted
- Date: 2026-07-15
- Decision: D-003
- Direct tasks: PER-001, REQ-001

## Context

Each employee currently owns a regular request. Procurement later consolidates valid company demand to negotiate and buy in bulk, then allocates planned quantities/costs back to requests and departments.

## Decision

Allow one current regular request per user and period. Edit, cancellation and replacement preserve immutable revision history and links; cancellation is not deletion and cannot be used to bypass uniqueness. At close, build one whole-company procurement basket from valid current revisions while retaining provenance to every request line and department snapshot. Store immutable allocation snapshots back to request/department scope.

## Consequences and guardrails

- Concurrent create/replacement must yield one winner and a conflict for the loser.
- Superseded, rejected, cancelled or deleted facts are excluded from the effective basket but remain auditable.
- Allocation is a planned procurement/distribution fact, not evidence of receipt, payment, inventory or accounting posting.
- Settled revisions are not overwritten; correction creates a new revision.
