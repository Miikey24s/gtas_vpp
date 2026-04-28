# P2 Service Layer Refactor

Date: 2026-04-26
Worker: Codex

## Summary

- P2.1: Extracted `IEnvironmentResolver` / `EnvironmentResolver` and wired it into `BaseServices`, `UnitOfWork`, `UnitOfWorkFactory`, and BE DI.
- P2.2: Extracted `IUserNameResolver` / `UserNameResolver` with cached reflection for user-name enrichment.
- P2.3: Extracted CRUD operations into `IGenericRepository<T>` / `GenericRepository<T>` and removed CRUD from `BaseServices`.
- P2.4: Extracted stored procedure/query execution into `IStoredProcedureExecutor` / `StoredProcedureExecutor` and removed `SP` / `Query` from `BaseServices`.
- P2.5: Replaced `TransactionScope` in `UnitOfWork` with EF Core `IDbContextTransaction`.
- P2.6: Removed `BusinessService` / `IBusinessService`; controllers now use repositories, stored procedure executor, username resolver, or existing direct relation queries.
- P2.7: Merged duplicated `GetMyOrdersAsync` / `GetMyOrdersSummaryAsync` query logic into `GetFilteredOrdersAsync`.

## Controller Notes

- `BaseGenericController`, `LibraryController`, `VPPRequestController`, `PermissionController`, `SQLController`, and `AuthController` no longer reference `BusinessService`.
- `PermissionController` company display data is read from `v_WFXCompanies` by `MemberCompanyCode`; no hardcoded `PHONG PHU INTERNATIONAL JSC` or `PPJ` remains in that controller.
- API routes, DTO contracts, DB schema, migrations, stored procedures, Auth/JWT/password logic, UI, and VPP workflow were not intentionally changed.

## Verification

- `dotnet build gtas_vpp/gtas_vpp.slnx` passed after each P2.1-P2.7 task.
- Final `dotnet build gtas_vpp/gtas_vpp.slnx` passed with 0 warnings and 0 errors.
- Final `dotnet test code-be/gtas_vpp_be.Tests/` passed: 17 passed, 0 failed, 0 skipped.
