# ADR-012 — A+ release cutline for 2026-08-15

- Status: Accepted
- Date: 2026-07-15
- Decision: D-012
- Direct tasks: DOC-002, REL-001

## Context

The owner is working full-time with a working deadline of 2026-08-15. The product needs credible core correctness and a polished Vietnamese-first demo without attempting every possible feature.

## Decision

Use the A+ cutline: core-first plus polished core UI. P0 containment and minimum P1 authorization, period/request/supplement, pricing/whole-company settlement, reporting, registration, durable inbox/email sandbox, Excel, rebrand, QA and thesis are mandatory. Local deterministic demo is the baseline; DigitalOcean is the preferred demo only after security/deploy/restore gates pass.

## Consequences and guardrails

- If schedule slips, cut P2/P3, WOW/AI, enhanced autosave/saved views and non-core route polish first.
- Do not cut correctness, privacy-safe handling of any existing drafts, registration security, dashboard/Excel, durable inbox or sandbox email evidence.
- Full English/dark mode, PDF, Teams/Zalo, full PO/inventory/accounting and AI mutation are outside this release.
- Thesis claims only features proven in the frozen release candidate.
