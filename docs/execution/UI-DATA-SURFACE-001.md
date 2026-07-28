# UI-DATA-SURFACE-001 — Chuẩn hóa data surface

> Trạng thái: `DRAFT — PENDING OWNER APPROVAL`
> Authority cha: [`UI-SYSTEM-001`](./UI-SYSTEM-001.md), triển khai lần lượt trong F5, F6 và F7.
> Phạm vi: frontend Blazor/Radzen; không đổi API, database, RBAC hoặc nghiệp vụ.

## 1. Bản một ánh nhìn

### Mục tiêu

Mọi màn danh sách/bảng dùng cùng một ngôn ngữ nhìn và tương tác:

```text
┌ Search ─ Filter 1 ─ Filter 2 ─ ... ─ Column picker? ─ Clear ┐  Toolbar
├ # ─ Column 1 ─ Column 2 ─ ... ─ Column n                  ┤  Header
├ 1 ─ Data / code popup / note popup                         ┤  Rows
├ …                                                          ┤
└ Summary / loading / selection                         Pager?┘  Footer
```

Khung, chiều cao theo profile, hover, focus, popup, footer và outer inset dùng chung. Route vẫn sở hữu cột, dữ liệu, paging/virtualization, permission và action nghiệp vụ.

### Phạm vi theo thứ tự

| Phase | Làm gì | Sau phase có gì | Model + effort | Gate owner |
|---|---|---|---|---|
| DS0 — Contract | Khóa motif, density, footer mode và consumer ledger | Một board nhìn là hiểu toàn hệ thống | **Sol · XHigh** — quyết định kiến trúc dài hạn | Duyệt motif và hai density profile |
| DS1 — Foundation | Tạo shared frame/toolbar/footer/popover + token/bridge | Một chỗ chỉnh visual/interaction | **Sol · High** | Duyệt 2 route đại diện |
| DS2 — Reference | History list, My Orders/History detail, Catalog | Nhóm M0–M2 thành mẫu canonical | **Terra · High**, **Sol · High review** | Duyệt board danh sách + chi tiết |
| DS3 — Workflow | Create Order, Department Summary, Period Review | Các workflow chính cùng motif | **Terra · High**, **Sol · High review** | Duyệt 4 route thật |
| DS4 — Admin | Library, Users, Permission và màn quản trị danh mục | Có column picker và paging chuẩn | **Terra · High**, **Sol · XHigh review** | Duyệt admin board |
| R1 — Refactor | Xóa adapter/CSS/state hết consumer, tách file quá tải | Code sạch hơn nhưng UI/behavior giữ nguyên | **Sol · XHigh plan/review**, **Terra · High migration** | Before/after visual + behavior parity |

### Bốn quyết định cần owner duyệt

