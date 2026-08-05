# Đề cương học source và bảo vệ GTAS VPP

Tài liệu này giúp sinh viên đọc source theo luồng, trình bày dự án và luyện trả lời phản biện. Đây là
tài liệu học, không thay thế luận văn hoặc source code hiện hành.

Nguồn đối chiếu chính:

- `LVTN/NguyenAnNam_DH52201078.docx`: nghiệp vụ và nội dung luận văn;
- `docs/CODE-READING-GUIDE.md`: bản đồ route, component, API và service;
- `docs/architecture/ARCH-001-MODULE-MAP.md`: kiến trúc và dependency;
- `docs/execution/FRONTEND-REFACTOR-001.md`: trạng thái refactor frontend;
- `docs/execution/BACKEND-REFACTOR-001.md`: trạng thái refactor backend;
- source và test hiện tại: bằng chứng kỹ thuật cuối cùng.

> Trước ngày bảo vệ, phải chạy lại các lệnh kiểm tra và lấy số liệu hiện tại. Không học thuộc số lượng
> test hoặc trạng thái plan cũ như một sự thật cố định.

---

## 0. Bản một ánh nhìn

| Nội dung | Cần nắm |
|---|---|
| Bài toán | Quản lý yêu cầu văn phòng phẩm nội bộ theo kỳ, có đơn thường, đơn bổ sung, bảng giá, chốt kỳ, báo cáo và phân quyền |
| Kiến trúc | `modular monolith` — ứng dụng nguyên khối nhưng chia module rõ; frontend Blazor, backend Web API, SQL Server |
| Luồng code | Browser → Razor component → typed feature client → Controller → Service → EF Core → SQL Server |
| Luồng nghiệp vụ nên thuộc | Đăng nhập, tạo đơn, đơn bổ sung, chốt kỳ, hiệu chỉnh kết quả, phân quyền |
| Điểm kỹ thuật nổi bật | transaction, unique constraint, rowversion, idempotency key, input hash, immutable snapshot, policy authorization |
| Kiểm thử | unit test, integration test, architecture test và Playwright browser test |
| Điều phải nói trung thực | AI hỗ trợ sinh/refactor code; sinh viên chịu trách nhiệm yêu cầu, kiểm chứng, giải thích và kết quả cuối |
| Trạng thái refactor | Frontend đã hoàn thành và được duyệt; backend đã hoàn thành B0R/B1, B2 mới audit và đang chờ triển khai production slice |

### Từ vựng nền tảng

- `end-to-end`: từ đầu đến cuối một luồng;
- `typed client`: lớp gọi API có method và kiểu dữ liệu rõ, không truyền URL/string tùy ý;
- `invariant`: điều kiện luôn phải đúng trong nghiệp vụ;
- `authorization`: kiểm tra người dùng có quyền làm hành động hay không;
- `persistence`: lớp lưu và đọc dữ liệu;
- `snapshot`: bản chụp dữ liệu tại một thời điểm, không đổi theo dữ liệu mới về sau.

---

## 1. Sau khi đã hiểu `project` và `src`, nên học gì tiếp?

Không cần đọc lần lượt hàng trăm file. Học theo bảy buổi, mỗi buổi phải trả lời được một câu hỏi lớn.

### Buổi 1 — Hợp đồng dữ liệu dùng chung

Đọc:

- `src/Shared/DTOs/Req/`: dữ liệu frontend gửi lên;
- `src/Shared/DTOs/Res/`: dữ liệu backend trả về;
- `src/Shared/Constants/Permissions.cs`;
- `src/Shared/Constants/CanonicalRbac.cs`.

Cần trả lời:

1. DTO khác entity như thế nào?
2. Vì sao frontend không được dùng trực tiếp entity database?
3. Ba vai trò `EMPLOYEE`, `MANAGER`, `DEV` có quyền gì?

`DTO — Data Transfer Object` là đối tượng truyền dữ liệu qua API. `Entity` là đối tượng được EF Core
ánh xạ vào bảng hoặc dữ liệu lưu trữ. Tách hai loại này giúp API không làm lộ toàn bộ cấu trúc database.

### Buổi 2 — Lần một màn hình frontend

Chọn màn **Đơn hàng của tôi** và lần theo:

```text
/dashboard?tab=0
  → Component_VPPRequest
  → Tab_Orders
  → VppOrderWorkspacePanel
  → RequestsQueryClient / RequestsCommandClient
  → DTO trong src/Shared
```

Cần trả lời:

1. File `.razor` nào vẽ giao diện?
2. File `.razor.cs` nào giữ state và xử lý sự kiện?
3. API client nào tải dữ liệu hoặc gửi lệnh?
4. Backend trả capability nào để bật/tắt nút?

`capability` là khả năng hiện tại do backend xác nhận, ví dụ `CanEdit`, `CanCancel`,
`CanCreateAdditional`.

