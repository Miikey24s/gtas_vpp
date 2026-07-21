# GTAS VPP — Figma Make Guidelines

## Mục tiêu

Tự thiết kế và dựng một bản xem trước React/Tailwind hoàn chỉnh, có thể tương tác
và đủ rõ để owner đánh giá trực tiếp. Không dừng ở audit hoặc kế hoạch.

## Quyền tự chủ thiết kế

- Tự chọn một art direction tốt nhất cho GTAS VPP.
- Được thay đổi information architecture, layout, typography, màu sắc, spacing,
  hình khối, component, table, chart, navigation, animation và micro-interaction.
- UI Blazor/React hiện tại, screenshot, Personal Design DNA, Apple, ChatGPT,
  Notion, Linear, Figma hoặc bất kỳ sản phẩm nào khác chỉ là nguồn nghiên cứu
  tùy chọn, không phải style guide bắt buộc.
- Không hỏi owner chọn style trước. Hãy tự đưa ra giả định hợp lý, thiết kế trực
  tiếp và để owner phản hồi trên kết quả nhìn thấy được.
- Không cần tạo `plan.md` hoặc chờ duyệt kế hoạch trước khi dựng bản xem trước.

## Những điều không được thiết kế sai

- GTAS VPP là hệ thống nội bộ quản lý nhu cầu và mua sắm văn phòng phẩm: tài
  khoản, đơn hàng, đơn bổ sung, duyệt theo cấp, kỳ đặt hàng, quyết toán, danh
  mục/master data, nhà cung cấp, bảng giá, phân quyền, báo cáo, thông báo và lịch
  sử audit.
- Giữ đúng business rule, permission, immutable history, dữ liệu và API/OpenAPI
  hiện hành. Không tự phát minh field, status, quyền hoặc workflow nghiệp vụ.
- Mọi nội dung do UI sở hữu phải có VI/EN, gồm label, tooltip, validation,
  notification, empty/error state, chart/table summary và accessible name.
- Thiết kế phải bao quát Light, Dark, Print; desktop, tablet, mobile; loading,
  empty, normal, long-data, filtered-empty, error, retry, unauthorized,
  forbidden, disabled, pending, conflict, success và offline khi phù hợp.
- Bảo đảm semantic HTML, keyboard/focus, contrast, reduced motion và không dựa
  riêng vào màu để truyền đạt trạng thái.

## Giới hạn kỹ thuật và an toàn

- Stack hiện hành: React 19, TypeScript, Vite, Tailwind CSS 4, shadcn/ui, React
  Router, TanStack Query/Table, React Hook Form, Zod, i18next, Lucide, Recharts
  và Motion.
- Dùng API types sinh từ OpenAPI; không tạo runtime mock để che contract thiếu.
- Không sửa backend, Blazor, database, deployment hoặc generated API files.
- Không đưa secret, cookie, token, connection string hoặc dữ liệu production vào
  Figma.
- Nếu làm trên codebase thật, dùng latest `origin/Nam` và branch `figma/*` riêng;
  không merge, deploy hoặc push trực tiếp vào `Nam`.

## Kết quả cần hiển thị cho owner

- Một prototype chạy được, điều hướng được và có design system nhất quán.
- Bao phủ toàn bộ route/capability React hiện có, với các state quan trọng đủ để
  review chứ không chỉ vài màn hình hero.
- Cuối cùng ghi ngắn gọn các quyết định lớn, giả định đã dùng và contract còn
  thiếu; phần chính vẫn là giao diện trực quan để owner tự xem và góp ý.
