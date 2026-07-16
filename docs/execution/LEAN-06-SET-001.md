# LEAN-06 SET-001 — Whole-company settlement preview and primary supplier

Status: DONE for the read-only preview slice. SET-002/UI-006 remains next for immutable confirmation, allocations and correction revisions.

## Delivered

- Added `POST /api/PeriodSettlement/preview` under `PeriodSettle` authorization.
- Aggregates only current regular submitted/approved requests plus current approved supplements for the target period; pending supplements, missing supplement provenance and empty baskets are explicit blockers.
- Reuses PRICE-002 comparison and `price-vat-v1` for coverage, MOQ, subtotal, discount/rebate/fee/shipping/VAT, lead time and deterministic ranking.
- Selects one eligible primary supplier/book deterministically, or pins a requested supplier/book only when it has complete coverage. Missing/ambiguous coverage never falls back to the first row.
- Produces a stable SHA-256 input hash over period, `PriceAsOfUtc`, aggregated item quantities, pinned selection and declared exceptions.
- Supplier exceptions require item, supplier and a 5–500 character reason; invalid exceptions block confirmation. Quote merging remains deferred to SET-002 where immutable exception evidence is stored.
- Updated the Radzen settlement panel so preview is required before the existing settle action is enabled and shows the chosen supplier, coverage, total and input-hash prefix.

## Evidence

- Targeted settlement preview + legacy settlement + wire contract tests: 12 passed.
- Release solution build: 0 warnings, 0 errors.
- Existing settlement mutation remains unchanged in this checkpoint; immutable snapshot confirmation is owned by SET-002.

## Recovery and cutline

This checkpoint is read-only and adds no schema. A stale or changed basket/price book produces a different input hash and must be previewed again. No purchase order, receipt, inventory or accounting behavior is introduced.