### Buổi 3 — Lần một API backend

Chọn luồng tạo đơn:

```text
POST /api/VPPRequest/orders
  → VPPRequestController
  → IVPPRequestService / VPPRequestService
  → policy + validation + transaction
  → IUnitOfWork / EF Core
  → SQL Server
```

Cần phân biệt:

- Controller: nhận HTTP request, kiểm quyền, binding và trả HTTP response;
- Service: chứa use case và luật nghiệp vụ;
- Domain/policy: điều kiện nghiệp vụ có thể kiểm tra độc lập;
- Persistence: đọc/ghi database.

### Buổi 4 — Đơn bổ sung và chốt kỳ

Đây là hai luồng dễ được giảng viên hỏi nhất.

Đọc theo thứ tự:

1. `VPPRequestService`: điều kiện tạo, duyệt và từ chối đơn bổ sung;
2. `PeriodCalculator` và period policy: thời hạn của kỳ;
3. `PeriodSettlementService`: preview, confirm và correction;
4. test vòng đời đơn và settlement.

Phải giải thích được:

- vì sao đơn bổ sung bắt buộc có lý do nhưng không bắt buộc có đơn thường;
- vì sao base request chỉ là metadata truy vết khi có;
- vì sao không cho tồn tại nhiều đơn bổ sung `Pending` cùng lúc;
- vì sao preview chưa phải chốt kỳ;
- vì sao correction tạo revision mới thay vì sửa bản cũ.

### Buổi 5 — Phân quyền và bảo mật

Đọc:

- authentication/authorization trong backend API;
- `PermissionState` và `CurrentUserState` ở frontend;
- policy trên controller;
- `CanonicalRbac`.

Phải thuộc câu này:

> Ẩn nút ở frontend chỉ cải thiện trải nghiệm. Quyền thật luôn được backend kiểm tra bằng policy và
> phạm vi dữ liệu của phiên đăng nhập.

### Buổi 6 — Kiểm thử

Đọc mỗi loại một test đại diện:

- unit test nghiệp vụ đơn;
- test settlement;
- architecture test dependency/route/contract;
- integration test LocalDB;
- Playwright UI test.

Cần giải thích:

- unit test kiểm một class/rule nhỏ;
- integration test kiểm nhiều lớp cùng database hoặc infrastructure;
- E2E/browser test kiểm hành vi người dùng trên hệ thống chạy thật;
- build pass không chứng minh giao diện đẹp hoặc đúng responsive.

### Buổi 7 — Chạy và triển khai

Đọc:

- `src/Hosting/AppHost/`: Aspire chạy local;
- `deploy/README.md`: production;
- `docker-compose.yml` và `deploy/nginx/`;
- `.github/workflows/`: CI/CD.

Phân biệt:

- `Aspire`: điều phối service khi phát triển và test local;
- `Docker Compose`: đóng gói/chạy các container;
- `Nginx reverse proxy`: nhận request bên ngoài rồi chuyển tới frontend/backend;
- `CI/CD`: tự động build, test và triển khai theo pipeline.

---

## 2. Cách giới thiệu dự án

### Bản 30 giây

> GTAS VPP là hệ thống web nội bộ quản lý yêu cầu văn phòng phẩm theo kỳ. Nhân viên tạo và theo dõi
> đơn; quản lý duyệt đơn bổ sung, quản lý bảng giá và chốt kỳ; quản trị viên quản lý tài khoản và
> quyền. Hệ thống được xây dựng bằng .NET 10, Blazor Interactive Server, ASP.NET Core Web API,
> Entity Framework Core và SQL Server, đồng thời có cơ chế lưu phiên bản, chống ghi trùng, phân quyền
> phía server và kiểm thử tự động.

### Bản 60 giây

> Vấn đề của quy trình thủ công bằng bảng tính hoặc email là khó kiểm soát thời hạn, đơn bổ sung,
> phiên bản, quyền truy cập và giá tại thời điểm chốt kỳ. GTAS VPP giải quyết bằng một quy trình tập
> trung theo kỳ. Mỗi người có tối đa một đơn thường hiện hành; đơn bổ sung có thể độc lập nhưng phải
> có lý do và được duyệt; bảng
> giá có vòng đời; kết quả chốt kỳ được lưu thành snapshot bất biến. Kiến trúc là modular monolith:
> frontend Blazor gọi backend Web API qua DTO dùng chung, backend kiểm policy và nghiệp vụ trước khi
> lưu SQL Server. Hệ thống có unit, integration, architecture và browser test để bảo vệ các luồng
> quan trọng.

---

## 3. Đề cương trình bày khoảng 12 phút

### 0:00–1:00 — Bài toán và lý do chọn đề tài

Nói:

