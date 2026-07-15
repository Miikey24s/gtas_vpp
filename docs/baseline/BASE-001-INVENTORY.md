# BASE-001 — Git, source, test and decision baseline

- Captured at: 2026-07-15 (Asia/Ho_Chi_Minh)
- Repository: `D:\WORK\gtas_vpp`
- Baseline commit: `6c7a70108ed6937115b8cca1903257dadbcddd27`
- Baseline commit date: `2026-07-14T13:33:05+07:00`
- Original branch/upstream at capture: `Nam` / `origin/Nam`, `+0 -0`
- Task branch: `codex/base-001-baseline`
- SDK: .NET `10.0.301`; application/test projects target `net10.0`
- Scope: documentation-only baseline; no source, configuration, database, server or Word mutation.

## 1. Worktree ownership and preservation boundary

Initial `git status --short --branch` before the task branch was created:

```text
## Nam...origin/Nam
 M LVTN/NguyenAnNam_DH52201078_working.docx
 M gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/App.razor
 M gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/wwwroot/css/vpp-login.css
 M gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/wwwroot/css/vpp-responsive.css
?? docs/
```

| Path | Owner/source | BASE-001 handling |
|---|---|---|
| `LVTN/NguyenAnNam_DH52201078_working.docx` | User-owned pre-existing thesis work | Preserve byte-for-byte; never stage, revert or render in BASE-001. |
| `gtas_vpp_fe/.../Components/App.razor` | User-owned pre-existing frontend work | Preserve; exclude from stage/commit and from baseline claims about clean source. |
| `gtas_vpp_fe/.../wwwroot/css/vpp-login.css` | User-owned pre-existing frontend work | Preserve; exclude from stage/commit. |
| `gtas_vpp_fe/.../wwwroot/css/vpp-responsive.css` | User-owned pre-existing frontend work | Preserve; exclude from stage/commit. |
| `docs/planning/*` | Codex-generated audit/master-plan artifacts approved before BASE-001 | In-scope baseline dependency; version together with decisions/evidence. |
| `docs/decisions/*`, `docs/baseline/*`, `docs/execution/*` | BASE-001 outputs | In scope; contain no secret values. The existing thesis filename necessarily exposes the owner name/student-code pattern inside this private baseline and remains assigned to D-009 anonymization before public/final evidence. |

Preservation fingerprints captured before staging show every user-owned file was last written on 2026-07-14, before BASE-001 began:

| Path | Last write (Asia/Ho_Chi_Minh) | Bytes | SHA-256 |
|---|---|---:|---|
| Thesis working `.docx` | `2026-07-14T17:52:47+07:00` | 8,664,240 | `E9E91C5A9A67F736E9CEAEC0DA1282DC68AF44E3D36039A465E214F0756AE17D` |
| `Components/App.razor` | `2026-07-14T15:37:20+07:00` | 4,821 | `E7FAD87A1CF792468E5378FA8ED3CFF0DFA5CB459F62DF72A9CFE3977F0F6B36` |
| `wwwroot/css/vpp-login.css` | `2026-07-14T15:58:37+07:00` | 13,330 | `622C4C976ABE287A7CB6ED78DC984D58AF04CE67B03FCAD9C351AF80BB248319` |
| `wwwroot/css/vpp-responsive.css` | `2026-07-14T14:51:12+07:00` | 5,805 | `81E49BD90CF3C823076CA3F8B0A3B5192038719E3D4605C4EC0EA176CDF28ACB` |

Rollback for this task is deletion/revert of the docs-only task commit/branch. It must never use a worktree reset that would discard the four user-owned paths above.

## 2. Solution/project inventory

| Area | Projects |
|---|---|
| Backend host/domain/data | `gtas_vpp_be`, `gtas_vpp_be.Service`, `gtas_vpp_be.Model`, `gtas_vpp_be.Migrations` |
| Shared contract | `gtas_vpp_be/gtas_vpp_shared` — the only shared DTO project |
| Frontend | `gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe` |
| Orchestration | `MyAspire.AppHost`, `MyAspire.ServiceDefaults` |
| Automated tests | `gtas_vpp_be.Tests`, `gtas_vpp_fe.Tests`, `gtas_vpp_fe.UITests` |

## 3. Backend API inventory

Current source contains 11 controller files: 10 concrete controllers plus abstract `BaseGenericController`. The concrete controllers expose 63 HTTP endpoint templates.

