# LEAN-05 — Request and supplement core

**Status:** DONE
**Date:** 2026-07-16
**Dependency:** LEAN-02 trusted access, LEAN-04 UI foundation
**Next package:** LEAN-06 procurement and immutable settlement

## Scope and decisions

- The business period is a persisted company aggregate with the Vietnam cycle
  `05 → 04`. Boundary calculations are deterministic in Vietnam wall-clock and
  stored as UTC. Recovery creates or advances periods after downtime without
  creating duplicate company/period rows.
- A user has at most one regular request series per period. Cancellation keeps
  the slot and history; later replacement is another immutable revision in the
  same series, not a second regular request.
- Update, cancel, approve and reject require rowversion. Idempotency keys replay
  the same command and reject a different payload. A concurrent loser receives
  a conflict, including when another revision already superseded the requested
  row.
- A supplement requires a base request in the same period and a reason of
  5–500 characters. The default policy permits one pending supplement, at most
  three approved supplements and six audited attempts per user/base/period,
  with a separate approval-grace deadline.
- Department approvers see their department queue unless they have explicit
  company-wide scope. Approval/rejection actions, actor, reason, revision and
  correlation data are recorded in the request audit timeline.
- No production database, DigitalOcean resource, real mail provider or waived
  historical Google-key alert was mutated by this package.

## Implemented surface

- Added `VPP00_Period`, period state/policy/services and the recovery worker;
  registered explicit period/deadline/quota/recovery configuration.
- Extended request headers and logs with period, immutable series/revision,
  lineage, supplement, cancellation, idempotency, actor and concurrency data.
- Added create/update/cancel/history/approve/reject API contracts and
  server-authoritative scope checks; synchronized runtime and migration EF
  mappings, filtered uniqueness and wire-contract tests.
- Added request history and rejection dialogs, period/status/quota guidance,
  supplement reason validation, replacement/cancel actions, safe errors,
  Vietnamese/English resources and per-user/per-period draft isolation with
  logout cleanup.
- Made the request page non-prerendered interactive server UI and connected the
  global busy state to the shell so stale prerender DOM and blocking loader
  overlays cannot intercept E2E/user actions.
- Hardened isolated Aspire/LocalDB E2E personas and semantic journeys for
  regular revision/cancel/history, supplement approve/reject/audit, company and
  department summaries and product catalog browsing.
- Fixed the existing Radzen custom column picker to preserve internal column
  visibility and trigger grid state without an unconditional data reload. Tab
  sticky CSS now supports both direct-nav and nav-container Radzen DOM forms.

## Database migration and upgrade evidence

- Migration:
  `20260716063247_AddPeriodRequestRevisionAndSupplementWorkflow`.
- The migration performs preflight validation, legacy period/revision lineage
  backfill, cancellation restoration, invariant checks, indexes and period FK
  creation. `Down` is intentionally forward-only and throws rather than
  pretending that the data transformation is reversible.
- Fresh apply is exercised by the disposable LocalDB fixture. All four opt-in
  LocalDB cases passed: migrate/seed/reseed/reset/cleanup, stale-manifest
  recovery, concurrent fixture isolation and concurrent period ensure.
- The retained legacy validation database was queried again after final build:
  `PeriodCount=2`, `BrokenLineage=0`, `RestoredCancellation=2`,
  `DuplicateCurrentRegularGroups=0`, `MigrationApplied=1`.
- The current-tree idempotent SQL script is `78,520` bytes with SHA-256
  `DEAD8CBF4C808ABBC1EB61DA21ABD87B2EEF98B158245F68AF86BCAFABCC68E6`.
- EF reports no pending model changes.

## Acceptance evidence

| Gate | Result |
|---|---:|
| `dotnet build gtas_vpp.sln -c Release --no-restore` | PASS — 0 warnings, 0 errors |
| Backend Release tests | PASS — 353/353 |
| Concurrent replacement race repetition | PASS — 10/10, then full backend green |
| Frontend Release tests | PASS — 94/94 |
| Integration safety suite | PASS — 14 passed, 4 explicit LocalDB opt-in skips |
| LocalDB opt-in cases | PASS — 4/4; cleanup leaves only `MSSQLLocalDB` and no QA temp root |
| Isolated UI E2E full suite | PASS — 16/16 in 9m55s |
| Current-tree regular update/cancel/history E2E after final backend correction | PASS — 1/1 |
| EF `has-pending-model-changes` | PASS — no pending changes |
| Idempotent migration script | PASS — generated from current tree, 78,520 bytes |
| Legacy LocalDB upgrade invariants | PASS — lineage/uniqueness/history checks above |
| Gitleaks v8.30.1 current-tree scan | PASS — no leaks found |
| `git diff --check` | PASS — exit 0; LF→CRLF advisories only |

The E2E suite ran only with `GTAS_E2E_ISOLATED=1` and the explicit mutation
acknowledgement. Plain authenticated and mutating runs remain fail-closed before
fixture creation.

## Rollback and recovery

- Code/UI rollback is a normal revert of the LEAN-05 commit only while the
  database has not applied the migration.
- After migration apply, database rollback is a verified paired backup/restore
  or a forward correction. Do not use the guarded `Down` path and do not claim
  that reverting source reverses the legacy backfill.
- If a request defect is found after release, freeze the affected period/action,
  preserve request/log history and issue a forward correction. Do not delete or
  rewrite request revisions to repair presentation.

## Explicit residuals

- `VPPRequestService` retains private legacy helpers that are dead code; remove
  them only in a dedicated hardening/refactor slice.
- Lineage is protected by service rules and unique indexes, but self-FKs and
  additional CHECK constraints remain deferred. `VPP00_Period.RowVersion`
  remains nullable.
- The product lookup still materializes the current lookup set; server paging
  and search for at least 1,000 products belong to LEAN-06.
- Full English/dark-theme polish, axe-core automation and broader performance
  work remain LEAN-08 scope.
- The owner's waiver for eight historical Google API-key alerts is unchanged;
  they remain open and are not described as revoked.
- Four user-owned files were preserved and must not be staged:
  - `LVTN/NguyenAnNam_DH52201078_working.docx` — `E9E91C5A9A67F736E9CEAEC0DA1282DC68AF44E3D36039A465E214F0756AE17D`
  - `src/Frontend/Blazor/Components/App.razor` — `E7FAD87A1CF792468E5378FA8ED3CFF0DFA5CB459F62DF72A9CFE3977F0F6B36`
  - `src/Frontend/Blazor/wwwroot/css/vpp-login.css` — `622C4C976ABE287A7CB6ED78DC984D58AF04CE67B03FCAD9C351AF80BB248319`
  - `src/Frontend/Blazor/wwwroot/css/vpp-responsive.css` — `81E49BD90CF3C823076CA3F8B0A3B5192038719E3D4605C4EC0EA176CDF28ACB`

## Handoff

LEAN-06 starts with `CAT-001 → PRICE-001 → PRICE-002 → SET-001 →
SET-002/UI-006`: typed catalog paging/search, effective net/VAT price books,
deterministic price resolution, company-wide quote preview, one primary supplier
with reasoned exceptions, immutable settlement/allocation snapshots, idempotent
close and correction revisions. Do not expand into full PO, inventory or
accounting.
