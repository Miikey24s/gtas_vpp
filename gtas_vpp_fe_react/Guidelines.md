# GTAS VPP — Figma Make Guidelines

Figma Make reads this file as standing context. Keep it concise; detailed product
context lives in `../docs/design/VPP-PULSE-FIGMA-MAKE-BUILD-BRIEF.md`.

## Required operating contract

- Work from the latest `origin/Nam`. The obsolete
  `agents/ui-improvements-font-size-alignment` branch is not a valid base.
- The working HEAD must contain commit `bf2cb10` as an ancestor. If it does not,
  stop and refresh the codebase before designing or editing.
- Create a dedicated `figma/*` branch. Never commit, force-push, merge, deploy, or
  open a production cutover directly from `Nam`.
- Before changing code, read this file, `AGENTS.md`, and
  `../docs/design/VPP-PULSE-REACT-FRONTEND-MIGRATION-PLAN.md`, then create or
  update `plan.md`.
- Preserve unrelated work. Do not edit the Blazor frontend, backend, database,
  deployment, authentication, or authorization unless the owner explicitly
  expands the scope.

## Product and source of truth

- GTAS VPP is an internal office-supplies ordering and procurement system.
- React is a modernization, not a pixel-for-pixel Blazor port. Improve the task
  flow while preserving business invariants, permissions, immutable history,
  audit evidence, and exact data.
- Priority: business rules and permissions → current API/OpenAPI → latest owner
  decisions → approved React browser runtime → design research/Figma → Blazor
  only as capability evidence.
- Do not invent API fields, permissions, statuses, data, or client-owned business
  rules. Record missing contracts as blockers.

## Visual and interaction direction

- Direction: **Operational Clarity** — light, precise, modern, calm, and
  data-first. Apple, ChatGPT, Notion, Linear, and Figma may be references, but no
  product is a template or exclusive art direction.
- Prefer square geometry with small consistent radii (`4–6px`), hairline borders,
  purposeful whitespace, Geist typography, and one sky/teal accent per action
  area.
- Avoid large rounded cards, excessive pills, glassmorphism, decorative
  gradients, heavy shadows, nested cards, dashboard-template KPI clutter, and
  animation without meaning.
- Remove prototype language such as “React preview”. Do not repeat user identity
  in both the sidebar and account menu unless each placement serves a distinct
  task.
- Use one interaction grammar for sidebar, tabs, header controls, hover, active,
  focus, disabled, loading, and selected states. Hit areas must be complete and
  keyboard-visible without shifting layout.
- Data screens follow: `Takeaway → Evidence → Context/Comparison → Next action →
Exact detail`. Do not repeat the same status, deadline, count, or action.
- Desktop `1366×768` and `1920×1080` are primary review canvases; tablet and
  mobile must remain usable without horizontal page overflow.
- Light is the baseline. Dark changes tokens, not layout. Print is real code:
  monochrome, action/navigation-free, and pagination-safe.
- Motion is feedback only: approximately `120ms` micro, `180ms` component, and
  `260ms` route transitions. Respect `prefers-reduced-motion`; never stagger
  table rows or animate width/height/layout-heavy properties.

## React, Tailwind, and component rules

- Keep the existing stack: React 19, TypeScript, Vite, Tailwind CSS 4, shadcn/ui,
  React Router, TanStack Query/Table, React Hook Form, Zod, i18next, Lucide,
  Recharts, and Motion.
- Reuse and improve repository-owned primitives in `src/components/ui` and
  `src/components/shared` before adding new components or packages.
- Use semantic tokens from `src/index.css`; do not scatter hard-coded colors,
  radii, shadows, or z-index values through route files.
- Tailwind class names must be complete static strings. Use `cn`, CVA, or an
  explicit variant map; never generate utilities such as `bg-${color}-600`.
- Pages compose feature components. Keep API/query/form orchestration out of
  giant route files and do not duplicate shared shell or table logic.
- API types come only from the generated OpenAPI client. Server state uses
  TanStack Query; forms use React Hook Form + Zod; small UI state uses React
  state/context.
- Every visible UI-owned string, validation message, tooltip, empty state, and
  accessible name must exist in both Vietnamese and English through i18next.
- Use semantic HTML, stable data IDs as React keys, explicit labels, focus return,
  and status text/icons that do not rely on color alone.

## Required state and quality coverage

- Cover relevant loading, empty, normal, long-data, filtered-empty, error, retry,
  unauthorized, forbidden, disabled, pending, conflict, success, and offline
  states.
- Destructive or high-impact actions require confirmation, pending locks, and
  durable feedback. Do not expose raw JSON, stack traces, or backend exception
  names.
- Do not use runtime mock DTOs or fake production data. Contract fixtures belong
  only in tests or clearly isolated visual prototypes.
- Before committing, run `npm run format:check`, `npm run check`, and relevant
  Playwright tests. Confirm VI/EN, Light/Dark/Print where applicable, keyboard
  behavior, no unexpected console/network errors, and no critical/serious axe
  violations.

## Delivery format

- First response for a new scope: audit findings, assumptions, proposed files,
  state matrix, accessibility risks, and a reversible implementation plan.
- Implement only the approved slice. Keep commits small and explain what was not
  changed because of these guidelines.
- A Figma screenshot is design evidence, not completion. Completion requires the
  React route running with the real contract and passing repository QA.