| Controller | Count | Route/action inventory |
|---|---:|---|
| `AuthController` | 2 | `POST login`; `GET me/permissions` |
| `LibraryController` | 6 | Generic `GET`, `GET /{id}`, `POST`, `PUT`, `PATCH`, `DELETE` over allowed library table codes |
| `NotificationsController` | 3 | Inbox list, mark one read, mark all read |
| `PeriodSettlementController` | 3 | Settle; status by year/month; settled-period list |
| `PermissionController` | 10 | Group, page-component, user and user-group read/write/delete surfaces |
| `ReportsController` | 3 | Summary, CSV export and insights |
| `SQLController` | 2 | Allowlisted generic stored-procedure gateway and test action |
| `VPPPriceController` | 7 | Price lookups, CRUD and set-default |
| `VPPPriceListController` | 9 | Price-list read/write/delete/default/clone |
| `VPPRequestController` | 18 | Own request CRUD/history/catalog, department/company summaries, filters/dashboard, supplement approve/reject |

Additional host surfaces:

- public `/health`;
- authenticated SignalR hubs `/hubs/permissions` and `/hubs/notifications`;
- Swagger is registered only outside Production.

Authorization classification at this commit:

- 1 anonymous endpoint: login, rate-limited by IP;
- 54 endpoints declare an explicit permission policy;
- 4 require authentication plus resource/scope validation inside the action;
- 4 require authentication plus own-user/company validation;
- report export additionally validates requested scope.

These are inventory facts, not proof that authorization is correct. Required follow-up tests must include direct API 401/403, own/department/company scope and stale-session revocation.

## 4. Permission inventory

`gtas_vpp_shared/Constants/Permissions.cs` defines 38 codes: menu/page components, request/library/settlement/permission/report capabilities and backend action permissions. The host registers dynamic policies; `PermissionAuthorizationHandler` delegates to `PermissionService`, which expects exactly one active user-group mapping and filters page/component mapping by the company claim.

Known baseline gaps routed to AUTH/QA tasks:

1. Legacy library component permission currently expands to both `LibraryView` and `LibraryManage`, so read access can imply broad generic-library write access.
2. `PERMISSION_USER` or `PERMISSION_COMPONENT` can each expand to both view and manage.
3. One generic `LibraryController` policy covers several entity types, limiting resource-level least privilege.
4. `SQLController` remains a generic stored-procedure gateway alongside typed APIs.
5. Authorization context must be proven to use the same environment/database boundary as business queries.
6. Registration/PendingApproval/recovery/activation is not present at this baseline; ADR-008 assigns it to AUTH-005/UI-007.

## 5. Stored procedure, SQL script and background-work inventory

Five SQL files exist under `gtas_vpp_be.Service/Helpers/SQL`. `02_StoredProcedures.sql` defines 12 procedures:

| Procedure | Current observed use |
|---|---|
| `sp_Authen` | Wrapper called by login and the generic SQL gateway |
| `sp_Authen_Login` | Login subtype through `sp_Authen` |
| `sp_Authen_CreateNewGroup` | Permission UI through generic SQL gateway |
| `sp_Authen_CopyFromGroup` | Permission UI through generic SQL gateway |
| `sp_Authen_GetPermissionSinglePage` | Wrapper allowlist; no current typed API/FE call found |
| `sp_Authen_Permission_GetPageWithComponentByGroupId` | Wrapper allowlist; no current typed API/FE call found |
| `sp_Authen_TabUser_SearchUser` | Wrapper allowlist; no current typed API/FE call found |
| `sp_Authen_TabUser_UserList` | Wrapper allowlist; no current typed API/FE call found |
| `sp_ComponentsToPage` | No runtime call-site found |
| `sp_CreateNewPageComponent` | No runtime call-site found |
| `sp_SaveComponent` | No runtime call-site found |
| `sp_SavePage` | No runtime call-site found |

The scripts are applied by `SeedData`, not represented as typed migrations. If the seed version is already marked applied, `CREATE OR ALTER` procedure updates can be skipped; this is a schema-drift risk for later migration work.

Static inventory found no `BackgroundService`, `IHostedService`, `AddHostedService` or periodic timer. Current notification flow persists and pushes SignalR directly; there is no durable outbox/email/retry/dead-letter worker. Startup migration/seed is the only configured host-side initialization work.

## 6. Frontend route/action inventory

There are 11 physical Razor `@page` directives:

