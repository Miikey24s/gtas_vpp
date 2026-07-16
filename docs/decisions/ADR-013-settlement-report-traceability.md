# ADR-013 — Immutable settlement evidence as the reporting source

- Status: Accepted
- Date: 2026-07-17
- Decision: D-006, D-007, D-010
- Direct tasks: SET-002, UI-006, REPORT-001/002/003, NOTIF-001/002

## Context

The request tables are workflow history and can retain legacy prices for
compatibility. A closed period needs a durable financial snapshot that can be
replayed, corrected without overwrite and reconciled by department/requester.
Reports and exported workbooks must not silently drift when a later price book
or request revision changes.

## Decision

`VPP04_Settlement` is the current immutable revision header. `VPP05` stores
item-level net/VAT/price-book evidence, `VPP06` stores signed commercial
charges, and `VPP07` stores allocation back to request detail and department.
The settlement service is the only writer for these snapshots. A correction
marks the previous revision non-current and inserts a new revision with a
reason and four-eyes actor; it never overwrites monetary snapshots.

When a selected period has a current settlement, report summaries and the XLSX
export use `VPP07` allocation amounts and expose grand-total/allocation variance.
CSV remains a compatibility export for legacy request rows. Notification
correlation and email outbox dedupe keys follow the same replay-first rule.

## Consequences

- A stale preview/hash or changed effective price book blocks confirmation.
- A report can prove zero variance for a closed period and still show scoped
  own/department allocations.
- Purchase orders, receiving, inventory and accounting remain outside scope.
- The existing legacy settle endpoint is retained only for old clients/tests;
  the UI uses confirm/correct snapshot workflows.
