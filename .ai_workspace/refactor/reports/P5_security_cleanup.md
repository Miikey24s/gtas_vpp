# Phase 5 - Security Fixes + Cleanup

Completed: 2026-04-26  
Worker: Codex

## Summary

Phase 5 completed the critical security fixes from the post-refactor review and added backend coverage for controllers and exception middleware.

## Changes

### P5.1 - SQL Injection Fix

- Changed `IStoredProcedureExecutor.ExecuteQueryAsync` to require `params object?[] parameters`.
- Changed `StoredProcedureExecutor` to call `FromSqlRaw(query, parameters)`.
- Updated `SQLController.Query` to accept a parameterized `SqlQueryRequest` payload:
  - `Query`: SQL with EF parameter placeholders.
  - `Parameters`: JSON values normalized to .NET primitive values before execution.
- Verified there are no remaining `FromSqlRaw(query)` calls.

### P5.2 - CORS Policy Fix

- Added `CorsSettings:AllowedOrigins` to:
  - `code-be/gtas_vpp_be/appsettings.json`
  - `code-be/gtas_vpp_be/appsettings.Development.json`
- Replaced `AllowAll` policy with `AllowFrontend`.
- Removed `AllowAnyOrigin()` and configured:
  - `WithOrigins(allowedOrigins)`
  - `AllowAnyMethod()`
  - `AllowAnyHeader()`
  - `AllowCredentials()`

### P5.3 - Cookie Security

- Added FE cookie hardening in `code-fe/gtas_vpp_fe/gtas_vpp_fe/Program.cs`:
  - `HttpOnly = true`
  - `SameSite = SameSiteMode.None`
  - `SecurePolicy = CookieSecurePolicy.Always`

### P5.4 - FE Commented Code Cleanup

- Removed stale commented-out code blocks in Components, especially old `BusinessService`, `BaseService`, `SPServiceRead`, and `.ContinueWith(...Result...)` fragments.
- Kept comments that explain active behavior, permission checks, filtering, and design-time fallbacks.
- Scan result: no remaining `.ContinueWith`, `.Result`, `_businessService`, `IBusinessService`, `EF_BASEMETHOD`, or `SPServiceRead` references in `Components/**/*.razor.cs`.

### P5.5 - Tests

Added:

- `code-be/gtas_vpp_be.Tests/MiddlewareTests/ExceptionHandlingMiddlewareTests.cs`
- `code-be/gtas_vpp_be.Tests/ControllerTests/VPPRequestControllerTests.cs`
- `code-be/gtas_vpp_be.Tests/ControllerTests/AuthControllerTests.cs`

Also added test project reference to `gtas_vpp_be` so controller and middleware tests can compile.

Coverage added:

- `KeyNotFoundException` -> 404 ProblemDetails
- `UnauthorizedAccessException` -> 403 ProblemDetails
- `InvalidOperationException` -> 400 ProblemDetails
- generic `Exception` -> 500 ProblemDetails
- `VPPRequestController.GetMyOrders` valid user -> Ok
- `VPPRequestController.GetMyOrders` missing `UserID` claim -> Unauthorized
- `VPPRequestController.CreateOrder` valid request -> Ok
- `VPPRequestController.GetOrderById` missing order -> NotFound
- `AuthController.Login` valid credentials -> Ok + token
- `AuthController.Login` invalid credentials -> Unauthorized

## Verification

- `dotnet build gtas_vpp/gtas_vpp.slnx` -> PASS
- `dotnet test code-be/gtas_vpp_be.Tests/` -> PASS, 27/27 tests

Note: sandboxed `dotnet` initially failed due local environment restrictions and a stale MSBuild PDB lock. Verification was rerun with approved external execution and passed.
