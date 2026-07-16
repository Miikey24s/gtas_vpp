# LEAN-06 PRICE-001 — Versioned effective price books and deterministic resolver

Status: DONE for the schema/backfill/resolver slice. PRICE-002 remains next for typed draft/publish/expire management and comparison UI.

## Delivered

- Extended the existing L07 price-list/L06 supplier mapping model additively with supplier ownership, version, UTC effectivity, status, currency, VAT policy, rowversion and legacy backfill status.
- Added decimal net-price storage and explicit VAT rate, MOQ, lead time and supplier SKU fields. Legacy `Price` remains as a compatibility alias and is backfilled into `NetPrice`.
- Added `PriceAsOfResolver` with half-open `[EffectiveFromUtc, EffectiveToUtc)` windows, locked-book precedence, contract/published/default precedence, supplier matching, duplicate/missing/expired/legacy blockers, MOQ validation and deterministic net/VAT/gross calculation version `price-vat-v1`.
- Published price books and mappings are immutable through the existing mutation services; callers must create a new draft version for corrections. Legacy unscoped lists remain readable but resolve as a typed backfill blocker instead of silently choosing a supplier.
- Added migration `20260716161156_Lean06PriceEffectivity` with negative-price preflight, deterministic supplier/version backfill, explicit `legacy-zero` VAT marker, decimal conversion, effectivity/status backfill, constraints and covering indexes. `Down` is intentionally forward-only because reverting would discard decimal/VAT/version data.
- Added `POST /api/VPPPrice/resolve` for authenticated read-only resolution. PRICE-002 owns publish/expire commands and management UI.

## Evidence

- Release solution build: 0 warnings, 0 errors.
- Targeted resolver/legacy pricing tests: 21 passed.
- Opt-in LocalDB SQL migration + resolver test: 1 passed; fresh fixture applied the new migration and executed the resolver against SQL Server translation.
- Idempotent migration script: `%TEMP%\\gtas_vpp_lean06_price001_idempotent.sql`, 97,565 bytes, SHA256 `C4652280DF4D56EDF8711EAE70B181E92C177C1A78BF335565E0B899706CAA74`.
- EF pending-model gate: `No changes have been made to the model since the last migration.`
- Existing price-list and supplier-mapping tests remain green after preserving legacy unscoped-list behavior.

## Recovery and data-quality boundary

The migration is additive and keeps legacy columns. Rows with exactly one active supplier receive ownership; rows with no or multiple active suppliers remain present with `LegacyBackfillStatus` (`NO_ACTIVE_ITEMS` or `MULTIPLE_SUPPLIERS_VAT_ZERO`) and are blocked by the resolver until PRICE-002 supplies an explicit version. Legacy VAT is recorded as `legacy-zero`; it is not presented as a verified supplier tax quote. Recovery is paired backup/restore or a forward correction migration, not a destructive `Down`.
