# ADR-014 — Standalone supplements with optional regular-order lineage

- Status: Accepted
- Date: 2026-08-05
- Decision: D-014
- Supersedes: the mandatory-base and per-base quota parts of ADR-005
- Direct tasks: SUP-001 amendment, UI-005 amendment, DOC-002, REL-001

## Context

The approved thesis workflow must allow a user to submit an unexpected need even
when that user has not created a regular order for the current period. Requiring a
regular order first creates an artificial step and prevents the intended
standalone-supplement journey.

The existing schema already keeps `BaseRequestId` and `BaseRequestSeriesId`
nullable. The remaining coupling was in service policy, UI capability text,
settlement blockers and unique indexes keyed by the base series.

## Decision

- A supplement requires a reason of 5–500 characters but does not require a
  regular order.
- If an eligible current regular order exists, the service may attach its ID and
  series as optional lineage metadata. If the client explicitly selects an
  ineligible base, the command is rejected instead of silently changing the base.
- Creating a standalone supplement does not consume the one-regular-order slot.
  The same user may create the regular order later while the submission window is
  open.
- `MaxApproved`, `MaxAttempts` and the one-Pending rule are enforced per
  user/period, independent of whether attempts are standalone or linked. The
  period already carries company scope.
- An approved standalone supplement contributes to demand aggregation and
  settlement exactly like an approved linked supplement. A Pending supplement
  continues to block settlement under the existing workflow rules.
- Approval scope, separation of duties, deadlines, rowversion, idempotency and
  immutable audit history are unchanged.

## Database and migration guardrails

- Keep the nullable base columns for optional traceability and backward
  compatibility; no data backfill or column drop is required.
- Replace the filtered one-Pending index key with
  `(CreatedByUserId, PeriodId)` and the attempt key with
  `(CreatedByUserId, PeriodId, SupplementAttemptNumber)`.
- Migration `20260805035706_AllowStandaloneSupplements` must fail before changing
  indexes if existing current data contains multiple Pending supplements or
  duplicate attempt numbers for one user/period.
- Production application still requires a timestamp-matched backup, reviewed SQL,
  maintenance/rollback plan and post-migration reconciliation.

## Consequences

- The UI exposes `Tạo đơn bổ sung` without instructing the user to create a regular
  order first.
- History, exports and approval details must tolerate missing base metadata.
- Quota and sequence remain coherent when a standalone attempt is followed by a
  regular order and later linked attempts.
- Thesis and presentation material must describe the base link as optional
  traceability, not a creation prerequisite.
