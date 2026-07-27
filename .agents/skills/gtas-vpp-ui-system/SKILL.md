---
name: gtas-vpp-ui-system
description: Plan, implement, refactor, review, or report GTAS VPP Blazor/Radzen UI using the owner-approved M0-M2 Atlas style contract, reusable design tokens and workspace patterns, responsive route-real QA, and the repository living plan. Use for UI plans, wave reports, visual review artifacts, or changes under src/Frontend/Blazor involving layout, design system, Radzen components, Atlas parity, responsive behavior, visual states, accessibility, or browser verification.
---

# GTAS VPP UI System

## Load the authority

Before editing:

1. Read the root and nearest `AGENTS.md`.
2. Run `./scripts/gtas.cmd preflight -Scope frontend`.
3. Read `docs/design/VPP-PULSE-BLAZOR-UI-RENOVATION-PLAN.md`, especially the authority order, current route ledger, retrofit queue, and AI-first architecture decision.
4. Read `docs/design/VPP-PULSE-UI-UX-AI-TOOLCHAIN.md` and `docs/execution/ATLAS-001.md` section 1.
5. Inspect the real route, DTO/API, permission, fixture, related tests, and the closest existing implementation.

## Classify before extracting

Place each reusable concern in the smallest correct layer:

1. Token: color, spacing, typography, radius, shadow, control height, motion.
2. Primitive: button, badge, field, content state, surface, icon action.
3. Composite: filter bar, action bar, KPI strip, entity header, order detail.
4. Workspace pattern: Account, Collection, ListDetail, SplitEditor, Operation, Analytics.
5. Route: API calls, permissions, business state, orchestration, and route-specific copy.

Use composition, not markup inheritance. Do not create `UniversalPage<T>`, `UniversalGrid<T>`, or a string-configured component framework. Extract an abstraction only after at least two real routes share the same layout and behavior.

## Divide Blazor and Radzen responsibilities

- Razor/HTML owns shell, navigation, card, toolbar, action bar, responsive layout, and content states.
- Radzen owns complex controls where it adds value: DataGrid, Dialog, DropDown, DatePicker, Numeric, validation, and similar widgets.
- Do not wrap all Radzen components. Normalize them through tokens, the Radzen bridge stylesheet, and purposeful composites.
- Keep one global `InteractiveServer` tree. Do not add local render modes without a new architecture decision.

## Implement one vertical slice

1. Reuse the approved M0-M2 visual language; Atlas is read-only reference and the real Blazor route is final authority.
2. Implement loading, empty, filtered-empty, error, denied, disabled, success, and long-data behavior that the route needs.
3. Use `@Loc[]`, semantic HTML, keyboard focus, stable geometry, and tokens instead of inline styles or new hard-coded colors.
4. Query Microsoft Learn or Radzen MCP only for the exact component/API question. If Radzen quota or key fails, stop the Radzen-dependent work.
5. Run focused tests while iterating, then verify the authenticated route with Playwright at `390×844`, `768×1024`, `1366×768`, and `1920×1080` when relevant.
6. Check VI/EN, Light/Dark, console/network, keyboard, accessibility, and Print where the route supports them.
7. Update the living plan in the same change-set when a design decision, owner feedback, route state, or retrofit requirement changes.

## Keep plans and reports coherent

- Use one canonical table when rows share the same axis such as wave, route, state, or component. Put implementation, owner-visible outcome, visual artifact, validation gate, and status in columns of that table.
- Do not place adjacent tables with the same row keys and ask the owner to reconcile them. Merge them; keep separate sections only for cross-cutting rules that do not repeat every row.
- Make the one-glance plan summarize and link to the canonical execution table instead of duplicating its ledger.
- Report each completed wave in one compact block: owner-visible result, Wave Review Board or route evidence, checks actually run, remaining risk, next gate, and commit status.
- Lead reports with what the owner can see or use. Distinguish clearly between technical foundation, partially migrated UI, full current-scope UI, and final hardening.

## Finish

Run `./scripts/gtas.cmd test-frontend` for a focused change and `./scripts/gtas.cmd verify` before a complete repository handoff when proportionate. Review the entire diff, keep evidence outputs ignored, and create a scoped local commit; never push without explicit user instruction.
