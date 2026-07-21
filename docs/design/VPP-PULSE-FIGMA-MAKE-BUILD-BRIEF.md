# VPP Pulse — Figma Make Build Brief

> **Audience:** Figma Make design/build agent using Claude Opus 4.8 Build mode
>
> **Repository:** `Miikey24s/gtas_vpp`
>
> **Authoritative branch:** `Nam`
>
> **Minimum valid source baseline:** commit `bf2cb10` must be an ancestor of the
> working HEAD
>
> **Scope:** React/Tailwind design exploration and implementation only; no direct
> production cutover

## 1. Why the old Make context must not continue

The local branch `agents/ui-improvements-font-size-alignment` points to
`da02844` from **2026-06-27**. It predates the current React frontend, business
data localization, request policy, UI research, and most backend capabilities.
It has no unique work that should be merged into the current branch.

Do not ask the agent to “continue where you left off” in that codebase. A long,
stale Make conversation also carries obsolete assumptions into every prompt.
Start a fresh Make session or duplicate only the visual experiment, then attach
or connect the current source.

## 2. Choose the correct Figma workflow

### A. Preferred — Make on a local/existing codebase beta

Use this only if the Figma desktop build exposes importing an existing GitHub
repository or local folder and supports Git branches/commits.

1. Fetch `origin` and check out the latest `Nam`.
2. Verify the current branch is clean and contains `bf2cb10`:

   ```powershell
   git fetch origin
   git switch Nam
   git pull --ff-only origin Nam
   git merge-base --is-ancestor bf2cb10 HEAD
   git status --short --branch
   ```

3. Create a branch such as `figma/react-foundation-review-20260721`.
4. Open `D:\WORK\gtas_vpp` as the codebase, but restrict changes to
   `gtas_vpp_fe_react/` and the React living plan.
5. Review the diff and repository tests before creating a pull request. Do not
   merge or deploy from Figma.

### B. Safe fallback — ordinary Figma Make prototype

Ordinary Make-to-GitHub sync is not a pull/sync mechanism for this repository.
It creates a Make-owned repository and pushes one way, so GitHub edits may not
return to Make and may be overwritten by a later Make push.

Use the Make file as a design prototype only:

1. Start a fresh Make file.
2. Attach no more context than the current task needs. A recommended first set is:
   - `Guidelines.md`
   - `gtas_vpp_fe_react/Guidelines.md`
   - `gtas_vpp_fe_react/AGENTS.md`
   - `docs/design/VPP-PULSE-REACT-FRONTEND-MIGRATION-PLAN.md`
   - `gtas_vpp_fe_react/package.json`
   - `gtas_vpp_fe_react/src/index.css`
   - `gtas_vpp_fe_react/src/components/app/app-shell.tsx`
   - `gtas_vpp_fe_react/src/components/app/app-navigation.ts`
   - `gtas_vpp_fe_react/src/app/router.tsx`
   - the route `.tsx` currently being reviewed

   Replace a general file with one representative API response `.json` or
   screenshot only when the route needs it; keep each prompt within Figma's
   attachment limit and avoid redundant context.

3. Explicitly tell Make whether each attachment is authoritative code, exact
   content, or visual inspiration.
4. Export the changed files/ZIP or send the design back for manual integration by
   Codex. Do not use Make's one-way repository as the new GTAS source of truth.

## 3. Current product context

GTAS VPP manages company office-supplies demand from employee request through
department/company review, supplements, period settlement, suppliers, price
books, access administration, reporting, and immutable audit history.

The React frontend is already broad, not a blank prototype:

- account flows and protected session bootstrap;
- responsive application shell, notification center, VI/EN and theme controls;
- My Orders, create/edit/detail/history, catalog;
- department/company management and supplement approval;
- period, settlement preview, correction and revision history;
- library/master data, price lists and prices;
- access users/groups/component permissions;
- reports, print/export and evidence-backed insights;
- Light, Dark, Print, reduced-motion and automated browser QA.

The current work queue remains in
`VPP-PULSE-REACT-FRONTEND-MIGRATION-PLAN.md`. The Figma agent must not replace it
with a generic dashboard plan.

## 4. Source-of-truth order

When inputs conflict, use this order:

1. Backend business invariant, permission and immutable audit behavior.
2. Current Swagger/OpenAPI and generated React client.
3. Latest owner decisions in the React living plan and this brief.
4. React browser runtime accepted by the owner.
5. Product blueprint, Personal Design DNA and Figma explorations.
6. Blazor as capability evidence only, never pixel or route authority.

## 5. Owner design DNA to preserve

- Modern, clear and professional, with a distinctive but controlled Gen-Z edge.
- Square geometry and small radii are preferred over soft, inflated rounded UI.
- Sky blue and teal are accents; content hierarchy must still work in grayscale.
- The owner dislikes repeated copy, duplicated status/actions, unused whitespace,
  crowded tables, arbitrary scrolling, and generic dashboard templates.
- “Wow” should come from structure, typography, responsive behavior, meaningful
  motion, and data storytelling—not decorative effects.
