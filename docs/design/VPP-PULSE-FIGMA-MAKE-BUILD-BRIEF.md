# VPP Pulse — Bối cảnh cho Figma Make

> **Agent:** Claude Opus 4.8 Build trong Figma Make
>
> **Repository:** `Miikey24s/gtas_vpp`
>
> **Frontend:** `gtas_vpp_fe_react/` — React + TypeScript + Tailwind + shadcn/ui
>
> **Mục tiêu:** tự thiết kế một prototype đầy đủ để owner xem và phản hồi
>
> **Môi trường thực tế:** Figma Make workspace trống, không đọc được repository

## 1. Cách dùng tài liệu này

Đây là bối cảnh sản phẩm và các invariant không được làm sai, không phải design
spec. Figma agent được toàn quyền chọn visual direction, layout, component,
interaction, motion và cách trình bày dữ liệu. Không cần bảo toàn style của UI
hiện tại và không phải làm giống Blazor, React cũ, Personal Design DNA hoặc một
sản phẩm tham khảo nào.

Dùng đúng một prompt tự chứa trong `VPP-PULSE-FIGMA-MAKE-STARTER-PROMPT.md`.
Không yêu cầu Figma đọc file/path GitHub vì chúng không tồn tại trong Make
workspace. Prompt đã nhúng route, persona, permission, business rule, state và
mock data cần thiết. Agent phải dựng thẳng giao diện để owner review, không dừng
ở `plan.md`.

## 2. Bối cảnh sản phẩm

GTAS VPP quản lý nhu cầu và mua sắm văn phòng phẩm nội bộ từ lúc nhân viên tạo
đơn đến duyệt, bổ sung, tổng hợp, quyết toán và lưu bằng chứng lịch sử.

Các nhóm capability hiện có gồm:

- đăng nhập, đăng ký, khôi phục mật khẩu và protected session;
- App Shell, navigation, account menu, notification, VI/EN và theme;
- My Orders, tạo/sửa/xem chi tiết/lịch sử đơn và catalog;
- duyệt phòng ban/công ty, đơn bổ sung và các trạng thái xử lý;
- kỳ đặt hàng, settlement, correction và revision history;
- library/master data, phòng ban, nhà cung cấp, bảng giá và giá mặt hàng;
- người dùng, nhóm quyền và component permission;
- báo cáo, print/export, insight và immutable audit history.

Router, navigation, generated OpenAPI client và source route hiện tại là danh
sách capability cụ thể để agent tự khảo sát. Không dựa vào một screenshot cũ để
suy luận toàn hệ thống.

## 3. Thứ tự nguồn sự thật

1. Business invariant, authorization và immutable audit behavior.
2. Backend source, Swagger/OpenAPI và generated React client hiện tại.
3. Quyết định nghiệp vụ mới nhất trong React living plan.
4. Dữ liệu TEST/fixture hợp lệ và browser runtime đã được owner duyệt.
5. Figma exploration và các nguồn nghiên cứu visual.
6. Blazor chỉ làm bằng chứng capability, không phải mẫu giao diện.

Nếu thiếu contract, ghi rõ blocker; không tự tạo field, permission, status hoặc
business rule ở client.

## 4. Tiêu chí bắt buộc của prototype

- Bao phủ toàn bộ route/capability React hiện có; không chỉ dựng vài hero screen.
- Một design system và interaction language xuyên suốt toàn ứng dụng.
- Toàn bộ text UI-owned có VI/EN.
- Có Light, Dark, Print và responsive desktop/tablet/mobile.
- Có các state cần thiết như loading, empty, long-data, error/retry, forbidden,
  pending, conflict, success và offline.
- Form có validation/feedback; dữ liệu dày có search/filter/paging/action/detail
  hợp lý; action quan trọng có trạng thái pending và confirmation phù hợp.
- Semantic HTML, keyboard/focus, contrast, accessible name và reduced motion.
- Dữ liệu, permission và audit không bị làm sai để phục vụ visual.

Những mục này là tiêu chí sản phẩm và chất lượng, không quy định UI phải dùng màu,
font, bo góc, card, sidebar, tab, chart hoặc animation theo cách nào.

## 5. Ranh giới an toàn

- Không tải secret, cookie, token, connection string hoặc dữ liệu production vào
  Figma.
- Không sửa backend, Blazor, database, deployment hoặc generated API files.
- Không commit, push, merge hoặc deploy trong vòng thiết kế.
- Nếu dùng Make trên local codebase, bắt đầu từ latest `origin/Nam` trên branch
  `figma/*` riêng. Nếu chỉ có Figma Make prototype thông thường, coi output là
  design evidence để Codex tích hợp sau khi owner duyệt.

## 6. Owner sẽ review gì

- Tổng thể có đẹp, có bản sắc và phù hợp hệ thống nội bộ hay không.
- Information architecture và luồng tác vụ có dễ hiểu hơn hay không.
- App Shell, auth, data-heavy screen, CRUD, report và state có cùng một hệ thống
  hay không.
- VI/EN, Light/Dark/Print, responsive, accessibility và motion có hợp lý không.
- Dữ liệu quan trọng, ngữ cảnh, next action và detail có được trình bày rõ không.

Agent tự đưa ra câu trả lời thiết kế tốt nhất trước; owner phản hồi trực tiếp trên
prototype thay vì phải chọn style bằng mô tả trừu tượng.
