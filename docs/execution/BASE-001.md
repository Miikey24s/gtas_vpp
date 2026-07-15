# BASE-001 — Freeze Git/source/test baseline and decision register

- Status: DONE
- Priority: P0
- A+ cutline class: safety-floor
- Path: STANDARD
- Owner/agent: Codex; project owner approves the handoff
- Branch: `codex/base-001-baseline`
- Base commit: `6c7a70108ed6937115b8cca1903257dadbcddd27`
- Started at (Asia/Ho_Chi_Minh): `2026-07-15T12:18:36+07:00`
- Finished at: `2026-07-15T12:43:01+07:00`
- Final master-plan SHA-256: `AAC278675BDD366710B1A3FC6DC9B767FF1E6C77967AFC0C2BC867021F4A126B`
- Final decision-register SHA-256: `F2E5FC8045EEAA6269A14E67E8A3D708F73DB07582A42A6E87CBEA341E13A1BA`
- Related decisions/ADRs: [`D-001..D-012 index`](../decisions/000-index.md)
- Dependencies verified: master plan approved; D-001..D-012 accepted
- User approval required: Yes — before any task after BASE-001
- User approval evidence: the active Goal explicitly authorizes only BASE-001 and requires a stop before the next task

## Objective

Create a repeatable docs-only baseline for the current Git/source/test state, preserve pre-existing dirty work, inventory routes/APIs/SPs/permissions, lock accepted decisions as ADRs and record safe test/data boundaries before any product mutation.

## Why now

The repository contains user work in progress and AI-generated source with architectural/security debt. Later rollback, migration and comparison are not defensible without a commit-specific baseline and explicit ownership boundary.

## Non-goals

- No business source, UI, configuration, database schema/data, Word or server change.
- No DigitalOcean, credential, production or external-provider access.
- No full authenticated Playwright run until QA-001 proves a disposable test target.
- No implementation of BASE-002, SEC-001, SEC-002, ENV-001 or any later task.

## Preflight

- [x] Read `AGENTS.md` and repository-wide instructions.
- [x] Read BASE-001 task card, decision register and execution template.
- [x] Saved initial `git status`; assigned ownership for every dirty/untracked path.
- [x] Created task branch from the captured base commit without overwriting worktree changes.
- [x] Ran baseline Release build and backend/frontend tests.
- [x] DB preflight marked N/A: task made no DB connection or mutation.
- [x] UI source/discovery boundary inspected; no UI source mutation, so UI editing instructions were not invoked.
- [x] Word marked out of scope and preserved.
- [x] External systems/secrets marked out of scope; no authority assumed.

### Initial git status

```text
## Nam...origin/Nam
 M LVTN/NguyenAnNam_DH52201078_working.docx
 M gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/App.razor
 M gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/wwwroot/css/vpp-login.css
 M gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/wwwroot/css/vpp-responsive.css
?? docs/
```

