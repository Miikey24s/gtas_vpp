# UI/UX Audit Report

## A. Run metadata
- RUN_ID: 20260528T025811Z
- MODE: full
- Scope: [DASHBOARD, LIBRARY, PERMISSION, REPORT]
- FRONTEND_URL: http://localhost:5203
- MCP used: Playwright MCP = true; Radzen MCP = true; mcp-accessibility-scanner = unavailable in this session; UX MCP = false
- Git commit: root 55bc104f11bc1c19bcd4d477fd6821afa0c8ccc4; FE submodule cd4e9bd0030a8344d404df95b42fd461d415a860
- Duration: ~38 minutes
- Notes: First run, so no baseline report existed. AppHost was restarted once to release a locked FE binary and serve the rebuilt frontend.

## B. Audited routes
| Key | Path | Viewports | Status |
| --- | --- | --- | --- |
| dashboard.my-orders | /dashboard?tab=0 | mobile, tablet, desktop | Pass |
| dashboard.history | /dashboard?tab=1 | mobile, tablet, desktop | Pass |
| dashboard.catalog | /dashboard?tab=2 | mobile, tablet, desktop | Pass |
| dashboard.management.department | /dashboard?tab=3&managementTab=department | mobile, tablet, desktop | Pass |
| dashboard.management.all | /dashboard?tab=3&managementTab=all | mobile, tablet, desktop | Pass |
| dashboard.period-operations | /dashboard?tab=5 | mobile, tablet, desktop | Pass; 3 Medium report-only |
| dashboard.order-create.new | /dashboard/order-create | mobile, tablet, desktop | Pass |
| dashboard.order-create.edit | /dashboard/order-create?orderId=5e83b7b1-befa-4f0f-965f-82139a36f020 | mobile, tablet, desktop | Pass; 6 Medium report-only |
| library.classes | /library?tab=0 | mobile, tablet, desktop | Pass |
| library.categories | /library?tab=1 | mobile, tablet, desktop | Pass; 1 Medium report-only |
| library.items | /library?tab=2 | mobile, tablet, desktop | Pass; 1 Medium report-only |
| library.suppliers | /library?tab=3 | mobile, tablet, desktop | Pass; 2 Medium report-only |
| library.departments | /library?tab=5 | mobile, tablet, desktop | Pass; 2 Medium report-only |
| library.pricing.price-lists | /library?tab=6&pricingTab=price-lists | mobile, tablet, desktop | Pass |
| library.pricing.prices | /library?tab=6&pricingTab=prices | mobile, tablet, desktop | Pass; 3 Medium report-only |
| permission.user | /permission?tab=0 | mobile, tablet, desktop | Pass |
| permission.component | /permission?tab=1 | mobile, tablet, desktop | Pass |
| report | /report | mobile, tablet, desktop | Pass |

## C. Skipped routes
None. All 18 authenticated RouteCatalog entries in scope were audited across 3 viewports.

## D. Bugs found
- **High** | layout.shared-header | mobile | fixed: Y
  Repro: At 390x844 the header brand title, logo, notification, and controls overlapped on authenticated routes.
  Screenshot: screenshots/dashboard.my-orders-mobile.png
  Suspect file: gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Layout/LeftSidebar.razor; gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/wwwroot/css/vpp-layout.css
- **High** | permission.user | mobile | fixed: Y
  Repro: Permission user grid group drop-zone and column picker collided with the search/reload toolbar at 390x844.
  Screenshot: screenshots/permission.user-mobile.png
  Suspect file: gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/Permission/Tabs/Tab_User.razor; gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/wwwroot/css/vpp-layout.css
- **Medium** | dashboard.period-operations | mobile | fixed: N
  Repro: /dashboard?tab=5 has interactive elements without accessible names: input.rz-numeric-input rz-inputtext rz-text-align-left
  Screenshot: screenshots/dashboard.period-operations-mobile.png
  Suspect file: gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/VPPRequest/Components/PeriodReviewPanel.razor
