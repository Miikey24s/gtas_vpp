# GTAS VPP Frontend

Blazor Server frontend dùng Radzen Blazor và shared DTO tại
`../Shared`. Không tạo bản sao DTO trong frontend.

## Quy ước thiết kế

- Phong cách Enterprise Modern: nền phẳng, border rõ, shadow nhẹ; bo góc mặc
  định 6 px và modal 8 px.
- Màu, khoảng cách, typography, radius, shadow và transition phải lấy từ
  `wwwroot/css/vpp-tokens.css`. Không thêm bộ token hoặc màu thương hiệu mới
  ngay trong component.
- Light mode là mặc định; dark mode dùng class `.rz-theme-dark`.
- Inter dùng cho nội dung. Tiêu đề dùng `--vpp-font-display`; không khai báo
  font riêng trong từng trang.
- Chỉ dùng animation 150–200 ms và luôn tôn trọng
  `prefers-reduced-motion`; không dùng spring/bounce.
- Ba mốc responsive chuẩn: mobile dưới 768 px, tablet 768–1199 px và desktop
  từ 1200 px. Kiểm tra tối thiểu ở 390×844, 768×1024 và 1920×1080.

## Component và accessibility

- Tra MCP `radzen-blazor` trước khi sửa API/component Radzen.
- Mọi chuỗi hiển thị dùng `@Loc[]` và phải có khóa tương ứng trong cả
  `Resources/App.resx` lẫn `Resources/App.en.resx`.
- Input có label liên kết bằng `RadzenLabel Component`/`Name`; trường bắt buộc
  có validator hoặc validation tương đương.
- Nút chỉ có icon phải có tên truy cập (`aria-label`) và tooltip/title.
- Trang phải có loading, empty và error state. DataGrid cần phương án mobile
  card hoặc cuộn ngang hợp lý.
- Không gọi JavaScript qua `eval`; bổ sung hàm có tên vào
  `wwwroot/js/vpp-interactions.js`.

## Kiểm tra trước commit

```powershell
dotnet build ../../gtas_vpp.sln -c Release
dotnet test ../../tests/Frontend.UnitTests/gtas_vpp_fe.Tests.csproj -c Release
```

UI test cần URL và tài khoản test truyền qua biến môi trường như hướng dẫn ở
`../../README.md`; tuyệt đối không lưu credential hoặc browser storage vào Git.
