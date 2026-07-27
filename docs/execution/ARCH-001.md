# ARCH-001 - Module boundaries and clean shared contracts

- Status: DONE
- Priority: P1
- A+ cutline class: mandatory-product
- Path: STANDARD
- Owner/agent: Codex
- Branch: `codex/arch-001-module-boundaries`
- Base commit: `1e25cca1e56c72cb396662ce542d0936dc6c1ce6`
- Started at (Asia/Ho_Chi_Minh): `2026-07-15T18:29:52+07:00`
- Finished at (Asia/Ho_Chi_Minh): `2026-07-15T19:30:51+07:00`
- Master-plan version/commit: `1.2-decisions` / base `1e25cca1e56c72cb396662ce542d0936dc6c1ce6`
- Related decisions/ADRs: no new business decision; D-001, D-002, D-004 and D-009 remain unchanged
- Dependencies verified: CP0 is complete (BASE-001 and QA-001 are DONE); ARCH-001
  does not depend on CP1, which remains incomplete because SEC-001 is BLOCKED_EXTERNAL
- User approval required: No additional approval
- User approval evidence: user explicitly started Goal ARCH-001

## Objective

Make the existing modular monolith's dependency direction explicit and enforceable,
while keeping wire behavior stable:

1. remove persistence and presentation dependencies from the Shared contract assembly;
2. remove unused EF Core and Mapster dependencies from the Blazor frontend;
3. move backend-only database projections and frontend-only state/display metadata to
   their owning assemblies;
4. lock the production project graph, complete wire DTO manifest and representative
   runtime JSON contracts with tests;
5. publish a migration-on-touch module map without a mass folder/project rewrite.

## Why now

Shared and frontend referenced persistence libraries that their runtime code did not
need. Shared also contained EF mapping, browser state, Radzen/CSS presentation and
mutable reflection metadata. That made it easy for AI-generated changes to place code
in the wrong layer and silently alter API contracts.

## Non-goals

- No authentication, permission, request, pricing, settlement or reporting rule change.
- No endpoint route, JSON property, database schema, migration or stored-procedure change.
- No generic repository/framework, microservice split, mass folder move or namespace rename.
- No UI redesign, deployment, external database/server operation or thesis Word edit.
- No fix to the discovered department-permission payload mismatch; AUTH-001 owns it.

## Preflight

- [x] Read `AGENTS.md`, `.codexrules`, `.github/copilot-instructions.md`, the task card
      and execution template.
- [x] Recorded branch/base and protected the four pre-existing user-owned dirty files.
- [x] Audited every production `.csproj`, project reference, Shared type and consumer.
- [x] Audited EF mapping for backend views before removing Shared EF attributes.
- [x] Audited JSON behavior and frontend transport options before moving contracts.
- [x] Checked Radzen guidance for dynamic DataGrid columns before replacing DTO attributes.
- [x] Confirmed no real database, external system, secret or production access was needed.

### Initial user-owned changes

```text
 M LVTN/NguyenAnNam_DH52201078_working.docx
 M src/Frontend/Blazor/Components/App.razor
 M src/Frontend/Blazor/wwwroot/css/vpp-login.css
 M src/Frontend/Blazor/wwwroot/css/vpp-responsive.css
```

These files remained unstaged and byte-for-byte unchanged by ARCH-001:

```text
E9E91C5A9A67F736E9CEAEC0DA1282DC68AF44E3D36039A465E214F0756AE17D  LVTN/NguyenAnNam_DH52201078_working.docx
E7FAD87A1CF792468E5378FA8ED3CFF0DFA5CB459F62DF72A9CFE3977F0F6B36  Components/App.razor
622C4C976ABE287A7CB6ED78DC984D58AF04CE67B03FCAD9C351AF80BB248319  wwwroot/css/vpp-login.css
81E49BD90CF3C823076CA3F8B0A3B5192038719E3D4605C4EC0EA176CDF28ACB  wwwroot/css/vpp-responsive.css
```

### Baseline evidence from QA-001/base commit

| Gate | Result |
|---|---|
| Release solution build | PASS - 0 warnings, 0 errors |
| Backend tests | PASS - 201/201 |
| Frontend tests | PASS - 32/32 |
| Default integration | PASS - 14 pass, 3 intentional LocalDB skips |
| Full disposable LocalDB integration | PASS - 17/17 twice |
| UI discovery / isolated login | PASS - 14 listed / 1 login |

## Implemented scope

### Shared contracts

- Removed the EF Core package and EF mapping attributes from Shared; the project now
  has no package or project dependency.
- Replaced the mixed Shared `StatusDisplay` type with framework-neutral
  `VppStatusContract`; CSS and Radzen names now live only in frontend helpers.
- Preserved every audited JSON property name, nested shape and computed serialized field.

### Backend ownership

- Moved `v_Users` and `v_WFXCompany` to `gtas_vpp_be.Model.View`.
- Retained equivalent keyless/view mapping in `VPPContext`. Runtime EF projection CLR
  ownership intentionally changed; the migration model and database schema did not.
