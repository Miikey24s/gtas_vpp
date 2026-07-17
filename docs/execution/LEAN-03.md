# LEAN-03 — Registration and recovery

**Status:** DONE
**Date:** 2026-07-16
**Dependency:** LEAN-02 access contracts
**Next package:** LEAN-04 UI foundation and independent brand

## Scope and decisions

- Identity remains application-owned; no reconnection to the company's user database is introduced.
- Self-registration creates an `AppUser` in `PendingApproval` with no role or department membership. Pending accounts cannot receive business claims or call business APIs.
- Username, normalized email, and optional employee code remain unique at the database/application boundary. Password policy is validated before duplicate lookup so registration does not disclose account existence.
- Only an authorized `PermissionManage` administrator can activate an account, assign the canonical role and primary department, or issue an administrative temporary password.
- Administrative reset invalidates sessions and sets `MustChangePassword`; the temporary credential is returned only once to the administrator UI and is never written to logs.
- Recovery and registration responses are generic and rate-limited. Email is an adapter, disabled by default; the audited admin fallback is the demo-safe path. Mailpit/local delivery proof is deliberately deferred to LEAN-07.
- Data Protection keys are persisted through the configured local path or the Compose `backend-keys` volume so Identity confirmation/reset tokens survive restarts.

## Implemented surface

- Backend lifecycle service/controller: registration, email confirmation, password recovery/reset, authenticated password change, activation, and admin reset.
- Atomic pending-account activation plus canonical membership upsert (serializable transaction/application lock) and durable in-app notifications/audit records.
- Server-authoritative `MustChangePassword` claim and middleware that blocks business APIs until the password is changed.
- Fixed-window limits: registration 3/10 minutes per IP, recovery 5/10 minutes per IP, confirmation 10/10 minutes per IP, password operations 5/10 minutes per user+IP.
- Vietnamese-first public routes: `/Account/Register`, `/Account/ForgotPassword`, `/Account/ResetPassword`, `/Account/ConfirmEmail`, and `/Account/ChangePassword`; login redirects temporary-password sessions to the change-password route.
- Permission user grid supports pending activation and active-account admin reset without exposing secrets in telemetry.
- Compose local/production files persist Data Protection keys; no schema migration was required because LEAN-02 already supplied Identity tables, token providers, normalized unique indexes, and the filtered employee-code index.

## Acceptance evidence

| Gate | Result |
|---|---:|
| `dotnet build gtas_vpp.sln -c Release --no-restore` | PASS — 0 warnings, 0 errors |
| Backend Release tests | PASS — 290/290 |
| Frontend Release tests | PASS — 75/75 |
| Disposable SQL Server LocalDB integration | PASS — 17/17 |
| Account lifecycle service targeted tests | PASS — 6/6 |
| Registration UI E2E (390×844, 768×1024, 1920×1080) | PASS — 1/1; no horizontal overflow or unlabeled visible form controls; PendingApproval login rejected |
| Compose YAML syntax | PASS — 2/2 |
| EF `has-pending-model-changes` | PASS — no changes since last migration |
| Gitleaks v8.30.1 current-tree scan | PASS — no leaks found |
| `git diff --check` | PASS |

## Rollback and recovery

- No database migration or destructive data operation is part of this package; rollback is a normal code revert to the prior LEAN-02 commit.
- If a lifecycle defect is discovered after deployment, disable public registration/recovery through configuration/rate-limit policy, retain existing active accounts, and use audited administrator activation/reset as the forward-correction path.
- Never restore or re-enable a compromised credential. Production was not mutated in this package because there is no active app-owner/department bootstrap authority in scope.
- Email delivery failures remain non-fatal and auditable; they do not turn a generic recovery response into an account-existence oracle.

## Explicit residuals

- `EmailNotifications.Enabled` remains false by default. Real provider credentials and Mailpit proof are LEAN-07 work, not a hidden claim of delivery in this package.
- The owner's 2026-07-15 waiver for eight historical Google API-key alerts remains unchanged; alerts stay open and are not described as revoked.
- Four user-owned files were preserved and not staged. Their verified SHA-256 values are recorded here for the package handoff:
  - `LVTN/NguyenAnNam_DH52201078_working.docx` — `E9E91C5A9A67F736E9CEAEC0DA1282DC68AF44E3D36039A465E214F0756AE17D`
  - `gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/Components/App.razor` — `E7FAD87A1CF792468E5378FA8ED3CFF0DFA5CB459F62DF72A9CFE3977F0F6B36`
  - `gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/wwwroot/css/vpp-login.css` — `622C4C976ABE287A7CB6ED78DC984D58AF04CE67B03FCAD9C351AF80BB248319`
  - `gtas_vpp_fe/gtas_vpp_fe/gtas_vpp_fe/wwwroot/css/vpp-responsive.css` — `81E49BD90CF3C823076CA3F8B0A3B5192038719E3D4605C4EC0EA176CDF28ACB`

## Handoff

LEAN-04 can proceed without changing the account contracts: use the typed lifecycle routes, existing Radzen shell, and the responsive/a11y baseline above. Keep registration/recovery generic, keep pending accounts zero-privilege, and do not add a framework rewrite or AI/provider dependency.