> Doanh nghiệp cần tổng hợp nhu cầu văn phòng phẩm từ nhiều nhân viên và phòng ban. Nếu dùng email
> hoặc bảng tính, dữ liệu dễ trùng, khó theo dõi ai sửa, khó khóa thời hạn và khó biết giá nào đã được
> dùng khi chốt. Vì vậy em xây dựng hệ thống quản lý yêu cầu theo kỳ, có lịch sử và phân quyền.

### 1:00–2:00 — Mục tiêu và phạm vi

Nói:

> Hệ thống tập trung vào tạo đơn, đơn bổ sung, tổng hợp, bảng giá, chốt kỳ, báo cáo, thông báo và phân
> quyền. Hệ thống không phải website bán hàng, không thanh toán điện tử, không quản lý tồn kho, không
> phát hành đơn mua hàng và chưa tích hợp chính thức với hệ thống nhân sự.

### 2:00–3:00 — Tác nhân

- `EMPLOYEE`: tạo và theo dõi đơn cá nhân;
- `MANAGER`: có chức năng nhân viên và thêm quyền quản lý/duyệt/vận hành kỳ;
- `DEV`: quản trị tài khoản và quyền, nhưng vẫn chịu ràng buộc nghiệp vụ.

Không nói “DEV có thể bỏ qua mọi luật”. Toàn quyền thao tác không có nghĩa được phép phá invariant.

### 3:00–5:00 — Kiến trúc

Nói theo sơ đồ:

```text
Browser
  → Blazor Interactive Server
  → ASP.NET Core Web API
  → Application Service
  → EF Core
  → SQL Server
```

Giải thích:

> Em chọn modular monolith vì quy mô luận văn chưa cần microservices. Các module vẫn tách ownership
> rõ nhưng deploy đơn giản, transaction dễ kiểm soát và phù hợp nguồn lực một sinh viên.

### 5:00–7:00 — Hai luồng nghiệp vụ nổi bật

#### Luồng đơn bổ sung

> Người dùng còn thời hạn có thể tạo đơn bổ sung dù chưa có đơn thường, nhưng phải nhập lý do, còn
> quota/lượt thử và không có đơn bổ sung đang chờ. Nếu đã có đơn thường hợp lệ, hệ thống lưu liên kết
> base để truy vết; nếu chưa có thì đơn bổ sung vẫn hợp lệ. Đơn được tạo ở trạng thái Pending. Người
> có quyền và khác người tạo mới được duyệt hoặc từ chối. Mọi quyết định được lưu vào lịch sử và phát
> thông báo.

#### Luồng chốt kỳ

> Quản lý chọn nguồn cung và bảng giá, tải preview, xử lý blocker rồi confirm. Backend tính lại input
> hash để chặn preview cũ, dùng idempotency key để chống tạo trùng và lưu immutable snapshot. Nếu cần
> sửa, hệ thống tạo correction revision mới theo nguyên tắc bốn mắt.

### 7:00–8:30 — Dữ liệu và tính toàn vẹn

Nêu bốn cơ chế:

1. unique constraint chặn dữ liệu trùng;
2. transaction bảo đảm ghi đồng bộ;
3. rowversion phát hiện hai người cùng sửa;
4. soft delete và revision giữ lịch sử.

### 8:30–9:30 — Bảo mật

> Phiên trình duyệt dùng cookie, frontend gọi backend bằng thông tin xác thực phù hợp và backend kiểm
> policy ở API. Phạm vi cá nhân/phòng ban/công ty được suy ra từ phiên đăng nhập, không tin mã phòng
> ban do frontend tự gửi. Secret không được lưu trong source.

### 9:30–10:30 — Kiểm thử và triển khai

> Hệ thống có unit test cho policy/service, integration test với database, architecture test khóa
> dependency và wire contract, cùng Playwright cho route thật. Aspire dùng cho local; production dùng
> container, Nginx và quy trình CI/CD. Trước khi bảo vệ em sẽ chạy lại verify và ghi đúng số test hiện
> tại vào slide.

### 10:30–11:30 — Kết quả, hạn chế và hướng phát triển

> Hệ thống đã hoàn thành các luồng chính trong phạm vi luận văn. Hạn chế hiện tại là chưa có dữ liệu
> vận hành dài hạn, chưa tích hợp HR/email production đầy đủ và chưa kiểm thử tải ở quy mô lớn. Hướng
> phát triển là load test, giám sát vận hành, báo cáo ngân sách/cảnh báo bất thường và quy trình backup
>–restore tự động có kiểm chứng.

### 11:30–12:00 — Chuyển sang demo

> Em xin minh họa ngắn ba điểm: nhân viên theo dõi đơn, quản lý xử lý đơn bổ sung và hệ thống lưu kết
> quả chốt kỳ/báo cáo theo đúng phạm vi quyền.

---

## 4. Kịch bản demo an toàn

