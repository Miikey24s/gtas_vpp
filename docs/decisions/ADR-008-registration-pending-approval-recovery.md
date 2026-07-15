# ADR-008 — Registration, PendingApproval and recovery

- Status: Accepted
- Date: 2026-07-15
- Decision: D-008
- Direct tasks: AUTH-005, UI-007

## Context

The thesis demo needs registration, but an internal management account must not become active or privileged merely because a public form was submitted.

## Decision

Self-registration creates a `PendingApproval` account with zero business privilege. Username and email are required and unique; employee code is unique when supplied. A System Admin maps the account to employee, primary department and active group before activation. Use email confirmation/reset when a sender is configured; otherwise provide an audited admin-only activation/reset fallback. Never auto-activate.

## Consequences and guardrails

- Registration, login and recovery responses use rate limiting and anti-enumeration behavior.
- Duplicate/mapping collisions are explicit and auditable.
- Activation events may appear in the durable inbox independently of the external email provider.
- Registration security is mandatory in the A+ demo slice.
