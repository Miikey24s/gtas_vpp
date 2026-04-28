Bạn là AI Worker (Codex) trong hệ thống refactor dự án GTAS VPP.

# Bắt buộc đọc trước
- `.ai_workspace/context.md` — cấu trúc project
- `.ai_workspace/architecture.md` — chuẩn coding
- `.ai_workspace/reports/post_refactor_review_v2.md` — review lần 2

# Nhiệm vụ: Phase 6 — Final Fixes

Build verify sau mỗi task: `dotnet build gtas_vpp/gtas_vpp.slnx`

---

## P6.1 — Loại bỏ SQLController.Query endpoint (CRITICAL)

**Vấn đề**: Endpoint `api/SQL/Query` cho phép FE gửi raw SQL string xuống BE để thực thi. Dù đã parameterize, đây là lỗi thiết kế: nếu token bị lộ, attacker có thể `DROP TABLE` hoặc đọc toàn bộ DB.

**Thực hiện**:
1. Đọc `code-be/gtas_vpp_be/Controllers/SQLController.cs` — xem endpoint Query nhận gì, trả gì
2. Tìm trong TOÀN BỘ project FE (`code-fe/`) xem có chỗ nào gọi `api/SQL/Query` không:
   - Grep tìm: `SQL/Query`, `sql/query`, `SQLController`, `SqlQueryRequest`
3. Nếu FE đang gọi endpoint này:
   - Xác định mục đích (query gì? data gì?)
   - Tạo API endpoint chuyên biệt thay thế (VD: `GET api/reports/...` hoặc SP call cụ thể)
4. Nếu FE KHÔNG gọi → xóa endpoint Query khỏi SQLController
5. Đánh giá endpoint `api/SQL/{spName}` — nếu spName đến từ user input → cũng cần whitelist

---

## P6.2 — Fix Obsolete Cryptography (SYSLIB0021)

**Vấn đề**: `PasswordHelpers.cs` dùng `MD5CryptoServiceProvider` và `TripleDESCryptoServiceProvider` — deprecated.

**Thực hiện**:
1. Trong `code-be/gtas_vpp_be.Service/Helpers/PasswordHelpers.cs`:
   - Thay `new MD5CryptoServiceProvider()` → `MD5.Create()`
   - Thay `new TripleDESCryptoServiceProvider()` → `TripleDES.Create()`
2. Giữ nguyên logic encrypt/decrypt, CHỈ đổi cách khởi tạo
3. Verify build không còn warning SYSLIB0021

---

## P6.3 — Fix Nullable Warnings (CS8618)

**Vấn đề**: Entities trong Model project thiếu `required` keyword hoặc constructor → warning `CS8618: Non-nullable property must contain a non-null value`.

**Thực hiện**:
1. Quét tất cả file trong:
   - `code-be/gtas_vpp_be.Model/Auth/`
   - `code-be/gtas_vpp_be.Model/Library/`
   - `code-be/gtas_vpp_be.Model/VPP/`
   - `code-be/gtas_vpp_be.Model/Helpers/`
2. Cho mỗi entity class, fix nullable warnings bằng 1 trong 2 cách:
   - **Cách 1**: Thêm `required` keyword cho non-nullable properties (C# 11+)
   - **Cách 2**: Khởi tạo default value (`= string.Empty;`, `= default!;`)
   - **Ưu tiên Cách 2** vì entities dùng với EF Core (EF cần parameterless constructor)
3. Fix warnings trong `MapsterConfig.cs` nếu có (null reference returns)
4. Mục tiêu: `dotnet build` với **0 warnings**

---

# Sau khi hoàn thành
1. `dotnet build gtas_vpp/gtas_vpp.slnx` — PASS, 0 errors, 0 warnings
2. `dotnet test code-be/gtas_vpp_be.Tests/` — ALL PASS
3. Cập nhật `.ai_workspace/tasks.json`: thêm Phase 6 tasks với status `"done"`
4. Ghi report vào `.ai_workspace/reports/P6_final.md`

# Quy tắc
- KHÔNG thay đổi DB schema/migrations
- KHÔNG thay đổi encrypt/decrypt behavior (chỉ đổi factory method)
- API routes có thể thay đổi nếu xóa/thay SQLController.Query
