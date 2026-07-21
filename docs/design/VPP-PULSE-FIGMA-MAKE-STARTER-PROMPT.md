# VPP Pulse — Prompt duy nhất cho Figma Make

Sử dụng nguyên prompt tiếng Việt dưới đây trong một phiên Figma Make mới với
Claude Opus 4.8 Build.

```text
Bạn là Principal Product Designer kiêm Senior React Frontend Architect, đang làm
việc trong Figma Make Build với Claude Opus 4.8 cho GTAS VPP.

Hãy đọc đầy đủ Guidelines.md, gtas_vpp_fe_react/Guidelines.md,
gtas_vpp_fe_react/AGENTS.md,
docs/design/VPP-PULSE-FIGMA-MAKE-BUILD-BRIEF.md,
docs/design/VPP-PULSE-REACT-FRONTEND-MIGRATION-PLAN.md và source hiện tại trong
gtas_vpp_fe_react/. Sau đó trực tiếp tự thiết kế và dựng bản xem trước UI/UX hoàn
chỉnh, có thể tương tác cho toàn bộ frontend React/Tailwind hiện có. Không chỉ
audit, không chỉ viết kế hoạch, không chờ tôi chọn style và không hỏi lại các câu
hỏi thẩm mỹ trước khi thiết kế.

Bạn được toàn quyền tự chọn một art direction tốt nhất: information architecture,
layout, typography, màu sắc, spacing, hình khối, component, navigation, table,
chart, data storytelling, animation và micro-interaction. Giao diện hiện tại,
screenshot, Blazor, Personal Design DNA và các sản phẩm như Apple, ChatGPT,
Notion, Linear hoặc Figma chỉ là dữ liệu tham khảo tùy chọn; không sao chép và
không xem bất kỳ style cũ nào là ràng buộc. Hãy dùng năng lực thiết kế và nghiên
cứu của bạn để tạo một hệ thống riêng, hiện đại, chuyên nghiệp, dễ hiểu và phù hợp
nhất với sản phẩm.

Bối cảnh nghiệp vụ: GTAS VPP là hệ thống nội bộ quản lý nhu cầu và mua sắm văn
phòng phẩm, gồm account/auth, My Orders, tạo/sửa/xem/lịch sử đơn, đơn bổ sung,
duyệt phòng ban/công ty, kỳ đặt hàng, quyết toán và revision, catalog/library,
master data, nhà cung cấp, bảng giá, người dùng/nhóm quyền, thông báo, báo cáo,
print/export và audit history. Hãy khảo sát router, navigation, OpenAPI types,
i18n và code của từng route để không bỏ sót capability hiện có.

Các điều bắt buộc phải giữ đúng:
- business rule, API contract, permission, audit và dữ liệu hiện hành; không tự
  phát minh field, status, quyền hoặc nghiệp vụ;
- mọi nội dung hiển thị đều có VI/EN, kể cả validation, tooltip, notification,
  empty/error state, bảng, biểu đồ và accessible name;
- Light, Dark và Print là ba mode thật;
- desktop là ưu tiên review nhưng tablet/mobile phải dùng được;
- có đầy đủ state phù hợp: loading, empty, normal, long-data, filtered-empty,
  error/retry, unauthorized/forbidden, disabled, pending, conflict, success và
  offline;
- accessibility: semantic, keyboard/focus, contrast, reduced motion và trạng
  thái không phụ thuộc riêng vào màu;
- không dùng secret, cookie, connection string, dữ liệu production hoặc mock
  runtime để che API còn thiếu;
- không sửa backend, Blazor, database, deployment hoặc business logic.

Hãy tạo một design system thống nhất và áp dụng nó xuyên suốt App Shell, auth,
dashboard/workspace, tất cả route nghiệp vụ và các modal/drawer/form/table/chart/
notification cần thiết. Mỗi route hiện có phải có ít nhất một màn hình hoàn chỉnh
để review; các luồng quan trọng phải có thêm state và interaction tiêu biểu. Hãy
dùng dữ liệu/contract hiện có để giao diện trông thật và kiểm chứng được, không
chỉ tạo vài màn hình showcase.

Hãy bắt đầu xây dựng ngay trong Figma Make và tự đưa ra các giả định hợp lý. Kết
quả cuối phải là prototype điều hướng được để tôi mở lên xem trực tiếp. Sau khi
dựng xong, chỉ cần kèm một bản tóm tắt ngắn về design direction đã chọn, các quyết
định UX lớn, giả định đã dùng và những contract thật sự còn thiếu. Không commit,
push, merge hay deploy; tôi sẽ review giao diện trước rồi mới quyết định phần code.
```