Không tạo dữ liệu ngẫu nhiên ngay trên môi trường trình bày nếu không cần. Chuẩn bị trước dữ liệu có
đủ trạng thái và ảnh dự phòng.

1. Đăng nhập bằng tài khoản Nhân viên.
2. Mở **Đơn hàng của tôi**: chỉ ra kỳ, đơn thường, đơn bổ sung và kỳ trước.
3. Mở **Lịch sử đơn**: lọc, chọn đơn, xem chi tiết và revision.
4. Đổi sang Quản lý: mở hàng chờ đơn bổ sung, giải thích duyệt/từ chối và nguyên tắc khác người tạo.
5. Mở vận hành kỳ: chỉ ra preview, blocker, bảng giá, nguồn cung và confirm.
6. Mở Báo cáo: chỉ ra scope và dữ liệu snapshot kỳ đã chốt.
7. Nếu mạng/app lỗi, chuyển ngay sang ảnh chụp đã chuẩn bị và tiếp tục giải thích luồng.

Không demo mutation nguy hiểm trên dữ liệu thật. Nếu cần demo tạo/duyệt/chốt, dùng database TEST cô lập.

---

## 5. Bộ câu hỏi phản biện kèm câu trả lời mẫu

### Nhóm A — Tổng quan và phạm vi

#### Câu 1. Hệ thống của em giải quyết vấn đề gì?

**Trả lời mẫu:** Hệ thống tập trung hóa quy trình yêu cầu văn phòng phẩm theo kỳ, thay cho việc tổng
hợp rời rạc bằng email hoặc bảng tính. Nó kiểm soát thời hạn, đơn thường, đơn bổ sung, bảng giá, chốt
kỳ, phạm vi dữ liệu và lịch sử thay đổi.

#### Câu 2. Đối tượng sử dụng là ai?

**Trả lời mẫu:** Có ba nhóm chính: Nhân viên, Quản lý và Quản trị hệ thống. Nhân viên làm việc với đơn
cá nhân; Quản lý xử lý dữ liệu phòng ban/vận hành kỳ; DEV quản lý tài khoản và quyền.

#### Câu 3. Vì sao hệ thống tổ chức theo kỳ?

**Trả lời mẫu:** Nhu cầu được tổng hợp theo tháng và có hạn gửi cụ thể. Thực thể `Period` giúp khóa
đúng cửa sổ thao tác, xác định trạng thái quy trình và tạo một mốc nhất quán cho đơn, bảng giá, chốt kỳ
và báo cáo.

#### Câu 4. Phạm vi nào không được thực hiện?

**Trả lời mẫu:** Không có bán hàng công khai, thanh toán, tồn kho, phát hành purchase order và tích hợp
HR chính thức. Đây là giới hạn có chủ đích để tập trung vào quy trình yêu cầu và kiểm soát dữ liệu.

#### Câu 5. Điểm khác biệt chính so với bảng tính là gì?

**Trả lời mẫu:** Hệ thống có workflow, policy quyền phía server, ràng buộc database, version history,
snapshot giá, notification và audit. Bảng tính có thể cộng tác nhưng khó đảm bảo các invariant này một
cách nhất quán.

#### Câu 6. Kết quả quan trọng nhất của đề tài là gì?

**Trả lời mẫu:** Em đã xây dựng được một luồng khép kín từ yêu cầu cá nhân đến duyệt bổ sung, lựa chọn
giá, chốt kỳ và báo cáo; đồng thời giữ được khả năng truy vết và kiểm soát quyền.

### Nhóm B — Kiến trúc và công nghệ

#### Câu 7. Kiến trúc của hệ thống là gì?

**Trả lời mẫu:** Đây là modular monolith. Các module Identity, Catalog/Pricing, Requests, Settlement,
Reports, Notifications và Platform có ownership rõ nhưng vẫn nằm trong một hệ thống triển khai thống
nhất.

#### Câu 8. Vì sao không chọn microservices?

**Trả lời mẫu:** Quy mô hiện tại chưa đủ lợi ích để bù chi phí vận hành phân tán. Modular monolith giúp
transaction và debug đơn giản hơn, giảm deployment complexity nhưng vẫn tạo ranh giới module để có thể
tách sau nếu có nhu cầu thật.

`deployment complexity` là độ phức tạp khi đóng gói, cấu hình, giám sát và triển khai hệ thống.

#### Câu 9. Vì sao chọn Blazor Interactive Server?

**Trả lời mẫu:** Dự án dùng chung hệ sinh thái .NET và DTO C#, phù hợp ứng dụng nội bộ cần giao diện
tương tác mạnh. Interactive Server giảm việc viết hai hệ type khác nhau và cho phép tổ chức component
nhanh. Đổi lại, hệ thống phụ thuộc kết nối SignalR nên phải xử lý reconnect và scale connection.

