# GTAS VPP — Comprehensive Production Plan (v2)
## Agent: DeepSeek v4-pro (Single Agent)

## RULES
- **SAU MOI PHASE**: `docker compose build backend frontend` phai PASS, khong loi
- Build lai toan bo sau moi phase de verify

---

## PRE-FLIGHT

| # | Action |
|---|--------|
| 1 | **XOA** `/opt/gtas_vpp/api-key.txt` — 8 Google Gemini keys exposed |
| 2 | **Rotate** tat ca 8 Gemini keys trong Google Cloud Console |
| 3 | **Generate JWT_KEY**: `openssl rand -base64 48` → `.env` |
| 4 | **Them** `api-key.txt` + `.env` vao `.gitignore` |
| 5 | **Git tag**: `git tag pre-refactor-v1.0` |

---

## PHASE 0 — CRITICAL SECURITY + BUG FIXES ✅ COMPLETED

### Build verified: `docker compose build backend frontend` PASS

### 0.1 Delete Exposed Secrets ✅
- **DELETE** `/opt/gtas_vpp/api-key.txt`
- **EDIT** `.gitignore`: them `api-key.txt` va `.env`
- **EDIT** `appsettings.Development.json`: xoa hardcoded DB credentials (line 10 `" luo"` key), xoa hardcoded JWT key

### 0.2 Fix SQL Injection in sp_Authen ✅
- **EDIT** `02_StoredProcedures.sql`:
  - Validate `@SpType` against whitelist (7 sub-procedures)
  - Dung `sp_executesql` voi parameterized query thay vi string concatenation
  ```sql
  EXEC sp_executesql @Query, N'@Param NVARCHAR(MAX)', @Param = @Param;
  ```

### 0.3 Password Encryption: TripleDES/ECB → BCrypt ✅
- **REWRITE** `PasswordHelpers.cs`: `BCrypt.Net-Next`, HashPassword/VerifyPassword APIs, [Obsolete] old methods
- **EDIT** `AuthController.cs`: bcrypt first, TripleDES fallback, auto re-hash
- **EDIT** `gtas_vpp_be.Service.csproj`: add `BCrypt.Net-Next` package

### 0.4 Rate Limiting on Login — SKIPPED (internal system only)

### 0.5 Fix Unconditional UI Lock (isBusyPage never released) ✅
- **EDIT** `Tab_PagePermission.razor` line 250: `@(glb.isBusyPage = true)` → `RadzenAlert` warning
- **EDIT** `Tab_User.razor` line 116: same fix

### 0.6 Fix GlobalClass Thread Safety ✅
- **EDIT** `GlobalClass.cs`: `_busyCounter++`/`--` → `Interlocked.Increment`/`Decrement`

### 0.7 Fix AIController Returning Ok(200) on Errors ✅
- **EDIT** `AIController.cs`: `catch (Exception)` → return `StatusCode(504)` / `StatusCode(503)` thay vi `Ok({IsSuccess=false})`

### 0.8 Fix Stack Trace Exposure in API ✅
- **EDIT** `LibraryController.cs` line 190: xoa `StackTrace = ex.StackTrace`
- **EDIT** `SQLController.cs` line 65: `BadRequest(ex.Message)` → `BadRequest("An error occurred")`

### 0.9 Fix Silent Exception Swallowing ✅
- **EDIT** `AuthController.cs` line 125-130: `Console.WriteLine` → `Serilog.Log.Warning(ex, ...)`
- **EDIT** `LibraryController.cs` line 155-171: empty `catch {}` → `catch (Exception ex) { Serilog.Log.Warning(ex, ...); ... }`
- **EDIT** `VPPRequestController.cs` line 194-197: same

### 0.10 Fix CSS Syntax Errors ✅
- **EDIT** `app.css`: `..rz-textbox` → `.rz-textbox`, `..rz-fieldset-legend` → `.rz-fieldset-legend`

