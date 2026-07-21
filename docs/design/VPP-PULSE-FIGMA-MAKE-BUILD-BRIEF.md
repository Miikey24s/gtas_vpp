# VPP Pulse — Bối cảnh cho Figma

> **Agent:** Claude Opus 4.8 Build trong Figma
>
> **Repository:** `Miikey24s/gtas_vpp`
>
> **Frontend chính:** `gtas_vpp_fe/` — Blazor Interactive Server + Radzen
>
> **Frontend phụ:** `gtas_vpp_fe_react/` — proof-of-concept đang tạm dừng
>
> **Mục tiêu:** thiết kế prototype đầy đủ để owner review trước khi Codex triển
> khai vào Blazor/Radzen

## 1. Workflow đúng

Import toàn repository, branch `Nam`, để agent đọc được Razor, CSS, resource,
route catalog, shared DTO, permission, test và living plan thật. Không chọn React
phụ làm project target chỉ vì môi trường code layer của Figma dùng React.

Figma được tự quyết visual direction, layout, component, interaction, motion và
cách trình bày dữ liệu. Nếu cần code React để dựng preview, output đó là design
evidence cô lập. Nó không được merge nguyên vẹn, không thay thế Blazor và không
biến `gtas_vpp_fe_react/` thành frontend mục tiêu.

Prompt duy nhất cho vòng import codebase nằm trong
`VPP-PULSE-FIGMA-CODEBASE-STARTER-PROMPT.md`. Chỉ dùng context attachment/fallback
khi chức năng import không khả dụng.

## 2. Bối cảnh sản phẩm

GTAS VPP quản lý nhu cầu và mua sắm văn phòng phẩm nội bộ từ lúc nhân viên tạo
đơn đến duyệt, bổ sung, tổng hợp, quyết toán và lưu bằng chứng lịch sử.

Capability hiện có gồm:

- đăng nhập, đăng ký, khôi phục mật khẩu và protected session;
- App Shell, navigation, account menu, notification, VI/EN và theme;
- My Orders, tạo/sửa/xem chi tiết/lịch sử đơn và catalog;
- duyệt phòng ban/công ty, đơn bổ sung và các trạng thái xử lý;
- kỳ đặt hàng, settlement, correction và revision history;
- library/master data, phòng ban, nhà cung cấp, bảng giá và giá mặt hàng;
- người dùng, nhóm quyền và component permission;
- báo cáo, print/export, insight và immutable audit history.

`RouteCatalog.cs`, navigation, Razor routes, API/shared DTO và tests là danh sách
capability cụ thể. Không suy luận toàn hệ thống từ một screenshot.

## 3. Thứ tự nguồn sự thật

1. Business invariant, authorization và immutable audit behavior.
2. Backend source, Swagger/API và shared DTO hiện tại.
3. Quyết định mới nhất trong Blazor living plan.
4. Razor/CSS/resource/test và dữ liệu TEST hợp lệ.
5. Browser runtime Blazor đã được owner duyệt.
6. Figma exploration, React POC và nguồn visual chỉ là evidence tham khảo.

Nếu thiếu contract, ghi blocker; không tự tạo field, permission, status hoặc
business rule trong prototype.

## 4. Tiêu chí bắt buộc

- Bao phủ route/flow/state theo từng phase, không chỉ dựng hero screen.
- Một design system và interaction language xuyên suốt ứng dụng.
- Toàn bộ text UI-owned có VI/EN.
- Light, Dark, Print và responsive desktop/tablet/mobile.
- Loading, empty, long-data, error/retry, forbidden, pending, conflict, success
  và offline khi phù hợp.
- Form có validation/feedback; màn hình dữ liệu dày có search/filter/paging,
  action/detail hợp lý; mutation có pending/confirmation.
- Semantic structure, keyboard/focus, contrast, accessible name và reduced motion.
- Thiết kế khả thi với Razor/HTML/CSS/SVG; Radzen chỉ cần giữ cho DataGrid, Dialog,
  DatePicker, validation và component phức tạp tạo giá trị thật.

## 5. Ranh giới an toàn và Git

- Không đưa secret, cookie, token, connection string hoặc production data vào
  Figma.
- Không sửa backend, database, deployment, React phụ hoặc production Blazor
  trong vòng thiết kế.
- Không commit, push, merge hoặc deploy trước owner review.
- Nếu Figma bắt buộc tạo branch/files, dùng `figma/*`; mọi output phải qua diff,
  QA và implementation Blazor riêng sau khi owner duyệt.
- Repository đã ignore build/test artifacts của .NET và React. Không thêm pattern
  `.figma*` suy đoán khi Figma không tạo file local được tài liệu hóa.

## 6. Owner review

- Tổng thể có đẹp, có bản sắc và phù hợp hệ thống nội bộ không.
- Information architecture và flow có dễ hiểu hơn không.
- App Shell, auth, data-heavy screen, CRUD, report và state có cùng một hệ thống
  không.
- VI/EN, Light/Dark/Print, responsive, accessibility và motion có hợp lý không.
- Data story có làm rõ scope, takeaway, evidence và next action không.

Agent đưa ra phương án tốt nhất trước; owner phản hồi trực tiếp trên prototype,
sau đó Codex mới triển khai vào frontend Blazor chính.
