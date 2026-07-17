# LEAN-06 CAT-001 — Typed L04 catalog and server-side search

Status: DONE for the L04 VPP catalog slice. LEAN-06 remains in progress for PRICE-001/002 and SET-001/002.

## Delivered

- Added typed L04 contracts for create, update and status changes in the shared DTO project.
- Added `IVppCatalogService`/`VppCatalogService` and `VppCatalogController`.
- Query path stays on `IQueryable`: search, allowlisted filter, allowlisted order, distinct values, count and `Skip/Take` are applied before page materialization.
- Search covers code, name, category code/name and UOM code/name. SQL Server uses `Vietnamese_100_CI_AI`; non-SQL providers use case-insensitive fallback. LIKE metacharacters are escaped.
- L04 create/update validate required fields, references and duplicate code. Status changes are soft delete/restore; hard delete and generic L04 mutations return `405 CatalogTypedEndpointRequired`.
- Request catalog `/products` and the legacy `/products/lookup` read path now use the same typed service. Lookup is capped at 100; the order wizard uses server paging.
- Added L04 unique code and covering category/status/code indexes. Migration preflight rejects blank/over-length values and duplicate codes before the non-null/unique change.

## Evidence

- `dotnet build gtas_vpp_be/gtas_vpp_be/gtas_vpp_be.csproj -c Release --no-restore`: 0 warnings, 0 errors.
- `dotnet build gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe.csproj -c Release --no-restore`: 0 warnings, 0 errors.
- Catalog/controller/request targeted backend tests: 25 passed (including 10,000 synthetic rows, Radzen filter shapes, typed endpoint and legacy-mutation rejection).
- Frontend catalog paging contract tests: 2 passed.
- Opt-in LocalDB SQL search test: 1 passed; verified accent-insensitive `but bi` search and literal `100%` search.
- Opt-in LocalDB migration run: clean database reached `20260716150843_Lean06CatalogIndexes`; `VPPCode`/`VPPName` are non-null with 64/250-character SQL limits, `UX_L04_VPP_VPPCode` is unique, and `IX_L04_VPP_Active_Category_Code` includes `VPPName,UOMId`.
- Idempotent script: `%TEMP%\\gtas_vpp_lean06_catalog_idempotent.sql`, 81,428 bytes, SHA256 `A093F86DB87F19CF4DA37F89FF081F0FD5F95C42FE0B10682DAEDBC2906AF564`.

## Follow-up

Price-book/effectivity/VAT resolver work starts at PRICE-001; no pricing or settlement behavior is changed in this checkpoint.
