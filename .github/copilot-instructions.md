# Copilot Instructions

`AGENTS.md` ở root là instruction authority. Trước khi sửa file, đọc thêm `AGENTS.md` gần file đó nhất và path-specific instruction phù hợp. File này chỉ là adapter ngắn cho GitHub Copilot.

## Project Guidelines

- For claim-to-DTO mapping, user prefers manual non-generic mapping instead of generic Mapster-based ToDto.
- When deleting data, use soft delete via IsDeleted flag and avoid hard deletes.
- Test stored procedures in SQL Server Management Studio (SSMS) before or alongside debugging them in code.
- Use `src/Shared/` as the only shared DTO source and preserve backend authorization boundaries.
- Run the scoped `preflight` and verification commands documented by the nearest `AGENTS.md`.
- Never commit secrets, generated artifacts, browser storage or files outside the requested scope.
- Khi tổ chức lại repository, mỗi project phải tự gom file trong các thư mục phẳng của chính nó; không chuyển file giữa project hoặc gom mọi loại file vào một thư mục Services chung.
- For the Blazor shared UI organization, use the folder name SharedUI with subfolders BaseComponents, Components, and Layouts; provide concise but complete progress reports in Vietnamese.
- Khi làm việc với codebase, ưu tiên giải thích ý nghĩa tên và kiến trúc để người dùng hiểu và nhớ, không tự đổi tên nếu tên hiện tại hợp lý; chỉ đổi khi có lợi ích rõ ràng và người dùng đồng ý. Báo cáo bằng tiếng Việt, ngắn gọn nhưng đầy đủ.

## Presentation Guidelines

- Khi trình bày plan hoặc giải thích thay đổi cho repository này, ưu tiên tiếng Việt và giải thích rõ ý nghĩa các tên thư mục/thuật ngữ tiếng Anh.

## Razor/Blazor Comment Guidelines

- Trong comment Razor/Blazor, ưu tiên dạng một dòng ngắn theo nhãn section như `@* HEADER: ... *@`, `@* SIDEBAR: ... *@`, `@* NAVIGATION: ... *@`; tránh comment nhiều dòng và các đường phân cách dài để file dễ quét.
- Khi comment code UI Blazor trong module VPPRequest, ưu tiên comment theo cấu trúc giao diện thực tế như HEADER, TOOLBAR, LINE 1 → 3, COLUMNS, ROWS, FOOTER; comment ngắn, dễ scan và phản ánh đúng vùng UI.
