# VPP Pulse — Full Masterplan Design Audit

> **Ngày:** 2026-07-18
> **Mục tiêu:** Thiết kế đầy đủ để nhìn toàn sản phẩm, nhưng vẫn tách rõ release A+ và phần có thể triển khai sau.

## Kết luận

Nên thiết kế full master plan trong Figma ngay, vì IA, role boundary và data pattern phải nhìn xuyên suốt trước khi code. Không nên triển khai full master plan cùng lúc. Figma là blueprint sản phẩm; `06-LEAN-A-PLUS-EXECUTION-PLAN.md` vẫn là cutline thực thi và AI/WOW chỉ được re-activate sau các gate tương ứng.

## Coverage đã có trong Figma

| Nhóm | Hiện trạng |
|---|---|
| Auth/account | Login, register, pending activation, recovery, password, logout/error states |
| Employee | My Orders, create request, selected tray, review/submit, detail/history/success |
| Management | Dashboard, department/all orders, approval queue, decision/detail |
| Procurement | Period, aggregate demand, supplier compare, settlement preview |
| Reports | KPI, trend/status/department/product, export, insight states |
| Library | Products, suppliers, price list/price lifecycle entry points |
| Access control | User activation, flat personas, permission matrix |
| System | Shell, notification, reconnect, 403/error, state panels |
| AI | Report insight, anomaly review, ask report, governance, safety state matrix |

## Full-plan candidates cần giữ trong blueprint

Các mục dưới đây nên có màn hình/trạng thái trong Figma trước khi code, dù có thể chưa nằm trong A+ release:

- department membership và mapping/đổi primary department;
- catalog CRUD đầy đủ: unit, active/inactive, duplicate, import/validation;
- price book lifecycle: draft, effective, expired, conflict, correction revision;
- supplier compare có exception/reason và snapshot trước settlement;
- period scheduler: upcoming/open/locked/closed/reopen denied;
- supplement diff, quota, one-pending, approve/reject/concurrency conflict;
- immutable settlement correction/revision và reconciliation mismatch;
- report export loading/limit/formula-safe/error, email sandbox/retry;
- durable notification inbox, unread/read/expired/deep-link;
- session invalidation, audit access và admin last-admin protection;
- AI insight/anomaly/ask/governance như page `11 — AI & Intelligence`;
- desktop 1440/1920 trước; tablet/mobile là contract riêng sau khi desktop direction được duyệt.

## Release labels

- **A+ / shipped evidence:** được phép dùng làm demo/release khi source/tests/gate tương ứng xanh.
- **Designed / not coded:** Figma và contract đã rõ, chờ vertical slice hoặc approval.
- **Deferred:** chỉ triển khai sau source freeze, đủ external gate và còn buffer.
- **Out of scope:** không đưa vào hướng VPP Pulse này vì làm tăng rủi ro hơn giá trị.

## Reviewer phải duyệt gì

1. IA và tên route theo persona.
2. Dữ liệu nào luôn nhìn thấy, dữ liệu nào drill-down, dữ liệu nào bị ẩn bởi permission.
3. Primary action và các mutation boundary.
4. Loading/empty/error/stale/permission/offline/reconnect/fallback cho từng route.
5. Component/token mapping và responsive contract.
6. Release label của từng feature: A+, designed, deferred hay out-of-scope.
7. Với AI: data boundary, evidence, nhãn AI/fallback, budget, retention và kill switch.

## Design-to-code rule

Figma không đảm bảo từng pixel giống browser. Figma phải khóa đúng cấu trúc, token, nội dung, state và interaction; sau đó Blazor/Radzen được chỉnh để khớp visual đã duyệt trong browser. Không sửa production chỉ để làm Figma “đúng” nếu nó làm sai nghiệp vụ hoặc accessibility.

## Liên kết

- [VPP Pulse Product Blueprint](./VPP-PULSE-PRODUCT-BLUEPRINT.md)
- [VPP Pulse AI Brief](./VPP-PULSE-AI-BRIEF.md)
- [LEAN-A-PLUS execution cutline](../planning/06-LEAN-A-PLUS-EXECUTION-PLAN.md)