#### Câu 10. Vai trò của Radzen là gì?

**Trả lời mẫu:** Radzen cung cấp DataGrid, dialog, form control và các component Blazor. Dự án không để
mỗi màn tự thiết kế lại; component Radzen được đặt trong design system và token chung để giữ giao diện,
responsive và accessibility nhất quán.

#### Câu 11. Tại sao có project `Shared`?

**Trả lời mẫu:** Shared chứa wire contract: DTO, enum và constant thực sự cần ở cả frontend/backend.
Nó không chứa EF entity, Radzen state hoặc logic trình bày. Nhờ vậy hai phía dùng cùng shape API nhưng
không trộn database với UI.

#### Câu 12. Controller và Service khác nhau thế nào?

**Trả lời mẫu:** Controller sở hữu HTTP: route, authorization, binding và status code. Service sở hữu
use case và business rule. Nếu đưa nghiệp vụ vào controller, code khó test và dễ tạo nhiều đường xử lý
không nhất quán.

#### Câu 13. Luồng một request chạy như thế nào?

**Trả lời mẫu:** Người dùng thao tác trong Razor component; typed client gọi Web API bằng DTO; controller
kiểm policy và gọi service; service validation và thao tác qua EF Core/UnitOfWork; kết quả được trả lại
thành response DTO để frontend render.

#### Câu 14. Aspire và Docker Compose khác nhau thế nào?

**Trả lời mẫu:** Aspire chủ yếu điều phối backend/frontend và dependency trong môi trường local/test.
Docker Compose đóng gói và chạy container, phù hợp deployment. Hai công cụ giải quyết hai bối cảnh khác
nhau, không phải hai kiến trúc nghiệp vụ.

#### Câu 15. Vì sao không tạo generic repository hoặc CRUD engine cho mọi module?

**Trả lời mẫu:** Các module có invariant và workflow khác nhau. Abstraction quá tổng quát có thể che
business rule, làm code khó debug và khó trình bày. Dự án chỉ tái sử dụng phần thật sự giống nhau như
transport, data surface hoặc primitive UI.

### Nhóm C — Nghiệp vụ, database và tính nhất quán

#### Câu 16. Làm sao chặn một người tạo hai đơn thường trong cùng kỳ?

**Trả lời mẫu:** Backend kiểm tra policy trước khi tạo, nhưng lớp bảo vệ cuối là unique constraint có
điều kiện ở database cho đơn thường hiện hành theo người dùng và kỳ. Vì vậy hai request đồng thời vẫn
không tạo được hai bản hợp lệ.

#### Câu 17. Điều kiện tạo đơn bổ sung là gì?

**Trả lời mẫu:** Không bắt buộc có đơn thường. Người dùng phải còn cửa sổ nghiệp vụ, nhập lý do hợp
lệ, còn quota/lượt thử và không có đơn bổ sung Pending trong cùng kỳ. Nếu có đơn thường hợp lệ thì
backend gắn base để truy vết; frontend chỉ hiển thị capability, backend mới là nơi quyết định cuối.

#### Câu 18. Vì sao không cho có nhiều đơn bổ sung chờ duyệt?

**Trả lời mẫu:** Nếu nhiều đơn Pending tồn tại cùng lúc, quota và thứ tự quyết định có thể xung đột.
Hệ thống yêu cầu xử lý đơn hiện tại trước, đồng thời có unique constraint để chống race condition.

`race condition` là lỗi do hai thao tác đồng thời đọc cùng trạng thái rồi cùng ghi kết quả không hợp lệ.

#### Câu 19. Tối đa ba đơn bổ sung có nghĩa là gì?

**Trả lời mẫu:** Chính sách nghiệp vụ giới hạn số đơn bổ sung được duyệt cho một người/kỳ, áp dụng
chung cho cả đơn bổ sung độc lập và có liên kết base. Hệ thống
còn có giới hạn lượt thử nội bộ để chống gửi lặp; hai khái niệm approved quota và attempt limit không
hoàn toàn giống nhau.

#### Câu 20. `RowVersion` dùng để làm gì?

**Trả lời mẫu:** RowVersion là optimistic concurrency token. Khi hai người cùng mở một bản ghi, người
ghi sau bằng version cũ sẽ bị từ chối thay vì âm thầm ghi đè thay đổi của người trước.

`optimistic concurrency` là chiến lược cho phép cùng đọc nhưng kiểm tra xung đột tại thời điểm ghi.

#### Câu 21. Transaction giải quyết vấn đề gì?

**Trả lời mẫu:** Một use case như tạo đơn phải ghi header, detail và log cùng nhau. Transaction bảo đảm
hoặc tất cả thành công, hoặc tất cả rollback; không để lại đơn thiếu chi tiết hay thiếu lịch sử.