Detailed ownership is recorded in [`BASE-001-INVENTORY.md`](../baseline/BASE-001-INVENTORY.md#1-worktree-ownership-and-preservation-boundary).

### Baseline commands/results

| Command | Result | Evidence |
|---|---|---|
| `dotnet build gtas_vpp.sln -c Release` | PASS — 0 warnings/errors | Inventory §7; current turn output |
| Backend Release tests | PASS — 147/147 | Inventory §7; current turn output |
| Frontend Release tests | PASS — 29/29 | Inventory §7; current turn output |
| UI test discovery | PASS — 12 listed; execution intentionally not run | Inventory §7 and test boundary |

## Expected scope

### Frontend

- Project/module/file expected: source read-only; only route/action/permission inventory in docs.
- UI states/routes/permissions affected: none at runtime.

### Backend

- Project/module/file expected: source read-only; only controller/policy/SP/job/test inventory in docs.
- API/policy/service/background job affected: none at runtime.

### Database

- Entity/table/index/SP/migration expected: inventory only.
- Additive/backfill/cutover/drop stage: N/A.

### Business rules

- Rule/state/invariant: examples lock accepted semantics for day-05 boundary, cancellation/replacement, supplements and whole-company settlement.
- Boundary/timezone/scope: Asia/Ho_Chi_Minh; single company; own/department/company scopes.

### Documentation/thesis

- Added ADR index and 12 ADRs, baseline inventory and this execution record.
- Clarified plan-only autosave cutline and assigned route-catalog parity to QA-003.
- Thesis `.docx` and diagrams were not changed.

## Implementation steps

1. [x] Capture base commit, branch/upstream and complete dirty-work ownership table.
2. [x] Create `codex/base-001-baseline` without touching existing modifications.
3. [x] Inventory projects, physical/logical routes, APIs, authorization, SPs, startup/background work and tests.
4. [x] Run required Release build and backend/frontend test commands.
5. [x] Discover UI tests without launching browser/Aspire/database mutation; document the unsafe boundary.
6. [x] Convert all accepted decisions into normative ADRs and verify direct/downstream task mapping.
7. [x] Record business boundary examples, backup strategy, findings and later-task owners.
8. [x] Complete independent docs/traceability QA, final Git checks and docs-only commit.

## Files actually changed

| File/group | Why | User-owned overlap? |
|---|---|---|
| `docs/planning/*` | Approved audit/master plan; decision link and two internal consistency clarifications | No |
| `docs/decisions/*` | ADR index plus D-001..D-012 normative records | No |
| `docs/baseline/BASE-001-INVENTORY.md` | Reproducible Git/source/test/data-boundary evidence | No |
| `docs/execution/BASE-001.md` | Required Target execution record | No |

The four pre-existing modified Word/frontend paths were not edited by BASE-001 and will not be staged.

## Deviations from plan

- No authenticated viewport screenshots were captured. This is the safe standard path, not a waived product test: credentials/base URL were absent, localhost was not ready, and the current UI harness can auto-start Aspire with `MigrateAndSeed` against an unproven `TestEnv`. The task card explicitly permits discovery plus recording missing credentials; QA-001 owns fixture isolation before browser execution.
- `rg.exe` was blocked by Windows execution policy, so read-only inventory used PowerShell `Get-ChildItem`/`Select-String` instead. Evidence scope was preserved.

## Database and migration

- Backup ID/path: N/A — no persistent DB opened.
- Backup restore verified: N/A.
- Source DB type: no DB target.
- Preflight duplicate/orphan result: N/A.
- Migration name/script/bundle hash: N/A.
- Generated SQL reviewed by: N/A.
- SSMS validation result: N/A.
- Fresh DB apply: N/A.
- Existing sanitized DB upgrade: N/A.
- Post-migration probes: N/A.
- Rollback/restore rehearsal: N/A for docs; future strategy recorded in inventory §8.
- Production applied: No.

### Migration output summary

```text
No SQL connection, migration, seed, backup or DigitalOcean action was performed.
```

### Data reconciliation

| Check | Before | After | Expected |
|---|---:|---:|---:|
| Persistent database rows | Not accessed | Not accessed | No mutation |

## Tests added/updated

No product tests were changed. BASE-001 records current evidence and routes missing coverage to QA-001/002/003.

## Required commands

| Command | Result | Count/time | Evidence |
|---|---|---|---|
| `dotnet build gtas_vpp.sln -c Release` | PASS | 0 warning, 0 error; 21.40 s | console output + inventory §7 |
| `dotnet test gtas_vpp_be.Tests/gtas_vpp_be.Tests.csproj -c Release --no-restore` | PASS | 147 passed; test duration 6 s | console output + inventory §7 |
| `dotnet test gtas_vpp_fe.Tests/gtas_vpp_fe.Tests.csproj -c Release --no-restore` | PASS | 29 passed; test duration 1 s | console output + inventory §7 |
| UI E2E discovery | PASS | 12 listed | `--list-tests`; no browser/data mutation |
| SQL/API integration | MISSING BASELINE | No SQL Server/Testcontainers suite | Routed to QA-001/002 |
| Vulnerability/secret scan | PASS | 0 likely secret/token/private-key/credential/connection-string value hits in staged docs | value-oriented docs scan |
| `dotnet format --verify-no-changes` | N/A | no C#/project source changed | docs-only task |
| `git diff --check` | PASS | working and staged checks | final Git verification |

## Failures

- Existing/new test failures: none in the executed Release build/backend/frontend suites.
- Tooling: `rg.exe` access denied; PowerShell fallback succeeded.
- Coverage gaps are recorded as baseline facts, not mislabeled as passing tests.

## UI verification

Runtime UI verification is N/A for a docs-only task. Static/source route discovery completed; no screenshots or interaction claims are made.

| Route/journey | Role | 390×844 | 768×1024 | 1920×1080 |
|---|---|---|---|---|
| Authenticated app | N/A | NOT RUN — unsafe fixture boundary | NOT RUN | NOT RUN |

### Evidence

- Screenshot/contact sheet path: N/A.
- Browser trace/video path: N/A.
- Console/network summary: no app/browser launched.
- Reviewer note: UI test names and source/static route inventory only; see baseline §§6–7.

## Acceptance

| Criterion from master plan | Result | Evidence |
|---|---|---|
| Baseline commit/source/commands/evidence are explicit and repeatable | PASS | Base commit/branch, replay commands, command results and containing docs-only commit |
| Route/API/SP/permission/background/test inventory exists | PASS | `docs/baseline/BASE-001-INVENTORY.md` §§2–7 |
| No user dirty file is staged or overwritten | PASS | 21 staged paths are all under `docs/`; four user files remain unstaged, with pre-task mtime and preservation fingerprints |
| Test-data boundary and backup strategy are documented | PASS | Inventory §§7–8; no DB connection |
| ADR exists for every answered decision | PASS | ADR index plus 12 linked files |
| Decision→direct-task mapping has no gap/blocker | PASS | ADR index and decision-register cross-check |
| Day-05, cancel/replacement, supplement and settlement examples are fixed | PASS | Inventory §9 |
| Release build and current backend/frontend suite pass | PASS | 0 warnings/errors; 147/147; 29/29 |
| UI discovery and missing credential/fixture boundary are recorded honestly | PASS | 12-test discovery plus environment/base-URL checks; no unsafe run |

### Negative/adversarial checks

- [x] No direct API/DB/browser mutation was performed under a documentation task.
- [x] No external server/secret/provider access was attempted.
- [x] No stale UI test target was assumed safe merely because it might respond.
- [x] No historical test count was copied as current evidence.
- [x] Final staged-index check proves no user-owned path is included.
- [x] Final docs scan proves no credential value/private connection string was introduced.

### Definition of done decision

`DONE` — all BASE-001 acceptance criteria have direct evidence; the containing commit is docs-only and the next task requires new user approval.

## Risks observed

| Risk | Probability/impact | Mitigation | Owner |
|---|---|---|---|
| Pre-existing dirty files accidentally enter commit | Medium / High | Explicit owner matrix; stage exact `docs/` paths; inspect cached diff | BASE-001 agent |
| Inventory becomes stale | High / Medium | Commit/timestamp/hash and replay commands; update per task | Each later task |
| UI suite mutates shared/real data | High / High | Discovery only now; QA-001 disposable boundary first | QA-001 |
| ADR prose diverges from task mapping | Medium / High | Direct/downstream index and automated/manual link validation | DOC-001 / checkpoint review |
| Docs accidentally contain a secret value | Low / High | Value-pattern scan plus staged diff review; no config values copied | BASE-001 agent |
| Existing thesis filename exposes owner name/student-code pattern in internal paths | Known / Medium for public release | Keep only where required for preservation instructions; D-009/UI-002/DOC-002 anonymize public/final evidence | Project owner / D-009 tasks |

## Rollback/recovery performed or rehearsed

- Application rollback: N/A — no application mutation.
- Database restore/forward correction: N/A — no database access.
- Feature flag/compatibility path: N/A.
- Docs rollback: revert/delete the BASE-001 docs commit/branch only; preserve all user dirty paths.
- Result: worktree preservation verified throughout execution; final status/index/hash check passed after the docs-only commit.

## Residual risk

Current passing unit suites do not prove SQL Server behavior, full middleware/API integration or authenticated role×route UI behavior. Those gaps are explicit owners for QA-001/002/003 and must not be presented as BASE-001 failures or as already-green product gates.

The private baseline still names the repository’s existing thesis working file, which contains the owner name/student-code pattern. This is not a credential and is necessary to preserve the correct file now, but it must not leak into public demo/evidence after the D-009 anonymization gate.

## Git

### Final status/diff review

- [x] `git diff --check` pass for working and staged changes.
- [x] Entire status plus staged/unstaged diff reviewed.
- [x] No secret/bin/obj/TestResults/log/cache/render/audit-temp staged.
- [x] Only the 21 Markdown files in task scope staged.
- [x] No Word/tooling/diagram/screenshot/instruction file deleted.

| Commit | Message | Scope | Build/test evidence |
|---|---|---|---|
| Containing task commit (resolve with `git log -1`) | `docs: freeze BASE-001 project baseline` | Planning, ADRs, baseline and execution record only | Build 0/0; BE 147; FE 29 |

- Branch pushed: No.
- PR: N/A.
- Last known good base commit: `6c7a70108ed6937115b8cca1903257dadbcddd27`.

## Blockers

None for BASE-001.

## What was exhausted

Not applicable; missing external/UI authority is intentionally outside this task and does not block the documentation baseline.

## Safe work that can continue

Only final docs QA/Git verification inside BASE-001. No next product task starts before user approval.

## Continuation note

- Current task/status: BASE-001 / DONE.
- Current branch/HEAD: `codex/base-001-baseline`; containing docs-only commit on base `6c7a701`.
- Working tree status: four preserved user modifications; BASE-001 docs committed.
- Files being edited: none after commit.
- Last completed step: final staged-index, docs value scan, whitespace and scope gates passed.
- Last command and summarized result: staged set contains 21 docs paths, 0 non-doc paths and 0 forbidden artifacts; cached diff check passes.
- Current database/migration state: untouched.
- Current browser/app/process state: no app/browser running.
- Decisions assumed/confirmed: D-001..D-012, linked through ADR index.
- Blocker: none.
- Next exact action: stop and wait for project-owner approval; do not start another task.
- Do not redo: Release build, 147 backend tests, 29 frontend tests and 12 UI-test discovery unless source/project inputs change.
- Warnings about user-owned changes: never stage/revert the Word, `App.razor`, `vpp-login.css` or `vpp-responsive.css` modifications.

## Completion summary

- Outcome first: repeatable BASE-001 Git/source/test/decision baseline completed without product/data mutation.
- Behavior changed: none.
- Behavior intentionally unchanged: all application, database, configuration, server and Word behavior.
- Build/test/migration/UI final result: build/backend/frontend green; DB and browser intentionally untouched.
- Commit(s): containing docs-only commit `docs: freeze BASE-001 project baseline`.
- Documentation/ADR/diagram updated: planning/ADR/baseline/execution docs; no diagram.
- Follow-up task(s): only as mapped in the master plan after owner approval.
- User action required: approve BASE-001 handoff before any next task.
- Master plan status updated at: `2026-07-15T12:43:01+07:00`.
