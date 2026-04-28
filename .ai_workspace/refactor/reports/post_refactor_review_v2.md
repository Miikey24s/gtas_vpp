# Full Code Review (v2) - Post Refactor Phase 5

## 1. Verify Phase 5 Fixes

### P5.1 — SQL Injection
- [x] **`StoredProcedureExecutor.ExecuteQueryAsync`**: Đã sửa thành nhận mảng tham số (`params object?[] parameters`) và truyền vào `FromSqlRaw`.
- [x] **`StoredProcedureExecutor.ExecuteSPAsync`**: Hàm gọi `FromSqlRaw("exec {0} @SpType={1}, @Param={2}", ...)` đã an toàn vì cơ chế params của `FromSqlRaw` sẽ tự động parameterize các tham số này.
- [!] **`SQLController.Query`**: Đã validate và dùng `NormalizeSqlParameters` để truyền parameters. **Tuy nhiên**, việc cho phép Frontend gửi trực tiếp câu lệnh SQL (raw string) xuống Backend thông qua endpoint `api/SQL/Query` để thực thi là một **lỗi thiết kế hệ thống (Security Design Flaw) cực kỳ nghiêm trọng**, bất kể có parameterize hay không. Nếu một user có quyền gọi API này bị lộ token, hacker có thể chạy `DROP TABLE` hoặc đọc toàn bộ DB.
  
### P5.2 — CORS
- [x] Đã xóa toàn bộ `AllowAnyOrigin()`.
- [x] Đã thiết lập `WithOrigins(allowedOrigins)` đọc từ cấu hình `appsettings.json`.
- [x] `AllowCredentials()` được sử dụng đúng cách kết hợp với specific origins, không có conflict. Policy mới `AllowFrontend` đã được apply an toàn.

### P5.3 — Cookie
- [x] `options.Cookie.SecurePolicy = CookieSecurePolicy.Always;` đã được cấu hình.
- [x] `options.Cookie.HttpOnly = true;` đã được cấu hình.

### P5.4 — Commented Code
- [x] Đã xóa sạch toàn bộ các khối comment rác chứa `.Result`, `.Wait()`, `.ContinueWith()` trong các file `.razor.cs` phía Frontend.
- [x] Đã loại bỏ hoàn toàn các references tới `BusinessService`, `IBussinessService`.

### P5.5 — Tests
- [x] 100% Tests Pass. Tổng số test đã tăng từ 17 lên 27 tests và tất cả đều pass.

---

## 2. Quét lại toàn bộ project

**Warnings & Issues phát hiện thêm:**
- 🔴 **Architecture/Security**: Endpoint `api/SQL/Query` trong `SQLController` nhận `SqlQueryRequest` với nội dung Query bất kỳ. Cần loại bỏ hoàn toàn feature này và thay thế bằng các API chuyên biệt (VD: `GetUsers()`, `GetReports()`) thay vì để FE tự định nghĩa SQL query.
- 🟡 **Obsolete Cryptography**: Trong `code-be/gtas_vpp_be.Service/Helpers/PasswordHelpers.cs`, các class `MD5CryptoServiceProvider` và `TripleDESCryptoServiceProvider` đang bị cảnh báo lỗi thời (SYSLIB0021). Cần đổi sang `MD5.Create()` và `TripleDES.Create()`.
- 🟡 **Nullable Warnings**: Rất nhiều models (Entities) đang dính warning `CS8618: Non-nullable property must contain a non-null value` do thiếu constructor khởi tạo hoặc thuộc tính `required` (VD: `VPP03_Log`, `L04_VPP`, `VPP01_RequestHeader`). Cần fix để đảm bảo Nullable Reference Types (NRT) hoạt động đúng đắn và clear sạch warning khi build.
- 🟢 **Mapster Warnings**: Có một số cảnh báo `Possible null reference return` trong file `MapsterConfig.cs`.

---

## 3. Tổng kết

- Danh sách vấn đề còn lại:
  - 🔴 **Critical**: `SQLController.Query` cho phép thực thi Arbitrary SQL từ Client (Design flaw).
  - 🟡 **Medium**: Hàm Cryptography lỗi thời (`PasswordHelpers.cs`).
  - 🟢 **Low**: Các warnings về Nullable Types và Mapster khi build.
  
**Đề xuất Phase 6 tasks:**
1. **P6.1 (Security Architecture)**: Loại bỏ hoàn toàn endpoint `api/SQL/Query`. Rà soát toàn bộ project FE xem đang gọi API này ở đâu để viết API/Repository thay thế cụ thể. Đánh giá lại cả endpoint `api/SQL/{spName}` xem có rủi ro tương tự không.
2. **P6.2 (Tech Debt)**: Cập nhật `PasswordHelpers.cs` để dùng các thuật toán/hàm khởi tạo mã hóa hiện tại, xóa bỏ warning SYSLIB0021.
3. **P6.3 (Clean Code)**: Fix toàn bộ các warning `CS8618` (Nullable reference types) trên các Entities trong project Model và các cảnh báo ở Mapster.

**Đánh giá code quality:**
- **Lần 1**: 8.5/10
- **Lần 2**: **9.0/10** (Đã fix toàn bộ các lỗi code-level critical ở Phase 5. Điểm trừ duy nhất giữ lại do phát hiện lỗi thiết kế kiến trúc nghiêm trọng tại `SQLController` chưa được giải quyết tận gốc).