#### Câu 22. Vì sao dùng soft delete?

**Trả lời mẫu:** Dữ liệu danh mục và nghiệp vụ đã từng được tham chiếu phải còn để báo cáo và audit.
Soft delete dùng `IsDeleted` để ngừng áp dụng mà không xóa vật lý lịch sử.

#### Câu 23. Bảng giá có trạng thái gì?

**Trả lời mẫu:** Vòng đời chính là Draft, Published và Expired. Draft còn chỉnh sửa; Published phải
được bảo vệ để không làm sai dữ liệu dùng trong nghiệp vụ; Expired đánh dấu hết hiệu lực nhưng vẫn giữ
lịch sử.

#### Câu 24. Nếu giá thay đổi sau khi chốt kỳ thì sao?

**Trả lời mẫu:** Kết quả chốt kỳ lưu snapshot giá và phân bổ tại thời điểm xác nhận. Báo cáo lịch sử
đọc snapshot đó, không tính lại theo bảng giá mới.

#### Câu 25. Preview khác Confirm thế nào?

**Trả lời mẫu:** Preview chỉ tính và trả dữ liệu để người dùng rà soát, chưa tạo kết quả chốt. Confirm
kiểm tra lại dữ liệu, tạo settlement revision bất biến và chuyển trạng thái kỳ khi mọi điều kiện đạt.

#### Câu 26. `InputHash` có tác dụng gì?

**Trả lời mẫu:** InputHash đại diện cho dữ liệu đầu vào đã preview. Khi confirm, server tính lại; nếu
hash khác nghĩa là đơn hoặc giá đã thay đổi, nên chặn việc xác nhận một preview cũ.

#### Câu 27. `Idempotency key` là gì?

**Trả lời mẫu:** Đây là khóa chống lặp lệnh. Nếu người dùng double-click hoặc client retry cùng một
lệnh, backend nhận ra key cũ và trả cùng kết quả thay vì tạo thêm đơn hoặc settlement.

#### Câu 28. Vì sao correction phải theo nguyên tắc bốn mắt?

**Trả lời mẫu:** Người tạo revision hiện hành không được tự xác nhận correction của chính mình. Người
thứ hai độc lập giúp giảm rủi ro tự sửa và tự duyệt dữ liệu tài chính.

#### Câu 29. Vì sao không sửa trực tiếp settlement cũ?

**Trả lời mẫu:** Ghi đè sẽ làm mất bằng chứng đã dùng để ra quyết định. Correction tạo revision mới,
giữ snapshot cũ nguyên vẹn và đánh dấu revision mới là hiện hành.

#### Câu 30. Báo cáo giới hạn dữ liệu như thế nào?

**Trả lời mẫu:** Backend suy ra scope cá nhân, phòng ban hoặc công ty từ phiên đăng nhập và permission.
Frontend không thể tự truyền một department code khác để mở rộng phạm vi.

### Nhóm D — Xác thực, phân quyền và bảo mật

#### Câu 31. Hệ thống đăng nhập như thế nào?

**Trả lời mẫu:** ASP.NET Core Identity kiểm tra password hash và trạng thái tài khoản. Trình duyệt duy
trì phiên bằng cookie; frontend lấy thông tin cần thiết để gọi backend; backend vẫn xác thực và kiểm
policy tại từng API quan trọng.

#### Câu 32. Ẩn nút có phải là bảo mật không?

**Trả lời mẫu:** Không. Ẩn nút chỉ tránh gây nhầm cho người dùng. Một người vẫn có thể tự gửi HTTP
request, nên backend phải kiểm authorization và business rule độc lập.

#### Câu 33. Làm sao chống người dùng xem dữ liệu phòng ban khác?

**Trả lời mẫu:** Backend lấy user, company, department và permission từ identity/session hiện hành.
Query luôn áp scope trước khi filter và paging; không tin scope do client tự cung cấp.

#### Câu 34. DEV có được bỏ qua quy trình nghiệp vụ không?

**Trả lời mẫu:** Không. DEV có quyền quản trị rộng nhưng vẫn phải tuân điều kiện như kỳ, trạng thái,
rowversion và four-eyes. Authorization rộng không làm business invariant biến mất.

#### Câu 35. Khi quyền thay đổi thì phiên đang mở xử lý thế nào?

**Trả lời mẫu:** Hệ thống có snapshot quyền và cơ chế refresh/realtime signal. Frontend cập nhật khả
năng hiển thị, còn backend áp policy hiện hành nên quyền cũ không được dùng để vượt API.

#### Câu 36. Secret được quản lý ở đâu?

**Trả lời mẫu:** Secret được đặt trong user secrets, environment variables hoặc GitHub/production
secret store. Source và file cấu hình commit chỉ giữ tên biến hoặc placeholder an toàn.

### Nhóm E — Kiểm thử, lỗi và triển khai

