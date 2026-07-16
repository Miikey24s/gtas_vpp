# LEAN-07 — Product proof: reports, Excel, inbox and email sandbox

Status: DONE for REPORT-001/002/003 and NOTIF-001/002.

## Delivered

- Report summaries retain own/department/all permission scopes and, for a
  settled period, use the current `VPP04` allocation snapshots for quantity
  and amount instead of the mutable legacy request price. The response exposes
  settlement revision, primary supplier, grand total, allocation total and
  variance so the dashboard can prove reconciliation.
- Added `GET /api/reports/export.xlsx`. The generated workbook is a real XLSX
  package with Summary, Items, Departments, Trend and TopProducts sheets;
  item rows contain allocation, net/VAT, commercial adjustment, gross amount,
  supplier, price book and explicit supplier-exception markers. Legacy CSV
  export remains available and formula-injection safe.
- Added durable notification idempotency for
  `(recipient, company, type, correlation)` and kept SignalR/manual refresh
  behavior unchanged.
- Added `N02_EmailOutbox`, deduplicated enqueue, retry state and a hosted
  exponential-backoff worker. Account email calls now enqueue through the
  outbox; SMTP delivery remains disabled by default and is Mailpit-compatible
  at localhost:1025 when explicitly enabled. In-app notifications remain the
  source of truth when email is unavailable.

## Evidence

- Backend Release tests: 384 passed.
- Frontend Release tests: 96 passed.
- Integration Release tests: 14 passed, 6 skipped by the existing LocalDB
  opt-in gate.
- Release solution build: 0 warnings, 0 errors.
- EF pending-model check: no changes since
  `20260716173404_Lean07EmailOutbox`.
- Idempotent SQL script: `%TEMP%\gtas_vpp_lean07_idempotent.sql`,
  116,225 bytes, SHA-256
  `0FC336D7387FF392344C0C05FA250E942A82B37ADD1E90D3184592D1FBF38AE6`.
- Targeted tests cover settlement-aware report totals, XLSX package sheets,
  notification dedupe and email outbox enqueue/retry/sent transitions.

## Recovery and cutline

- CSV and existing report routes remain backward compatible; workbook export is
  additive.
- If SMTP is disabled or unavailable, pending outbox rows remain retryable and
  the inbox continues to work. No real provider, credential or production
  mailbox is required.
- AI insight remains an existing optional/fallback path; no new AI behavior was
  added.
