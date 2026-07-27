# GTAS VPP — AI Code Review Contract

Review phải tìm lỗi thật, không tạo danh sách style noise.

1. Đọc root/scoped `AGENTS.md`, mục tiêu task và diff trước khi kết luận.
2. Báo finding theo mức `P0` mất dữ liệu/bảo mật, `P1` sai nghiệp vụ/regression, `P2` maintainability/test gap có tác động cụ thể.
3. Mỗi finding phải có file/line, điều kiện tái hiện, hậu quả và lý do test hiện tại không bắt được.
4. Ưu tiên authorization, data integrity, migration/rollback, concurrency/idempotency, error handling, localization, accessibility và scope leak.
5. Với UI, build/unit pass không chứng minh browser correctness. Với database, unit test không chứng minh SQL Server behavior. Với Word, structural check không thay visual render.
6. Không yêu cầu refactor/rename/dependency ngoài scope nếu không cần cho correctness.
7. Nếu không có finding có thể hành động, nói rõ không tìm thấy finding và nêu gate/rủi ro chưa được kiểm chứng.
