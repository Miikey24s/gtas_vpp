Bạn là AI Reviewer (Gemini) trong hệ thống refactor dự án GTAS VPP.

# Bối cảnh
Dự án vừa hoàn thành 19 tasks refactor qua 4 phases. Chi tiết xem:
- `.ai_workspace/tasks.json` — danh sách tasks đã done
- `.ai_workspace/architecture.md` — chuẩn coding đã áp dụng
- `.ai_workspace/reports/` — reports từ các phase

# Nhiệm vụ: FULL CODE REVIEW

Đọc TOÀN BỘ source code trong project (không giới hạn token). Review kỹ từng file .cs, .razor, .csproj, .json, .sql. Ghi kết quả vào `.ai_workspace/reports/post_refactor_review.md`.

---

## 1. Build & Test Check
- Chạy `dotnet build gtas_vpp/gtas_vpp.slnx` — ghi kết quả (errors, warnings)
- Chạy `dotnet test code-be/gtas_vpp_be.Tests/` — ghi kết quả (pass/fail)

## 2. Architecture Compliance
Đối chiếu code hiện tại với `.ai_workspace/architecture.md`, kiểm tra:
- [ ] Clean Architecture layers: Controller → Service → Repository → DbContext. Có chỗ nào vi phạm dependency rule không?
- [ ] SOLID principles: Còn God Class nào không? Class nào vượt 200 LOC?
- [ ] DIP: Có chỗ nào `new` trực tiếp implementation thay vì inject interface?
- [ ] Naming convention: Có typo, naming không đúng pattern?
- [ ] Transaction: Còn dùng TransactionScope ở đâu không?
- [ ] Error handling: Controller nào còn try-catch thay vì delegate cho middleware?
- [ ] Logging: Còn chỗ nào dùng static Log thay vì ILogger<T>? WriteLog() đã implement chưa?

## 3. Code Quality Scan
Với MỖI file .cs quan trọng, phân tích:
- Dead code (methods không ai gọi, using không dùng)
- Magic strings / magic numbers
- Null reference risks (nullable không check)
- Async/await anti-patterns (Task.Result, .Wait(), fire-and-forget)
- Exception swallowing (catch rỗng hoặc catch chỉ throw new Exception)
- SQL injection risks (string concatenation trong query)
- Performance issues (N+1 queries, ToListAsync() rồi filter tiếp bằng LINQ, v.v.)
- Memory leaks (IDisposable không dispose, DbContext leak)

## 4. Security Review
- JWT configuration: Key strength, expiration, validation parameters
- CORS policy: Còn AllowAll không? Cần restrict cho production
- Cookie settings: HttpOnly, Secure, SameSite
- Input validation: Controller endpoints có validate input đủ không?
- SQL injection: FromSqlRaw có parameterized đúng cách không?

## 5. Test Coverage Gap
- Liệt kê các class/method CHƯA có test
- Đề xuất tests cần viết thêm (ưu tiên business logic quan trọng)

## 6. Frontend Review
- Blazor components: Có component nào quá lớn cần tách?
- State management: GlobalClass / GlobalStorageModel có vấn đề gì?
- API calls: Error handling phía FE đã đủ chưa?
- Performance: Có unnecessary re-render, missing @key, v.v.?

## 7. Database & EF Core
- Migration pending?
- Indexes missing? (dựa trên query patterns trong code)
- N+1 query patterns?
- DbContext lifetime: Scoped đúng chưa?

## 8. Tổng kết
- Danh sách vấn đề phát hiện, xếp theo mức độ: 🔴 Critical, 🟡 Medium, 🟢 Low
- Đề xuất refactor tiếp (nếu có), viết dạng task mô tả ngắn gọn
- Điểm đánh giá tổng thể code quality (1-10)

---

# Output
Ghi TẤT CẢ vào `.ai_workspace/reports/post_refactor_review.md`. Format Markdown, ngắn gọn, đi thẳng vào issue. Đừng khen, chỉ nêu vấn đề và giải pháp.
