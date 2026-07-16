# LEAN-06 PRICE-002 — Typed price-book workflow and quote comparison

Status: DONE for draft/publish/expire and comparison data. SET-001 remains next for whole-company settlement preview and primary-supplier selection.

## Delivered

- Added typed `publish` and `expire` commands for L07 price books. Publish requires supplier ownership, VND, effectivity, at least one active item, supplier-consistent mappings, no duplicate active item, valid net/VAT/MOQ/lead values, commercial-term bounds and no overlapping published item for the same supplier. Published books are immutable.
- Added rowversion/reason checks and status audit fields (`PublishedAtUtc`, `PublishedByUserId`, `ExpiredAtUtc`, `ExpiredByUserId`, `StatusReason`). Expire preserves history and closes the half-open validity interval.
- Added deterministic whole-basket comparison at `POST /api/VPPPriceList/compare`: coverage/missing items, MOQ blockers, subtotal, discount, rebate, fee, shipping, VAT, grand total, lead time, validity and deterministic rank. It uses `PriceCalculationEngine` version `price-vat-v1`, the same engine/version consumed by PRICE-001 resolver.
- Added minimal Radzen library adaptation: supplier/effectivity/version/commercial-term editor fields, Draft/Published/Expired display, publish/expire actions, published-row mutation guards and supplier-aware item editing.
- Added migration `20260716163450_Lean06PriceCommercialTerms` for commercial terms and status-audit columns; it backfills zero terms explicitly and is forward-only.

## Evidence

- Targeted pricing workflow/resolver/legacy tests: 26 passed.
- Release solution build after workflow/UI changes: 0 warnings, 0 errors.
- LocalDB opt-in migration/resolver path: 1 passed after both PRICE-001 and PRICE-002 migrations.
- Idempotent migration script: `%TEMP%\\gtas_vpp_lean06_price002_idempotent.sql`, 103,597 bytes, SHA256 `6DA325AB73E901F5A31FCF30DF309C37BD4B80517021D3F8DF268BA07BF6A06`.

## Recovery and cutline

Published rows are not edited or hard-deleted. Corrections use a new draft/version; expiry retains the previous interval and audit reason. Comparison returns blockers instead of selecting an arbitrary supplier/book, so SET-001 can require complete coverage before primary-supplier confirmation.