- **Medium** | dashboard.period-operations | tablet | fixed: N
  Repro: /dashboard?tab=5 has interactive elements without accessible names: input.rz-numeric-input rz-inputtext rz-text-align-left
  Screenshot: screenshots/dashboard.period-operations-tablet.png
  Suspect file: gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/VPPRequest/Components/PeriodReviewPanel.razor
- **Medium** | dashboard.period-operations | desktop | fixed: N
  Repro: /dashboard?tab=5 has interactive elements without accessible names: input.rz-numeric-input rz-inputtext rz-text-align-left
  Screenshot: screenshots/dashboard.period-operations-desktop.png
  Suspect file: gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/VPPRequest/Components/PeriodReviewPanel.razor
- **Medium** | dashboard.order-create.edit | mobile | fixed: N
  Repro: /dashboard/order-create?orderId=5e83b7b1-befa-4f0f-965f-82139a36f020 has clipped interactive/control text: button "Up" 14x14->14x18 | button "Down" 14x14->14x18 | button "Up" 14x14->14x18 | button "Down" 14x14->14x18 | button "Up" 14x14->14x18 | button "Down" 14x14->14x18
  Screenshot: screenshots/dashboard.order-create.edit-mobile.png
  Suspect file: gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/VPPRequest/OrderCreateStep2.razor
- **Medium** | dashboard.order-create.edit | mobile | fixed: N
  Repro: /dashboard/order-create?orderId=5e83b7b1-befa-4f0f-965f-82139a36f020 has interactive elements without accessible names: input.rz-numeric-input rz-inputtext rz-text-align-left | input.rz-numeric-input rz-inputtext rz-text-align-left | input.rz-numeric-input rz-inputtext rz-text-align-left
  Screenshot: screenshots/dashboard.order-create.edit-mobile.png
  Suspect file: gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/VPPRequest/OrderCreateStep2.razor
- **Medium** | dashboard.order-create.edit | tablet | fixed: N
  Repro: /dashboard/order-create?orderId=5e83b7b1-befa-4f0f-965f-82139a36f020 has clipped interactive/control text: button "Up" 14x14->14x18 | button "Down" 14x14->14x18 | button "Up" 14x14->14x18 | button "Down" 14x14->14x18 | button "Up" 14x14->14x18 | button "Down" 14x14->14x18
  Screenshot: screenshots/dashboard.order-create.edit-tablet.png
  Suspect file: gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/VPPRequest/OrderCreateStep2.razor
- **Medium** | dashboard.order-create.edit | tablet | fixed: N
  Repro: /dashboard/order-create?orderId=5e83b7b1-befa-4f0f-965f-82139a36f020 has interactive elements without accessible names: input.rz-numeric-input rz-inputtext rz-text-align-left | input.rz-numeric-input rz-inputtext rz-text-align-left | input.rz-numeric-input rz-inputtext rz-text-align-left
  Screenshot: screenshots/dashboard.order-create.edit-tablet.png
  Suspect file: gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/VPPRequest/OrderCreateStep2.razor
- **Medium** | dashboard.order-create.edit | desktop | fixed: N
  Repro: /dashboard/order-create?orderId=5e83b7b1-befa-4f0f-965f-82139a36f020 has clipped interactive/control text: button "Up" 14x14->14x18 | button "Down" 14x14->14x18 | button "Up" 14x14->14x18 | button "Down" 14x14->14x18 | button "Up" 14x14->14x18 | button "Down" 14x14->14x18
  Screenshot: screenshots/dashboard.order-create.edit-desktop.png
  Suspect file: gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/VPPRequest/OrderCreateStep2.razor
