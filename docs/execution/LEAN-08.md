# LEAN-08 — Release candidate quality and recovery

Status: DONE for the local release candidate gates and approved local-only
recovery alternative.

## Gates

- Release solution build: 0 warnings, 0 errors.
- Backend Release tests: 384 passed.
- Frontend Release tests: 96 passed.
- Integration Release tests: 14 passed, 6 skipped by the existing explicit
  LocalDB opt-in gate.
- UI harness syntax smoke (`ComposeConfigurationSyntaxTests`): 2 passed.
- EF `has-pending-model-changes`: no changes since
  `20260716173404_Lean07EmailOutbox`.
- Current-tree gitleaks scan: clean.
- `git diff --check`: clean before checkpoint staging.
- Protected Word/App.razor/login CSS/responsive CSS SHA-256 values remain the
  handoff values and are not staged.

## Recovery and release evidence

- DEP-002 local-only alternative passed on a disposable unique LocalDB
  instance: backup with checksum, `RESTORE VERIFYONLY`, drop, restore, marker
  probe after restore, second checksum verification, and full cleanup. The
  backup evidence is recorded in `docs/execution/DEP-002.md`; no shared,
  production or deployed database was touched.
- Idempotent migration script was generated through
  `20260716173404_Lean07EmailOutbox`; the latest package script is recorded in
  `docs/execution/LEAN-07.md`.
- Settlement snapshots use forward-only rollback, idempotency replay and
  correction revisions; email outbox keeps failed sends pending with bounded
  exponential retry.
- Authenticated Playwright journeys require the repository's
  `GTAS_E2E_ISOLATED=1` harness-owned LocalDB fixture. A direct run without the
  opt-in is rejected by the safety contract. With the isolated fixture and the
  explicit mutation opt-in, the complete Release UI suite finished with
  `16 passed, 0 failed, 0 skipped` in 427.0 seconds; the targeted
  `ProductCatalogTests` rerun separately also finished `1 passed` in 35.8
  seconds. A second clean isolated rehearsal then finished with
  `16 passed, 0 failed, 0 skipped` in 406.9 seconds. Production credentials,
  external URLs and real mail providers were not used.

## Cutline

This local RC is suitable for source freeze and thesis traceability work. The
two clean-state UI demonstrations and the approved local-only recovery
alternative are complete. A deployed/server backup, off-host copy and
production restore remain conditional owner-controlled evidence and are not
claimed by this local release.
