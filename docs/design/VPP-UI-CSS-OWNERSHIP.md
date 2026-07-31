# VPP UI CSS Ownership

> **Trạng thái:** `ACTIVE — UI-SYSTEM-001 motif consolidation`
>
> **Mục tiêu:** giúp người và AI agent biết giá trị visual phải sửa ở đâu, tránh thêm override vào file bất kỳ rồi làm cascade khó kiểm soát.

## Bản một ánh nhìn

| Lớp | File authority | Được chứa | Không được chứa |
|---|---|---|---|
| Foundation | `vpp-tokens.css` | palette thô, semantic token Light/Dark, typography, spacing, radius, shadow, motion, z-index | selector riêng của route, mapping `--rz-*` |
| Radzen bridge | `vpp-radzen-theme.css` | ánh xạ `--rz-*` sang `--vpp-*`, chrome dropdown/popup/pager/button và normalization nhỏ có bằng chứng runtime | hex/RGB/HSL, design value mới, layout/nghiệp vụ route |
| Shared UI | `vpp-layout.css`, `vpp-sidebar.css`, `vpp-tabs.css`, `vpp-datagrid.css`, `vpp-login.css` và CSS isolation của design-system component | contract shell/component dùng nhiều consumer | màu theme mới nếu token đã biểu diễn được |
| Feature/workspace | `vpp-admin.css`, `vpp-kpi.css` và `.razor.css` gần component | layout/behavior đặc thù của workspace hoặc component | override global Radzen không có scope |
| Cross-cutting | `vpp-a11y.css`, `vpp-loading.css`, `vpp-polish.css`, `vpp-responsive.css`, `vpp-toast.css`, `vpp-casing.css` | accessibility, print, loading, responsive và behavior toàn cục có owner rõ | token palette song song hoặc scrollbar chrome toàn cục |
| Retired legacy | `wwwroot/app.css` đã xóa | không nhận consumer mới; architecture gate chặn khôi phục | mọi rule runtime |
| Vendor | `wwwroot/lib/**`, Radzen base CSS | dependency đóng gói | sửa trực tiếp |

## Quy tắc thêm hoặc sửa CSS

1. Giá trị đổi theo Light/Dark hoặc dùng từ hai consumer trở lên phải vào semantic token.
2. `vpp-radzen-theme.css` chỉ tiêu thụ token VPP; source guard chặn màu literal và token chưa được định nghĩa.
3. Component/route mới ưu tiên `.razor.css`; shared CSS chỉ nhận rule có ít nhất hai consumer thật.
4. Không thêm global `::-webkit-scrollbar`/`scrollbar-color`; giữ scrollbar native, trừ vùng tab có affordance cuộn riêng.
5. `!important` chỉ dùng khi selector/cascade của third-party đã được chứng minh trên DOM runtime; ghi lý do sát rule.
6. Không tạo lại stylesheet compatibility trung tâm. Rule mới phải có owner canonical hoặc CSS isolation gần component.
7. Motion dùng token canonical trong `vpp-tokens.css`: `80ms` pressed, `120ms` hover/focus, `160–180ms` transient/navigation và `220ms` layout. Không dùng `transition: all`, forced reflow ripple hoặc duplicate `@keyframes`.
8. `vpp-polish.css` sở hữu motion project-wide; `vpp-radzen-theme.css` chỉ bridge popup/dialog portal của Radzen sang cùng token. Mọi motion phải có nhánh `prefers-reduced-motion` và không làm dịch anchor/layout.
9. Radzen dropdown, popup option và page-size pager chỉ có một CSS owner là `vpp-radzen-theme.css`; feature/polish không được lặp geometry, hover hoặc selected state.

## Phân loại debt sau F1

| Nhóm | Quyết định F1 | Wave xử lý tiếp |
|---|---|---|
| Global custom scrollbar trùng ở token/polish | Retired; dùng native scrollbar theo contract đã duyệt | F1 hoàn tất |
| `.vpp-glass` và `--vpp-shadow-glass` không có consumer | Retired; art direction không dùng glassmorphism | F1 hoàn tất |
| `--ppj-logo-*` không có consumer, trùng hai file | Retired khỏi foundation/legacy root | F1 hoàn tất |
| Alias cũ `--vpp-bg-*` | Giữ tương thích, trỏ về semantic surface mới | Migrate dần F2–F6 |
| `app.css`, adapter account/KPI/status/mobile và selector `librariestab` | Retired sau khi migrate consumer về contract canonical | Architecture gate chặn quay lại |
| Nhiều `!important` lịch sử | Không tăng trong F1; giảm khi component/route được chạm và browser gate pass | F2–F7 |

## Gate cơ học

- Architecture test kiểm tra semantic token có đủ Light/Dark.
- Bridge không chứa hex/RGB/HSL và không tham chiếu token VPP chưa định nghĩa.
- Foundation không chứa experiment đã retire hoặc custom scrollbar toàn cục.
- Project-owned CSS không có duplicate keyframe hoặc `transition: all`; JavaScript không tạo forced-reflow ripple/View Transition riêng.
- Browser test đối chiếu token VPP với biến Radzen đã resolve trên route thật ở Light/Dark.