- **Medium** | dashboard.order-create.edit | desktop | fixed: N
  Repro: /dashboard/order-create?orderId=5e83b7b1-befa-4f0f-965f-82139a36f020 has interactive elements without accessible names: input.rz-numeric-input rz-inputtext rz-text-align-left | input.rz-numeric-input rz-inputtext rz-text-align-left | input.rz-numeric-input rz-inputtext rz-text-align-left
  Screenshot: screenshots/dashboard.order-create.edit-desktop.png
  Suspect file: gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/VPPRequest/OrderCreateStep2.razor
- **Medium** | library.categories | mobile | fixed: N
  Repro: /library?tab=1 has clipped interactive/control text: div "VPPCategoryCode filter_alt" 19x42->30x42 | div "VPPCategoryName filter_alt" 19x42->30x42 | div "Id filter_alt" 19x42->30x42 | div "Description filter_alt" 19x42->30x42 | div "CreateUserId filter_alt" 19x42->47x42 | div "CreateDate filter_alt" 19x42->47x42 | div "UpdateUserId filter_alt" 19x42->47x42 | div "UpdateDate filter_alt" 19x42->47x42 | div "IsDeleted filter_alt" 19x42->47x42
  Screenshot: screenshots/library.categories-mobile.png
  Suspect file: gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/Lib/Component_ShareGrid.razor
- **Medium** | library.items | tablet | fixed: N
  Repro: /library?tab=2 has clipped interactive/control text: div "VPPCode filter_alt" 12x42->30x42 | div "VPPName filter_alt" 12x42->30x42 | div "Id filter_alt" 12x42->30x42 | div "Description filter_alt" 12x42->30x42 | div "CreateUserId filter_alt" 12x42->47x42 | div "CreateDate filter_alt" 12x42->47x42 | div "UpdateUserId filter_alt" 12x42->47x42 | div "UpdateDate filter_alt" 12x42->47x42 | div "IsDeleted filter_alt" 12x42->47x42
  Screenshot: screenshots/library.items-tablet.png
  Suspect file: gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/Lib/Component_ShareGrid.razor
- **Medium** | library.suppliers | mobile | fixed: N
  Repro: /library?tab=3 has clipped interactive/control text: div "SupplierShortName filter_alt" 12x42->30x42 | div "SupplierName filter_alt" 12x42->30x42 | div "Address1 filter_alt" 12x42->30x42 | div "Address2 filter_alt" 12x42->30x42 | div "Address3 filter_alt" 12x42->30x42 | div "Ward filter_alt" 12x42->30x42 | div "City filter_alt" 12x42->30x42 | div "Id filter_alt" 12x42->30x42 | div "Description filter_alt" 12x42->30x42 | div "CreateUserId filter_alt" 12x42->47x42 | div "CreateDate filter_alt" 12x42->47x42 | div "UpdateUserId filter_alt" 12x42->47x42
  Screenshot: screenshots/library.suppliers-mobile.png
  Suspect file: gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/Lib/Component_ShareGrid.razor
- **Medium** | library.suppliers | tablet | fixed: N
  Repro: /library?tab=3 has clipped interactive/control text: div "CreateUserId filter_alt" 32x42->47x42 | div "CreateDate filter_alt" 32x42->47x42 | div "UpdateUserId filter_alt" 32x42->47x42 | div "UpdateDate filter_alt" 32x42->47x42 | div "IsDeleted filter_alt" 32x42->47x42
  Screenshot: screenshots/library.suppliers-tablet.png
  Suspect file: gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/Lib/Component_ShareGrid.razor
- **Medium** | library.departments | mobile | fixed: N
  Repro: /library?tab=5 has clipped interactive/control text: div "LEX02Code filter_alt" 16x42->30x42 | div "LEX02Name filter_alt" 16x42->30x42 | div "LEX02Type filter_alt" 16x42->30x42 | div "ParentId filter_alt" 16x42->30x42 | div "Id filter_alt" 16x42->30x42 | div "Description filter_alt" 16x42->30x42 | div "CreateUserId filter_alt" 16x42->47x42 | div "CreateDate filter_alt" 16x42->47x42 | div "UpdateUserId filter_alt" 16x42->47x42 | div "UpdateDate filter_alt" 16x42->47x42 | div "IsDeleted filter_alt" 16x42->47x42
  Screenshot: screenshots/library.departments-mobile.png
  Suspect file: gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/Lib/Component_ShareGrid.razor
