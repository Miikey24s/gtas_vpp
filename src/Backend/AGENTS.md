# Backend instructions

Áp dụng cho toàn bộ `src/Backend/`.

- Đọc root `AGENTS.md`, `docs/architecture/ARCH-001-MODULE-MAP.md` và API/service/model/migration liên quan trước khi sửa.
- Dùng repo skill `.agents/skills/gtas-vpp-db-safety/` khi thay đổi entity mapping, migration, stored procedure, seed/backfill hoặc data repair.
- Giữ hướng dependency: API → Application → Domain; migration project chỉ chứa persistence/migration concerns. Shared contract duy nhất nằm trong `src/Shared/`.
- Authorization phải được kiểm tra ở backend theo permission/policy; UI visibility không phải security boundary.
- Không thay đổi nghiệp vụ, API hoặc database chỉ để khớp Atlas hoặc luận văn.
- Không generic hóa mapping claim/DTO hoặc business workflow nếu làm mất tính đọc hiểu; ưu tiên mapping rõ ràng theo contract.
- Soft delete dùng `IsDeleted`; hard delete hoặc migration destructive cần approval và rollback/backup phù hợp.
- Stored procedure phải được đối chiếu trong SSMS trước hoặc song song với debug code.
- Identifier giữ tiếng Anh; comment nghiệp vụ khó đoán viết tiếng Việt ngắn gọn và không lặp lại điều code đã thể hiện rõ.
- Không log token, password, connection string, personal data hoặc secret.

Workflow tối thiểu:

```powershell
./scripts/gtas.cmd preflight -Scope backend
./scripts/gtas.cmd test-backend
```

Nếu thay đổi migration/model, chạy EF pending-model check và integration test phù hợp; không coi unit test là bằng chứng database thật.
