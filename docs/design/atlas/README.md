# GTAS VPP Design Atlas

Prototype hình ảnh cô lập phục vụ owner review trước khi triển khai vào Blazor/Radzen.

## Vị trí và quy tắc sử dụng

Bản này được đưa vào repository ngày 2026-07-26 theo quyết định D5 trong
`docs/execution/ATLAS-001.md`. Bản gốc trước đó nằm trong thư mục cache của Codex
(`~/.codex/visualizations/...`) và có thể bị dọn bất cứ lúc nào, kéo theo mất nguồn của
16 hình giao diện đang dùng trong luận văn (Hình 3-28 đến 3-43).

- Đây là **design reference đóng băng**, không phải UI Lab. Không phát triển tính năng mới ở đây.
- Chỉ sửa Atlas khi owner duyệt một thay đổi thiết kế, và phải sửa kèm route Blazor tương ứng
  cùng route ledger trong `docs/design/VPP-PULSE-BLAZOR-UI-RENOVATION-PLAN.md`.
- Khi Atlas và Blazor khác nhau: nghiệp vụ theo luận văn, bố cục theo Atlas, dữ liệu theo
  backend hiện hành. Xem thứ tự thẩm quyền đầy đủ ở mục 1 của `ATLAS-001.md`.
- Thư mục `output/` là ảnh render và báo cáo audit, **không commit**; tái tạo bằng `render-atlas.cjs`.

## Phạm vi vòng đầu

- 28 màn hình Light chuẩn ở 1920×1080 cho ảnh QA; chế độ duyệt tự khớp viewport và không yêu cầu zoom trình duyệt.
- 4 Dark representatives: M0 shell, History data workspace, Order Create form, Permission matrix.
- Trạng thái đang tải, chưa có dữ liệu và lỗi dùng các mẫu đại diện chung; không nhân thành màn hình riêng cho mọi route.
- Atlas dùng ba vai trò thống nhất với luận văn: `EMPLOYEE`, `MANAGER` và `DEV`.
  Trên giao diện, `DEV` được trình bày là **Quản trị hệ thống** và có toàn quyền
  phục vụ phát triển, kiểm thử.

## Nhóm màn hình

Các nhóm được sắp theo hành trình sử dụng và ranh giới nghiệp vụ, không theo
thứ tự menu kỹ thuật:

| Nhóm | Phạm vi |
|---|---|
| M0 | Nền tảng giao diện và trạng thái dùng chung |
| M1 | Tài khoản và phiên làm việc |
| M2 | Vòng đời đơn cá nhân |
| M3 | Tổng hợp quản lý |
| M4 | Phê duyệt đơn bổ sung, rà soát, gom nhu cầu, chọn nguồn cung và chốt kỳ |
| M5A | Loại danh mục, danh mục, mặt hàng và phòng ban |
| M5B | Nhà cung cấp, bảng giá và giá mặt hàng |
| M6 | Người dùng và phân quyền |
| M7 | Báo cáo |
| M8 | Trạng thái vận hành đại diện: thông báo, mất kết nối, thiếu quyền, lỗi, rỗng và đang tải |

`supplement-approval` là màn hình chuẩn cho hàng chờ duyệt; các trạng thái
chờ duyệt, đã duyệt và từ chối là biến thể của cùng màn hình, không nhân bản
thành các screen ID khác nhau. M0 sở hữu quy tắc hiển thị trạng thái dùng
chung; M8 chỉ là bảng kiểm tra trực quan tập trung.

## Cấu trúc

- `manifest.json`: screen inventory và review metadata.
- `atlas.css`: design tokens + shared primitives.
- `atlas.js`: screen composition từ shared primitives và deterministic fixture data.
- `render-atlas.cjs`: render PNG + contact sheet bằng Playwright.
- `output/review-manifest.json`: trạng thái DRAFT/APPROVED của từng screen/board.

## Render

```powershell
node .\render-atlas.cjs
```

## Hợp đồng điều hướng dữ liệu

| Cách điều hướng | Màn hình/vùng áp dụng |
|---|---|
| Server paging | Catalog; danh sách đơn của Lịch sử và Tổng hợp; hàng chờ duyệt; điều kiện/gom dữ liệu kỳ; Library; Người dùng; Báo cáo |
| Scroll + virtualization | Mặt hàng trong đơn; chọn mặt hàng khi tạo đơn; chi tiết đơn; tổng nhu cầu mặt hàng; đối chiếu nguồn cung; ma trận quyền |

- Search/filter/sort chạy trên toàn bộ tập dữ liệu được cấp quyền trước `Count` và `Skip/Take`.
- Một grid không được vừa có pager vừa cuộn liên tục. Màn master–detail có thể dùng paging ở danh sách record và virtualization riêng trong vùng chi tiết mặt hàng.
- `Trạng thái` ở Catalog nhân viên không hiển thị vì API chỉ trả mặt hàng đang hoạt động. Trong màn quản trị, trạng thái chỉ biểu diễn soft-delete: `Hoạt động` (`IsDeleted = false`) hoặc `Ngừng áp dụng` (`IsDeleted = true`). Trạng thái vòng đời riêng chỉ dùng ở bảng giá (`Draft/Published/Expired`).

## Duyệt tương tác

```powershell
node .\serve-atlas.cjs
```

Mở `http://127.0.0.1:4178`. Atlas cho phép lọc board và đổi đồng loạt sidebar, theme, density trước khi render lại.

- Link **Mở vừa cửa sổ** dùng layout theo viewport thật (`100% × 100dvh`); shell không tạo scrollbar cho toàn trang.
- `mode=capture` chỉ dành cho Playwright xuất ảnh cố định 1920×1080. Không dùng chế độ này để đánh giá mật độ ở trình duyệt thủ công.
- QA responsive tự chạy cho toàn bộ 28 màn hình tại 1904×914, 1536×864 và 1366×768; nếu cần cuộn, chỉ vùng nội dung bên trong được phép cuộn.

Global feedback phải sửa tại token/primitive rồi render lại toàn bộ; chỉ page-specific feedback mới sửa composition của screen tương ứng.