### 0.11 Fix Tab_Orders Security Bypass ✅
- **EDIT** `Tab_Orders.razor`: `@if (CanView || !CanView)` → `@if (CanView)`

### 0.12 DB Health Check — DEFERRED to Phase 6

### 0.13 Frontend Healthcheck — DEFERRED to Phase 6

### 0.14 Disable isShowServer in Production ✅
- **EDIT** `LoginPage.razor.cs`: inject `IWebHostEnvironment`, `!env.IsDevelopment()` guard

### 0.15 Remove /debug/endpoints from Production ✅
- **EDIT** `gtas_vpp_fe/Program.cs`: wrap with `@if (app.Environment.IsDevelopment())`

---

## PHASE 1 — BACKEND FOUNDATION ✅ COMPLETED

### Build verified: `docker compose build backend frontend` PASS

### 1.1 GlobalClass: IsAIEnabled + CurrentLanguage ✅
```csharp
public bool IsAIEnabled { get; set; } = true;
public string CurrentLanguage { get; set; } = "vi";
```

### 1.2 Login Audit Logging ✅ (done in Phase 0.3)

### 1.3 Fix v_WFXCompany View ✅
- Join GTAS_MENU.dbo.tblUsers to populate CompanyName instead of NULL

---

## PHASE 2 — i18n INFRASTRUCTURE ✅ COMPLETED

### Build verified: `docker compose build backend frontend` PASS

### 2.1 Localization in Program.cs ✅
- Added `AddLocalization`, `UseRequestLocalization` (default "vi", support "en")

### 2.2 Resource Files ✅
- Created `Resources/App.resx` (Vietnamese, 64 keys)
- Created `Resources/App.en.resx` (English, 64 keys)

### 2.3 _Imports ✅
- Added `@using Microsoft.Extensions.Localization` + `@inject IStringLocalizer<App> Loc`

---

## PHASE 3 — LANGUAGE TOGGLE + ALL i18n ✅ COMPLETED

### Build verified: `docker compose build backend frontend` PASS

### 3.1 Working Language Toggle ✅
- Enabled VI↔EN button in header with `ToggleLanguage()` — persist to ProtectedLocalStore, force reload
- Moved code from `@code` block to `LeftSidebar.razor.cs` (proper partial class)

### 3.2 Sidebar Icon Fixes ✅
- Library submenu: Class→`class`, Category→`category`, Operations→`inventory`, Suppliers→`local_shipping`

### 3.3-3.6 Localized All Pages ✅ (~20 files, 60+ keys)
- **LoginPage**: Username, Password, Login button
- **LeftSidebar**: All 20+ menu items, logout, dates
- **Library tabs**: ClassDefinitions, OperationCategories, Operations, Suppliers, Departments
- **Library dialogs**: ClassCode, ClassName, ClassModule, ClassDetailCode, ClassDetailValue, ExtraField1-3, SortOrder, Description, Save, Cancel
- **VPPRequest tabs**: MyOrders, History, ProductCatalog, DepartmentSummary, AllOrdersSummary, AdminApproval
- **OrderCreate**: BackToOrders, AISmartSearch, SelectedItems, ClearAll, Cancel, ProductCatalog
- **Tab_Orders**: NewOrder, CopyPreviousOrder, ActiveOrderPeriod, PeriodEnd, TotalLineItems, TotalQuantity, RequestAdditional, Additional, Reload
- **Tab_History/Tab_AdminApproval**: OrderDetails
- **Tab_DepartmentSummary/Tab_AllOrdersSummary**: OrderItems, Reload
- **Permission pages**: UserGroup, GroupPagePermission, GroupName, GroupNameRequired, CopyFrom, CreateNewGroup, Create, Description, NoPermission, Search, Reload
- **Report**: Reports
- **Tab_User**: Search, Reload, NoPermission
- **Resource files**: 96 keys each (VI + EN)