- Updated service/controller consumers to depend on the backend model, not Shared.

### Frontend ownership

- Removed direct EF Core and Mapster packages because production frontend source had no use.
- Moved `GlobalClass`, `GlobalStorageModel` and `DropdownModel` into frontend state/models.
- Replaced Shared `GridColumnPropertyAttribute` and its mutable global ordering counter
  with a deterministic immutable frontend metadata registry for L01/L02/L04 grids.
- Fixed the frontend solution-slice path so every `.slnx` project entry resolves.

### Architecture and contract gates

- Locked the exact allowed production `ProjectReference` graph and forbidden dependency
  families with backend architecture tests.
- Added representative Shared JSON contract tests for login, permissions, catalog,
  requests, reports, notifications and the stored-procedure response envelope.
- Added frontend boundary, status presentation, grid metadata and API transport tests.
- Added `docs/architecture/ARCH-001-MODULE-MAP.md` as the ownership and
  migration-on-touch rule for later goals.

## Files changed by area

| Area | Purpose |
|---|---|
| Shared `.csproj`, DTOs, constants | remove EF/UI/runtime concerns while preserving wire shape |
| Model view projections and `VPPContext` | own backend-only database views and retain fluent mapping |
| Service/API consumers | use backend view models and framework-neutral status semantics |
| Frontend `.csproj`, state/models/helpers/components | own client state and deterministic presentation metadata |
| Backend/frontend tests | enforce dependency graph, serialization and UI metadata/transport behavior |
| Architecture/execution/master-plan docs | make boundaries, evidence, residuals and rollback explicit |

## Deviations from plan

- Physical folders were not reorganized wholesale. The module map defines logical
  ownership and requires migration only when an approved feature already touches code.
- The root `.sln` remains canonical; `.slnx` files remain intentional slices. Tests
  validate their paths rather than requiring project-count parity.
- Existing library-grid browser assertions failed on both the ARCH branch and a detached
  base-commit comparison. They were not edited to manufacture a green result.
- The task card suggested per-project reference-removal commits. The final implementation
  uses one atomic commit because package removal, type moves and consumer namespace changes
  are compile-coupled. The coupled rollback groups are listed below.

## Database and migration

- Backup/restore: N/A - no persistent database was opened or changed.
- Migration added: none.
- Stored procedure changed/SSMS validation: N/A.
- EF migration-model check: PASS - `No changes have been made to the model since the last migration.`
  This does not claim identical runtime metadata: the two keyless projection CLR types
  intentionally moved assemblies while retaining equivalent view/column mapping.
- Fresh/upgrade/rollback apply: N/A.
- Production/DigitalOcean applied: No.

## Tests added or updated

| Test area | Invariant | Result |
|---|---|---|
| Production dependency rules | canonical production catalog and exact allowed references; Shared has no packages/framework/backend/FE refs | PASS - 15/15 focused backend architecture/contract tests overall |
| Complete wire DTO manifest | every exported Shared DTO type/property, JSON key/type/attribute and enum value except the eight approved non-wire types moved by ARCH-001 | PASS - base/current SHA-256 both `4829EDEF2D89AA6D34A8157916DC81BB1885AFD650B617E07726B72ECF8A2482` |
| Shared serialization | representative exact JSON keys/nested shapes; sensitive login field absent | PASS |
| Frontend dependency rules | no direct/transitive EF or Mapster dependency | PASS - exact package command recorded below |
| Grid metadata | 25 legacy definitions, deterministic order/title/width/edit flags | PASS |
| Status presentation | semantic resource/text stable; frontend CSS/Radzen mapping stable | PASS |
| API transport | exact camelCase login request; PascalCase and camelCase responses accepted | PASS - 3/3 |

## Verification results

| Command/gate | Result | Evidence |
|---|---|---|
| `dotnet build gtas_vpp.sln -c Release` | PASS | 0 warnings, 0 errors; 4.55 s final rerun |
| backend Release tests | PASS | 212/212 |
| frontend Release tests | PASS | 53/53 |
| default integration tests | PASS | 14 pass, 3 intentional LocalDB skips |
| full disposable LocalDB integration | PASS | 17/17; 41 s |
| `dotnet ef migrations has-pending-model-changes --project <Migrations.csproj> --startup-project <API.csproj> --context VPPMigrationDbContext --configuration Release --no-build` | PASS | no migration-model/schema changes |
| `dotnet list ... reference` | PASS | exact graph in the module map |
| `dotnet list <Shared.csproj> package --include-transitive` | PASS | no package dependencies |
| `dotnet list <FE.csproj> package --include-transitive` | PASS | no direct/transitive EF Core or Mapster |
| focused frontend architecture/transport/status | PASS | 21/21 |
| `dotnet format ... --verify-no-changes` on 15 new C# files | PASS | no formatting delta |
| `git diff --check` | PASS | no whitespace errors |

## UI regression check

