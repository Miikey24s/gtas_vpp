# GTAS VPP — Comprehensive Production Plan (v2)
## Agent: DeepSeek v4-pro (Single Agent)

---

## PRE-FLIGHT

| # | Action | Status |
|---|--------|--------|
| 1 | **XOA** api-key.txt | ✅ |
| 2 | **Rotate** 8 Gemini keys | ⚠️ Manual |
| 3 | **Generate JWT_KEY**: `openssl rand -base64 48` | ⚠️ Manual |
| 4 | **Add** api-key.txt + .env to .gitignore | ✅ |
| 5 | **Git tag**: `git tag pre-refactor-v1.0` | ⚠️ Manual |

---

## PHASE 0 — CRITICAL SECURITY + BUG FIXES (COMPLETED ✅)

### 0.1 Delete Exposed Secrets ✅
- **DELETED** api-key.txt
- **EDITED** .gitignore: added api-key.txt + .env
- **EDITED** appsettings.Development.json: removed DB creds + JWT key

### 0.2 Fix SQL Injection in sp_Authen ✅
- **EDITED** 02_StoredProcedures.sql: whitelist 7 sub-procedures, sp_executesql parameterized

### 0.3 Password: TripleDES/ECB -> BCrypt ✅
- **EDITED** PasswordHelpers.cs: HashPassword/VerifyPassword, [Obsolete] old methods
- **EDITED** AuthController.cs: Serilog login logging
- **EDITED** .csproj: BCrypt.Net-Next package

### 0.4 Rate Limiting — SKIPPED (internal system)

### 0.5 Fix isBusyPage UI Lock ✅
- **EDITED** Tab_PagePermission.razor: RadzenAlert instead of glb.isBusyPage = true
- **EDITED** Tab_User.razor: same

### 0.6 Fix GlobalClass Thread Safety ✅
- **EDITED** GlobalClass.cs: Interlocked.Increment/Decrement

### 0.7 Fix AIController 200 on Errors ✅
- **EDITED** AIController.cs: 504/503 instead of Ok(error)

### 0.8 Fix Stack Trace + Silent Catch ✅
- **EDITED** LibraryController.cs, SQLController.cs, VPPRequestController.cs, AuthController.cs

### 0.9 Fix CSS Errors ✅
- **EDITED** app.css: ..rz-textbox -> .rz-textbox, ..rz-fieldset-legend -> .rz-fieldset-legend

### 0.10 Fix Tab_Orders Bypass ✅
- **EDITED** Tab_Orders.razor: CanView || !CanView -> CanView

### 0.11 DB Health Check — DEFERRED to Phase 6

### 0.12 Disable isShowServer + /debug/endpoints ✅
- **EDITED** LoginPage.razor.cs: IWebHostEnvironment guard
- **EDITED** gtas_vpp_fe/Program.cs: /debug/endpoints in IsDevelopment()

---

## PHASE 1 — BACKEND FOUNDATION

### 1.1 GlobalClass: IsAIEnabled + CurrentLanguage
- **EDIT** GlobalClass.cs: add properties

### 1.2 Login Audit Logging
- Already done in Phase 0.3 (AuthController.cs)

### 1.3 Fix v_WFXCompany View
- **EDIT** 01_Views.sql: populate CompanyName from P06

---

## PHASE 2 — i18n INFRASTRUCTURE

### 2.1 Localization in Program.cs
### 2.2 Resource Files (App.resx + App.en.resx)
### 2.3 _Imports.razor

---

## PHASE 3 — LANGUAGE TOGGLE + ALL i18n

### 3.1 Working Language Toggle
### 3.2 Localize All Pages (~26 files)
### 3.3 Sidebar Icon Fixes

---

## PHASE 4 — AI TOGGLE (Full System)

### 4.1 New Component Code: AI_TOGGLE
### 4.2 AI Toggle Switch (Admin Only)
### 4.3 Guard All AI Components
### 4.4 AIToggle in Permission UI

---

## PHASE 5 — UI/UX REFACTORING

### 5.1 Skeleton Components
### 5.2 Apply Skeleton to All Grids
### 5.3 Dashboard Charts
### 5.4 Tab URL Sync
### 5.5 Report Page
### 5.6 ReconnectModal
### 5.7 Fix DataGrid Dual Virtualization+Paging

---

## PHASE 6 — PERFORMANCE + DOCKER HARDENING

### 6.1 Fix Unbounded Queries
### 6.2 Fix Library Pagination
### 6.3 Add Missing Index
### 6.4 Docker Security
### 6.5 Nginx Hardening
### 6.6 .env.example
### 6.7 Cleanup

---

## PHASE 7 — VERIFICATION

### Checklist (28 items)
- Run `docker compose build backend frontend`
- Run `docker compose up -d`
- Verify all items

---

## EXECUTION ORDER

Phase 0 (DONE) -> Phase 1 -> Phase 2 -> Phase 3 -> Phase 4 -> Phase 5 -> Phase 6 -> Phase 7