1. Dùng composition `frame → toolbar → route-owned grid → footer`, không tạo `UniversalGrid<T>` hoặc cây kế thừa markup.
2. Không ép mọi hàng cao bằng nhau tuyệt đối; dùng hai profile `Compact` và `RichTwoLine` ở [mục 3.2](#32-nhịp-chiều-cao-khuyến-nghị).
3. Footer virtualization luôn có thông tin hữu ích, nhưng không có pager giả; xem [mục 3.3](#33-footer-theo-data-behavior).
4. Cleanup nhỏ làm liên tục trong slice; refactor rộng chỉ mở ở R1 sau khi reference UI được duyệt.

## 2. Hiện trạng và nguyên nhân trùng code

- Repository có 18 Razor page/component dùng `RadzenDataGrid`.
- `VppFilterSearch` hiện được dùng trực tiếp ở Product Catalog, Create Order, Permission Users và trong `VppOrderItemsSurface`.
- `HistoryOrderList` vẫn tự dựng `vpp-history-search`, `vpp-history-select`, menu state và một block CSS route riêng.
- Đây là kết quả của migration theo vertical slice ở F4: shared filters được chứng minh trên consumer mới/chạm tới, còn History list giữ implementation cũ để tránh big-bang cùng lúc với server paging, popup state và responsive mobile list.
- Cách làm cũ an toàn trong ngắn hạn nhưng đã tạo visual drift. DS2 sẽ migrate History list sang canonical component, rồi chỉ xóa CSS/state legacy khi consumer ledger về 0.

## 3. Contract data surface đề xuất

### 3.1 Cấu trúc

| Vùng | Contract chung | Phần route được tùy biến |
|---|---|---|
| Toolbar | Search trước; filter theo thứ tự nghiệp vụ; column picker nếu cần; clear ở cuối | Placeholder, option, số filter, debounce, permission |
| Header | Nền/chữ/weight/alignment/token chung; cột `#` là mặc định cho danh sách cần dò vị trí | Tên, độ rộng, sort, cột hiển thị |
| Row | Hover/selected/focus chung; padding và vertical alignment theo density | Cell template, action, badge, business state |
| Cell popup | Một component cho code/note bị rút gọn, copy và viewport anchoring | Label, value, icon, có/không cho copy |
| Footer | Một nhịp cao và semantic chung | Summary, pager, loading, selection, aggregate |

Không bắt buộc cột `#` cho tree, permission matrix hoặc bề mặt có thứ tự riêng không mang ý nghĩa cho người dùng.

### 3.2 Nhịp chiều cao khuyến nghị

Không nên áp literal `toolbar = header = mọi data row = footer` cho toàn hệ thống:

- Dòng một tầng có thể giữ `40px`.
- Dòng có `tên + code` cần `52px`; ép xuống `40px` sẽ chật/cắt chữ, nâng mọi bảng lên `52px` lại lãng phí diện tích.

Contract đề xuất:

| Token/profile | Chiều cao | Dùng cho |
|---|---:|---|
| Control | `32px` | Search, select, clear, column picker trigger |
| Toolbar shell | `42px` | Chứa control với inset trên/dưới cân nhau |
| Header | `40px` | Header bảng desktop |
| `Compact` row | `40px` | Một dòng text/badge/số |
| `RichTwoLine` row | `52px` | Tên + code hoặc nội dung hai tầng |
| Footer | `42px` | Summary và pager |

Như vậy các đường biên vẫn thẳng, đều và có nhịp; chỉ phần nội dung thật sự cần hai dòng mới cao hơn.

### 3.3 Footer theo data behavior

| Mode | Nội dung nên có | Không nên có |
|---|---|---|
| Server paging | `Hiển thị a–b trên tổng n` + pager; page size nếu nghiệp vụ cần | Continuous scroll trong cùng grid |
| Virtualized/continuous | `Đang hiển thị x trên tổng n`; loading/end-of-list; aggregate hoặc selected count nếu hữu ích | Pager giả hoặc vùng trắng trang trí |
| Static/small list | `Hiển thị x mục`; aggregate ngắn nếu có giá trị | Footer rỗng chỉ để đủ chiều cao |
| Total chưa biết | `Đã tải x mục` + trạng thái đang tải | Khẳng định tổng giả |

Footer virtualization không bị “trống” nếu nó trả lời được ít nhất một câu: đang thấy bao nhiêu, tổng bao nhiêu, đã chọn bao nhiêu, tổng số lượng bao nhiêu, hoặc còn đang tải không.

### 3.4 Responsive

- Desktop/tablet rộng giữ toolbar một dòng khi đủ chỗ.
- Tablet hẹp/mobile: search chiếm toàn hàng; filter chia grid; clear chiếm toàn hàng khi cần.
- Bảng nhiều cột có thể chuyển sang mobile card/list hoặc horizontal region có chủ đích; không ép desktop row-height lên mobile card.
- Popup/popover phải dùng cùng transient motion, không đổi geometry khi mở và không vượt viewport.

## 4. Kiến trúc component

```text
VppDataSurfaceFrame
├─ Toolbar slot → VppDataToolbar
│  ├─ VppFilterSearch
│  ├─ VppFilterSelect<T>
│  ├─ optional ColumnPicker
│  └─ VppClearFiltersButton
├─ Data slot → RadzenDataGrid hoặc virtualized route component
└─ Footer slot
   ├─ Radzen paginator đã được bridge style, hoặc
   └─ VppDataSummaryFooter cho virtualized/static
```

Shared foundation dự kiến:

- `VppDataSurfaceFrame`: chỉ sở hữu geometry/border/overflow/slot, không biết DTO hoặc endpoint.
- `VppDataToolbar`: layout toolbar và responsive; filter vẫn là typed child component.
- `VppDataSummaryFooter`: typed mode cho summary/loading/selection/aggregate; không tự paging dữ liệu.
- `VppCellValuePopover`: code/note/copy với anchor + viewport + accessibility chung.
- `VppDataDensity`: `Compact`, `RichTwoLine`; map sang semantic token.
- Radzen bridge: header/row/paginator dùng token chung theo class opt-in, không override mọi DataGrid chưa audit.

Không tạo:

- `UniversalGrid<T>` nhận endpoint/property bằng string;
- reflection để sinh cột/action;
- base class sở hữu UI state của nhiều nghiệp vụ;
- wrapper cho toàn bộ API Radzen.

## 5. Kế hoạch chi tiết

### DS0 — Contract và visual board

- Lập consumer ledger cho 18 DataGrid và các custom list/table.
- Phân loại từng surface: `Paged`, `Virtualized`, `Static`, `Matrix/Tree`, `Dialog`.
- Chụp route thật đại diện: History list, My Orders detail, Product Catalog, Users.
- Tạo board chú thích trực tiếp toolbar/header/row/footer, hai density và outer inset ở sidebar expanded/collapsed.
- Khóa acceptance criteria trước khi sửa shared CSS.

Gate: owner duyệt motif, density và rollout order.

### DS1 — Shared foundation

- Thêm semantic tokens cho toolbar/header/row/footer và density.
- Tạo `VppDataSurfaceFrame`, `VppDataToolbar`, `VppDataSummaryFooter`, `VppCellValuePopover`.
- Giữ `VppFilterSearch`, `VppFilterSelect<T>`, `VppClearFiltersButton` làm canonical filter controls.
- Thêm architecture tests: không duplicate focus ring; shared transient surface; opt-in Radzen bridge; không reflection/endpoint string.
- Chứng minh trên hai consumer khác behavior: một paged và một virtualized.

Gate: build/unit + route-real desktop/mobile + owner visual review.

### DS2 — Reference M0–M2

1. Migrate `HistoryOrderList` sang shared search/select/clear; xóa `OpenFilterMenu` và legacy CSS chỉ khi không còn dùng.
2. Đưa History list và `VppOrderItemsSurface` vào shared frame/footer contract nhưng giữ server paging so với virtualization.
3. Chuẩn hóa Product Catalog theo cùng toolbar/header/footer/density.
4. Rà My Orders ở sidebar expanded/collapsed và 4 viewport.

Gate: History list/detail + My Orders + Catalog giống motif, nhưng không đổi API/action.

### DS3 — Workflow chính

- Create Order catalog bên trái: shared toolbar, row density, code/note popup và virtual footer.
- Department Summary: shared toolbar, paged footer và detail-on-demand giữ nguyên.
- Period Review/Rà soát: filter/column/summary theo cùng motif; không nhập business action vào shared frame.
- Kiểm tra empty, filtered-empty, loading, error và long-data.

Gate: board 4 route thật, console/network sạch và không document-level scroll ngoài contract.

### DS4 — Library, admin và permission

- Chuẩn hóa page quản trị danh mục, Users và Permission theo shared toolbar/frame.
- Column picker là optional toolbar slot; profile cột vẫn thuộc route.
- Grid nhiều cột dùng server paging/detail-on-demand; không nhồi mọi cột vào viewport.
- Permission matrix/tree được ghi ngoại lệ nếu motif table thường không phù hợp.

Gate: owner duyệt admin board và keyboard/accessibility trace.

### R1 — Behavior-preserving UI refactor

Tên chính xác cho việc “đổi code nhưng UI/tương tác không đổi” là **behavior-preserving refactor**; trong đợt này có thể gọi ngắn là **structural UI refactor**.

- Xóa adapter, selector, state và CSS chỉ khi consumer ledger bằng 0.
- Tách file route/composite quá tải theo responsibility.
- Merge token/selector trùng; không rename diện rộng nếu không tạo giá trị đo được.
- So sánh trước/sau bằng DOM contract, screenshot, interaction, console/network và test route thật.
- Không kết hợp refactor rộng với redesign đang còn review.

## 6. Cách cleanup trong lúc còn thiết kế

Khuyến nghị hybrid:

- **Làm ngay trong mỗi slice:** xóa code vừa bị thay thế, merge CSS trùng cục bộ, cập nhật test và consumer ledger.
- **Để R1:** đổi cấu trúc folder lớn, tách nhiều component, rename xuyên route, xóa toàn bộ legacy và tối ưu architecture toàn frontend.

Không nên để toàn bộ cleanup tới cuối vì debt sẽ tiếp tục nhân lên; cũng không nên refactor rộng ngay bây giờ vì UI contract còn đổi. Mốc nhắc R1 là sau DS2 owner approval, rồi thực thi sau DS4 hoặc khi một cụm legacy đã hết consumer.

## 7. Nguyên tắc code AI-first đề xuất

Áp dụng theo tài liệu OpenAI chính thức:

1. Mỗi task ghi rõ `goal · context · constraints · done when`; task lớn dùng một goal và execution plan sống.
2. `AGENTS.md` chỉ giữ layout, command, convention, constraint và definition of done bền vững; chi tiết dài nằm ở architecture/plan.
3. Abstraction theo tầng `token → primitive → composite → pattern → route`; chỉ trích xuất sau ít nhất hai consumer thật.
4. Typed enum/record/slot thay string config, reflection và endpoint động.
5. Một implementer chính; đổi model ở checkpoint lớn; worktree tách biệt khi có nhiều agent cùng ghi.
6. Build/unit không chứng minh UI: phải có route-real DOM geometry, screenshot nhìn bằng mắt, interaction, console/network và accessibility.
7. Workflow lặp lại ổn định mới cập nhật `gtas-vpp-ui-system` skill; invariant cơ học mới đưa vào script/test/hook.
8. Commit theo vertical slice; diff review và consumer ledger là gate trước khi xóa legacy.

Sau khi DS2 chứng minh workflow trên ít nhất hai route, mới cập nhật skill UI bằng checklist data-surface. Chưa cần tạo plugin riêng vì capability vẫn chỉ thuộc GTAS VPP.

Nguồn:

- [Codex best practices](https://learn.chatgpt.com/guides/best-practices)
- [AGENTS.md guidance](https://learn.chatgpt.com/docs/agent-configuration/agents-md)
- [Long-running work and Goal mode](https://learn.chatgpt.com/docs/long-running-work)
- [Codex ExecPlans](https://developers.openai.com/cookbook/articles/codex_exec_plans)
- [GPT-5.6 model guidance](https://developers.openai.com/api/docs/guides/latest-model.md)

## 8. Validation bắt buộc

- Frontend build Release, unit/architecture tests và `git diff --check`.
- Route-real: `390×844`, `768×1024`, `1366×768`, `1920×1080`.
- Sidebar expanded/collapsed; VI/EN; Light/Dark đại diện.
- Keyboard/focus, hover/selected, popup viewport, paging/virtualization, empty/error/loading.
- Không document-level overflow ngoài route contract; native scrollbar giữ nguyên.
- Screenshot/board được người kiểm tra nhìn bằng mắt; geometry test chỉ chống regression.
- Full `verify -Scope frontend` ở checkpoint; blocker ngoài scope phải tách rõ.

## 9. Rủi ro và cách khóa

| Rủi ro | Cách khóa |
|---|---|
| Big-bang migration làm vỡ nhiều route | Vertical slice + consumer ledger + commit riêng |
| Shared component thành generic quá mức | Chỉ sở hữu chrome/geometry; route giữ data/action/API |
| Ép row height làm cắt text | Hai density profile + long-text fixture |
| Popup lệch/nhảy | Một transient/anchor contract + frame sampling + visual review |
| CSS global làm ảnh hưởng grid chưa audit | Opt-in class + bridge test |
| Refactor đổi behavior âm thầm | Before/after route-real parity và owner gate |

## 10. Definition of done

- Các route trong scope dùng canonical toolbar/filter/footer/density theo ledger.
- Không còn duplicate History filter implementation.
- Outer inset giữ đúng ở sidebar expanded/collapsed.
- Footer đúng data behavior; không pager giả hoặc footer rỗng trang trí.
- Legacy adapter/CSS chỉ còn khi ledger chứng minh vẫn có consumer.
- F5/F6 visual board được owner duyệt; R1 giữ UI/interaction parity; F7 full gate pass.