- **Medium** | library.departments | tablet | fixed: N
  Repro: /library?tab=5 has clipped interactive/control text: div "CreateUserId filter_alt" 41x42->47x42 | div "CreateDate filter_alt" 41x42->47x42 | div "UpdateUserId filter_alt" 41x42->47x42 | div "UpdateDate filter_alt" 41x42->47x42 | div "IsDeleted filter_alt" 41x42->47x42
  Screenshot: screenshots/library.departments-tablet.png
  Suspect file: gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/Lib/Component_ShareGrid.razor
- **Medium** | library.pricing.prices | mobile | fixed: N
  Repro: /library?tab=6&pricingTab=prices has interactive elements without accessible names: input.rz-textbox rz-state-empty
  Screenshot: screenshots/library.pricing.prices-mobile.png
  Suspect file: gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/Lib/Tabs/Tab_PriceLibrary.razor
- **Medium** | library.pricing.prices | tablet | fixed: N
  Repro: /library?tab=6&pricingTab=prices has interactive elements without accessible names: input.rz-textbox rz-state-empty
  Screenshot: screenshots/library.pricing.prices-tablet.png
  Suspect file: gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/Lib/Tabs/Tab_PriceLibrary.razor
- **Medium** | library.pricing.prices | desktop | fixed: N
  Repro: /library?tab=6&pricingTab=prices has interactive elements without accessible names: input.rz-textbox rz-state-empty
  Screenshot: screenshots/library.pricing.prices-desktop.png
  Suspect file: gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/Lib/Tabs/Tab_PriceLibrary.razor

## E. Files modified
| File | Purpose | Diff |
| --- | --- | --- |
| gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Layout/LeftSidebar.razor | Source fix | diffs/fe-layout-permission-fixes.patch |
| gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Layout/LeftSidebar.razor.cs | Source fix | diffs/fe-layout-permission-fixes.patch |
| gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/Pages/Permission/Tabs/Tab_User.razor | Source fix | diffs/fe-layout-permission-fixes.patch |
| gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/wwwroot/css/vpp-layout.css | Source fix | diffs/fe-layout-permission-fixes.patch |

## F. Build/test results
- Login: successful with provided test credentials; storage state saved in this run artifact.
- Dynamic sample: dashboard.order-create.edit used order id 5e83b7b1-befa-4f0f-965f-82139a36f020.
- Radzen MCP: consulted for DataGrid empty/header/grouping, Dialog, TemplateForm, and responsive toolbar guidance before fixing Radzen-backed layout.
- Build: initial required build failed because the running FE process locked gtas_vpp_fe.exe; after restarting dev orchestration, the required build command succeeded with 0 warnings and 0 errors.
- Retest: full retest completed 54 route/viewport checks after fixes; no Critical or High issues remain.
- A11y: mcp-accessibility-scanner tools were not installed/discoverable, so per-viewport JSON files contain Playwright DOM/a11y heuristics instead of scanner output.

## G. Baseline diff
No BASELINE_REPORT existed because this was the first run. NEW/FIXED/PERSISTING classification starts from the next run.

## H. Risks & uncovered
- Remaining issues are Medium and report-only by policy: Radzen numeric inputs/spin buttons without explicit accessible names, several wide DataGrid headers clipped inside horizontally scrollable tables, and a price search input without an explicit accessible name.
- Form validation was inspected at render/DOM level only for this full pass; destructive submit/update flows were not executed against live data.
- Accessibility scanner MCP was unavailable, so WCAG 2.2 AA contrast/ARIA results should be treated as heuristic until the scanner connector is installed.
