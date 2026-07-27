# Copilot Instructions

`AGENTS.md` ở root là instruction authority. Trước khi sửa file, đọc thêm `AGENTS.md` gần file đó nhất và path-specific instruction phù hợp. File này chỉ là adapter ngắn cho GitHub Copilot.

## Project Guidelines

- For claim-to-DTO mapping, user prefers manual non-generic mapping instead of generic Mapster-based ToDto.
- When deleting data, use soft delete via IsDeleted flag and avoid hard deletes.
- Test stored procedures in SQL Server Management Studio (SSMS) before or alongside debugging them in code.
- Use `src/Shared/` as the only shared DTO source and preserve backend authorization boundaries.
- Run the scoped `preflight` and verification commands documented by the nearest `AGENTS.md`.
- Never commit secrets, generated artifacts, browser storage or files outside the requested scope.
