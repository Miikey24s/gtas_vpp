# ADR-005 — Supplement quota and approval policy

- Status: Superseded in part by ADR-014 (2026-08-05)
- Date: 2026-07-15
- Decision: D-005
- Direct tasks: PER-001, SUP-001, UI-005

## Context

The existing “maximum three” rule lacks a stable quota key, concurrency contract and rejected/cancelled semantics.

## Decision

A supplement must link to the user’s regular base request and include a reason. Configure `MaxApproved` per user/base request/period, default 1; allow at most one Pending supplement. Rejected/cancelled attempts do not consume the final approved quota but remain audited. Configure a separate anti-spam `MaxAttempts`, default 6 for the demo.

Creation closes with the regular submission deadline. An existing Pending item may be approved until the period’s separate `SupplementApprovalDeadline`. A pending or ambiguous supplement blocks settlement.

## Consequences and guardrails

- Enforce quota and one-Pending rules inside one transaction/database constraint path.
- Four concurrent approvals cannot produce more than three approved records.
- Self-approval is forbidden under ADR-004.
- Values are configuration/policy, not scattered hard-coded UI constants.

## Supersession note

ADR-014 replaces only the mandatory-base and per-base quota key. A supplement may
now be standalone, while reason, deadline, one-Pending, approval, audit,
idempotency and configurable quota rules remain in force.