#### Câu 37. Dự án có những loại test nào?

**Trả lời mẫu:** Unit test kiểm rule/service; integration test kiểm database/infrastructure; architecture
test khóa dependency, route và wire contract; Playwright kiểm route và hành vi trình duyệt thật.

#### Câu 38. Tại sao unit test pass vẫn chưa đủ?

**Trả lời mẫu:** Unit test có thể bỏ sót mapping, DI, database constraint, CSS, responsive và browser
interaction. Vì vậy cần nhiều tầng test và kiểm tra route thật.

#### Câu 39. Kiểm thử database được làm an toàn thế nào?

**Trả lời mẫu:** Test SQL dùng database QA/LocalDB có thể tạo lại, không dùng database thật. Migration,
pending model và constraint được kiểm tra trong môi trường disposable trước khi kết luận pass.

#### Câu 40. Nếu API lỗi thì giao diện xử lý ra sao?

**Trả lời mẫu:** Lỗi API được map thành thông báo an toàn và state có hành động tiếp theo như retry.
Không hiển thị raw exception hoặc stack trace cho người dùng cuối.

#### Câu 41. Vì sao phải kiểm tra responsive bằng browser thật?

**Trả lời mẫu:** Build chỉ kiểm code compile. CSS có thể crop, overflow hoặc sai interaction dù build
pass. Browser test và screenshot ở desktop/tablet/mobile mới chứng minh hình học render thật.

#### Câu 42. Production được triển khai như thế nào?

**Trả lời mẫu:** Ứng dụng được đóng gói bằng container; Nginx nhận HTTPS/WSS và reverse proxy tới
frontend/backend. CI/CD build, test và phát hành image; secret và connection string được cung cấp từ
môi trường triển khai.

### Nhóm F — AI, trách nhiệm cá nhân và câu hỏi khó

#### Câu 43. Em có dùng AI để viết code không?

**Trả lời mẫu trung thực:** Có. Em dùng AI như một công cụ hỗ trợ nghiên cứu, sinh code, refactor và
viết test. Em chịu trách nhiệm xác định yêu cầu, duyệt thay đổi, chạy kiểm thử, đối chiếu luận văn với
source và giải thích các luồng chính. Em không xem output AI chưa được kiểm chứng là kết quả đúng.

#### Câu 44. Vậy phần đóng góp của em là gì?

**Trả lời mẫu:** Phần đóng góp của em là phân tích bài toán, quyết định phạm vi, thiết kế quy trình,
kiểm tra tính đúng, điều chỉnh giao diện/nghiệp vụ, tổ chức evidence và chịu trách nhiệm cho sản phẩm
cuối. AI hỗ trợ tốc độ triển khai nhưng không thay thế việc xác nhận yêu cầu và kết quả.

#### Câu 45. Làm sao chứng minh em hiểu code do AI sinh?

**Trả lời mẫu:** Em có thể lần một use case từ route/frontend đến API, service và database; giải thích
invariant, test bảo vệ và lý do chọn kiến trúc. Em cũng có sổ tay đọc code và bộ test characterization
để khóa hành vi trước khi refactor.

#### Câu 46. Nếu không có AI, em có sửa được không?

**Trả lời mẫu:** Em có thể tự xác định module, lần theo call chain, đọc DTO/service/test và sửa theo
vertical slice nhỏ. AI giúp nhanh hơn ở phần lặp lại hoặc tra cứu, nhưng quy trình kiểm chứng vẫn là
build, test, diff review và chạy route thật.

`vertical slice` là một lát chức năng hoàn chỉnh từ UI/API tới dữ liệu và kiểm thử, thay vì sửa dở nhiều
lớp cùng lúc.

#### Câu 47. Điểm yếu lớn nhất của hệ thống hiện tại là gì?

**Trả lời mẫu:** Hệ thống chưa có dữ liệu vận hành dài hạn và load test ở quy mô lớn. Ngoài ra một số
module backend vẫn đang trong kế hoạch refactor tiếp, nên em không khẳng định toàn bộ source đã đạt cấu
trúc cuối cùng.

#### Câu 48. Nếu có thêm ba tháng, em ưu tiên gì?

**Trả lời mẫu:** Em ưu tiên load test và observability, backup/restore drill, tích hợp email/HR thật,
sau đó hoàn thành backend refactor theo module và bổ sung báo cáo ngân sách/cảnh báo bất thường.

---

## 6. Cách trả lời khi không nhớ chính xác

Dùng công thức bốn bước:

1. **Kết luận ngắn:** trả lời trực tiếp câu hỏi;
2. **Cơ chế:** giải thích hệ thống làm như thế nào;
3. **Bằng chứng:** chỉ ra module/test/constraint liên quan;
4. **Giới hạn:** nói rõ phần chưa kiểm chứng hoặc chưa triển khai.

