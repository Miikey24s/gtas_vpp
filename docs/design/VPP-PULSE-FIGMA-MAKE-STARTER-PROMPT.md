# VPP Pulse — Opus 4.8 Build Starter Prompts

Use a fresh Figma Make conversation. The prompts are written in English for a
precise code/build contract; the product UI must still support Vietnamese and
English.

## Prompt 1 — Re-baseline and create the plan only

```text
You are working in Figma Make Build mode with Claude Opus 4.8 on the GTAS VPP
production React codebase.

IMPORTANT: Do not change application code in this prompt. Your only output change
is to create or replace plan.md.

Preflight:
1. Read gtas_vpp_fe_react/Guidelines.md, gtas_vpp_fe_react/AGENTS.md, and
   docs/design/VPP-PULSE-REACT-FRONTEND-MIGRATION-PLAN.md completely.
2. Verify the Git branch is a new figma/* branch based on the latest origin/Nam.
3. Verify commit bf2cb10 is an ancestor of HEAD. If it is not, stop and report
   that this codebase is stale. Do not design against the old
   agents/ui-improvements-font-size-alignment branch or its chat history.
4. Inspect the current React package.json, src/index.css, AppShell, navigation,
   router, i18n, My Orders, and one dense CRUD route. Do not assume the attached
   screenshot or old Make version is current.

Create plan.md for a shared UI foundation review covering:
- App shell: remove prototype wording, resolve duplicate user identity, improve
  expanded/collapsed sidebar, header utilities, notification, and account menu.
- My Orders: takeaway, four or fewer useful evidence points, a single source of
  truth for the next action, exact order detail, and complete state handling.
- One dense CRUD route: Library Items or Access Users, including filter, paging,
  column density, row actions, drawer/dialog, and long-data behavior.

The visual direction is Operational Clarity: square, precise, low-radius,
sky/teal-accented, data-first, calm, and distinctive. Apple, ChatGPT, Notion,
Linear, and Figma are references only—do not copy them and do not use an
Apple-only art direction. Avoid generic dashboard cards, excessive pills,
glassmorphism, decorative gradients, heavy shadows, duplicated copy/actions,
and animation without meaning.

plan.md must include:
- verified branch and HEAD;
- current issues backed by inspected source;
- business, permission, API, audit, and localization invariants;
- design tokens and shared primitives to reuse or change;
- a state matrix for loading, empty, normal, long-data, filtered-empty, error,
  retry, forbidden, disabled, pending, conflict, success, and offline where
  relevant;
- desktop 1366x768 and 1920x1080 designs, then tablet 768x1024 and mobile
  390x844 behavior;
- VI/EN, Light/Dark/Print, keyboard/focus, reduced-motion, axe, console/network,
  and performance coverage;
- exact files expected to change;
- QA commands and rollback boundary;
- explicit non-goals and anything blocked by a missing backend contract.

Do not install dependencies, rewrite routing, invent DTOs, use mock production
data, change backend/Blazor/deployment, commit, push, or open a PR in this prompt.
Finish by summarizing the proposed design direction and the decisions that need
owner review.
```

## Prompt 2 — Implement the approved foundation slice

Use only after reviewing and editing `plan.md`.

```text
Use the latest approved plan.md and implement only its first shared-foundation
slice in the current figma/* branch.

Follow gtas_vpp_fe_react/Guidelines.md. Preserve API, permission, audit, i18n,
Light/Dark/Print, and reduced-motion contracts. Use current repository-owned
shadcn/Tailwind primitives and semantic tokens; do not add a UI framework or
construct Tailwind class names dynamically.

Work in this order:
1. shared tokens and primitives;
2. App shell and both expanded/collapsed navigation states;
3. My Orders normal/empty/error states;
4. the selected dense CRUD route;
5. tests and visual/accessibility QA;
6. update the React living plan and retrofit queue.

Run npm run format:check, npm run check, the relevant Playwright tests, and npm
audit --audit-level=high. Inspect all four required viewports, VI/EN, Light/Dark,
Print where applicable, keyboard focus, reduced motion, console/network, and axe.

Do not modify backend, Blazor, database, deployment, generated API files, or
unrelated routes. Do not merge, deploy, or push directly to Nam. At the end,
provide the changed-file list, test evidence, screenshots/views that need owner
review, remaining risks, and a small commit proposal.
```

## Prompt 3 — A focused owner correction

Use this pattern after selecting an element or attaching one screenshot:

```text
Apply only this owner correction to the selected element and every instance of
the same shared primitive: <describe the correction>.

First identify the owning shared component and token. Do not patch each route
with duplicate classes. Preserve layout at 390, 768, 1366, and 1920 widths,
VI/EN text expansion, keyboard focus, Light/Dark/Print, and reduced motion.
Show the exact files changed and the focused QA performed. Do not modify other
visual decisions or business behavior.
```