---

## PHASE 4 — AI TOGGLE (Full System)

### 4.1 New Component Code: `AI_TOGGLE` (Database Permission System)
- **EDIT** `SeedData.cs` (backend):
  - Add `P03_Component` with `ComponentCode = "AI_TOGGLE"`, `ComponentName = "AI Toggle Switch"`
  - Add `P05_PageComponentMapping`: map `AI_TOGGLE` → Page `SIDEBAR`
  - Add `P06_GroupPageComponentMapping`: Admin group gets `AI_TOGGLE` with `IsVisible=true, IsEnable=true`
- **EDIT** `gtas_vpp_fe/Helpers/Config.cs`:
  ```csharp
  public const string AIToggle = "AI_TOGGLE";
  ```
- **EDIT** `gtas_vpp_shared/Constants/Permissions.cs`:
  ```csharp
  public const string AIToggle = "AI_TOGGLE";
  // Add to Permissions.All array
  ```

### 4.2 AI Toggle Switch in Header (Admin Only)
- **EDIT** `LeftSidebar.razor`:
```razor
<AuthorizeView Policy="@Permissions.AIToggle">
    <RadzenSwitch @bind-Value="glb.IsAIEnabled" Change="@OnAIToggleChange"
                  Title="@(glb.IsAIEnabled ? Loc["AIEnabled"] : Loc["AIDisabled"])" />
</AuthorizeView>
```
- **EDIT** `LeftSidebar.razor.cs`: persist to `ProtectedLocalStore`, load on init

### 4.3 Guard All AI-Related Components
| Component | Action |
|-----------|--------|
| LeftSidebar "AI Management" menu item | `@if (glb.IsAIEnabled)` wrap |
| Page_AI.razor.cs | Redirect `/dashboard` if `!glb.IsAIEnabled` |
| Tab_AIChat, Tab_AIVPPChat, Tab_KeyManage | Guarded at page level redirect |
| Page_OrderCreate.razor — AI Smart Search card | `@if (glb.IsAIEnabled)` |
| Page_OrderCreate.razor.cs — GetAISuggestionsAsync() | `if (!glb.IsAIEnabled) return;` |
| MainLayout.razor — AIChatBox | `@if (glb.IsAIEnabled) { <AIChatBox /> }` |
| Backend AIController.cs | Return 503 if AI disabled |

### 4.4 AIToggle Visible in Permission UI
- **EDIT** `Page_Permission/Tab_PagePermission.razor`: `AI_TOGGLE` tu dong hien thi trong grid permission cho admin quan ly

---

## PHASE 5 — UI/UX REFACTORING

### 5.1 Skeleton Components
- **NEW** `Shared/SkeletonGrid.razor` — shimmer grid placeholder
- **NEW** `Shared/SkeletonStatCards.razor` — shimmer stat cards
- **EDIT** `app.css` — `.shimmer` animation + Design System Tokens

### 5.2 Apply Skeleton to All Grids (11 files)
All tabs: `if (IsLoading) { <SkeletonGrid /> }` pattern

### 5.3 Dashboard Charts
- **NEW** Backend endpoint: `GET /api/VPPRequest/dashboard-charts`
- **EDIT** `Tab_Orders.razor`: `RadzenPieSeries` + `RadzenColumnSeries`

### 5.4 Tab URL Sync
- **EDIT** 4 page components: `[SupplyParameterFromQuery(Name = "tab")]`

### 5.5 Report Page
- **EDIT** `Report.razor`: summary cards + export button

### 5.6 ReconnectModal Redesign
- **EDIT** `ReconnectModal.razor`: animated spinner + i18n

### 5.7 Fix DataGrid: Remove Dual Virtualization+Paging
- **EDIT** `Tab_History.razor`, `Tab_AdminApproval.razor`, `Tab_AllOrdersSummary.razor`, `Tab_DepartmentSummary.razor`
  - Remove `AllowVirtualization="true"` (keep `AllowPaging="true"`)