Ví dụ:

> Theo implementation hiện tại, quyền thật được backend kiểm tra bằng policy. Frontend chỉ dùng
> permission snapshot để ẩn/hiện action. Bằng chứng nằm ở policy trên controller và test authorization.
> Em sẽ không khẳng định một API cụ thể nếu chưa mở source để kiểm lại policy name.

Câu trả lời này tốt hơn việc đoán hoặc nói “AI viết nên em không biết”.

---

## 7. Những câu không nên nói khi bảo vệ

- “Em không biết, AI làm hết.”
- “Nút đã ẩn nên người dùng không gọi được API.”
- “Unit test pass nghĩa là toàn bộ hệ thống đúng.”
- “DEV là admin nên muốn sửa dữ liệu gì cũng được.”
- “Microservices luôn tốt hơn monolith.”
- “Database constraint không cần vì service đã kiểm tra.”
- “Em nhớ hình như có khoảng N test.” — hãy chạy lại và dùng số thật.
- “Backend refactor đã hoàn tất toàn bộ.” — hiện B0R/B1 hoàn thành, các wave production sau vẫn còn.

---

## 8. Từ điển English–Vietnamese để học nhanh

| English | Nghĩa dễ hiểu |
|---|---|
| `Requirement` | Yêu cầu cần hệ thống đáp ứng |
| `Constraint` | Ràng buộc bắt buộc |
| `Invariant` | Điều kiện luôn phải đúng |
| `Workflow` | Luồng các bước xử lý |
| `Use case` | Tình huống người dùng sử dụng hệ thống |
| `Contract` | Hợp đồng dữ liệu/hành vi giữa các phần |
| `DTO` | Đối tượng truyền dữ liệu |
| `Entity` | Đối tượng dữ liệu được lưu/ánh xạ database |
| `Controller` | Điểm nhận HTTP request |
| `Service` | Lớp thực hiện use case/nghiệp vụ |
| `Policy` | Quy tắc quyền hoặc nghiệp vụ |
| `Scope` | Phạm vi dữ liệu được xem/thao tác |
| `State` | Trạng thái hiện tại |
| `State machine` | Máy trạng thái và chuyển đổi hợp lệ |
| `Transaction` | Nhóm thao tác cùng thành công hoặc cùng rollback |
| `Concurrency` | Nhiều thao tác xảy ra đồng thời |
| `RowVersion` | Dấu phiên bản chống ghi đè |
| `Idempotency` | Gửi lặp vẫn chỉ tạo một kết quả |
| `Hash` | Giá trị đại diện dữ liệu để phát hiện thay đổi |
| `Snapshot` | Bản chụp dữ liệu tại thời điểm |
| `Revision` | Phiên bản mới, vẫn giữ phiên bản cũ |
| `Audit` | Kiểm tra/truy vết ai làm gì, khi nào |
| `Soft delete` | Đánh dấu ngừng dùng thay vì xóa vật lý |
| `Fallback` | Phương án dự phòng khi cách chính lỗi |
| `Retry` | Thử lại thao tác thất bại |
| `Responsive` | Giao diện thích ứng kích thước màn hình |
| `Accessibility` | Khả năng sử dụng cho nhiều nhóm người dùng/công cụ hỗ trợ |
| `Dependency` | Phần mà một module cần dùng |
| `Composition root` | Nơi đăng ký và lắp ghép dependency khi khởi động |
| `Modular monolith` | Một ứng dụng triển khai chung nhưng chia module rõ |
| `Refactor` | Đổi cấu trúc trong, giữ hành vi quan sát được |
| `Characterization test` | Test khóa hành vi hiện tại trước khi refactor |
| `Observability` | Khả năng theo dõi log, metric, trace và lỗi vận hành |

---

## 9. Kế hoạch luyện phản biện trong bảy ngày

| Ngày | Nội dung | Kết quả phải làm được |
|---|---|---|
| 1 | Học phần giới thiệu, phạm vi, tác nhân | Nói trôi chảy bản 60 giây |
| 2 | Học kiến trúc và một call chain | Vẽ lại Browser → DB không nhìn tài liệu |
| 3 | Học tạo đơn và đơn bổ sung | Giải thích transaction, unique, pending và permission |
| 4 | Học settlement | Giải thích preview, hash, idempotency, snapshot, correction |
| 5 | Học security và test | Trả lời được vì sao ẩn nút không phải bảo mật |
| 6 | Chạy demo và quay màn hình | Hoàn thành demo dưới 5 phút, có ảnh dự phòng |
| 7 | Mock defense | Trả lời ngẫu nhiên 20 câu, mỗi câu dưới 90 giây |

Mỗi câu trả lời nên dài 30–60 giây. Chỉ mở rộng khi giảng viên hỏi sâu hơn.
