# ADR-015 — Controlled two-supplier settlement recommendation

- Status: **Under review — execution paused**
- Date: 2026-08-18
- Decision: D-015
- Direct tasks: PRICE-IMPORT-001, SET-002, UI-SYSTEM-001
- Supersedes: the one-supplier-only interpretation of D-006; the primary-supplier snapshot remains

> Owner review `2026-08-18`: quyết định dùng thật một hay hai nhà cung cấp được mở lại. UI giữ tắt; chưa triển khai mở rộng thêm. ADR này ghi lại thiết kế đã có, nhưng không còn là approval để bật tính năng. Xem `docs/execution/ORDERING-PRICING-REVISION-20260818.md`.

## Context

One supplier remains the simplest default, but price lists may differ by item and two suppliers may together cover demand or reduce the total after VAT. Shipping and contract terms are not reliable enough in the current product to optimize automatically.

## Decision

- Manual selection of one supplier remains the default workflow.
- The backend may recommend at most two suppliers. It compares deterministic item prices plus VAT only.
- Each item and its full quantity is assigned to exactly one supplier; quantities are never split across suppliers.
- A cost-saving recommendation requires complete item coverage and at least 2% saving versus the cheapest complete one-supplier quote.
- If no supplier covers all items alone, a two-supplier plan may be recommended when their combined coverage is complete.
- The recommendation is never applied automatically. A manager explicitly chooses `Dùng đề xuất` and sees the allocation again in the settlement preview.
- The primary supplier and price list stay on the settlement header. Lines assigned to the second supplier use the existing explicit exception snapshot with a locked supplier and price list.
- Quotes with discount, rebate, fee or shipping terms are excluded from automatic optimization until those terms have a trustworthy model.

## Consequences and guardrails

- No new database table is required: `SettlementItem` already snapshots `SupplierId`, `PriceListId`, price and VAT per item.
- Excel/PDF settlement exports must identify the supplier used by each item.
- The existing four-eyes rule and immutable settlement revisions remain unchanged.
- This is not a purchase-order, contract, receiving, payment or logistics module.
