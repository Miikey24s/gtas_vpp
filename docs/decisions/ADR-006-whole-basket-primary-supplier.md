# ADR-006 — Primary supplier for each effective settlement revision

- Status: Accepted
- Date: 2026-07-15
- Decision: D-006
- Direct tasks: PRICE-001, SET-001

## Context

The company normally consolidates the entire period and buys in bulk from one supplier for better commercial terms. A supplier may still lack a quoted item.

## Decision

Each effective whole-company settlement revision has exactly one `PrimarySupplier`. Apply its effective quote/price book to all compatible items. A line may use another supplier only as an explicit exception with permission, actor, timestamp, reason and quote evidence. Missing, expired or ambiguous coverage blocks confirmation; never silently select the first mapping.

A correction may change the primary supplier only by creating a new immutable revision; prior snapshots remain unchanged.

## Consequences and guardrails

- Preview must show coverage, exceptions and unresolved lines before confirmation.
- Every confirmed line snapshots supplier, quote/price-book version and price inputs.
- This is not a full purchase-order, receiving or accounts-payable module.
