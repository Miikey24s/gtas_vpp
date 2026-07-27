# Phạm vi thư mục

Thư mục này không còn là frontend React. Nó chỉ giữ dependency Playwright để các
script chụp hình luận văn hiện có tiếp tục tìm thấy
`gtas_vpp_fe_react/node_modules/playwright` mà không cần sửa file trong `LVTN/`.

- Frontend chính: `../src/Frontend/Blazor/`.
- Không thêm lại source React, Vite, Docker hoặc runtime deployment tại đây.
- Bản React POC cũ được lưu ở tag `archive/react-poc-2026-07-27` và bản ZIP ngoài repository.
- Sau khi đổi dependency, chạy `npm ci` và `npm audit`.
