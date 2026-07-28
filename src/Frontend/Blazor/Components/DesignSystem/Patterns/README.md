# Workspace patterns

Pattern chỉ sở hữu bố cục và cách ghép các vùng nội dung. Route vẫn sở hữu API, permission, business state, copy và action nghiệp vụ.

| Pattern | Dùng khi | Slot/contract chính | Consumer tham chiếu |
|---|---|---|---|
| `VppAccountWorkspace` | Luồng account ẩn danh có một form tập trung | brand, language switch, form content | Login, Forgot Password và các account route qua `VppAccountShell` |
| `VppCollectionWorkspace` | Danh sách/catalog có toolbar, summary và footer tùy chọn | `Header`, `Summary`, `Toolbar`, `ChildContent`, `Footer` | Product Catalog, Price Library |
| `VppListDetailWorkspace` | Danh sách và inspector cùng hiện trên desktop | `List`, `Detail`, `VppListDetailRatio` | Library shared grid, Permission Users |
| `VppSplitEditorWorkspace` | Hai vùng chỉnh sửa/chọn dữ liệu; có thể resize | `Primary`, `Secondary`, `VppSplitEditorRatio`, `Resizable` | Lookup Library, Order Create |
| `VppOperationWorkspace` | Quy trình vận hành theo bước và action | `Header`, `Steps`, `Summary`, `ChildContent`, `Actions` | Period Operations, Pending Approval |
| `VppAnalyticsWorkspace` | KPI/chart/list/detail cùng tạo một data story | `Context`, `Summary`, `Visualization`, `ChildContent`, `Detail` | History, Department Summary |

## Quy tắc chọn pattern

1. Chỉ dùng khi route khớp mục đích và behavior, không chọn vì hai màn nhìn hơi giống nhau.
2. Không thêm endpoint string, reflection, DTO generic hoặc permission orchestration vào pattern.
3. Nếu cần ngoại lệ nghiệp vụ, giữ ngoại lệ ở route qua slot hoặc hạ abstraction xuống composite/primitive.
4. Pattern mới cần ít nhất hai consumer thật, architecture test và route-real browser evidence.
5. Không tạo `UniversalPage`, `UniversalGrid` hoặc một pattern chứa mọi loại màn hình.

Khoảng cách ngoài page do bốn token `--vpp-page-inset-*` ở `vpp-tokens.css` sở hữu. Inline-start/end mặc định đối xứng; block-start/end có thể khác nhau. Pattern root và top-level route panel không được cộng thêm padding ngoài ẩn — spacing bên trong thuộc slot/component con.
