# LEAN-A-PLUS traceability matrix

This matrix is source-first: each claim points to the implementation and the
execution evidence that was actually run in the local release candidate.

| Decision / package | Source of truth | Verification evidence |
|---|---|---|
| D-003 request revisions and supplements | `VPPRequestService`, `VPP01_RequestHeader`, `VPP02_RequestDetail` | `docs/execution/LEAN-05.md`; backend request/supplement tests |
| D-004 flat RBAC and four-eyes | `CanonicalRbac`, permission policies, account membership services | `docs/execution/LEAN-02.md`; authorization tests |
| D-005 supplement provenance/quota | `VPPRequestService` supplement workflow and period deadlines | `docs/execution/LEAN-05.md`; supplement tests |
| D-006 primary supplier/exception | `PeriodSettlementService`, `VPP04`–`VPP07` snapshots | `docs/execution/LEAN-06-SET-001.md`, `LEAN-06-SET-002.md`; 3 confirmation tests |
| D-007 net/VAT source | `PriceAsOfResolver`, `PriceCalculationEngine`, `VPP05_SettlementItem` | PRICE-001/002 execution records; resolver/workflow tests |
| D-008 registration/recovery | app-owned Identity, activation/recovery services, rate limiting | `docs/execution/LEAN-03.md`; auth/account tests |
| D-009 independent rebrand | GTAS VPP UI resources and protected visual shell | `docs/execution/LEAN-04.md`; frontend tests |
| D-010 inbox/email/Excel | `N01_Notification`, `N02_EmailOutbox`, `ReportWorkbookBuilder` | `docs/execution/LEAN-07.md`; notification/report tests |
| D-012 A+ cutline | package plan and release gate records | `docs/execution/LEAN-08.md`; Release build/tests/scan |

## Package checkpoints

| Package | Commit checkpoint | Status |
|---|---|---|
| LEAN-05 | `11677b0` | DONE |
| LEAN-06 CAT | `02a4bc4` | DONE |
| LEAN-06 PRICE-001 | `289dda0` | DONE |
| LEAN-06 PRICE-002 | `c00af56` | DONE |
| LEAN-06 SET-001/SET-002 | `e97f781`, `ca3b594` | DONE |
| LEAN-07 | `d6cde93` | DONE |
| LEAN-08 local RC | `8b51a45` | DONE for local gates; authenticated E2E/owner backup remains explicit manual evidence |

## Release claims

- Backend Release: 384 passed.
- Frontend Release: 96 passed.
- Integration Release: 14 passed, 6 skipped by explicit LocalDB opt-in.
- Solution Release build: 0 warnings, 0 errors.
- Current-tree gitleaks: clean.
- Protected working DOCX and UI shell hashes are preserved; no protected file
  is staged by the checkpoints above.
