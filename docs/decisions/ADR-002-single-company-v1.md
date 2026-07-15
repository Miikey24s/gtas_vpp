# ADR-002 — Single-company thesis release

- Status: Accepted
- Date: 2026-07-15
- Decision: D-002
- Direct tasks: AUTH-001, AUTH-002, PER-001

## Context

The source carries company-like fields but does not provide tenant-safe membership, query and permission isolation. A half-multi-company product would create a false security boundary.

## Decision

Release v1 as one company. Do not expose Test/Live selection as multi-tenancy and do not claim tenant isolation. Keep domain keys and interfaces migration-friendly where that has low cost, but do not add tenant infrastructure during the thesis release.

## Consequences and guardrails

- Every account has one active group and one primary department under ADR-004.
- Scope remains own / department / whole company within that company.
- A future multi-company version requires a new ADR, schema/query audit and isolation tests.