`LibraryGridScrollTests` was run against both the ARCH working tree and a detached
base-commit worktree with the same four protected user files overlaid.

| Test/journey | ARCH branch | Base comparison | Classification |
|---|---|---|---|
| Category grid layout | FAIL | FAIL | existing stale sticky-container assertion |
| Pricing grid layout | FAIL | FAIL | existing stale sticky-container assertion |
| Class grid layout | FAIL | FAIL | existing/flaky overflow-picker-width assertion |

Both sides were 0/3 in the same three test methods. Category and pricing rendered the
affected grids and cells before reaching their stale sticky assertion. ARCH-001 did not
change the protected layout CSS or relax any UI assertion. This is a baseline residual,
not evidence of an introduced UI regression; QA-003/UI work owns the browser-test update.

## Acceptance

| Criterion from master plan | Result | Evidence |
|---|---|---|
| Shared DTOs build without EF | PASS | zero-package Shared build and architecture tests |
| FE does not carry unused persistence/mapping dependencies | PASS | package graph and frontend boundary tests |
| Explicit minimal dependency rules and module map | PASS | exact graph tests and architecture document |
| HTTP wire DTO shape remains stable | PASS | complete base/current manifest plus representative runtime serialization/transport tests |
| Required build/unit suites remain green | PASS | Release 212 backend and 53 frontend tests; browser baseline residual compared separately |
| No database/business-rule change | PASS | no migration-model/schema delta, equivalent runtime view mapping and scoped diff review |

The Shared CLR assembly surface intentionally changed for internal ownership: eight
backend/frontend-only types left Shared and `VppStatusContract` replaced the mixed UI
helper. Those types are not HTTP wire contracts; their removal is enforced by boundary
tests and is not presented as CLR binary compatibility.

Definition of done: **DONE**. All ARCH-001 acceptance criteria are verifiable; the
browser residual reproduces at the protected base and no assertion was weakened.

## Risks and rollback

| Risk | Mitigation / owner |
|---|---|
| Hidden consumer relies on old Shared type namespace | full solution compile, exact reference search and contract tests; revert the internal move slice if discovered |
| JSON drift from DTO cleanup | exact property-set/nested-shape characterization; preserve legacy names/computed getters |
| EF view mapping drift | equivalent fluent keyless/view mapping retained, runtime consumers compiled, migration-model gate green |
| Future architecture erosion | exact graph/package/assembly tests plus module map review rule |

Application rollback is a revert of this single atomic commit; there is no database
restore or forward correction. If a surgical rollback is required, keep each coupled
group intact: (1) Shared EF/package removal + backend view move + consumers; (2) status
contract split + backend/frontend consumers; (3) frontend state/grid metadata move +
consumers; (4) their architecture/contract tests. Do not restore one side of a move
without its consumers. Characterization tests should be retained where possible.

## Residual findings and follow-up owners

| Finding | Follow-up |
|---|---|
| FE user-group payload omits department while backend upsert expects it and can coalesce missing data to `Guid.Empty` | AUTH-001/AUTH-002; requires business-safe contract/controller tests |
| Duplicate authentication/permission DTO families | AUTH-001 canonical contract migration |
| `SQLController` combines Newtonsoft serialization with `Ok(string)` | ARCH-002 typed endpoint migration |
| No-op DTO disposal and apparently dead contracts | ARCH-002 after consumer/public API audit |
| Mixed money and date/time semantics | PRICE/SET/ARCH compatibility and migration plan |
| Flat folders/direct `VPPContext` controller access | migrate per approved AUTH/CAT/REQ task; no mass rewrite |
| Stale/flaky `LibraryGridScrollTests` assertions | QA-003/UI task |

## Git

- `git diff --check`: PASS.
- New-file format ratchet: PASS for all 15 added C# files. A broader touched-file
  probe also reported legacy file-wide whitespace in existing controllers/services;
  it was not auto-applied because that would mix repository-wide formatting into this task.
- Protected user files: hash-identical and excluded from staging.
- Secret/build-output/cache review: PASS.
- Commit strategy: one scoped atomic architecture commit; the compile-coupled deviation
  from per-project commit granularity and partial rollback groups are documented above.
- Branch pushed / PR: N/A.

## Completion summary

- Outcome: Shared is framework-neutral, FE is free of unused EF/Mapster dependencies,
  backend/FE ownership is explicit, and dependency/JSON boundaries are executable tests.
- Behavior changed: internal type ownership and deterministic frontend grid metadata only.
- Behavior intentionally unchanged: HTTP API/wire behavior, auth, permissions, database
  schema/data and product UI.
- Build/test/database result: solution and all unit suites green; disposable integration
  green; migration model/schema unchanged; runtime projections remapped equivalently;
  browser failures reproduced at base and documented.
- Documentation: module map, this execution record and master plan synchronized.
- Follow-up: AUTH-001 owns the highest-risk discovered contract defect, but remains
  blocked until SEC-001 completes CP1.
- User action required: review/approve the next goal; ARCH-001 does not start it.