| Route | Component / role |
|---|---|
| `/` | Authorized home redirect to first accessible route |
| `/Account/Login` | Login page |
| `/loginprocess` | Login redirect/process component |
| `/logoutprocess` | Logout process component |
| `/Error` | Error page |
| `/not-found` | Not-found page |
| `/dashboard/{Per?}` | Request/dashboard tab host |
| `/dashboard/order-create` | Regular/additional create/edit journey |
| `/library` | Catalog, supplier and pricing tab host |
| `/permission` | User/group/page-component administration |
| `/report` | Scoped report/dashboard/export |

`RouteCatalog` expands these into 18 authenticated logical journeys and 6 anonymous entries. Important gated actions include:

- Dashboard: own orders, history, catalog, department summary, company summary, period operations and create/edit.
- Requests: create/update/cancel own; supplement approve/reject.
- Library: class, category, item, supplier, department, price list and price tabs.
- Permission: user-group and page/component tabs.
- Report: own/department/company scopes and export.

Two minimal API endpoints are not represented by Razor `@page`: `/perform-login` and `/set-language`. Login return URL must receive a same-origin/open-redirect adversarial test.

Baseline route metadata drift, assigned to QA-003 rather than fixed in BASE-001 source:

- `RouteCatalog` documents `copyFromOrderId`/`additional`, while current navigation uses `copyFrom=previous`/`isAdditional=true`;
- logical query parameters omit current `periodTab` and order-create variants;
- preferred-route and sidebar paths are not identical for all management/period tabs;
- authenticated behavior currently relies on a mix of `AuthorizeRouteView`, page-base guards and component/action checks, so browser visibility is not sufficient API authorization evidence.

## 7. Build and automated-test baseline

Commands were run on the task branch without connecting to a configured SQL Server:

| Command | Result | Current evidence |
|---|---|---|
| `dotnet build gtas_vpp.sln -c Release` | PASS | 0 warnings, 0 errors; elapsed 21.40 s |
| `dotnet test gtas_vpp_be.Tests/gtas_vpp_be.Tests.csproj -c Release --no-restore` | PASS | 147 passed, 0 failed, 0 skipped; test duration 6 s |
| `dotnet test gtas_vpp_fe.Tests/gtas_vpp_fe.Tests.csproj -c Release --no-restore` | PASS | 29 passed, 0 failed, 0 skipped; test duration 1 s |
| `dotnet test gtas_vpp_fe.UITests/gtas_vpp_fe.UITests.csproj -c Release --no-restore --list-tests` | DISCOVERY PASS | 12 Playwright tests listed; no browser/test-data mutation performed |

Test boundary:

- Backend tests primarily use EF InMemory; the regular-request concurrency suite uses in-memory SQLite. There is no SQL Server/Testcontainers relational integration baseline.
- Frontend unit/component tests do not prove an authenticated browser journey.
- Playwright harness requires `GTAS_TEST_USERNAME` and `GTAS_TEST_PASSWORD` for authenticated tests. At capture, both variables and `UITEST_BASE_URL` were absent and `http://127.0.0.1:5000/` was not ready.
- If no ready URL is supplied, the harness can launch Aspire; current AppHost config uses `DatabaseInitialization__Mode=MigrateAndSeed` against `TestEnv`. There is no proven disposable/reset boundary. Therefore BASE-001 intentionally performed discovery only and did not launch the UI suite.
- Existing UI tests cover login/accessibility smoke, permission refresh, selected request/dashboard/catalog and library-grid paths. They do not yet prove report, inbox/email, settlement, complete supplement or role×route behavior; several assertions are load-level rather than business-outcome assertions.

## 8. Database access and backup boundary

BASE-001 did not open a SQL connection, run migration/seed, inspect real rows or touch DigitalOcean. Later DB/external tasks must record all of the following before mutation:

1. target classification: disposable local, sanitized clone, staging or production;
2. exact environment binding and fail-closed protection against Test→Live selection;
3. preflight duplicate/orphan/count/total checks;
4. backup identifier with a restore rehearsal for any persistent target;
5. reviewed generated SQL and fresh plus existing-sanitized upgrade evidence;
6. forward-correction/restore plan—never assume `Down()` is sufficient for destructive or settlement facts;
7. explicit user authority for DigitalOcean, provider secrets and production-like data.

Unit-test in-memory stores are disposable and need no persistent backup. Any UI fixture must first be isolated by QA-001 and must never auto-clean a shared database.

## 9. Locked business examples

These examples clarify accepted ADR behavior for later tests; they do not change runtime source in BASE-001.

### Period boundary — Asia/Ho_Chi_Minh, half-open interval

