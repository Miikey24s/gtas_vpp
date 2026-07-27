# Copilot Instructions

`AGENTS.md` ở root là instruction authority. Trước khi sửa file, đọc thêm `AGENTS.md` gần file đó nhất; với UI đọc `src/Frontend/Blazor/AGENTS.md` và living plan. File này chỉ là adapter ngắn cho GitHub Copilot, không phải nguồn luật song song.

## Project Guidelines
- For claim-to-DTO mapping, user prefers manual non-generic mapping instead of generic Mapster-based ToDto.
- When deleting data, use soft delete via IsDeleted flag and avoid hard deletes.
- Test stored procedures in SQL Server Management Studio (SSMS) before or alongside debugging them in code.

## UI Design Preferences

- M0–M2 Atlas là style contract đã được owner chuẩn hóa; browser Blazor thật là visual authority cuối.
- Dùng Blazor + Radzen theo mô hình hybrid và composition `token → primitive → composite → workspace pattern → route`; không tạo component universal hoặc kế thừa markup.
- Giá trị visual lấy từ `vpp-tokens.css`; Radzen mapping nằm trong `vpp-radzen-theme.css`. Không sao chép bảng màu sang instruction này.
- UI dùng `@Loc[]`, Light/Dark, responsive và các state cần thiết. Tra Radzen MCP theo đúng component/API; nếu quota/key lỗi thì dừng phần phụ thuộc Radzen.
- Chạy `./scripts/gtas.cmd preflight -Scope frontend` trước task phức tạp và test/browser verification theo scoped `AGENTS.md`.