---

## PHASE 6 — PERFORMANCE + DOCKER HARDENING

### 6.1 Fix Unbounded Embedding Query
- **EDIT** `SqlVPPEmbeddingStore.cs`: add `.Take(1000)` or pagination before `.ToListAsync()`

### 6.2 Fix Library Unbounded Result Sets
- **EDIT** `BaseGenericController.cs` -> `LibraryController.cs`: apply `skip/top` at database level

### 6.3 Add Missing Index
- **EDIT** AI migration: add index on `AI_VPPEmbedding(VectorDimension, ModelName)`

### 6.4 Docker Security
- **EDIT** `Dockerfile` (backend + frontend): add `USER app` after creating non-root user
- **EDIT** `docker-compose.yml`: `read_only: true` for volumes, `mem_limit` cho backend+frontend
- DB Health Check: `AddHealthChecks().AddSqlServer(...)`

### 6.5 Nginx Hardening
- **EDIT** `nginx/gtas-vpp.conf`: `server_tokens off`, Permissions-Policy, COOP, `limit_req_zone`

### 6.6 .env.example
- **NEW** `.env.example`

### 6.7 Cleanup
- **DELETE** `NavMenu.razor`, `jobs/Screenshot *.png`
- **DELETE** `RightSidebar.razor` / **REPLACE** with recent activity panel

---

## PHASE 7 — VERIFICATION

```bash
docker compose build backend frontend
docker compose up -d
docker ps | grep gtas-vpp
docker logs gtas-vpp-backend --tail 20
```

### FINAL CHECKLIST (28 items):

**Security:**
- [ ] api-key.txt deleted, keys rotated
- [ ] appsettings.Development.json: no hardcoded secrets
- [ ] JWT_KEY strong random (min 32 chars)
- [ ] .env in .gitignore
- [ ] Password: bcrypt, TripleDES removed
- [ ] Rate limiting active on login
- [ ] sp_Authen SQL injection fixed
- [ ] Stack traces not exposed in API
- [ ] /debug/endpoints disabled in prod

**Bugs:**
- [ ] isBusyPage unconditional lock fixed (2 locations)
- [ ] GlobalClass counter: Interlocked
- [ ] AIController errors return 500 (not 200)
- [ ] Empty catch blocks replaced
- [ ] CSS errors fixed
- [ ] Tab_Orders tautology fixed

**Features:**
- [ ] Language toggle VI↔EN works
- [ ] All UI from .resx
- [ ] AI toggle: ComponentCode `AI_TOGGLE` in DB + Admin-only switch
- [ ] AI toggle guards all components cleanly
- [ ] Skeleton loading
- [ ] Charts render
- [ ] Tab URL sync

**Infrastructure:**
- [ ] DB health check active
- [ ] Frontend healthcheck in compose
- [ ] Nginx security headers present
- [ ] Docker non-root user
- [ ] NavMenu/RightSidebar dead code cleaned
- [ ] Build → deploy → login → CRUD ok
- [ ] Dark mode + permissions unbroken

---

## EXECUTION ORDER

```
Phase 0 (DONE) → Phase 1 → Phase 2 → Phase 3 → Phase 4 → Phase 5 → Phase 6 → Phase 7
```

## BUILD VERIFICATION LOG

| Phase | `docker compose build backend frontend` | Date |
|-------|----------------------------------------|------|
| 0 | ✅ PASS | 2026-05-06 |
| 1 | ✅ PASS | 2026-05-06 |
| 2 | ✅ PASS | 2026-05-06 |
| 2 | ⏳ Pending | |
| 3 | ✅ PASS | 2026-05-06 |
| 4 | ⏳ Pending | |
| 5 | ⏳ Pending | |
| 6 | ⏳ Pending | |
| 7 | ⏳ Pending | |
