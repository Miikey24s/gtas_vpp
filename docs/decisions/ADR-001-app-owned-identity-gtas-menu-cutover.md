# ADR-001 — App-owned identity and local GTAS_MENU cutover

- Status: Accepted
- Date: 2026-07-15
- Decision: D-001
- Direct tasks: AUTH-003, AUTH-006

## Context

`GTAS_MENU` is a local compatibility schema created for this project, not a company user database that must be reconnected. Keeping TripleDES/stored-procedure login as the permanent account authority would retain avoidable password and coupling risks.

## Decision

Use app-owned ASP.NET Core Identity for account credentials, token/session lifecycle, lockout and recovery. Import/map required local account-to-employee and business-RBAC data once, with collision and reconciliation evidence. Do not migrate legacy passwords; issue/reset demo credentials. Disable the TripleDES/SP login path after verified cutover and do not maintain a fake long-lived external user provider.

## Consequences and guardrails

- P02/P04/P06-style business permissions may be mapped, but credentials belong only to Identity.
- Cutover must be additive and reversible until reconciliation passes; do not drop legacy data in the first migration.
- Duplicate username/email/employee mappings, account state and session invalidation require explicit tests.
- This ADR does not authorize connection to any company database.
