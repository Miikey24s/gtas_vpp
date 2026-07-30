---
name: gtas-vpp-ui-system
description: Plan, implement, refactor, review, or report GTAS VPP Blazor/Radzen UI using the owner-approved OpenAI/Codex-inspired visual contract, reusable design tokens and workspace patterns, responsive route-real QA, and the repository living plan. Use for UI plans, wave reports, visual review artifacts, or changes under src/Frontend/Blazor involving layout, design system, Radzen components, Atlas reference parity, responsive behavior, visual states, accessibility, or browser verification.
---

# GTAS VPP UI System

## Load the authority

Before editing:

1. Read the root and nearest `AGENTS.md`.
2. Run `./scripts/gtas.cmd preflight -Scope frontend`.
3. Read `docs/design/VPP-PULSE-BLAZOR-UI-RENOVATION-PLAN.md`, especially the authority order, current route ledger, retrofit queue, and AI-first architecture decision.
4. Read `docs/design/VPP-UI-MOTIF-CATALOG.md` and resolve the target route in `Helpers/UiRouteCatalog.cs`; these two files are the canonical mapping from UI intent to component/pattern.
5. Read `docs/design/VPP-PULSE-UI-UX-AI-TOOLCHAIN.md` and `docs/execution/ATLAS-001.md` section 1.
6. Inspect the real route, DTO/API, permission, fixture, related tests, and the closest existing implementation.

## Resolve the motif before writing markup

For every changed region, name its motif ID from `VPP-UI-MOTIF-CATALOG.md` before implementation. Use the route profile to determine workspace, data-source mode, density, toolbar, footer, responsive strategy, and required states.

- Collection actions belong to `VppCollectionHeader`; query/display actions belong to `VppDataToolbar`; row actions belong to the row action column; workflow actions belong to the workflow footer.
- Filter, decision, page-size, and header-tab selectors share tokens but keep different typed semantics. Never replace them with one string-configured universal selector.
- A route may own columns, copy, API, permission, and business actions, but it may not redefine shared border, popup, focus, row rhythm, footer anchor, page inset, or motion.
- When the catalog has no matching motif, keep the first implementation route-local and record `NEEDS MOTIF REVIEW`; extract only after two real consumers share behavior.

## Classify before extracting

Place each reusable concern in the smallest correct layer:

1. Token: color, spacing, typography, radius, shadow, control height, motion.
2. Primitive: button, badge, field, content state, surface, icon action.
3. Composite: filter bar, action bar, KPI strip, entity header, order detail.
4. Workspace pattern: Account, Collection, ListDetail, SplitEditor, Operation, Analytics.
5. Route: API calls, permissions, business state, orchestration, and route-specific copy.

Use composition, not markup inheritance. Do not create `UniversalPage<T>`, `UniversalGrid<T>`, or a string-configured component framework. Extract an abstraction only after at least two real routes share the same layout and behavior.

## Retire legacy without compatibility drift

Classify touched legacy material as `KEEP`, `MERGE`, `MIGRATE`, or `DELETE` in the execution/consumer ledger.

Delete an adapter, component, selector, CSS block, or JS module only after source scan proves zero consumers, the canonical replacement has real consumers, focused build/tests pass, and route-real behavior remains equivalent. Once those gates pass, remove the legacy implementation and its aliases entirely instead of keeping a second visual path “for safety”.

Architecture tests must prevent retired names and files from returning. Do not mass-delete vendor CSS, persisted theme compatibility, or an external integration contract merely because repository source has no direct string reference.

## Divide Blazor and Radzen responsibilities

- Razor/HTML owns shell, navigation, card, toolbar, action bar, responsive layout, and content states.
- Radzen owns complex controls where it adds value: DataGrid, Dialog, DropDown, DatePicker, Numeric, validation, and similar widgets.
- Do not wrap all Radzen components. Normalize them through tokens, the Radzen bridge stylesheet, and purposeful composites.
- Keep one global `InteractiveServer` tree. Do not add local render modes without a new architecture decision.

## Implement one vertical slice

1. Reuse the approved OpenAI/Codex-inspired minimal visual language; Atlas is read-only layout/business reference and the real Blazor route is final authority.
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
- For a non-trivial wave plan, include `Model + effort` in that same canonical table. Base the recommendation on current official OpenAI guidance, task risk, and a dated owner-provided account/quota snapshot; ask once if the active task has no current snapshot.
- Treat quota/account counts as volatile `THREAD/GOAL` data, not repository invariants. Recommend model changes only at wave or major checkpoint boundaries, and distinguish the recommended route from the model that is actually active.
- Report each completed wave in one compact block: owner-visible result, Wave Review Board or route evidence, checks actually run, remaining risk, next gate, and commit status.
- Lead reports with what the owner can see or use. Distinguish clearly between technical foundation, partially migrated UI, full current-scope UI, and final hardening.

## Finish

Run `./scripts/gtas.cmd test-frontend` for a focused change and `./scripts/gtas.cmd verify` before a complete repository handoff when proportionate. Review the entire diff, keep evidence outputs ignored, and create a scoped local commit; never push without explicit user instruction.