- Dense business data uses progressive disclosure: essential table columns,
  column profiles, filters, paging, row actions and detail drawer/page.
- Do not force every long workflow into one viewport by shrinking type or targets.
  Prefer sticky regions and intentional internal scrolling when the task requires.

## 6. Open React feedback the agent must account for

- Remove “React preview” from the product shell; this is real product UI.
- Avoid duplicate user identity in sidebar footer and account menu. Each location
  must have a distinct task or one should be removed.
- Sidebar collapse/expand may learn from ChatGPT's clarity and compact icon rail,
  but must use GTAS information architecture and tokens rather than copying it.
- Header, utility buttons, sidebar, tabs and account menu need one consistent
  hover/active/focus grammar.
- All visible strings—including empty/error states, tooltips, validation,
  notifications and chart summaries—must switch VI/EN.
- Light, Dark and Print are product modes, not Figma-only variants.
- Data-driven pages must identify the question, evidence, comparison, next action
  and exact detail before choosing cards, tables or charts.
- Animation must explain feedback or spatial relation and remain equivalent with
  reduced motion.

## 7. First design/build mission

Do not redesign all routes in one generation. First establish a reusable visual
contract across three representative surfaces:

1. **App shell:** expanded/collapsed sidebar, header utilities, notification and
   account menu.
2. **My Orders:** period takeaway, evidence, single-source action, exact order
   detail and empty/normal/error states.
3. **Dense CRUD reference:** Library Items or Access Users, proving filter,
   column density, paging, row action, drawer/dialog and long-data behavior.

For each surface, review `1366×768` and `1920×1080` first, then prove tablet and
mobile behavior. Show Light and Dark; prove Print on the data-heavy route.

The goal is a shared system that can be propagated, not three unrelated mockups.

## 8. Plan-first output contract

The first Opus 4.8 Build prompt must make no code changes. It creates `plan.md`
with:

- verified branch and HEAD;
- files and current components inspected;
- preserved business/permission contracts;
- current visual problems with evidence;
- proposed token and shared-component changes;
- route/state/viewport matrix;
- VI/EN, Light/Dark/Print and motion coverage;
- accessibility and performance risks;
- exact files expected to change;
- QA commands and rollback boundary;
- what will deliberately not change.

Only after the owner reviews `plan.md` should Make implement the selected slice.

## 9. Technical guardrails

- Keep React 19 + TypeScript + Vite + Tailwind CSS 4 + shadcn/ui.
- Reuse repository components. Do not install a new UI kit or replace Tailwind.
- Use complete static Tailwind utility names; dynamic string construction is not
  detected reliably by Tailwind's source scanner.
- Generated OpenAPI types are the only API types. Never hand-copy backend DTOs.
- Do not hard-code bilingual copy in JSX; use i18next.
- Do not create runtime mock APIs, fake permission logic, or local business-rule
  fallbacks.
- Do not edit generated API files by hand.
- Use stable domain IDs for React list keys, not array indexes or random values.
- Keep route files compositional and shared primitives reusable.
- Do not expose credentials, tokens, cookies, connection strings or production
  data to Figma attachments or generated code.

## 10. Acceptance and handoff

Before a Figma-authored change is proposed for integration:

```powershell
cd D:\WORK\gtas_vpp\gtas_vpp_fe_react
npm run format:check
npm run check
npm run test:e2e
npm audit --audit-level=high
```

Also verify:

- no horizontal page overflow at `390×844`, `768×1024`, `1366×768`, and
  `1920×1080`;
- keyboard-only navigation and visible focus;
- focus returns after dialog/drawer/menu closes;
- no unexpected console error or failed request;
- axe has no critical/serious violations;
- VI/EN and Light/Dark work on the changed route;
- Print remains usable where applicable;
- reduced-motion preserves feedback and information;
- no unrelated Blazor/backend/deployment diff.

A visual preview is not an approval. The owner approves the React browser result,
then Codex reviews/integrates the branch and updates the living plan.

## 11. Research basis

- [Figma Make guidelines](https://help.figma.com/hc/en-us/articles/33665861260823-Add-guidelines-to-Figma-Make)
- [Figma Make prompt attachments](https://help.figma.com/hc/en-us/articles/31304529835671-Attach-designs-and-images-to-a-prompt)
- [Figma Make plan-before-build workflow](https://help.figma.com/hc/en-us/articles/35710574222487-Beyond-the-basics-Using-Figma-Make)
- [Figma Make AI-credit/context practices](https://help.figma.com/hc/en-us/articles/40097793879191-Best-practices-for-optimizing-AI-credits-in-Figma-Make)
- [Figma Make on local code](https://www.figma.com/blog/figma-make-now-on-your-local-code/)
- [Tailwind source detection](https://tailwindcss.com/docs/detecting-classes-in-source-files)
- [React list keys](https://react.dev/learn/rendering-lists#keeping-list-items-in-order-with-key)
- [WCAG 2.2](https://www.w3.org/TR/WCAG22/)
