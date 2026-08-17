# LEAN-06 SET-002/UI-006 — Immutable settlement close and correction revisions

Status: DONE for the retained settlement slice.

## Delivered

- Added immutable snapshot tables `VPP04_Settlement`, `VPP05_SettlementItem`,
  `VPP06_SettlementCharge` and `VPP07_SettlementAllocation`.
- Added `POST /api/PeriodSettlement/confirm` with preview hash verification,
  short transaction scope, idempotency-key replay/conflict handling and one
  current revision per company/period.
- Confirmation snapshots mã mặt hàng hệ thống, net unit price, VAT rate, MOQ và lead time
  time, commercial charges and allocation back to request detail, requester
  and department. VND money is rounded to whole units through calculation
  version `price-vat-v2-vnd-whole`; residual rounding is allocated to the
  deterministic final row.
- Confirmation requires the persisted period to be `Pricing` and advances it
  to `Settled` in the same transaction. The legacy `/settle` endpoint remains
  for old clients/tests but is no longer used by the settlement panel.
- Added `POST /api/PeriodSettlement/{settlementId}/correct`. A correction
  requires a 5–500 character reason, a different confirming user (four-eyes),
  a fresh preview hash and a new immutable revision. Prior snapshots are kept
  unchanged and only their current-revision marker is closed.
- Supplier exceptions are explicit, reasoned and resolver-backed; they are
  never silently substituted. The primary price book remains the commercial
  source for the basket, while an exception stores its alternate supplier and
  price-book evidence in the item snapshot.
- UI-006 now confirms only after preview and reuses a stable idempotency key;
  settled periods expose a correction reason field and correction action.

## Evidence

- `SettlementConfirmationTests`: 3 passed (snapshot/reconcile/period advance,
  stale hash rejection, four-eyes correction revision).
- Backend Release tests: 380 passed. Frontend Release tests: 96 passed.
- Integration Release tests: 14 passed, 6 skipped by existing LocalDB gate.
- Release solution build: 0 warnings, 0 errors.
- EF pending-model check: no changes since `20260716171158_Lean06SettlementSnapshots`.
- Idempotent SQL script: `%TEMP%\gtas_vpp_lean06_set002_idempotent.sql`,
  113,862 bytes, SHA-256
  `1C84352104F25337E279EE4E618041B1670D58E5418B761DB3F182FA1333FCF5`.

## Recovery and cutline

- The migration creates only new audit tables and has a forward-only `Down`;
  snapshot history must not be destroyed by rollback.
- A stale hash, changed price book, concurrent current revision or reused key
  with different payload is rejected. Retry the same payload/key for a safe
  replay; use a fresh preview and key for a correction.
- No purchase order, receipt, inventory or accounting behavior is introduced.
