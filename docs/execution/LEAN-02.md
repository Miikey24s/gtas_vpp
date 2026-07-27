# LEAN-02 - Trusted app-owned identity and canonical RBAC

- Status: DONE
- Priority: P0 (security floor)
- A+ cutline class: mandatory-product
- Path: STANDARD
- Owner/agent: Codex; production mutation remains owner-controlled
- Branch: `codex/sec-001-secret-rotation`
- Finished at (Asia/Ho_Chi_Minh): `2026-07-16`
- Related decisions/ADRs: D-001, D-004, D-008, D-009, D-011, D-012;
  [`ADR-001`](../decisions/ADR-001-app-owned-identity-gtas-menu-cutover.md),
  [`ADR-004`](../decisions/ADR-004-flat-rbac-separation-of-duties.md),
  [`ADR-008`](../decisions/ADR-008-registration-pending-approval-recovery.md)
- Dependencies verified: BASE-001, QA-001, SEC-001, SEC-002, ARCH-001 are complete
- Next package: LEAN-03 (registration, password change/recovery and notification journey)

## Objective and boundary

LEAN-02 establishes a trusted, app-owned authentication and authorization
boundary without reconnecting to the company's user database. `GTAS_MENU` is
retained only as historical/compatibility reference data; active application
accounts are owned by `AspNetUsers` in `GTAS_VPP_LIVE` and authenticate with
ASP.NET Core Identity password hashing.

This package does not implement self-registration, password recovery, email
delivery, request workflow, procurement settlement, or a production account
mutation. Those remain in LEAN-03 and later packages.

## Accepted security/business decisions

- One active account has exactly one canonical flat persona and one primary
  department. The four personas are Employee, Department Approver, Procurement
  Admin and System Admin.
- System Admin manages access but does not receive procurement/period-settlement
  authority. Backend action grants are fixed by the reviewed matrix; legacy UI
  visibility cannot become API authority.
- Account status is explicit (`PendingApproval`, `Active`, `Disabled`), and every
  session is checked against account status, membership and session version on
  each request.
- UI component toggles are allowed only inside the canonical role ceiling.
  Action mappings are immutable. System Admin access-navigation cannot be
  disabled by the UI.
- The first owner account is provisioned only through the guarded one-shot
  bootstrap command documented in `deploy/README.md`, after the owner supplies
  credentials and the department code. No production mutation was performed in
  this package.

## Preflight and preservation boundary

The following pre-existing user-owned files stayed unstaged and byte-for-byte
unchanged:

```text
E9E91C5A9A67F736E9CEAEC0DA1282DC68AF44E3D36039A465E214F0756AE17D  LVTN/NguyenAnNam_DH52201078_working.docx
E7FAD87A1CF792468E5378FA8ED3CFF0DFA5CB459F62DF72A9CFE3977F0F6B36  src/Frontend/Blazor/Components/App.razor
622C4C976ABE287A7CB6ED78DC984D58AF04CE67B03FCAD9C351AF80BB248319  src/Frontend/Blazor/wwwroot/css/vpp-login.css
81E49BD90CF3C823076CA3F8B0A3B5192038719E3D4605C4EC0EA176CDF28ACB  src/Frontend/Blazor/wwwroot/css/vpp-responsive.css
```

No company database, DigitalOcean resource, owner credential, external mail
provider or production account was accessed or changed.

## Implemented scope

### Backend identity and request authorization

- Added `AppUser : IdentityUser<int>` with app-owned profile fields, explicit
  account status, `MustChangePassword`, session version and company/employee
  metadata.
- Added `CurrentUserContext` and `AppAuthenticationService` for server-authoritative
  identity, `/api/auth/me`, login, logout and permission snapshot retrieval.
- Added request-by-request JWT validation for account status, session version,
  canonical company, canonical group code/id and active primary department.
- Added typed membership administration with one active membership, optimistic
  row-version checks, serializable/applock protection, self-change protection,
  last-System-Admin protection, audit rows and session invalidation.
- Removed the generic SQL controller/executor and legacy TripleDES password
  encoder. Authentication no longer depends on a stored procedure or reversible
  password material.
- Kept policies action-only. UI/menu codes are not registered as backend policy
  grants.

### Canonical reference data and migration

- Added `CanonicalRbac` and stable action/page mapping identifiers for the four
  personas and reviewed action matrix.
- Added `A01_SecurityAudit` and `A02_AuthBootstrapOperation` ledgers.
- Added migration `20260715160616_AddTrustedAccessIdentity` with:
  - Identity tables/profile columns;
  - canonical group metadata and `P04` account/membership columns;
  - filtered unique active-membership protection and account/membership checks;
  - migration preflight for duplicate active memberships, ID collisions and
    unknown canonical groups;
  - destructive `Down` guard requiring an explicit sentinel.
