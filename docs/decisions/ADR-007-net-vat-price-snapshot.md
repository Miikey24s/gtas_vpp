# ADR-007 — Net price, VAT and gross snapshots

- Status: Accepted
- Date: 2026-07-15
- Decision: D-007
- Direct tasks: PRICE-001, SET-001, REPORT-001

## Context

Supplier quotes and contracts can change. Reports cannot recompute historical totals from mutable current prices or one global VAT percentage.

## Decision

Store and validate net unit price plus VAT rate at quote level. On settlement, snapshot net, VAT rate/amount and gross values together with price-book/version, supplier and unit-of-measure context. Represent order discounts and fees explicitly and allocate them using a recorded basis. Use one documented deterministic VND rounding rule with a stored residual adjustment.

## Consequences and guardrails

- Header invariant: subtotal − discounts + fees + VAT = grand total.
- Item/allocation quantity and amount totals reconcile to the header after rounding.
- Editing later prices, departments or VAT does not rewrite a confirmed revision.
- Label “savings” only when a comparable baseline snapshot exists; otherwise report discount/estimate accurately.