| Local timestamp | Expected period/result |
|---|---|
| `2026-07-04 23:59:59.999` | Period 06/2026; not yet period 07 |
| `2026-07-05 00:00:00` | Period 07/2026 begins inclusively |
| `2026-08-04 23:59:59.999` | Still period 07/2026 |
| `2026-08-05 00:00:00` | Period 08/2026; period 07 regular submission is closed |

All commands re-evaluate server-side period state/time; client clocks are not authoritative.

### Cancel and replacement

1. Employee U01 submits R07-v1, then edits before deadline: create R07-v2, mark v1 `Superseded`, keep one current pointer and include only v2 in the basket.
2. Cancellation preserves the revision and excludes it from the basket. Continuing demand uses a linked replacement command, not an unrelated second request.
3. Two concurrent replacements yield one current revision and one 409/rowversion conflict.
4. User edit/cancel after submission close or settlement is rejected; an authorized correction/reopen uses a new audited revision and mandatory reason.

### Supplements

1. A supplement created before regular close enters Pending and blocks settlement while unresolved.
2. It may be approved after `SubmissionClosed` only before `SupplementApprovalDeadline`; an approved item joins the basket, while rejected/cancelled attempts remain audit facts.
3. `MaxApproved=3` per user/base request/period, one Pending and `MaxAttempts=6` by demo default; concurrent operations cannot exceed the invariants.
4. Self-approval is 403 under ADR-004.

### Whole-company basket and allocation fixture (VND)

Demand: Department A/U1 requests Pen ×10; Department B/U2 requests Pen ×20; Department A/U3 has approved supplement Paper ×5. Basket aggregates Pen ×30 and Paper ×5 while preserving each request-line and department snapshot.

Primary supplier S1 quote: Pen net 10,000 with VAT 8%; Paper net 20,000 with VAT 10%. Order discount is 40,000; shipping is 20,000 with VAT 8%. Allocate discount pro-rata on pre-discount net and shipping pro-rata on post-discount net; round VND deterministically and store any residual adjustment.

| Allocation | Calculation summary | Gross |
|---|---|---:|
| U1 / Dept A | 100,000 − 10,000 + 5,000 + 7,200 item VAT + 400 shipping VAT | 102,600 |
| U2 / Dept B | 200,000 − 20,000 + 10,000 + 14,400 item VAT + 800 shipping VAT | 205,200 |
| U3 / Dept A | 100,000 − 10,000 + 5,000 + 9,000 item VAT + 400 shipping VAT | 104,400 |
| Company | `400,000 − 40,000 + 20,000 + 32,200 VAT` | **412,200** |

Department A totals 207,000; Department B totals 205,200; allocations reconcile to 412,200. Call 40,000 “savings” only if a comparable baseline snapshot exists; otherwise label it entered/estimated discount. If S1 lacks Paper, confirmation blocks until an audited supplier/quote exception (for example S2) is selected; no silent fallback. The output remains a planned allocation/distribution manifest, not proof of delivery or payment.

## 10. Decision and follow-up routing

All decisions D-001..D-012 are accepted and indexed at [`docs/decisions/000-index.md`](../decisions/000-index.md). Direct decision-to-task mapping was checked against the master-plan registry; no `BLOCKED_DECISION` task remains.

| Baseline finding | Owning later task(s) |
|---|---|
| Generic/legacy permission expansion and SQL gateway risk | AUTH-001/002/003/006, SEC-002, QA-002 |
| SQL scripts/SP drift outside typed migration flow | ARCH-001/003, QA-001/002 |
| No scheduler/outbox/email worker | PER-001, NOTIF-001/002/003 |
| RouteCatalog/sidebar/query drift and weak E2E assertions | QA-003 |
| UI harness may target ready shared server or auto-run MigrateAndSeed | QA-001 before any authenticated UI execution |
| No SQL Server relational integration baseline | QA-001 |
| Existing draft privacy is mandatory; enhanced autosave is cuttable | UI-003 under ADR-012 |

## 11. Replay commands

```powershell
git rev-parse HEAD
git status --short --branch
dotnet --version
dotnet build gtas_vpp.sln -c Release
dotnet test gtas_vpp_be.Tests/gtas_vpp_be.Tests.csproj -c Release --no-restore
dotnet test gtas_vpp_fe.Tests/gtas_vpp_fe.Tests.csproj -c Release --no-restore
dotnet test gtas_vpp_fe.UITests/gtas_vpp_fe.UITests.csproj -c Release --no-restore --list-tests
git diff --check
git status --short
```

Do not replace UI discovery with a full Playwright run until QA-001 proves a disposable target and supplies scoped test credentials.
