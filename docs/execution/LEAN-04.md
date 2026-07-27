# LEAN-04 — UI foundation and independent brand

**Status:** DONE
**Date:** 2026-07-16
**Dependency:** LEAN-02 access contracts, ARCH-001, D-009
**Next package:** LEAN-05 request and supplement core

## Scope and decisions

- Kept Blazor Server, Radzen and the existing token/CSS system. No React rewrite, second frontend or broad visual reimplementation was introduced.
- Radzen documentation/MCP was checked before implementation for theme variables, `RadzenLayout`/`RadzenSidebar`/`RadzenPanelMenu`, responsive patterns and form validation. The selected approach uses Radzen primitives plus thin GTAS VPP components and existing `--vpp-*` tokens.
- The visible shell is now independently branded as GTAS VPP; the legacy PPJ image is no longer rendered. Historical image files remain untouched for safe rollback and repository history.
- Vietnamese remains the default UI language. English resources continue to stay paired, but a full English-content and dark-theme polish pass remains outside the one-month cutline.
- Error transport uses RFC 7807 metadata (`errorCode`, `traceId`, `safeDetail`). Raw response bodies are never rendered. Only allowlisted business/concurrency details may cross the UI boundary; generic production failures stay redacted.
- `RouteCatalog` is the canonical source for preferred authenticated deep links. Stale dashboard/library tab paths are no longer maintained in a second list.

## Implemented surface

- Added reusable semantic `VppPageHeader` and `VppStatePanel` primitives plus responsive, reduced-motion-aware styling.
- Reworked the application shell with a CSS brand mark, named primary-navigation landmark, semantic account-menu button/menu roles, Escape close behavior and accessible backdrops.
- Removed the one-second `System.Threading.Timer` that rerendered the full authenticated shell. The sidebar displays a stable current-date value instead.
- Added no-op compatibility assets for persisted `material3`/`material3-dark` cookie names. Radzen 11 serves the actual `material` base theme; the compatibility files prevent missing-theme 404s without duplicating the 750 KB theme payload.
- Localized and hardened not-found, authorization-denied and error pages. The error page exposes only a trace ID, never development instructions or exception internals.
- Added `ApiRequestException`, `UiErrorMapper` and `AsyncLoadVersion`; applied safe errors to page-access, report and password-change flows, and latest-response-wins protection to report data.
- Added consistent headers/descriptions for library, permission and report routes and paired every new resource key in Vietnamese and English.
- Fixed the login smoke test to wait for completed page load and classify only the known image navigation abort caused by changing viewport/navigation.

## Core route/browser evidence

The authenticated shell test visits these six retained demo surfaces at 390×844, 768×1024 and 1920×1080:

1. `/dashboard?tab=0`
2. `/dashboard/order-create`
3. `/dashboard?tab=5&periodTab=review`
4. `/library?tab=2`
5. `/permission?tab=0`
6. `/report`

Every route passed horizontal-overflow, main-landmark, named-navigation, account-menu semantics, independent-brand, legacy-logo absence, icon-button accessible-name, console-error, missing-request and failed-network checks.

## Acceptance evidence

| Gate | Result |
|---|---:|
| `dotnet build gtas_vpp.sln -c Release --no-restore` | PASS — 0 warnings, 0 errors |
| Backend Release tests | PASS — 292/292 |
| Frontend Release tests | PASS — 86/86 |
| Disposable SQL Server LocalDB integration | PASS — 17/17 |
| Login responsive/a11y smoke (3 viewports) | PASS — 1/1 |
| Authenticated shell, 6 routes × 3 viewports | PASS — 1/1 |
| EF `has-pending-model-changes` | PASS — no model changes |
| Gitleaks v8.30.1 current-tree scan | PASS — no leaks found |
| `git diff --check` | PASS |

## Database and rollback

- No entity, migration, stored procedure or production database was changed.
- Roll back with a normal revert of the LEAN-04 commit. The two tiny theme compatibility files may be removed only together with changing persisted theme names/Radzen theme initialization; otherwise the browser 404 returns.
- If the new shell has a release-only regression, revert the shell/component/CSS slice while retaining the RFC 7807 redaction and tests. Error-detail redaction is a security floor, not a cosmetic rollback candidate.

## Explicit residuals

- Some legacy feature tabs still compose toast text around `Exception.Message`. API exceptions now contain only safe codes, but those call sites should adopt `UiErrorMapper` while LEAN-05/06 touches their request, supplement, pricing and settlement journeys.
- The duplicate historical `EmptyState.razor`, old PPJ assets/classes, broad theme cleanup and full English/dark-mode QA are deferred; deleting them now would add risk without improving the thesis demo.
- Automated axe-core WCAG rule execution is deferred to the final LEAN-08 quality gate. LEAN-04 establishes semantic/keyboard/label/overflow/console/network checks and reduced-motion behavior.
- The owner's waiver for eight historical Google API-key alerts is unchanged. They remain open and are not described as revoked.
- Four user-owned files were preserved and not staged:
  - `LVTN/NguyenAnNam_DH52201078_working.docx` — `E9E91C5A9A67F736E9CEAEC0DA1282DC68AF44E3D36039A465E214F0756AE17D`
  - `src/Frontend/Blazor/Components/App.razor` — `E7FAD87A1CF792468E5378FA8ED3CFF0DFA5CB459F62DF72A9CFE3977F0F6B36`
  - `src/Frontend/Blazor/wwwroot/css/vpp-login.css` — `622C4C976ABE287A7CB6ED78DC984D58AF04CE67B03FCAD9C351AF80BB248319`
  - `src/Frontend/Blazor/wwwroot/css/vpp-responsive.css` — `81E49BD90CF3C823076CA3F8B0A3B5192038719E3D4605C4EC0EA176CDF28ACB`

## Handoff

LEAN-05 should keep the shell and error contracts stable. Apply `UiErrorMapper` when touching request/supplement screens, use the shared header/state primitives for new journey states, and prove period/request/supplement invariants through service, API, LocalDB and browser tests without weakening the existing one-request and permission boundaries.
