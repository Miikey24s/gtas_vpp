# LEAN-08 — Release candidate quality and recovery

Status: DONE for the local release candidate gates.

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

- Idempotent migration script was generated through
  `20260716173404_Lean07EmailOutbox`; the latest package script is recorded in
  `docs/execution/LEAN-07.md`.
- Settlement snapshots use forward-only rollback, idempotency replay and
  correction revisions; email outbox keeps failed sends pending with bounded
  exponential retry.
- Authenticated Playwright journeys require the repository's
  `GTAS_E2E_ISOLATED=1` harness-owned LocalDB fixture. A direct run without the
  opt-in is rejected by the safety contract; the isolated runner was started
  but did not emit a complete result summary in this environment. No E2E pass
  is claimed from that run. Production credentials, external URLs and real
  mail providers were not used.

## Cutline

This local RC is suitable for source freeze and thesis traceability work. The
remaining external/manual evidence is a harness-owned authenticated browser
run plus any owner-authorized server backup/restore rehearsal; neither is
silently represented as completed here.
