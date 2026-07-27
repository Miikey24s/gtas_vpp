---
name: gtas-vpp-db-safety
description: Implement or review GTAS VPP database changes safely. Use for EF entity mapping, migrations, indexes, constraints, stored procedures, seed/backfill, data repair, schema validation, LocalDB integration, backup/restore planning, or any change under src/Backend/Migrations that can affect persisted data.
---

# GTAS VPP Database Safety

## Load authority

1. Read root `AGENTS.md`, `src/Backend/AGENTS.md`, `docs/architecture/ARCH-001-MODULE-MAP.md`, and the related execution record.
2. Run `./scripts/gtas.cmd preflight -Scope backend`.
3. Inspect the entity configuration, current model snapshot, neighboring migrations, database tests, and every runtime consumer of the changed contract.

## Classify risk

- Mapping-only: no schema delta expected; prove with pending-model check.
- Additive: new nullable column/table/index or compatible reference data.
- Backfill/constraint: existing rows must be validated and transformed before enforcing a rule.
- Destructive: drop, narrowing conversion, hard delete, irreversible settlement/history change, or shared/live mutation.

Never silently treat a backfill or destructive change as a normal code edit.

## Design the change

- Prefer additive schema, deterministic backfill, compatibility period, then later cleanup.
- Add duplicate/orphan/range preflight before a new unique key, foreign key, non-null or check constraint.
- Keep authorization and business invariants in backend code and database constraints where appropriate.
- Use `IsDeleted` for normal deletion. Hard delete requires explicit approval and recovery evidence.
- Make seeds, repair operations and stored procedures idempotent when reruns are expected.
- If `Down()` would discard valid data, guard it or make the migration forward-only and document verified backup/restore or corrective migration as recovery.
- Validate stored procedures in SSMS before or alongside code debugging.

## Respect environment boundaries

- Use TEST/DEMO or harness-owned disposable LocalDB by default.
- Never print, save or commit connection strings, passwords or database credentials.
- Shared, staging or production mutation requires explicit authority, a verified timestamp-matched backup, reviewed SQL, maintenance/rollback plan and post-change reconciliation.

## Verify

Run the proportionate gates:

```powershell
./scripts/gtas.cmd test-backend
dotnet test tests/Backend.IntegrationTests/gtas_vpp_be.IntegrationTests.csproj -c Release
dotnet tool restore
dotnet tool run dotnet-ef migrations has-pending-model-changes --project src/Backend/Migrations/gtas_vpp_be.Migrations.csproj --startup-project src/Backend/Api/gtas_vpp_be.csproj --context VPPMigrationDbContext --configuration Release --no-build
```

For a real migration, also review generated SQL, test fresh apply and sanitized/disposable upgrade, verify key row counts/invariants, and rehearse the actual recovery path. Do not call unit tests proof of SQL Server behavior.

## Finish

Record migration name, preflight, SQL/SSMS review, fresh/upgrade result, reconciliation, recovery and residual risk in the execution record. Review the complete diff and create a scoped local commit; never push, deploy or mutate production without explicit user instruction.
