# ADR-004 — Flat personas and separation of duties

- Status: Accepted
- Date: 2026-07-15
- Decision: D-004
- Direct tasks: AUTH-001, AUTH-002, SUP-001, SET-002, UI-004, UI-005

## Context

Admin/User alone is too broad, while hierarchical group inheritance is not implemented or testable enough for this thesis scope.

## Decision

Use four flat personas: Employee, Department Approver, Procurement/Period Admin and System Admin. Each account has one active group and one primary department in v1. Permissions are explicit and flat; no implied inheritance. A System Admin does not automatically receive procurement authority.

## Consequences and guardrails

- A requester cannot approve their own supplement.
- Permission administration and settlement authority are separable.
- Correction/reopen requires a dedicated permission, reason and audit; use a different confirmer where two eligible people exist.
- Last-admin and concurrent membership changes require transactional protection and tests.
