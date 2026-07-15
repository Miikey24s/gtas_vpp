# ADR-010 — Durable inbox, email, CSV and Excel

- Status: Accepted
- Date: 2026-07-15
- Decision: D-010
- Direct tasks: REPORT-003, NOTIF-002, NOTIF-003

## Context

Users may be away from the internal application, while reporting must remain useful and verifiable without expanding into every messaging/export channel.

## Decision

Implement a durable in-app inbox as source of truth, scope-safe CSV/Excel export, and email notification through templates, adapter/outbox, retry and delivery status. Local/sandbox email evidence is mandatory for the A+ release. A real provider/domain/server rollout remains an external gate. PDF, Teams and Zalo OA are deferred.

## Consequences and guardrails

- Business transactions commit independently of email delivery failure.
- Each event/recipient/channel delivery is idempotent; offline users recover from the inbox.
- Deep links recheck current authorization.
- Exports mirror active filters/scope, reconcile with SQL/UI and prevent spreadsheet formula injection.
