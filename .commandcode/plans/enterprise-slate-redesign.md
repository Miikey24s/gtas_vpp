# GTAS VPP — Enterprise Slate Redesign Plan

**Direction:** B — Enterprise Slate (Electric Blue `#3B82F6`, Dark Sidebar `#0F172A`, Slate Background `#F8FAFC`)
**Reference:** Retool, Datadog, Stripe Dashboard
**Approach:** Keep token system, CSS-only changes, per-page/phase incremental delivery

---

## Phase 1 — Token Update (Foundation)

Change color palette in `vpp-tokens.css` only. Zero structural changes.

| Token | New Value | Notes |
|-------|-----------|-------|
| `--vpp-primary` | `#3B82F6` | Electric Blue |
| `--vpp-primary-hover` | `#2563EB` | Darker blue |
| `--vpp-primary-light` | `#EFF6FF` | Blue-50 tint |
| `--vpp-sidebar-bg` | `#0F172A` | Always-dark, both themes |
| `--vpp-sidebar-text` | `#CBD5E1` | Slate-300 |
| `--vpp-sidebar-active` | `#1E293B` | Slate-800 |
| `--vpp-sidebar-icon` | `#64748B` | Slate-500 |
| `--vpp-bg` | `#F8FAFC` | Slate-50 page bg |
| `--vpp-surface` | `#FFFFFF` | White cards |
| `--vpp-border` | `#E2E8F0` | Slate-200 |
| `--vpp-text-primary` | `#0F172A` | Slate-900 |
| `--vpp-text-secondary` | `#475569` | Slate-600 |
| `--vpp-text-muted` | `#94A3B8` | Slate-400 |
| New: `--vpp-sidebar-badge-bg` | `#EF4444` | Red badge on dark sidebar |
| New: `--vpp-priority-high` | `#EF4444` | Red left-border |
| New: `--vpp-priority-medium` | `#F59E0B` | Amber left-border |
| New: `--vpp-priority-low` | `#22C55E` | Green left-border |

**Files:** `vpp-tokens.css`, `vpp-radzen-theme.css`  
**Risk:** Very Low  

---

## Phase 2 — CSS Cleanup (14 Bugs)

| # | Bug | Action |
|---|-----|--------|
| 1 | Duplicate `.my-popup` | Keep app.css, delete from LeftSidebar.razor.css |
| 2 | Duplicate `.customclosebutton` | Merge into one, delete duplicate |
| 3 | Login CSS conflict | Deprecate LoginPage.razor.css → `/* DEPRECATED */` |
| 4 | Dead sidebar gradient | Delete MainLayout.razor.css |
| 5 | Hardcoded colors in vpp-login.css | Replace with `var(--vpp-*)` tokens |
| 6 | Hardcoded colors in vpp-ai.css | Tokenize all hardcoded values |
| 7 | Hardcoded colors in AIChatBox.razor.css | Tokenize all hardcoded values |
| 8 | Duplicate scrollbar | Keep in vpp-tokens.css, delete from vpp-polish.css |
| 9 | `--rz-danger` instead of `--vpp-danger` | Find-replace in app.css |
| 10 | Duplicate `--ppj-logo-primary-color` | Keep in vpp-tokens.css only |
| 11 | Duplicate shimmer keyframes | Keep in vpp-polish.css |
| 12 | hr border-color visible on dark bg | → `var(--vpp-border)` |
| 13 | 90+ `!important` in app.css | Categorize, refactor with proper specificity |
| 14 | Component-scoped cleanup | Empty LeftSidebar.razor.css, delete dead files |

**Files:** `app.css`, `vpp-login.css`, `vpp-ai.css`, `vpp-polish.css`, `vpp-tokens.css`,  
`LeftSidebar.razor.css`, `MainLayout.razor.css`, `LoginPage.razor.css`, `AIChatBox.razor.css`  
**Risk:** Medium (small fixes, many files)

---

## Phase 3 — Layout Polish (Sidebar + Header)