- Removed the body of `sp_Authen_Login` from the canonical stored-procedure
  source. `04_RetireLegacyAuth.sql` remains an idempotent upgrade drop for
  databases that already contain the procedure.
- Added a guarded, audited, one-shot owner bootstrap. It is disabled by
  default, requires the migrator execution mode, a reference value and an empty
  ledger; it does not seed demo accounts.

### Frontend session and access administration

- Versioned the auth cookie and kept only stable claims in the cookie; `/me` and
  `/me/permissions` are server-authoritative.
- Added a single-use login-ticket cache, safe return-url handling, uniform 401
  invalidation, backend logout and permission refresh coordination.
- Reworked permission administration to typed canonical roles/memberships:
  immutable role definitions, guarded UI mappings, row-version membership
  updates/deactivation, role/department lookup and no hard delete/self-change.
- Separated API action rows from UI components in page-access calculation so an
  API grant cannot keep a hidden navigation page visible.
- Updated the permission E2E journey to assert semantic report heading markup,
  independent of the active Vietnamese/English locale.

### Deployment and operator documentation

- Removed the obsolete password-encryption and Radzen API-key configuration
  surface from app settings, compose, Aspire, CI and environment examples.
- Updated `deploy/README.md`, `README.md` and `SECURITY.md` with the app-owned
  Identity model, guarded first-owner bootstrap, audit/verification queries and
  forward-only recovery guidance.

## Database and rollback strategy

Migration SQL was generated and inspected locally. The forward path is:

1. take a verified, timestamp-matched backup before any shared/staging/live
   apply;
2. run the normal EF migration and required reference scripts;
3. execute the one-shot owner bootstrap only after credentials and department
   code are confirmed;
4. verify the bootstrap ledger, active account/membership counts, login and
   `/me/permissions` before opening traffic.

Generated ignored evidence files are retained locally for reuse:

- `tmp/lean02-idempotent.sql` - collision, active-membership and unknown-group
  preflight plus the forward schema/reference checks;
- `tmp/lean02-rollback.sql` - guarded rollback probe containing
  `AUTH_DESTRUCTIVE_DOWN_BLOCKED`.

Rollback is forward-only for a shared/live database: stop writers, restore the
paired database backup only if a verified restore is required, or fix forward
with a corrective migration. Never restore legacy credentials or reactivate the
contained demo identities. The EF `Down` path is intentionally blocked unless a
human supplies the explicit destructive sentinel.

## Verification evidence

| Gate | Result |
|---|---|
| `dotnet build gtas_vpp.sln -c Release` | PASS - 0 warnings, 0 errors |
| Backend unit/architecture tests | PASS - 263/263 |
| Frontend unit tests | PASS - 63/63 |
| Disposable SQL Server LocalDB integration | PASS - 17/17; legacy login procedure count 0; cleanup verified |
| `dotnet ef migrations has-pending-model-changes ...` | PASS - no pending model changes |
| Permission mutation E2E (isolated LocalDB + explicit mutation opt-in) | PASS - 1/1 |
| Gitleaks v8.30.1 current-tree scan | PASS - no leaks; archive checksum/version pinned |
| `git diff --check` | PASS |
| Protected-file hashes | PASS - all four unchanged |

The Gitleaks invocation was run with a process-scoped PowerShell execution
policy bypass because the host policy blocks unsigned script invocation; the
machine policy was not changed. The pinned scanner downloaded to a temporary
folder and was removed after the scan. Playwright Chromium v1228 was installed
for the isolated E2E harness and intentionally remains available for later
packages.

## Acceptance

- [x] Active authentication is app-owned and hashed; no company user DB
      reconnect is required.
- [x] Legacy login code/procedure cannot authenticate an application session.
- [x] Canonical four-role action ceiling and separation of duties are enforced
      in backend policy and service checks.
- [x] Account/membership changes are typed, audited, concurrency-safe and
      invalidate current sessions.
- [x] UI permission changes are limited to canonical UI components and update
      the current session immediately.
- [x] Migration forward/preflight/rollback behavior is executable and tested on
      disposable SQL Server.
- [x] No protected user file, production database or production account was
      mutated.

## Residuals and handoff to LEAN-03

1. Production has no active app owner yet. Owner credentials and a valid
   department code are required before running the documented bootstrap.
2. Registration remains `PendingApproval` and must add rate limiting, duplicate
   username/email checks, admin activation, password-change and recovery flows,
   audit events and in-app/email notifications in LEAN-03.
3. The eight historical Google API-key alerts remain open under the owner's
   explicit waiver. They were not claimed as revoked and must not be used as a
   release gate.
4. Report/request/procurement business workflows are intentionally unchanged;
   later packages own their implementation and acceptance tests.

Definition of done for LEAN-02: **DONE**. Continue directly with LEAN-03.