CSS-only to `vpp-sidebar.css` and `vpp-layout.css`.

### Sidebar
- Always-dark `#0F172A`, 40px menu items, 8px border-radius
- Active: left 3px Electric Blue bar + slate-800 bg
- Icons: 20px, slate-500 → slate-300 on active
- Badge counts for pending approvals (red pills)
- Smooth collapse animation

### Header
- 56px height, white surface, bottom border `var(--vpp-border)`
- Clean toggle buttons, glass effect removed

### UserMenu
- 32px avatar circle, 240px dropdown, slate styling

**Files:** `vpp-sidebar.css`, `vpp-layout.css`, `LeftSidebar.razor` (minimal badge markup)  
**Risk:** Low

---

## Phase 4 — Dashboard Tabs (6 tabs)

Apply Enterprise Slate patterns per-tab. CSS additions to `vpp-datagrid.css`, `vpp-kpi.css`.

### Tab_Orders
- KPI stat cards: 4 cards, 28px numbers, subtle shadow
- Priority bars: 4px colored left-border (red/amber/green/blue)
- Hover actions: buttons appear on row hover (`opacity: 0→1`)
- Metadata chips: relative dates "2h ago", status pills

### Tab_History
- Clean grid + date filter pills ("Today", "This Week", "This Month")
- Timeline-style status column

### Tab_ProductCatalog
- Card grid with thumbnail, SKU chip, stock badge

### Tab_DepartmentSummary
- Department cards with aggregated KPIs, mini CSS sparkline bars

### Tab_AllOrdersSummary
- Full-width clean table, summary footer, export button

### Tab_AdminApproval
- Hover action buttons, "Approve All" primary button, Accept/Reject styling

**Files:** `vpp-datagrid.css`, `vpp-kpi.css`  
**Risk:** Low (CSS additions only)

---

## Phase 5 — Order Create Wizard

Vertical stepper (Retool-style) + live order summary panel (Stripe pattern).

### Vertical Stepper
- Left-rail: circles with numbers, connecting lines, labels
- Active/Completed/Disabled states with color + icon transitions
- 2-column layout: stepper+form (flex:1) + summary panel (320px sticky)

### Live Summary Panel
- "🛒 Your Order: N items • Total Qty: M"
- Updates in real-time as items are added

**Files:** `vpp-wizard.css`, order create page layout (minimal Razor flex wrapper)  
**Risk:** Low-Medium

---

## Phase 6 — Library & Permission

Apply consistent grid CSS from Phase 4.

- Library: component cards with category chips
- Permission: styled toggle switches, role chips (Admin=purple, Manager=blue)
- Save button FAB-style, bottom-right

**Files:** `vpp-datagrid.css`  
**Risk:** Very Low

---

## Phase 7 — AI & Report

Final polish.

- AI chat: tokenized colors, user/AI message bubbles, typing indicator
- Report: date range picker, loading skeleton, download buttons

**Files:** `vpp-ai.css`, `AIChatBox.razor.css`, `vpp-charts.css`  
**Risk:** Low

---

## PR Structure

```
PR #1: Phase 1 + 2  →  Foundation + Bug Fixes (no visible layout change)
PR #2: Phase 3      →  Sidebar/Header transformation
PR #3: Phase 4      →  Dashboard tab improvements
PR #4: Phase 5      →  Wizard vertical stepper
PR #5: Phase 6 + 7  →  Library/Permission/AI/Report
```

Each PR independently testable and deployable. Each produces visible improvement.

---

## Risk Summary

| Phase | Risk | Files Changed |
|-------|------|-------------|
| 1 — Tokens | Very Low | 2 CSS |
| 2 — Cleanup | Medium | 9 CSS |
| 3 — Layout | Low | 2 CSS + 1 Razor |
| 4 — Dashboard | Low | 2 CSS |
| 5 — Wizard | Low-Medium | 1 CSS + layout wrapper |
| 6 — Library/Permission | Very Low | 1 CSS |
| 7 — AI/Report | Low | 3 CSS |
