const $ = (selector, root = document) => root.querySelector(selector);

const icon = (name) => `<span class="ms" aria-hidden="true">${name}</span>`;
const badge = (text, tone = "") => `<span class="badge ${tone}">${text}</span>`;
const button = (text, kind = "", glyph = "") => `<button class="btn ${kind}" type="button">${glyph ? icon(glyph) : ""}<span>${text}</span></button>`;
const screenTarget = (id) => `data-screen-target="${id}"`;
const linkButton = (target, text, kind = "", glyph = "") => `<button class="btn ${kind} screen-link" type="button" ${screenTarget(target)}>${glyph ? icon(glyph) : ""}<span>${text}</span></button>`;
const drawerButton = (text, kind = "", glyph = "", mode = "edit") => `<button class="btn ${kind}" type="button" data-admin-drawer-trigger data-admin-drawer-mode="${mode}">${glyph ? icon(glyph) : ""}<span>${text}</span></button>`;
const softDeleteButton = () => `<button class="btn danger" type="button" data-admin-soft-delete>${icon("block")}<span>Vô hiệu hóa</span></button>`;
const columnPresetControl = () => `<button class="select-box column-preset-control" type="button" data-column-presets="Mặc định|Nghiệp vụ|Nhật ký|Tất cả" data-column-preset-index="0" title="Chọn nhóm cột áp dụng cho toàn bộ dữ liệu"><span>Cột · Mặc định</span>${icon("view_column")}</button>`;
const searchBox = (text = "Tìm kiếm") => `<div class="search-box">${icon("search")}<span>${text}</span></div>`;
const selectBox = (text) => `<div class="select-box"><span>${text}</span>${icon("expand_more")}</div>`;
// Xuất theo từng đơn được backend hỗ trợ thật từ 2026-07-26 (quyết định D10):
// GET /api/VPPRequest/orders/{id}/export.pdf và /export.xlsx, không chứa giá.
const orderExportActions = (compact = false) => `<div class="order-export-actions ${compact ? "is-compact" : ""}">
  <button class="btn quiet order-export-action" type="button">${icon("picture_as_pdf")}<span>${compact ? "PDF" : "Xuất PDF"}</span></button>
  <button class="btn quiet order-export-action" type="button">${icon("table_view")}<span>${compact ? "Excel" : "Xuất Excel"}</span></button>
</div>`;
const formatIndex = (value) => String(value).padStart(2, "0");
const roleLabels = {
  PUBLIC: "Không cần đăng nhập",
  EMPLOYEE: "Nhân viên",
  MANAGER: "Quản lý",
  DEV: "Quản trị hệ thống"
};
const roleLabel = (role) => roleLabels[role] || role;

const boardLabels = {
  M0: "Nền tảng giao diện và trạng thái dùng chung",
  M1: "Tài khoản và phiên làm việc",
  M2: "Vòng đời đơn cá nhân",
  M3: "Tổng hợp quản lý",
  M4: "Phê duyệt và chốt kỳ",
  M5A: "Danh mục và tổ chức",
  M5B: "Nhà cung cấp và giá",
  M6: "Người dùng và phân quyền",
  M7: "Báo cáo",
  M8: "Trạng thái vận hành đại diện"
};
const boardLabel = (board) => boardLabels[board] || board;

const dashboardTabs = [
  ["orders", "Đơn hàng của tôi"],
  ["history", "Lịch sử đơn"],
  ["catalog", "Danh mục mặt hàng"],
  ["management", "Quản lý"],
  ["operations", "Vận hành kỳ"]
];

const libraryTabs = [
  ["classes", "Loại danh mục"],
  ["categories", "Danh mục"],
  ["items", "Mặt hàng"],
  ["suppliers", "Nhà cung cấp"],
  ["pricing", "Bảng giá"],
  ["departments", "Phòng ban"]
];

const permissionTabs = [
  ["users", "Người dùng"],
  ["permissions", "Nhóm & quyền"]
];

const adminEntityTemplates = new Set([
  "library-classes",
  "library-categories",
  "library-items",
  "library-suppliers",
  "library-price-lists",
  "library-prices",
  "library-departments",
  "users"
]);

const tabTargets = {
  orders: "my-orders",
  history: "history",
  catalog: "catalog",
  classes: "classes",
  categories: "categories",
  items: "items",
  suppliers: "suppliers",
  pricing: "price-lists",
  departments: "departments",
  users: "users",
  permissions: "permissions",
  reports: "reports"
};

function tabTarget(screen, tab) {
  if (tab === "management") return "department-summary";
  if (tab === "operations") return "period-review";
  return tabTargets[tab] || screen.id;
}

const nestedHeaderPaths = {
  "settlement-flow": ["Vận hành kỳ", "Chốt kỳ"],
  "department-summary": ["Quản lý", "Tổng hợp phòng ban"],
  "period-demand": ["Vận hành kỳ", "Gom nhu cầu"],
  "supply-allocation": ["Vận hành kỳ", "Chọn nguồn cung"],
  approval: ["Vận hành kỳ", "Duyệt đơn bổ sung"],
  "period-review": ["Vận hành kỳ", "Rà soát kỳ"],
  settlement: ["Vận hành kỳ", "Chốt kỳ"],
  "library-price-lists": ["Bảng giá", "Danh sách bảng giá"],
  "library-prices": ["Bảng giá", "Giá mặt hàng"]
};

function nestedHeaderTab(path) {
  const visiblePath = path.length <= 3 ? path : [path[0], "…", path[path.length - 1]];
  return `<span class="nested-header-path">${visiblePath.map((label, index) => {
    const isLeaf = index === visiblePath.length - 1;
    return `${index > 0 ? `<span class="nested-header-separator">${icon("chevron_right")}</span>` : ""}<span class="${isLeaf ? "nested-header-leaf" : "nested-header-ancestor"}">${label}</span>`;
  }).join("")}</span>`;
}

function headerTabs(screen) {
  const tabs = screen.nav === "library"
    ? libraryTabs
    : screen.nav === "permissions"
      ? permissionTabs
      : screen.nav === "reports"
        ? [["reports", "Báo cáo"]]
        : dashboardTabs;
  const nestedPath = nestedHeaderPaths[screen.id] || nestedHeaderPaths[screen.template];
  return `<div class="header-tabs">${tabs.map(([id, label]) => {
    const isActive = screen.tab === id;
    const content = isActive && nestedPath ? nestedHeaderTab(nestedPath) : `<span>${label}</span>`;
    return `<button class="header-tab ${isActive ? "active" : ""} ${isActive && nestedPath ? "is-nested" : ""}" type="button" ${screenTarget(tabTarget(screen, id))}>${content}</button>`;
  }).join("")}</div>`;
}

function orderWorkflowHeader() {
  return `<div class="order-workflow-header" aria-label="Tiến trình tạo đơn">
    <div class="steps workflow-steps">
      <div class="step active"><span class="step-number">1</span><span>Chọn mặt hàng</span></div>
      <div class="step"><span class="step-number">2</span><span>Kiểm tra và gửi</span></div>
    </div>
  </div>`;
}

function navRow(label, glyph, options = {}) {
  const classes = ["nav-row", options.level || "", options.active ? "active" : ""].filter(Boolean).join(" ");
  return `<li class="${classes}" ${options.target ? screenTarget(options.target) : ""}>${icon(glyph)}<span class="nav-label">${label}</span>${options.parent ? `<span class="nav-chevron">${icon(options.open ? "expand_more" : "chevron_right")}</span>` : ""}</li>`;
}

function sidebar(screen) {
  const dashboardOpen = screen.nav === "dashboard";
  const libraryOpen = screen.nav === "library";
  const permissionOpen = screen.nav === "permissions";
  const dashboardActive = ["orders", "history", "catalog", "management", "operations"].includes(screen.tab);
  const libraryPricing = ["pricing"].includes(screen.tab);
  const managementTarget = tabTarget(screen, "management");
  const operationsTarget = tabTarget(screen, "operations");
  return `
    <aside class="sidebar">
      <div class="sidebar-brand">
        <span class="app-mark">V</span>
        <span class="brand-name">GTAS VPP</span>
        <button class="collapse-button" type="button" aria-label="Thu gọn sidebar">${icon("left_panel_close")}</button>
      </div>
      <nav class="sidebar-nav" aria-label="Điều hướng chính">
        <ul class="nav-group">
          ${navRow("Bảng điều khiển", "space_dashboard", { parent: true, open: dashboardOpen, active: dashboardActive && !dashboardOpen, target: "my-orders" })}
          ${dashboardOpen ? [
            navRow("Đơn hàng của tôi", "shopping_bag", { level: "child", active: screen.tab === "orders", target: "my-orders" }),
            navRow("Lịch sử đơn", "history", { level: "child", active: screen.tab === "history", target: "history" }),
            navRow("Danh mục mặt hàng", "inventory_2", { level: "child", active: screen.tab === "catalog", target: "catalog" }),
            navRow("Quản lý", "groups", { level: "child", parent: true, open: screen.tab === "management", active: screen.tab === "management", target: managementTarget }),
            screen.tab === "management" ? navRow("Tổng hợp phòng ban", "table_view", { level: "grandchild", active: true, target: screen.id }) : "",
            navRow("Vận hành kỳ", "event_repeat", { level: "child", parent: true, open: screen.tab === "operations", active: screen.tab === "operations", target: operationsTarget }),
            screen.tab === "operations" ? navRow(screen.template === "period-review" ? "Rà soát kỳ" : screen.template === "period-demand" ? "Gom nhu cầu" : screen.template === "supply-allocation" ? "Chọn nguồn cung" : screen.template === "settlement" ? "Chốt kỳ" : "Duyệt đơn bổ sung", "fact_check", { level: "grandchild", active: true, target: screen.id }) : ""
          ].join("") : ""}
          ${navRow("Quản trị danh mục", "folder_open", { parent: true, open: libraryOpen, active: libraryOpen, target: "classes" })}
          ${libraryOpen ? [
            navRow("Loại danh mục", "schema", { level: "child", active: screen.tab === "classes", target: "classes" }),
            navRow("Danh mục", "category", { level: "child", active: screen.tab === "categories", target: "categories" }),
            navRow("Mặt hàng", "inventory", { level: "child", active: screen.tab === "items", target: "items" }),
            navRow("Nhà cung cấp", "local_shipping", { level: "child", active: screen.tab === "suppliers", target: "suppliers" }),
            navRow("Bảng giá", "sell", { level: "child", parent: true, open: libraryPricing, active: libraryPricing, target: "price-lists" }),
            libraryPricing ? navRow(screen.template === "library-prices" ? "Giá mặt hàng" : "Danh sách bảng giá", "price_change", { level: "grandchild", active: true, target: screen.id }) : "",
            navRow("Phòng ban", "apartment", { level: "child", active: screen.tab === "departments", target: "departments" })
          ].join("") : ""}
          ${navRow("Báo cáo", "analytics", { active: screen.nav === "reports", target: "reports" })}
          ${navRow("Phân quyền", "admin_panel_settings", { parent: true, open: permissionOpen, active: permissionOpen, target: "users" })}
          ${permissionOpen ? [
            navRow("Người dùng", "group", { level: "child", active: screen.tab === "users", target: "users" }),
            navRow("Nhóm & quyền", "rule", { level: "child", active: screen.tab === "permissions", target: "permissions" })
          ].join("") : ""}
        </ul>
      </nav>
      <footer class="sidebar-footer">
        <div class="sidebar-user">
          <span class="avatar">NN</span>
          <div class="sidebar-user-copy"><strong>Nguyễn An Nam</strong><small>Quản trị hệ thống · TEST</small></div>
        </div>
      </footer>
    </aside>`;
}

function shell(screen, content) {
  const headerContent = screen.template === "order-create" ? orderWorkflowHeader() : headerTabs(screen);
  return `<div class="app-shell">
    ${sidebar(screen)}
    <header class="primary-header ${screen.template === "order-create" ? "is-workflow" : ""}">${headerContent}<span class="review-role" title="${screen.role}">${icon("verified_user")} ${roleLabel(screen.role)}</span></header>
    <main class="page-canvas">
      ${content}
      ${adminEntityTemplates.has(screen.template) ? adminEntityDrawer(screen) : ""}
    </main>
  </div>`;
}

function heading(title, subtitle, actions = "") {
  return `<div class="page-heading"><div><h2>${title}</h2><p>${subtitle}</p></div><div class="page-actions">${actions}</div></div>`;
}

function kpis(items) {
  return `<div class="kpi-grid">${items.map(item => `<article class="kpi"><div class="kpi-label"><span>${item.label}</span>${icon(item.icon || "query_stats")}</div><div class="kpi-value">${item.value}</div><div class="kpi-meta">${item.meta || "Cập nhật từ dữ liệu TEST"}</div></article>`).join("")}</div>`;
}

function table(headers, rows, widths = []) {
  return `<table class="data-table"><colgroup>${headers.map((header, index) => `<col${widths[index] ? ` style="width:${widths[index]}"` : ""}${header?.scope ? ` data-admin-column="${header.scope}"` : ""}>`).join("")}</colgroup><thead><tr>${headers.map(header => `<th class="${header.align || ""}"${header.scope ? ` data-admin-column="${header.scope}"` : ""}>${header.label || header}</th>`).join("")}</tr></thead><tbody>${rows.map(row => `<tr>${row.map((cell, index) => `<td class="${headers[index]?.align || ""}"${headers[index]?.scope ? ` data-admin-column="${headers[index].scope}"` : ""}>${cell}</td>`).join("")}</tr>`).join("")}</tbody></table>`;
}

function pagingFooter(summary, pageCount = 3) {
  const pages = Array.from({ length: Math.max(1, pageCount) }, (_, index) => `<span class="${index === 0 ? "current" : ""}">${index + 1}</span>`).join("");
  return `<div class="pager"><span>${summary}</span><div class="pager-pages">${pageCount > 1 ? "<span>‹</span>" : ""}${pages}${pageCount > 1 ? "<span>›</span>" : ""}</div></div>`;
}

function virtualizedFooter(summary) {
  return `<div class="virtualized-grid-footer"><span>${summary}</span></div>`;
}

function historyScope(active = "all") {
  const options = [
    ["all", "Tất cả kỳ"],
    ["current", "Kỳ này"],
    ["3m", "3 tháng"],
    ["6m", "6 tháng"],
    ["12m", "12 tháng"],
    ["custom", `Tùy chọn ${icon("calendar_month")}`]
  ];
  return `<div class="history-scope" role="group" aria-label="Khoảng thời gian">${options.map(([value, label]) => `<button class="${active === value ? "active" : ""}">${label}</button>`).join("")}</div>`;
}

function periodFlowNav(active) {
  const steps = [
    ["review", "period-review", "Rà soát"],
    ["demand", "period-demand", "Gom nhu cầu"],
    ["source", "supply-allocation", "Chọn nguồn cung"],
    ["settle", "settlement-flow", "Chốt kỳ"]
  ];
  return `<nav class="period-flow-nav" aria-label="Quy trình vận hành kỳ">${steps.map(([id,target,label],index)=>`<button class="period-flow-step ${active === id ? "active" : ""}" type="button" ${screenTarget(target)}><span>${index + 1}</span><strong>${label}</strong></button>`).join("")}</nav>`;
}

// Danh mục mặt hàng dùng cho dòng fixture sinh thêm. Mỗi dòng sinh thêm lấy một
// mặt hàng KHÁC NHAU từ đây: đơn và bản gom nhu cầu không được trùng mặt hàng
// theo luận văn §2.3.1.2 ("không trùng văn phòng phẩm").
const fixtureCatalog = [
  { name: "Giấy in A3 IK Plus 80gsm", category: "Giấy in các loại", unit: "Ram" },
  { name: "Bút lông bảng Thiên Long WB-03", category: "Bút viết", unit: "Cây" },
  { name: "Bút dạ quang Stabilo Boss", category: "Bút viết", unit: "Cây" },
  { name: "Sổ lò xo A5 200 trang", category: "Sổ và tập", unit: "Quyển" },
  { name: "Giấy phân trang 5 màu UNC", category: "Giấy note", unit: "Xấp" },
  { name: "Kẹp bướm 25mm Deli", category: "Kẹp và bấm", unit: "Hộp" },
  { name: "Kẹp giấy C62 Deli", category: "Kẹp và bấm", unit: "Hộp" },
  { name: "Bấm kim số 10 Plus", category: "Kẹp và bấm", unit: "Cái" },
  { name: "Kim bấm số 10 Plus", category: "Kẹp và bấm", unit: "Hộp" },
  { name: "Kéo văn phòng 21cm Deli", category: "Dụng cụ cắt", unit: "Cái" },
  { name: "Dao rọc giấy lớn SDI", category: "Dụng cụ cắt", unit: "Cái" },
  { name: "Hồ khô 21g Thiên Long", category: "Keo dán", unit: "Cây" },
  { name: "Băng keo hai mặt 2cm", category: "Băng keo, bấm kim", unit: "Cuộn" },
  { name: "Bìa còng 5cm ABBA", category: "Bìa hồ sơ", unit: "Cái" },
  { name: "Bìa nhựa lá A4 Plus", category: "Bìa hồ sơ", unit: "Xấp" },
  { name: "Phong bì trắng 12×22", category: "Giấy in các loại", unit: "Xấp" },
  { name: "Mực dấu Shiny xanh", category: "Con dấu và mực", unit: "Lọ" },
  { name: "Pin AAA Panasonic", category: "Phục vụ văn phòng", unit: "Vỉ" },
  { name: "Chuột quang Logitech B100", category: "Phục vụ văn phòng", unit: "Cái" },
  { name: "Giấy note 3×4 hồng UNC", category: "Giấy note", unit: "Xấp" }
];

// Mã fixture sinh từ hash tên mặt hàng để cùng định dạng VPP_ + 12 ký tự hex
// với mã seed, thay vì lộ mã giả dạng VPP_ORDER_006.
function fixtureCode(name) {
  let hash = 0x811c9dc5;
  for (let index = 0; index < name.length; index += 1) {
    hash ^= name.charCodeAt(index);
    hash = Math.imul(hash, 0x01000193) >>> 0;
  }
  const head = hash.toString(16).toUpperCase().padStart(8, "0");
  const tail = (Math.imul(hash, 0x9e3779b1) >>> 0).toString(16).toUpperCase().padStart(8, "0");
  return `VPP_${head}${tail.slice(0, 4)}`;
}

function expandIndexedRows(seedRows, targetCount, codePrefix = "ATLAS", columns = { name: 1, category: 2, unit: 3 }) {
  const rows = [];
  for (let index = 0; index < targetCount; index += 1) {
    const row = [...seedRows[index % seedRows.length]];
    row[0] = String(index + 1);
    if (index >= seedRows.length) {
      const item = fixtureCatalog[(index - seedRows.length) % fixtureCatalog.length];
      if (typeof row[columns.name] === "string") {
        row[columns.name] = `<strong>${item.name}</strong><span class="subline">${fixtureCode(item.name)}</span>`;
      }
      if (columns.category != null && typeof row[columns.category] === "string") {
        row[columns.category] = item.category;
      }
      if (columns.unit != null && typeof row[columns.unit] === "string") {
        row[columns.unit] = item.unit;
      }
    }
    rows.push(row);
  }
  return rows;
}

function tableCard(title, subtitle, headers, rows, options = {}) {
  const tableMarkup = table(headers, rows, options.widths || []);
  return `<section class="card data-card paged-grid ${options.className || ""}" data-grid-mode="paging">
    <div class="card-header"><div><h3>${title}</h3><p>${subtitle}</p></div>${options.headerAction || ""}</div>
    ${options.headerContent || ""}
    ${options.scrollable ? `<div class="data-table-scroll ${options.adminGrid ? "admin-grid-table" : ""}">${tableMarkup}</div>` : tableMarkup}
    <div class="paged-grid-fill" aria-hidden="true"></div>
    ${pagingFooter(options.summary || `Hiển thị 1–${rows.length} trên ${rows.length} bản ghi`, options.pageCount)}
  </section>`;
}

function virtualizedTableCard(title, subtitle, headers, rows, options = {}) {
  return `<section class="card data-card virtualized-list-card ${options.className || ""}">
    <div class="card-header"><div><h3>${title}</h3><p>${subtitle}</p></div>${options.headerAction || ""}</div>
    <div class="interaction-grid-scroll virtualized-grid" data-grid-mode="scroll-virtualized">${table(headers, rows, options.widths || [])}</div>
    ${virtualizedFooter(options.summary || `Tổng cộng ${rows.length} bản ghi`)}
  </section>`;
}

function orderItemsRegion(rows, options = {}) {
  return `<div class="order-detail-toolbar">${searchBox(options.search || "Tìm mặt hàng")}${selectBox(options.category || "Tất cả danh mục")}${selectBox(options.unit || "Tất cả đơn vị")}${button("Xóa bộ lọc", "quiet", "filter_alt_off")}</div><div class="order-detail-table virtualized-grid" data-grid-mode="scroll-virtualized">${table(["#", "Mặt hàng", "Danh mục", "Đơn vị", {label:"Số lượng",align:"number"}, "Ghi chú"], rows, ["5%","31%","18%","12%","14%","20%"])}</div>${virtualizedFooter(options.summary || `Tổng cộng ${rows.length} mặt hàng`)}`;
}

function orderDetailSheet(options = {}) {
  const fields = options.fields || [
    ["Kỳ", "07/2026"],
    ["Loại đơn", badge("Đơn thường", "blue")],
    ["Người đặt", "Nguyễn An Nam"],
    ["Phòng ban", "Công nghệ thông tin"]
  ];
  const placementClass = options.placement === "approval" ? "inspector approval-order-detail" : "history-detail";
  return `<aside class="card ${placementClass} order-detail-panel is-compact printable-order-sheet">
    <div class="order-sheet-header order-detail-header"><div><span class="order-sheet-kicker">Phiếu chi tiết đơn</span><div class="history-detail-code"><strong>${options.code || "DEMO-PPJ-0726"}</strong>${options.status || badge("Đã gửi", "blue")}</div><p>${options.meta || "Gửi lúc 09:42 · 23/07/2026"}</p></div>${orderExportActions(true)}</div>
    <div class="order-sheet-summary"><dl class="order-sheet-field-grid">${fields.slice(0, 4).map(([label, value]) => `<div><dt>${label}</dt><dd>${value}</dd></div>`).join("")}</dl><div class="order-sheet-totals"><div><strong>${options.itemCount || "18"}</strong><span>Mặt hàng</span></div><div><strong>${options.quantity || "86"}</strong><span>Tổng số lượng</span></div></div><div class="order-sheet-note"><div><span>${options.noteLabel || "Ghi chú đơn"}</span><p>${options.note || "Vật tư văn phòng tháng 7"}</p>${options.supportingMeta ? `<small>${options.supportingMeta}</small>` : ""}</div>${options.showHistory === false ? "" : `<button type="button" ${screenTarget("history")}>Xem lịch sử ${icon("chevron_right")}</button>`}</div></div>
    <div class="approval-items-heading"><div><h4>${options.itemsTitle || "Danh sách mặt hàng"}</h4><p>${options.itemsSubtitle || "Nội dung đầy đủ dùng khi xem và xuất phiếu"}</p></div>${badge(`${options.itemCount || options.rows?.length || 0} mặt hàng`, "blue")}</div>
    ${orderItemsRegion(options.rows || [])}
    ${options.actions ? `<div class="inspector-actions">${options.actions}</div>` : ""}
  </aside>`;
}

function orderDetailAside(options = {}) {
  return orderDetailSheet(options);
}

function filterBar(options = {}) {
  return `<div class="filter-bar">${searchBox(options.search || "Tìm mã hoặc tên")}${(options.filters || ["Tất cả trạng thái"]).map(selectBox).join("")}<span class="filter-spacer"></span>${options.action || button("Xóa bộ lọc", "quiet", "filter_alt_off")}</div>`;
}

function inspector(title, code, fields, actions = "") {
  return `<aside class="card inspector"><div class="inspector-hero"><div class="badge blue">Đang chọn</div><h3>${title}</h3><div class="mono">${code}</div></div><div class="inspector-section"><h4>Thông tin chính</h4><dl class="key-values">${fields.map(([key, value]) => `<dt>${key}</dt><dd>${value}</dd>`).join("")}</dl></div><div class="inspector-section"><h4>Nhật ký gần nhất</h4><p style="margin:0;color:var(--atlas-muted);font-size:11px;line-height:1.55">Cập nhật bởi Nguyễn An Nam · 09:42 23/07/2026<br>Mã theo dõi được giữ trong nhật ký kỹ thuật.</p></div><div class="inspector-actions">${actions || `${button("Chỉnh sửa", "", "edit")}${button("Xem lịch sử", "quiet", "history")}`}</div></aside>`;
}

function adminInspector(title, code, fields, actions = "") {
  const tabs = ["Thông tin chung", "Quan hệ", "Bản dịch", "Nhật ký", "Kỹ thuật"];
  const panels = [
    `<div class="inspector-section admin-general-section"><h4>Thông tin chung</h4><dl class="key-values">${fields.map(([key, value]) => `<dt>${key}</dt><dd>${value}</dd>`).join("")}</dl></div>`,
    `<div class="inspector-section"><h4>Quan hệ nghiệp vụ</h4><dl class="key-values"><dt>Phạm vi</dt><dd>Dữ liệu danh mục chuẩn</dd><dt>Quan hệ</dt><dd>Hiển thị theo đúng loại bản ghi đang mở</dd><dt>Đang được sử dụng</dt><dd>${badge("Có liên kết", "blue")}</dd></dl></div>`,
    `<div class="inspector-section"><h4>Bản dịch</h4><dl class="key-values"><dt>Tiếng Việt</dt><dd>${title}</dd><dt>Tiếng Anh</dt><dd>Localized display value</dd><dt>Fallback</dt><dd>Tiếng Việt</dd></dl></div>`,
    `<div class="inspector-section"><h4>Nhật ký</h4><dl class="key-values"><dt>Ngày tạo</dt><dd>01/07/2026 · 08:30</dd><dt>Người tạo</dt><dd>Dữ liệu khởi tạo hệ thống</dd><dt>Cập nhật</dt><dd>23/07/2026 · 09:42</dd><dt>Người cập nhật</dt><dd>Nguyễn An Nam</dd></dl></div>`,
    `<div class="admin-technical-preview"><div><span>ID bản ghi</span><code>7b9f34ee-c2c4-489a…</code></div><div><span>RowVersion</span><code>0x00000000000007D3</code></div><div><span>Khóa ngoại</span><code>Xem theo quan hệ nghiệp vụ</code></div><div><span>Trường bí mật</span><strong>Không hiển thị</strong></div><div><span>Trạng thái dữ liệu</span><span data-admin-record-status>${badge("Đang hoạt động", "green")}</span></div></div>`
  ];
  return `<aside class="card inspector admin-record-inspector"><div class="inspector-hero"><div class="badge blue">Đang chọn</div><h3>${title}</h3><div class="mono">${code}</div></div><div class="record-inspector-tabs" role="tablist" aria-label="Nhóm dữ liệu bản ghi">${tabs.map((tab,index)=>`<button class="${index === 0 ? "active" : ""}" type="button" role="tab" aria-selected="${index === 0}" data-admin-inspector-tab="${index}">${tab}</button>`).join("")}</div><div class="admin-inspector-panels">${panels.map((panel,index)=>`<div class="admin-inspector-panel ${index === 0 ? "active" : ""}" data-admin-inspector-panel="${index}">${panel}</div>`).join("")}</div><div class="inspector-actions">${actions || `${drawerButton("Chỉnh sửa", "", "edit")}${softDeleteButton()}`}</div></aside>`;
}

const adminFormDefinitions = {
  "library-classes": { label:"loại danh mục", general:[["Mã loại","Uom"],["Tên loại","Đơn vị tính"],["Mô-đun","VPP"],["Mô tả","Danh mục đơn vị tính dùng cho mặt hàng"],["Trạng thái","Hoạt động"]], relations:[["Ngôn ngữ gốc","Tiếng Việt"],["Kiểu giá trị","Mã và tên hiển thị"]] },
  "library-categories": { label:"danh mục", general:[["Mã danh mục","PAPER"],["Tên danh mục","Giấy in các loại"],["Mô tả","Nhóm mặt hàng giấy in"],["Trạng thái","Hoạt động"]], relations:[["Ngôn ngữ gốc","Tiếng Việt"]] },
  "library-items": { label:"mặt hàng", general:[["Mã mặt hàng","VPP_7B9F34EE"],["Tên mặt hàng","Giấy in A4 Double A 70gsm"],["Mô tả","Giấy in dùng cho nghiệp vụ văn phòng"],["Trạng thái","Hoạt động"]], relations:[["Danh mục","Giấy in"],["Đơn vị","Ram"]] },
  "library-suppliers": { label:"nhà cung cấp", general:[["Tên viết tắt","ANPHAT"],["Tên nhà cung cấp","An Phát Office"],["Địa chỉ","12 Nguyễn Văn Linh"],["Phường/Xã","Tân Phong"],["Tỉnh/Thành phố","TP. Hồ Chí Minh"],["Trạng thái","Hoạt động"]], relations:[["Ngôn ngữ gốc","Tiếng Việt"]] },
  "library-price-lists": { label:"bảng giá", general:[["Mã bảng giá","PL-2026-H2"],["Tên bảng giá","Bảng giá nửa cuối 2026"],["Ngày hiệu lực","01/07/2026"],["Ngày hết hiệu lực","31/12/2026"],["Trạng thái","Đã công bố"]], relations:[["Nhà cung cấp","An Phát Office"],["Nguồn dữ liệu","Quản lý bảng giá"]] },
  "library-prices": { label:"giá mặt hàng", general:[["Đơn giá","145.000 ₫"],["Thuế VAT","8%"],["MOQ","1"],["Chiết khấu","0%"],["Phí bổ sung","0 ₫"]], relations:[["Bảng giá","PL-2026-H2"],["Mặt hàng","Giấy in A4 Double A 70gsm"],["Đơn vị","Ram"]] },
  "library-departments": { label:"phòng ban", general:[["Mã phòng ban","IT"],["Tên phòng ban","Công nghệ thông tin"],["Mô tả","Đơn vị phụ trách hệ thống và hạ tầng"],["Trạng thái","Hoạt động"]], relations:[["Phòng ban cấp trên","Không có"],["Ngôn ngữ gốc","Tiếng Việt"]] },
  users: { label:"người dùng", general:[["Tên đăng nhập","annam"],["Họ và tên","Nguyễn An Nam"],["Email","annam@gtas.local"],["Trạng thái","Hoạt động"]], relations:[["Phòng ban","Công nghệ thông tin"],["Nhóm quyền","DEV"],["Môi trường","Phát triển / kiểm thử"]] }
};

function adminDrawerControl([label, value], index) {
  const isLong = label === "Mô tả";
  const isSelect = ["Trạng thái","Danh mục","Đơn vị","Nhà cung cấp","Bảng giá","Mặt hàng","Phòng ban","Nhóm quyền","Ngôn ngữ gốc","Phòng ban cấp trên"].includes(label);
  return `<label class="${isLong ? "admin-form-wide" : ""}">${label}${isSelect ? `<span class="select-box"><span data-admin-field-value data-original="${value}">${value}</span>${icon("expand_more")}</span>` : `<span class="input ${isLong ? "input-multiline" : ""} ${index === 0 ? "mono" : ""}" data-admin-field-value data-original="${value}">${value}</span>`}</label>`;
}

function adminEntityDrawer(screen) {
  const definition = adminFormDefinitions[screen.template] || adminFormDefinitions["library-items"];
  const entityLabel = definition.label;
  return `<div class="admin-drawer-layer" hidden>
    <button class="admin-drawer-backdrop" type="button" aria-label="Đóng biểu mẫu" data-admin-drawer-close></button>
    <section class="admin-entity-drawer" role="dialog" aria-modal="true" aria-labelledby="admin-drawer-title">
      <header class="admin-drawer-header">
        <div><span class="admin-drawer-kicker" data-admin-drawer-kicker>Biểu mẫu quản trị</span><h2 id="admin-drawer-title" data-admin-drawer-title data-entity-label="${entityLabel}">Chỉnh sửa ${entityLabel}</h2><p>Chỉ các trường được phép thay đổi mới có thể nhập.</p></div>
        <button class="icon-button" type="button" aria-label="Đóng" data-admin-drawer-close>${icon("close")}</button>
      </header>
      <div class="admin-drawer-body">
        <section class="admin-form-section"><div><h3>Thông tin chung</h3><p>Dữ liệu nhận diện và trạng thái nghiệp vụ.</p></div><div class="admin-form-grid">${definition.general.map(adminDrawerControl).join("")}</div></section>
        <section class="admin-form-section"><div><h3>Quan hệ nghiệp vụ</h3><p>Chọn dữ liệu liên quan thay vì nhập khóa ngoại thủ công.</p></div><div class="admin-form-grid">${definition.relations.map(adminDrawerControl).join("")}</div></section>
        <section class="admin-form-section is-readonly"><div><h3>Kỹ thuật chỉ đọc</h3><p>Cho phép kiểm tra nhưng không chỉnh trực tiếp.</p></div><dl class="admin-readonly-grid"><div><dt>ID</dt><dd class="mono">7b9f34ee-c2c4-489a…</dd></div><div><dt>RowVersion</dt><dd class="mono">0x00000000000007D3</dd></div><div><dt>Tạo lúc</dt><dd>09:42 · 23/07/2026</dd></div><div><dt>Cập nhật bởi</dt><dd>Nguyễn An Nam</dd></div></dl></section>
      </div>
      <footer class="admin-drawer-actions"><button class="btn quiet" type="button" data-admin-drawer-close><span>Hủy</span></button><button class="btn primary" type="button" data-admin-drawer-save>${icon("save")}<span>Lưu bản ghi</span></button></footer>
    </section>
  </div>`;
}

function collectionWorkspace(options = {}) {
  const summary = options.summary || "";
  const filters = options.filters || "";
  const content = options.content || "";
  return `<div class="page-stack workspace workspace-collection">${summary}${filters}${content}</div>`;
}

function masterDetailWorkspace(options = {}) {
  const summary = options.summary || "";
  const filters = options.filters || "";
  const list = options.list || "";
  const detail = options.detail || "";
  const variant = options.variant ? ` ${options.variant}` : "";
  return collectionWorkspace({
    summary,
    filters,
    content: `<div class="split-layout master-detail-workspace${variant}">${list}${detail}</div>`
  });
}

function approvalOrderDetail(options = {}) {
  return orderDetailSheet({
    ...options,
    placement: "approval",
    status: badge("Chờ duyệt", "amber"),
    fields: options.fields || [["Kỳ", "07/2026"], ["Loại đơn", badge("Đơn bổ sung", "amber")], ["Người đặt", "Nguyễn Thanh Thủy"], ["Phòng ban", "Công nghệ thông tin · IT"]],
    itemCount: options.itemCount || String(options.rows?.length || 0),
    quantity: options.quantity || "48",
    noteLabel: "Ghi chú đơn",
    note: options.note || "Thiếu vật tư dự án",
    supportingMeta: options.supportingMeta || "Hạn mức còn lại: 38 sản phẩm",
    itemsTitle: "Mặt hàng cần duyệt",
    itemsSubtitle: "Kiểm tra đầy đủ mặt hàng và số lượng trước khi quyết định",
    showHistory: false
  });
}

function chart(title = "Xu hướng số lượng theo kỳ", options = {}) {
  const bars = options.bars || [[62, 28], [86, 34], [118, 47], [92, 36], [142, 58], [126, 45]];
  const labels = options.labels || bars.map((_, index) => `0${index + 2}/26`);
  return `<section class="card chart-card"><div class="card-header"><div><h3>${title}</h3><p>${options.subtitle || "Đơn thường và đơn bổ sung · 6 kỳ gần nhất"}</p></div><div>${badge(options.primaryLegend || "Đơn thường", "blue")} ${badge(options.secondaryLegend || "Bổ sung", "green")}</div></div><div class="chart-area">${bars.map((pair, index) => `<div class="bar-group"><i class="bar" style="height:${pair[0]}px"></i><i class="bar alt" style="height:${pair[1]}px"></i><span class="bar-label">${labels[index]}</span></div>`).join("")}</div></section>`;
}

function myOrders(screen) {
  const rows = expandIndexedRows([
    ["1", `<strong>Giấy note vàng 3×3 – UNC</strong><span class="subline">VPP_B345DC72BD6A87306ED6EF11</span>`, "Giấy in các loại", "Xấp", "3", "Đã duyệt"],
    ["2", `<strong>Pin đồng hồ Pin 2A Energizer</strong><span class="subline">VPP_B5DF2B25582F9B4C295B4D88</span>`, "Phục vụ văn phòng", "Vỉ", "2", "Dùng cho phòng họp"],
    ["3", `<strong>Băng keo trong 5cm 100y</strong><span class="subline">VPP_16B403E8EA58A2284F7516F7</span>`, "Băng keo, bấm kim", "Cuộn", "5", "–"],
    ["4", `<strong>Bìa lỗ A4 Plus</strong><span class="subline">VPP_802FA6E3E8DBD0F6D09A0</span>`, "Bìa hồ sơ", "Xấp", "10", "Lưu chứng từ"],
    ["5", `<strong>Bút bi Thiên Long TL-027</strong><span class="subline">VPP_74CBE20A69F8CE87C3A1AA09</span>`, "Bút viết", "Cây", "8", "Mực xanh"]
  ], 18, "ORDER");
  return shell(screen, `<div class="page-stack">
    <section class="period-hero"><div><div class="period-eyebrow">Kỳ đặt hàng hiện tại</div><div class="period-title">07/2026</div><div class="period-meta"><strong>Còn 14 ngày</strong> · Hạn gửi đơn 17:00, 06/08/2026</div></div><div class="page-actions">${linkButton("order-create", "Tạo đơn bổ sung", "primary", "add")}</div></section>
    <div class="segment-grid"><article class="segment-card active"><strong>Đơn kỳ hiện tại</strong><span>18 mặt hàng · tổng số lượng 86 · Đã gửi</span></article><article class="segment-card"><strong>Đơn bổ sung</strong><span>Chưa có đơn · Có thể tạo trong kỳ mở</span></article><article class="segment-card"><strong>Kỳ trước · 06/2026</strong><span>12 mặt hàng · Chỉ xem</span></article></div>
    <section class="card data-card order-detail-panel is-wide"><div class="card-header order-detail-header"><div><h3>DEMO-PPJ-0726</h3><p>Gửi lúc 09:42, 23/07/2026 · ${badge("Đã gửi", "blue")}</p></div><div class="page-actions">${button("Sửa", "quiet", "edit")}${linkButton("history", "Lịch sử đơn", "quiet", "history")}${orderExportActions(true)}${button("Hủy", "danger", "cancel")}</div></div>${orderItemsRegion(rows)}</section>
  </div>`);
}

function selectedDraftItem(name, code, quantity, unit, note) {
  const noteId = `item-note-${code.toLowerCase().replace(/[^a-z0-9]+/g, "-")}`;
  return `<article class="selected-draft-item">
    <div class="selected-draft-row">
      <div class="selected-product-cell"><strong>${name}</strong><span class="subline">${code}</span></div>
      <div class="quantity-stepper"><button type="button" aria-label="Giảm số lượng">−</button><span>${quantity}</span><button type="button" aria-label="Tăng số lượng">+</button></div>
      <span class="selected-unit">${unit}</span>
      <div class="selected-row-actions">
        <button class="icon-button item-note-trigger" type="button" data-note-trigger aria-expanded="false" aria-controls="${noteId}" title="Ghi chú mặt hàng" aria-label="Xem hoặc sửa ghi chú của ${name}">${icon("edit_note")}</button>
        <button class="icon-button selected-remove-action" type="button" title="Bỏ mặt hàng" aria-label="Bỏ ${name} khỏi đơn">${icon("close")}</button>
      </div>
    </div>
    <section class="item-note-popover" id="${noteId}" data-note-popover data-item-note-popover role="dialog" aria-label="Ghi chú của ${name}" hidden>
      <div class="item-note-popover-header"><div><span>Ghi chú mặt hàng</span><strong>${name}</strong></div><button class="icon-button item-note-close" type="button" data-item-note-close aria-label="Đóng">${icon("close")}</button></div>
      <div class="item-note-popover-body"><div class="item-note-editor" role="textbox" aria-label="Nội dung ghi chú">${note}</div><small>Ghi chú này chỉ áp dụng cho mặt hàng đang chọn.</small></div>
      <div class="item-note-popover-actions"><button class="btn quiet" type="button" data-note-close>Hủy</button><button class="btn primary" type="button" data-note-save>${icon("check")}<span>Lưu ghi chú</span></button></div>
    </section>
  </article>`;
}

function orderCreate(screen) {
  const selectedAction = (name) => `<button class="icon-button catalog-remove-action" type="button" title="Bỏ mặt hàng" aria-label="Bỏ ${name} khỏi đơn">${icon("close")}</button>`;
  const rows = [
    ["1", `<strong>Giấy in A4 Double A 70gsm</strong><span class="subline">VPP_7B9F34EEC2C489</span>`, "Giấy in", "Ram", selectedAction("Giấy in A4 Double A 70gsm")],
    ["2", `<strong>Bút bi Thiên Long TL-027</strong><span class="subline">VPP_74CBE20A69F8</span>`, "Bút viết", "Cây", button("Thêm", "compact", "add")],
    ["3", `<strong>File còng 7cm Kokuyo</strong><span class="subline">VPP_D942B0AF782D</span>`, "Bìa hồ sơ", "Cái", button("Thêm", "compact", "add")],
    ["4", `<strong>Giấy note vàng 3×3 – UNC</strong><span class="subline">VPP_B345DC72BD6A</span>`, "Giấy note", "Xấp", selectedAction("Giấy note vàng 3×3 – UNC")],
    ["5", `<strong>Băng keo trong 5cm 100y</strong><span class="subline">VPP_16B403E8EA58</span>`, "Băng keo", "Cuộn", button("Thêm", "compact", "add")]
  ];
  const overflowItems = [
    ["Bìa lỗ A4 Plus", "Bìa hồ sơ", "Xấp", true], ["Kẹp bướm 25mm Deli", "Kẹp giấy", "Hộp", true],
    ["Bút dạ quang Stabilo", "Bút viết", "Cây", true], ["Sổ lò xo A5 Campus", "Sổ tay", "Quyển", true],
    ["Kim bấm số 10 Plus", "Bấm kim", "Hộp", false], ["Bút xóa kéo Kokuyo", "Bút xóa", "Cái", false],
    ["Giấy phân trang 5 màu", "Giấy note", "Xấp", false], ["Khay hồ sơ 3 tầng", "Lưu trữ", "Bộ", false],
    ["Thước nhựa 30cm", "Dụng cụ", "Cái", false], ["Dao rọc giấy SDI", "Dụng cụ", "Cái", false],
    ["Băng keo hai mặt 2cm", "Băng keo", "Cuộn", false], ["Nhãn dán hồ sơ Tomy", "Nhãn", "Xấp", false]
  ];
  overflowItems.forEach((item, index) => rows.push([
    String(index + 6),
    `<strong>${item[0]}</strong><span class="subline">VPP_DEMO_${String(index + 6).padStart(3, "0")}</span>`,
    item[1], item[2], item[3] ? selectedAction(item[0]) : button("Thêm", "compact", "add")
  ]));
  const selectedItems = [
    selectedDraftItem("Giấy in A4 Double A 70gsm", "VPP_7B9F34EEC2C489", "2", "Ram", "In hồ sơ dự án quý III"),
    selectedDraftItem("Giấy note vàng 3×3 – UNC", "VPP_B345DC72BD6A", "3", "Xấp", "Dùng tại phòng họp"),
    selectedDraftItem("Bìa lỗ A4 Plus", "VPP_DEMO_006", "4", "Xấp", "Phân loại chứng từ tháng 7"),
    selectedDraftItem("Kẹp bướm 25mm Deli", "VPP_DEMO_007", "2", "Hộp", "Kẹp bộ hồ sơ nghiệm thu"),
    selectedDraftItem("Bút dạ quang Stabilo", "VPP_DEMO_008", "5", "Cây", "Phân màu tài liệu họp"),
    selectedDraftItem("Sổ lò xo A5 Campus", "VPP_DEMO_009", "1", "Quyển", "Ghi chép công việc tháng 7")
  ].join("");
  return shell(screen, `<div class="page-stack"><div class="product-picker"><section class="card data-card interaction-grid-card"><div class="card-header"><div><h3>Danh mục được phép đặt</h3><p>6 mặt hàng đã chọn · 286 mặt hàng khả dụng</p></div></div>${filterBar({search:"Tìm tên hoặc mã mặt hàng",filters:["Tất cả danh mục","Tất cả đơn vị"]})}<div class="interaction-grid-scroll virtualized-grid" data-grid-mode="scroll-virtualized">${table(["#","Mặt hàng","Danh mục","Đơn vị",{label:"Thao tác",align:"center"}], rows, ["5%","43%","22%","10%","20%"])}</div>${virtualizedFooter("Tổng cộng 286 mặt hàng")}</section><aside class="card order-draft-panel"><div class="card-header"><div><h3>Đơn đang tạo</h3><p>Kỳ 07/2026</p></div><div class="draft-badges">${badge("6 mặt hàng", "blue")}${badge("Nháp", "amber")}</div></div><div class="order-draft-body"><div class="selected-order-scroll-region" data-navigation="scroll"><div class="selected-order-grid-header" aria-hidden="true"><span>Mặt hàng</span><span>SL</span><span>Đơn vị</span><span></span></div><div class="selected-order-list">${selectedItems}</div></div><div class="selected-order-summary"><span>6 mặt hàng</span><strong>Tổng số lượng 17</strong></div></div><div class="inspector-actions order-draft-actions"><div class="order-draft-back-action">${linkButton("my-orders", "Quay lại", "quiet", "arrow_back")}</div><div class="order-draft-forward-actions"><button class="btn quiet order-save-draft" type="button" data-save-draft>${icon("save")}<span>Lưu nháp</span></button><button class="btn quiet order-note-trigger" type="button" data-note-trigger aria-expanded="false" aria-controls="order-note-popover">${icon("edit_note")}<span>Ghi chú</span></button>${button("Tiếp tục", "primary", "arrow_forward")}</div></div><section class="item-note-popover order-note-popover" id="order-note-popover" data-note-popover data-order-note-popover role="dialog" aria-label="Ghi chú đơn" hidden><div class="item-note-popover-header"><div><span>Ghi chú chung</span><strong>Đơn kỳ 07/2026</strong></div><button class="icon-button item-note-close" type="button" data-note-close aria-label="Đóng">${icon("close")}</button></div><div class="item-note-popover-body"><div class="item-note-editor order-note-input" role="textbox" aria-label="Nội dung ghi chú đơn">Bổ sung vật tư phục vụ dự án và phòng họp trong tháng 7</div><small>Ghi chú này áp dụng cho toàn bộ đơn.</small></div><div class="item-note-popover-actions"><button class="btn quiet" type="button" data-note-close>Hủy</button><button class="btn primary" type="button" data-note-save>${icon("check")}<span>Lưu ghi chú</span></button></div></section></aside></div></div>`);
}

function history(screen) {
  const rows = [
    ["1", "07/2026", "DEMO-PPJ-0726", badge("Đơn thường", "blue"), badge("Đã gửi", "blue"), "18", "86", "23/07/2026", "Vật tư tháng 7"],
    ["2", "06/2026", "SUP-0626", badge("Đơn bổ sung", "amber"), badge("Đã duyệt", "green"), "3", "12", "28/06/2026", "Bổ sung phòng họp"],
    ["3", "05/2026", "DEMO-0526", badge("Đơn thường", "blue"), badge("Đã chốt", "green"), "11", "47", "19/05/2026", "–"],
    ["4", "04/2026", "DEMO-0426", badge("Đơn thường", "blue"), badge("Đã chốt", "green"), "8", "31", "17/04/2026", "–"],
    ["5", "03/2026", "DEMO-0326", badge("Đơn thường", "blue"), badge("Đã hủy", "red"), "2", "4", "06/03/2026", "Hủy theo yêu cầu"]
  ];
  const detailRows = expandIndexedRows([
    ["1", `<strong>Giấy note vàng 3×3 – UNC</strong><span class="subline">VPP_B345DC72BD6A</span>`, "Giấy note", "Xấp", "3", "Đã duyệt"],
    ["2", `<strong>Pin đồng hồ Pin 2A Energizer</strong><span class="subline">VPP_B5DF2B25582F</span>`, "Phục vụ văn phòng", "Vỉ", "2", "Phòng họp"],
    ["3", `<strong>Băng keo trong 5cm 100y</strong><span class="subline">VPP_16B403E8EA58</span>`, "Băng keo", "Cuộn", "5", "–"],
    ["4", `<strong>Bìa lỗ A4 Plus</strong><span class="subline">VPP_802FA6E3E8DB</span>`, "Bìa hồ sơ", "Xấp", "10", "Lưu chứng từ"],
    ["5", `<strong>Bút bi Thiên Long TL-027</strong><span class="subline">VPP_74CBE20A69F8</span>`, "Bút viết", "Cây", "8", "Mực xanh"]
  ], 18, "HISTORY");
  const summaryKpis = kpis([
    {label:"Số kỳ có dữ liệu",value:"6",icon:"calendar_month",meta:"02/2026–07/2026"},
    {label:"Tổng số đơn",value:"9",icon:"receipt_long",meta:"7 thường · 2 bổ sung"},
    {label:"Mặt hàng đã đặt",value:"42",icon:"inventory_2",meta:"Phiên bản hiện hành"},
    {label:"Tổng số lượng",value:"186",icon:"bar_chart",meta:"Sản phẩm đã đặt"}
  ]);
  const orderHeaders = ["#","Kỳ","Mã đơn","Loại đơn","Trạng thái",{label:"Mặt hàng",align:"number"},{label:"Số lượng",align:"number"},"Ngày gửi","Ghi chú"];
  return shell(screen, `<div class="history-page">${historyScope("all")}<div class="history-kpis">${summaryKpis}</div><div class="history-chart">${chart("Số lượng theo kỳ")}</div><section class="card history-orders"><div class="history-orders-toolbar"><h3>Danh sách đơn</h3><div class="history-filters">${searchBox("Tìm mã đơn hoặc ghi chú")}${selectBox("Tất cả loại đơn")}${selectBox("Tất cả trạng thái")}${button("Xóa bộ lọc","quiet","filter_alt_off")}</div></div><div class="history-orders-table">${table(orderHeaders, rows, ["4%","9%","15%","13%","12%","10%","10%","13%","14%"])}</div>${pagingFooter(`Hiển thị 1–${rows.length} trên 9 đơn · 6 kỳ có dữ liệu`, 2)}</section>${orderDetailAside({rows: detailRows})}</div>`);
}

function catalog(screen) {
  const rows = [
    [`<strong>Giấy in A4 Double A 70gsm</strong><span class="subline">VPP_7B9F34EEC2C489</span>`, "Giấy in các loại", "Ram", "Giấy in dùng cho hồ sơ và tài liệu văn phòng"],
    [`<strong>Bút bi Thiên Long TL-027</strong><span class="subline">VPP_74CBE20A69F8CE</span>`, "Bút viết", "Cây", "Bút bi mực xanh dùng hằng ngày"],
    [`<strong>File còng 7cm Kokuyo</strong><span class="subline">VPP_D942B0AF782DBB</span>`, "Bìa hồ sơ", "Cái", "Lưu trữ chứng từ và hồ sơ khổ A4"],
    [`<strong>Pin 2A Energizer</strong><span class="subline">VPP_B5DF2B25582F9B</span>`, "Phục vụ văn phòng", "Vỉ", "Pin dùng cho thiết bị văn phòng"],
    [`<strong>Băng keo trong 5cm 100y</strong><span class="subline">VPP_16B403E8EA58A2</span>`, "Băng keo", "Cuộn", "Băng keo trong dùng đóng gói"],
    [`<strong>Bìa lỗ A4 Plus</strong><span class="subline">VPP_802FA6E3E8DBD0</span>`, "Bìa hồ sơ", "Xấp", "Bìa lỗ trong dùng phân loại tài liệu"],
    [`<strong>Kẹp bướm 25mm Deli</strong><span class="subline">VPP_D8139A2B1C76E4</span>`, "Kẹp giấy", "Hộp", "Kẹp cố định bộ hồ sơ"],
    [`<strong>Sổ lò xo A5 Campus</strong><span class="subline">VPP_A5C19F622B48D1</span>`, "Sổ tay", "Quyển", "Sổ ghi chép công việc khổ A5"],
    [`<strong>Bút dạ quang Stabilo</strong><span class="subline">VPP_3F907B82CDA151</span>`, "Bút viết", "Cây", "Bút đánh dấu nội dung tài liệu"],
    [`<strong>Khay hồ sơ 3 tầng</strong><span class="subline">VPP_9C4D77A13E20B8</span>`, "Lưu trữ", "Bộ", "Khay phân loại hồ sơ trên bàn làm việc"]
  ];
  return shell(screen, collectionWorkspace({
    content: tableCard("286 mặt hàng có thể đặt", "Danh mục chỉ đọc; tìm kiếm, lọc và sắp xếp trên toàn bộ dữ liệu được cấp quyền", ["Mặt hàng","Danh mục","Đơn vị","Mô tả"], rows, {className:"catalog-readonly-list",headerAction:filterBar({search:"Tìm mã hoặc tên mặt hàng",filters:["Tất cả danh mục","Tất cả đơn vị"],action:button("Xóa bộ lọc","quiet","filter_alt_off")}),widths:["37%","22%","11%","30%"],summary:"Hiển thị 1–10 trên 286 mặt hàng",pageCount:3})
  }));
}

function management(screen, all = false) {
  if (!all) {
    const rows = [
      ["1", "07/2026", "DEMO-PPJ-0726", "Nguyễn An Nam", badge("Đơn thường", "blue"), badge("Đã gửi", "blue"), "23/07/2026", "Vật tư tháng 7"],
      ["2", "07/2026", "SUP-THU-0726", "Nguyễn Thanh Thủy", badge("Đơn bổ sung", "amber"), badge("Đã gửi", "blue"), "22/07/2026", "Bổ sung phòng họp"],
      ["3", "07/2026", "DEMO-MINH-0726", "Phạm Tuấn Minh", badge("Đơn thường", "blue"), badge("Nháp", "amber"), "–", "Vật tư dự án"],
      ["4", "07/2026", "DEMO-HUNG-0726", "Lê Việt Hùng", badge("Đơn thường", "blue"), badge("Đã gửi", "blue"), "21/07/2026", "Thiết bị nhóm"],
      ["5", "07/2026", "DEMO-LAN-0726", "Nguyễn Thu Lan", badge("Đơn thường", "blue"), badge("Chưa tạo", "red"), "–", "–"]
    ];
    const detailRows = expandIndexedRows([
      ["1", `<strong>Giấy note vàng 3×3 – UNC</strong><span class="subline">VPP_B345DC72BD6A</span>`, "Giấy note", "Xấp", "3", "Dùng tại phòng họp"],
      ["2", `<strong>Pin đồng hồ Pin 2A Energizer</strong><span class="subline">VPP_B5DF2B25582F</span>`, "Phục vụ văn phòng", "Vỉ", "2", "Phòng họp"],
      ["3", `<strong>Băng keo trong 5cm 100y</strong><span class="subline">VPP_16B403E8EA58</span>`, "Băng keo", "Cuộn", "5", "–"],
      ["4", `<strong>Bìa lỗ A4 Plus</strong><span class="subline">VPP_802FA6E3E8DB</span>`, "Bìa hồ sơ", "Xấp", "10", "Lưu chứng từ"],
      ["5", `<strong>Bút bi Thiên Long TL-027</strong><span class="subline">VPP_74CBE20A69F8</span>`, "Bút viết", "Cây", "8", "Mực xanh"]
    ], 18, "DEPARTMENT");
    const summaryKpis = kpis([
      {label:"Nhân sự",value:"5",icon:"group",meta:"4 đã gửi · 1 chưa tạo"},
      {label:"Đơn hàng",value:"9",icon:"receipt_long",meta:"7 thường · 2 bổ sung"},
      {label:"Mặt hàng",value:"42",icon:"inventory_2",meta:"Không trùng lặp"},
      {label:"Tổng số lượng",value:"186",icon:"bar_chart",meta:"Kỳ 07/2026"}
    ]);
    const orderHeaders = [{label:"#",align:"center"}, {label:"Kỳ",align:"center"}, "Mã đơn", "Họ tên", {label:"Loại đơn",align:"center"}, {label:"Trạng thái",align:"center"}, {label:"Ngày gửi",align:"center"}, "Ghi chú"];
    const departmentChart = chart("Số lượng theo nhân sự", {
      subtitle: "Đối chiếu mặt hàng và tổng số lượng trong kỳ 07/2026",
      labels: ["An Nam", "T. Thủy", "T. Minh", "V. Hùng", "T. Lan"],
      bars: [[126, 48], [94, 36], [62, 24], [112, 42], [18, 8]],
      primaryLegend: "Số lượng",
      secondaryLegend: "Mặt hàng"
    });
    return shell(screen, `<div class="history-page department-history-page">${historyScope("current")}<div class="history-kpis">${summaryKpis}</div><div class="history-chart">${departmentChart}</div><section class="card history-orders management-orders"><div class="history-orders-toolbar"><h3>Danh sách đơn</h3><div class="history-filters">${searchBox("Tìm mã đơn hoặc họ tên")}${selectBox("Tất cả loại đơn")}${selectBox("Tất cả trạng thái")}${button("Xóa bộ lọc","quiet","filter_alt_off")}</div></div><div class="history-orders-table management-orders-table">${table(orderHeaders, rows, ["4%","9%","17%","18%","14%","12%","13%","13%"])}</div>${pagingFooter(`Hiển thị 1–${rows.length} trên 9 đơn · 5 nhân sự`, 2)}</section>${orderDetailAside({rows:detailRows, fields:[["Kỳ","07/2026"],["Loại đơn",badge("Đơn thường","blue")],["Người đặt","Nguyễn An Nam"],["Phòng ban","Công nghệ thông tin"]]})}</div>`);
  }
  const rows = [
    ["1", "07/2026", "DEMO-PPJ-0726", "Nguyễn An Nam", "Công nghệ thông tin", badge("Đơn thường", "blue"), badge("Đã gửi", "blue"), "23/07/2026"],
    ["2", "07/2026", "SUP-HR-0726", "Nguyễn Bảo Trâm", "Nhân sự", badge("Đơn bổ sung", "amber"), badge("Đã gửi", "blue"), "22/07/2026"],
    ["3", "07/2026", "DEMO-FA-0726", "Lê Minh Châu", "Tài chính – Kế toán", badge("Đơn thường", "blue"), badge("Đã duyệt", "green"), "21/07/2026"],
    ["4", "07/2026", "DEMO-OPS-0726", "Phạm Hoàng Sơn", "Vận hành", badge("Đơn thường", "blue"), badge("Nháp", "amber"), "–"],
    ["5", "07/2026", "DEMO-MKT-0726", "Vũ Thanh Hà", "Marketing", badge("Đơn thường", "blue"), badge("Đã gửi", "blue"), "20/07/2026"]
  ];
  const detailRows = expandIndexedRows([
    ["1", `<strong>Giấy in A4 Double A 70gsm</strong><span class="subline">VPP_7B9F34EEC2C489</span>`, "Giấy in", "Ram", "5", "Hồ sơ dự án"],
    ["2", `<strong>Bút bi Thiên Long TL-027</strong><span class="subline">VPP_74CBE20A69F8</span>`, "Bút viết", "Cây", "8", "Mực xanh"],
    ["3", `<strong>File còng 7cm Kokuyo</strong><span class="subline">VPP_D942B0AF782D</span>`, "Bìa hồ sơ", "Cái", "4", "Lưu hồ sơ"],
    ["4", `<strong>Pin đồng hồ Pin 2A Energizer</strong><span class="subline">VPP_B5DF2B25582F</span>`, "Thiết bị", "Vỉ", "6", "Phòng họp"],
    ["5", `<strong>Băng keo trong 5cm 100y</strong><span class="subline">VPP_16B403E8EA58</span>`, "Băng keo", "Cuộn", "7", "–"]
  ], 18, "COMPANY");
  const summaryKpis = kpis([
    {label:"Phòng ban",value:"9",icon:"apartment",meta:"7 đã gửi · 2 đang xử lý"},
    {label:"Tổng số đơn",value:"86",icon:"receipt_long",meta:"Theo phiên bản hiện hành"},
    {label:"Mặt hàng",value:"286",icon:"inventory_2",meta:"Theo thư viện đang áp dụng"},
    {label:"Tổng số lượng",value:"4.820",icon:"bar_chart",meta:"Tăng 8,2% so với kỳ trước"}
  ]);
  const orderHeaders = [{label:"#",align:"center"}, {label:"Kỳ",align:"center"}, "Mã đơn", "Họ tên", "Phòng ban", {label:"Loại đơn",align:"center"}, {label:"Trạng thái",align:"center"}, {label:"Ngày gửi",align:"center"}];
  const companyChart = chart("Số lượng theo phòng ban", {
    subtitle: "So sánh mặt hàng và tổng số lượng trong kỳ 07/2026",
    labels: ["IT", "HR", "FA", "OPS", "MKT"],
    bars: [[118, 44], [82, 34], [96, 38], [148, 58], [72, 30]],
    primaryLegend: "Số lượng",
    secondaryLegend: "Mặt hàng"
  });
  return shell(screen, `<div class="history-page company-history-page">${historyScope("current")}<div class="history-kpis">${summaryKpis}</div><div class="history-chart">${companyChart}</div><section class="card history-orders management-orders"><div class="history-orders-toolbar"><h3>Danh sách đơn</h3><div class="history-filters">${searchBox("Tìm mã đơn hoặc họ tên")}${selectBox("Tất cả phòng ban")}${selectBox("Tất cả loại đơn")}${selectBox("Tất cả trạng thái")}${button("Xóa bộ lọc","quiet","filter_alt_off")}</div></div><div class="history-orders-table management-orders-table">${table(orderHeaders, rows, ["4%","9%","15%","15%","18%","13%","12%","14%"])}</div>${pagingFooter(`Hiển thị 1–${rows.length} trên 86 đơn · 9 phòng ban`, 3)}</section>${orderDetailAside({rows:detailRows, fields:[["Kỳ","07/2026"],["Loại đơn",badge("Đơn thường","blue")],["Người đặt","Nguyễn An Nam"],["Phòng ban","Công nghệ thông tin"]]})}</div>`);
}

function approval(screen) {
  const queueSeedRows = [
    ["SUP-0726-014", "Nguyễn Thanh Thủy", "IT", "12", "48", "Thiếu vật tư dự án", badge("Chờ duyệt", "amber")],
    ["SUP-0726-012", "Lê Việt Hùng", "IT", "2", "8", "Bổ sung phòng họp", badge("Chờ duyệt", "amber")],
    ["SUP-0726-009", "Trần Hoài An", "HR", "4", "16", "Nhân sự mới", badge("Chờ duyệt", "amber")],
    ["SUP-0726-006", "Phạm Minh Anh", "OPS", "5", "27", "Mở rộng ca trực", badge("Cần làm rõ", "red")]
  ];
  const rows = Array.from({ length: 18 }, (_, index) => {
    const row = [...queueSeedRows[index % queueSeedRows.length]];
    if (index >= queueSeedRows.length) row[0] = `SUP-0726-${String(index + 15).padStart(3, "0")}`;
    return row;
  });
  const detailRows = expandIndexedRows([
    ["1", `<strong>Giấy in A4 Double A 70gsm</strong><span class="subline">VPP_7B9F34EEC2C489</span>`, "Giấy in", "Ram", "5", "Dùng cho nhóm dự án"],
    ["2", `<strong>Bút bi Thiên Long TL-027</strong><span class="subline">VPP_74CBE20A69F8</span>`, "Bút viết", "Cây", "4", "Mực xanh"],
    ["3", `<strong>File còng 7cm Kokuyo</strong><span class="subline">VPP_D942B0AF782D</span>`, "Bìa hồ sơ", "Cái", "3", "Lưu hồ sơ dự án"]
  ], 12, "APPROVAL");
  return shell(screen, masterDetailWorkspace({
    filters: filterBar({search:"Tìm mã đơn hoặc người đặt",filters:["Kỳ 07/2026","Tất cả phòng ban","Chờ duyệt"]}),
    list: tableCard("Danh sách cần xử lý", "Sắp xếp theo thời gian chờ lâu nhất", ["Mã đơn","Người đặt","Phòng",{label:"Mặt hàng",align:"number"},{label:"Số lượng",align:"number"},"Ghi chú","Trạng thái"], rows.slice(0, 5), {className:"approval-queue-list",widths:["17%","18%","8%","10%","10%","22%","15%"],summary:"Hiển thị 1–5 trên 18 đơn đang chờ",pageCount:4}),
    detail: approvalOrderDetail({
      code: "SUP-0726-014",
      meta: "Tạo lúc 08:30 · 23/07/2026",
      fields: [["Kỳ","07/2026"],["Loại đơn",badge("Đơn bổ sung","amber")],["Người đặt","Nguyễn Thanh Thủy"],["Phòng ban","Công nghệ thông tin · IT"]],
      rows: detailRows,
      quantity: "48",
      note: "Thiếu vật tư dự án",
      supportingMeta: "Hạn mức còn lại: 38 sản phẩm · Đã chờ 4 giờ 12 phút",
      actions: `${button("Từ chối", "danger", "close")}${button("Phê duyệt", "primary", "check")}`
    }),
    variant: "approval-workspace"
  }));
}

function periodReview(screen) {
  const blockerRows = [
    ["SUP-0726-014","Nguyễn Thanh Thủy","IT","Thiếu vật tư dự án",badge("Chờ quản lý xử lý","amber")],
    ["SUP-0726-006","Phạm Minh Anh","OPS","Mở rộng ca trực",badge("Cần làm rõ","red")]
  ];
  return shell(screen, `<div class="page-stack period-operation-workspace">${periodFlowNav("review")}<section class="period-hero is-blocked"><div><div class="period-eyebrow">Kỳ 07/2026 · Còn nhận đơn</div><div class="period-title">Chưa thể chốt kỳ</div><div class="period-meta">7/9 phòng ban hoàn tất · còn 2 đơn bổ sung cần quản lý xử lý</div></div><div class="period-hero-actions">${linkButton("supplement-approval", "Duyệt đơn bổ sung", "", "fact_check")}${button("Chốt kỳ", "disabled", "lock")}</div></section><div class="split-layout period-review-layout"><section class="card"><div class="card-header"><div><h3>Điều kiện trước khi gom đơn</h3><p>Quản lý rà soát toàn bộ điều kiện trước khi chốt kỳ</p></div></div><div class="readiness-list"><div class="readiness-row">${icon("check_circle")}<div><p>Phiên bản đơn hiện hành hợp lệ</p><small>86 đơn đã được kiểm tra theo phiên bản mới nhất</small></div>${badge("Đạt", "green")}</div><div class="readiness-row">${icon("warning")}<div><p>Còn phòng ban chưa hoàn tất</p><small>OPS và LEGAL chưa gửi đơn cuối cùng</small></div>${badge("Cảnh báo", "amber")}</div><div class="readiness-row">${icon("error")}<div><p>Đơn bổ sung chưa được xử lý</p><small>Quản lý có quyền duyệt hoặc từ chối đơn bổ sung</small></div>${badge("Chặn", "red")}</div><div class="readiness-row">${icon("schedule")}<div><p>Chờ hết hạn nhận đơn</p><small>Chưa thể lưu kết quả khi dữ liệu vẫn có thể thay đổi</small></div>${badge("14 ngày", "blue")}</div></div></section>${tableCard("Điều kiện ngăn chốt kỳ đang theo dõi","Mở màn hình duyệt đơn bổ sung để xử lý",["Mã đơn","Người đặt","Phòng","Ghi chú","Trạng thái"],blockerRows,{className:"period-blocker-list",summary:"Hiển thị 1–2 trên 2 điều kiện",pageCount:1})}</div></div>`);
}

function periodDemand(screen) {
  const orderRows = [
    ["IT","Công nghệ thông tin","18","122",badge("Hoàn tất","green"),"23/07/2026"],
    ["HR","Nhân sự","12","87",badge("Hoàn tất","green"),"22/07/2026"],
    ["FA","Tài chính – Kế toán","15","96",badge("Hoàn tất","green"),"22/07/2026"],
    ["OPS","Vận hành","0","0",badge("Loại khỏi bản gom","red"),"—"],
    ["LEGAL","Pháp chế","0","0",badge("Loại khỏi bản gom","red"),"—"]
  ];
  const demandRows = expandIndexedRows([
    ["1",`<strong>Giấy in A4 Double A 70gsm</strong><span class="subline">VPP_7B9F34EEC2C489</span>`,"Ram","146",badge("Đủ dữ liệu","green")],
    ["2",`<strong>Bút bi Thiên Long TL-027</strong><span class="subline">VPP_74CBE20A69F8</span>`,"Cây","284",badge("Đủ dữ liệu","green")],
    ["3",`<strong>Pin 2A Energizer</strong><span class="subline">VPP_B5DF2B25582F</span>`,"Vỉ","42",badge("Cần kiểm tra","amber")]
  ],18,"DEMAND",{ name: 1, unit: 2 }).map((row, index) => {
    if (index < 3) return row;
    // Số lượng và trạng thái của dòng sinh thêm không kế thừa theo chu kỳ seed
    // để "Cần kiểm tra" không lặp máy móc trên 1/3 danh sách.
    row[3] = String(12 + ((index * 11) % 220));
    row[4] = index % 7 === 5 ? badge("Cần kiểm tra", "amber") : badge("Đủ dữ liệu", "green");
    return row;
  });
  return shell(screen, `<div class="page-stack period-operation-workspace">${periodFlowNav("demand")}${filterBar({search:"Tìm phòng ban hoặc mã đơn",filters:["Kỳ 07/2026","Đơn hợp lệ hiện hành","Tất cả phòng ban"],action:linkButton("supply-allocation","Tiếp tục chọn nguồn cung","primary","arrow_forward")})}${kpis([{label:"Đơn hợp lệ",value:"86",icon:"receipt_long",meta:"7/9 phòng ban được gom"},{label:"Mặt hàng duy nhất",value:"286",icon:"inventory_2",meta:"Đã gộp không trùng"},{label:"Tổng số lượng",value:"4.820",icon:"stacked_bar_chart",meta:"Đơn thường + bổ sung đã duyệt"},{label:"Điều kiện ngăn chốt",value:"0",icon:"task_alt",meta:"2 phòng không có đơn bị loại"}])}<div class="split-layout period-demand-layout">${tableCard("Đơn toàn công ty","Chỉ phiên bản hợp lệ được đưa vào dữ liệu tổng hợp kỳ 07/2026",["Mã phòng","Phòng ban",{label:"Mặt hàng",align:"number"},{label:"Số lượng",align:"number"},"Trạng thái","Ngày gửi"],orderRows,{summary:"Hiển thị 1–5 trên 9 phòng ban",pageCount:2})}${virtualizedTableCard("Tổng nhu cầu tạm tính","Gom từ phiên bản hợp lệ mới nhất",["#","Mặt hàng","Đơn vị",{label:"Số lượng",align:"number"},"Kiểm tra"],demandRows,{className:"period-demand-items",summary:"Tổng cộng 286 mặt hàng"})}</div></div>`);
}

function supplyAllocation(screen) {
  // Hai mặt hàng đầu đã được xử lý ngoại lệ (khớp panel bên phải) nên không còn
  // mang trạng thái "Thiếu giá"; các dòng "Thiếu giá" sinh thêm đại diện cho
  // 12 mặt hàng còn lại trong tổng 14 mặt hàng cần ngoại lệ.
  const sourceRows = expandIndexedRows([
    ["1",`<strong>Giấy in A4 Double A 70gsm</strong><span class="subline">VPP_7B9F34EEC2C489</span>`,"146","145.000 ₫",badge("Có giá","green")],
    ["2",`<strong>Bút bi Thiên Long TL-027</strong><span class="subline">VPP_74CBE20A69F8</span>`,"284","4.500 ₫",badge("Có giá","green")],
    ["3",`<strong>Pin 2A Energizer</strong><span class="subline">VPP_B5DF2B25582F</span>`,"42","98.000 ₫",badge("Ngoại lệ","amber")],
    ["4",`<strong>Kẹp bướm 32mm Deli</strong><span class="subline">VPP_D51C0B7A9E24</span>`,"36","21.500 ₫",badge("Ngoại lệ","amber")],
    ["5",`<strong>Giấy note vàng 3×3 – UNC</strong><span class="subline">VPP_B345DC72BD6A</span>`,"128","7.800 ₫",badge("Có giá","green")],
    ["6",`<strong>Băng keo trong 5cm 100y</strong><span class="subline">VPP_16B403E8EA58</span>`,"64","—",badge("Thiếu giá","red")]
  ],18,"SOURCE",{ name: 1 }).map((row, index) => {
    if (index < 6) return row;
    // Dòng sinh thêm chỉ mang "Có giá" hoặc "Thiếu giá"; trạng thái "Ngoại lệ"
    // chỉ thuộc về đúng hai mặt hàng đã khai báo trong panel bên phải.
    const missingPrice = (index + 1) % 5 === 0;
    row[2] = String(24 + ((index * 7) % 90));
    row[3] = missingPrice ? "—" : `${18 + ((index * 13) % 160)}.000 ₫`;
    row[4] = missingPrice ? badge("Thiếu giá", "red") : badge("Có giá", "green");
    return row;
  });
  const exceptionRows = [
    ["Pin 2A Energizer","Minh Hải","PL-MH-2026-H2","Không có trong bảng giá chính"],
    ["Kẹp bướm 32mm Deli","Hồng Phúc","PL-HP-2026-Q3","Nhà cung cấp chính hết hàng"]
  ];
  return shell(screen, `<div class="page-stack period-operation-workspace supply-allocation-workspace">${periodFlowNav("source")}<section class="card source-selection-card"><div class="source-selection-copy"><h3>Nguồn cung chính</h3><p>Chọn nhà cung cấp trước; bảng giá chỉ hiển thị phiên bản còn hiệu lực của nhà cung cấp đó.</p></div><button class="source-select-control" type="button" data-supplier-selector data-suppliers="An Phát Office|Minh Hải Stationery|Hồng Phúc Trading" data-price-lists="PL-2026-H2|PL-MH-2026-H2|PL-HP-2026-Q3" data-coverages="272/286|279/286|268/286"><span><small>Nhà cung cấp</small><strong data-supplier-value>An Phát Office</strong></span>${icon("expand_more")}</button><div class="source-select-control is-readonly"><span><small>Bảng giá còn hiệu lực</small><strong data-price-list-value>PL-2026-H2</strong></span>${icon("lock")}</div><div class="source-coverage"><small>Độ phủ bảng giá</small><strong data-source-coverage>272/286</strong><span>14 mặt hàng cần ngoại lệ</span></div></section><div class="split-layout source-allocation-layout">${virtualizedTableCard("Đối chiếu giá theo nhu cầu","Bảng giá được lọc theo nhà cung cấp chính",["#","Mặt hàng",{label:"Số lượng",align:"number"},{label:"Đơn giá",align:"number"},"Kết quả"],sourceRows,{className:"source-price-coverage",widths:["6%","44%","14%","20%","16%"],summary:"Tổng cộng 286 mặt hàng cần đối chiếu"})}<section class="card source-exception-panel"><div class="card-header"><div><h3>Ngoại lệ nguồn cung</h3><p>Mỗi mặt hàng bắt buộc chọn nguồn thay thế và nhập lý do</p></div>${badge("2 ngoại lệ","amber")}</div><div class="exception-list">${exceptionRows.map(([item,supplier,priceList,reason])=>`<article class="exception-row"><strong>${item}</strong><div><span>Nhà cung cấp</span><b>${supplier}</b></div><div><span>Bảng giá</span><b>${priceList}</b></div><label>Lý do<span class="input">${reason}</span></label><small>${icon("history")} Lưu người chọn, thời điểm và thay đổi vào nhật ký</small></article>`).join("")}</div><div class="inspector-actions">${linkButton("period-demand","Quay lại gom nhu cầu","quiet","arrow_back")}${linkButton("settlement-flow","Kiểm tra và chốt","primary","arrow_forward")}</div></section></div></div>`);
}

function settlement(screen) {
  const settlementRows = [
    ["An Phát Office","PL-2026-H2","272","4.702","76.840.000 ₫"],
    ["Minh Hải Stationery","PL-MH-2026-H2","1","42","1.764.000 ₫"],
    ["Hồng Phúc Trading","PL-HP-2026-Q3","1","76","920.000 ₫"]
  ];
  return shell(screen, `<div class="page-stack period-operation-workspace settlement-workspace">${periodFlowNav("settle")}${kpis([{label:"Mặt hàng",value:"286",icon:"inventory_2",meta:"Đủ 100% nguồn giá"},{label:"Tổng số lượng",value:"4.820",icon:"stacked_bar_chart",meta:"Từ 86 đơn hợp lệ"},{label:"Ngoại lệ",value:"2",icon:"call_split",meta:"Đều có lý do và nhật ký"},{label:"Tạm tính",value:"79,5M",icon:"payments",meta:"Đã gồm VAT theo bảng giá"}])}<div class="split-layout settlement-layout">${tableCard("Phân bổ nguồn cung","Xem trước dữ liệu sẽ được lưu tại thời điểm chốt kỳ",["Nhà cung cấp","Bảng giá",{label:"Mặt hàng",align:"number"},{label:"Số lượng",align:"number"},{label:"Tạm tính",align:"number"}],settlementRows,{summary:"Hiển thị 1–3 trên 3 nguồn cung",pageCount:1})}<section class="card settlement-confirmation"><div class="card-header"><div><h3>Xác nhận chốt kỳ 07/2026</h3><p>Lưu kết quả chốt kỳ và khóa phiên bản đơn hiện hành</p></div>${badge("Sẵn sàng","green")}</div><div class="card-body"><div class="account-alert">${icon("lock")}<span>Sau khi xác nhận, đơn trong kỳ chuyển sang chỉ xem. Nguồn cung, bảng giá, ngoại lệ và tổng tiền đã lưu không tự thay đổi theo dữ liệu danh mục sau này.</span></div><div class="readiness-list compact"><div class="readiness-row">${icon("check_circle")}<div><p>Đơn hợp lệ đã được gom</p><small>86 phiên bản hiện hành</small></div>${badge("Đạt","green")}</div><div class="readiness-row">${icon("check_circle")}<div><p>Độ phủ giá đạt 100%</p><small>Nhà cung cấp chính + 2 ngoại lệ</small></div>${badge("Đạt","green")}</div><div class="readiness-row">${icon("check_circle")}<div><p>Nhật ký ngoại lệ đầy đủ</p><small>Có lý do, người chọn và bảng giá nguồn</small></div>${badge("Đạt","green")}</div></div><div class="form-group settlement-reason"><label>Ghi chú chốt kỳ</label><div class="input">Đã hoàn tất rà soát nguồn cung và giá áp dụng</div></div></div><div class="inspector-actions">${linkButton("supply-allocation","Quay lại nguồn cung","quiet","arrow_back")}${button("Xác nhận và chốt kỳ","primary","lock")}</div></section></div></div>`);
}

const libraryDefinitions = {
  "library-classes": { search:"Tìm mã hoặc tên loại", title:"Loại danh mục", subtitle:"Định nghĩa nhóm giá trị dùng chung trong hệ thống", total:1, noun:"loại danh mục", headers:["Mã loại","Tên loại","Mô-đun","Giá trị","Trạng thái"], rows:[["Uom","Đơn vị tính","VPP","12",badge("Hoạt động","green")]]},
  "library-categories": { search:"Tìm mã hoặc tên danh mục", title:"Danh mục", subtitle:"Nhóm phân loại trực tiếp của mặt hàng", total:18, noun:"danh mục", headers:["Mã","Tên danh mục","Mặt hàng","Trạng thái"], rows:[["PAPER","Giấy in các loại","32",badge("Hoạt động","green")],["PEN","Bút viết","48",badge("Hoạt động","green")],["FILE","Bìa và hồ sơ","39",badge("Hoạt động","green")],["TAPE","Băng keo, bấm kim","24",badge("Hoạt động","green")],["BATTERY","Pin và thiết bị nhỏ","15",badge("Ngừng áp dụng","red")]]},
  "library-items": { search:"Tìm mã hoặc tên mặt hàng", title:"Mặt hàng", subtitle:"Dữ liệu mặt hàng dùng cho danh mục và đơn đặt", total:286, noun:"mặt hàng", headers:["Mã mặt hàng","Tên mặt hàng","Danh mục","Đơn vị","Trạng thái"], rows:[["VPP_7B9F34EE","Giấy in A4 Double A 70gsm","Giấy in","Ram",badge("Hoạt động","green")],["VPP_74CBE20A","Bút bi Thiên Long TL-027","Bút viết","Cây",badge("Hoạt động","green")],["VPP_D942B0AF","File còng 7cm Kokuyo","Bìa hồ sơ","Cái",badge("Hoạt động","green")],["VPP_B5DF2B25","Pin 2A Energizer","Thiết bị","Vỉ",badge("Ngừng áp dụng","red")],["VPP_16B403E8","Băng keo trong 5cm 100y","Băng keo","Cuộn",badge("Hoạt động","green")]]},
  "library-suppliers": { search:"Tìm tên viết tắt hoặc nhà cung cấp", title:"Nhà cung cấp", subtitle:"Đối tác cung ứng và thông tin địa chỉ", total:5, noun:"nhà cung cấp", headers:["Tên viết tắt","Tên nhà cung cấp","Tỉnh/Thành phố","Mặt hàng","Trạng thái"], rows:[["ANPHAT","An Phát Office","TP. Hồ Chí Minh","186",badge("Hoạt động","green")],["MINHHAI","Minh Hải Stationery","Hà Nội","92",badge("Hoạt động","green")],["HONGPHUC","Hồng Phúc Trading","Đà Nẵng","74",badge("Hoạt động","green")],["VIETPHUONG","Việt Phương","TP. Hồ Chí Minh","41",badge("Hoạt động","green")],["VP24H","Văn phòng 24h","Hà Nội","0",badge("Ngừng áp dụng","red")]]},
  "library-price-lists": { search:"Tìm mã bảng giá", title:"Danh sách bảng giá", subtitle:"Các phiên bản giá theo nhà cung cấp và thời gian hiệu lực", total:4, noun:"bảng giá", headers:["Mã bảng giá","Tên bảng giá","Nhà cung cấp","Hiệu lực","Mặt hàng","Trạng thái"], rows:[["PL-2026-H2","Bảng giá nửa cuối 2026","An Phát Office","01/07–31/12","286",badge("Đã công bố","green")],["PL-2026-Q2","Bảng giá quý II/2026","Minh Hải","01/04–30/06","274",badge("Hết hiệu lực","amber")],["PL-2026-Q1","Bảng giá quý I/2026","An Phát Office","01/01–31/03","268",badge("Hết hiệu lực","amber")],["PL-DRAFT-03","Bảng giá dự thảo 03","Hồng Phúc","Dự kiến 01/08","120",badge("Bản nháp","blue")]]},
  "library-prices": { search:"Tìm mặt hàng trong bảng giá", title:"Giá mặt hàng", subtitle:"PL-2026-H2 · An Phát Office", total:286, noun:"mức giá", headers:["Mã mặt hàng","Tên mặt hàng","Đơn vị",{label:"Đơn giá",align:"number"},"Thuế","Hiệu lực"], rows:[["VPP_7B9F34EE","Giấy in A4 Double A 70gsm","Ram","145.000 ₫","8%","31/12/2026"],["VPP_74CBE20A","Bút bi Thiên Long TL-027","Cây","4.500 ₫","8%","31/12/2026"],["VPP_D942B0AF","File còng 7cm Kokuyo","Cái","68.000 ₫","8%","31/12/2026"],["VPP_B5DF2B25","Pin 2A Energizer","Vỉ","42.000 ₫","10%","31/12/2026"],["VPP_16B403E8","Băng keo trong 5cm 100y","Cuộn","18.000 ₫","8%","31/12/2026"]]},
  "library-departments": { search:"Tìm mã hoặc tên phòng ban", title:"Phòng ban", subtitle:"Phạm vi phòng ban dùng cho đơn yêu cầu và báo cáo", total:9, noun:"phòng ban", headers:["Mã","Tên phòng ban","Phòng ban cấp trên","Thành viên","Trạng thái"], rows:[["IT","Công nghệ thông tin","—","18",badge("Hoạt động","green")],["HR","Nhân sự","—","12",badge("Hoạt động","green")],["FA","Tài chính – Kế toán","—","15",badge("Hoạt động","green")],["OPS","Vận hành","—","24",badge("Hoạt động","green")],["MKT","Marketing","—","10",badge("Hoạt động","green")]]}
};

function adminTableModel(headers, rows, noun) {
  const scopedHeaders = headers.map((header, index) => ({
    ...(typeof header === "string" ? {label:header} : header),
    scope: index < 2 ? "identity" : "default"
  }));
  scopedHeaders.push(
    {label:"Mô tả nghiệp vụ",scope:"business"},
    {label:"Ngày tạo",scope:"audit"},
    {label:"Cập nhật bởi",scope:"audit"},
    {label:"ID",scope:"technical"},
    {label:"RowVersion",scope:"technical"},
    {label:"Thao tác",align:"center",scope:"action"}
  );
  const scopedRows = rows.map((row,index) => [
    ...row,
    `${noun.charAt(0).toUpperCase()}${noun.slice(1)} dùng trong dữ liệu vận hành`,
    `${String(index + 1).padStart(2,"0")}/07/2026`,
    index === 0 ? "Nguyễn An Nam" : "System seed",
    `<span class="mono">${String(index + 1).padStart(4,"0")}-7b9f34ee…</span>`,
    `<span class="mono">0x${String(2001 + index).padStart(16,"0")}</span>`,
    `<button class="btn compact quiet" type="button" data-admin-row-open>${icon("chevron_right")}<span>Mở</span></button>`
  ]);
  return { headers:scopedHeaders, rows:scopedRows };
}

function library(screen) {
  const data = libraryDefinitions[screen.template];
  const selected = data.rows[0];
  const adminTable = adminTableModel(data.headers, data.rows, data.noun);
  const filters = screen.template === "library-items"
    ? ["Tất cả danh mục", "Tất cả đơn vị", "Tất cả trạng thái"]
    : screen.template === "library-prices"
      ? ["Tất cả đơn vị", "Tất cả trạng thái"]
      : ["Tất cả trạng thái"];
  const relatedAction = screen.template === "library-price-lists"
    ? linkButton("prices", "Xem giá mặt hàng", "quiet", "price_change")
    : screen.template === "library-prices"
          ? linkButton("price-lists", "Danh sách bảng giá", "quiet", "arrow_back")
      : screen.template === "library-items"
        ? linkButton("prices", "Xem giá mặt hàng", "quiet", "payments")
        : screen.template === "library-suppliers"
          ? linkButton("price-lists", "Xem bảng giá", "quiet", "sell")
          : screen.template === "library-departments"
            ? linkButton("period-demand", "Xem nhu cầu toàn công ty", "quiet", "table_view")
            : "";
  const detailStatus = screen.template === "library-price-lists"
    ? badge("Đã công bố", "green")
    : badge("Hoạt động", "green");
  const detailSource = screen.template === "library-price-lists"
    ? "Quản lý bảng giá"
    : "Danh mục hệ thống";
  const terminalAction = screen.template === "library-price-lists"
    ? button("Đánh dấu hết hiệu lực", "danger", "event_busy")
    : softDeleteButton();
  return shell(screen, masterDetailWorkspace({
    filters: filterBar({search:data.search,filters,action:`${columnPresetControl()}${drawerButton(`Thêm ${data.title.toLowerCase()}`,"primary","add","create")}`}),
    list: tableCard(data.title,data.subtitle,adminTable.headers,adminTable.rows,{className:"admin-entity-list",scrollable:true,adminGrid:true,summary:`Hiển thị 1–${data.rows.length} trên ${data.total} ${data.noun}`,pageCount:data.total > data.rows.length ? 3 : 1}),
    detail: adminInspector(String(selected[1]), String(selected[0]), [["Trạng thái",detailStatus],["Nguồn dữ liệu",detailSource],["Ngày tạo","01/07/2026"],["Cập nhật","23/07/2026"],["Người cập nhật","Nguyễn An Nam"]], `${drawerButton("Chỉnh sửa", "", "edit")}${relatedAction || button("Xem lịch sử", "quiet", "history")}${terminalAction}`),
    variant: "library-workspace admin-entity-workspace"
  }));
}

function users(screen) {
  const rows = [["annam","Nguyễn An Nam","annam@gtas.local","IT",badge("DEV","red"),badge("Hoạt động","green")],["thuynt","Nguyễn Thanh Thủy","thuynt@gtas.local","IT",badge("EMPLOYEE"),badge("Hoạt động","green")],["binhtt","Trần Thanh Bình","binhtt@gtas.local","IT",badge("MANAGER","blue"),badge("Hoạt động","green")],["linhpm","Phạm Mỹ Linh","linhpm@gtas.local","PROC",badge("MANAGER","blue"),badge("Hoạt động","green")]];
  const adminTable = adminTableModel(["Tài khoản","Họ tên","Email","Phòng","Nhóm quyền","Trạng thái"], rows, "tài khoản");
  return shell(screen, masterDetailWorkspace({
    filters: filterBar({search:"Tìm tài khoản, họ tên hoặc email",filters:["Tất cả nhóm","Tất cả trạng thái"],action:`${columnPresetControl()}${drawerButton("Thêm người dùng","primary","person_add","create")}`}),
    list: tableCard("Người dùng và nhóm quyền","Mỗi tài khoản chỉ có một nhóm quyền đang hiệu lực",adminTable.headers,adminTable.rows,{className:"admin-entity-list",scrollable:true,adminGrid:true,summary:"Hiển thị 1–4 trên 4 tài khoản",pageCount:1}),
    detail: adminInspector("Nguyễn An Nam","NGƯỜI DÙNG · ANNAM",[["Email","annam@gtas.local"],["Phòng ban","Công nghệ thông tin · IT"],["Nhóm quyền",badge("DEV","red")],["Vai trò hiển thị","Quản trị hệ thống"],["Môi trường","Phát triển / kiểm thử"],["Đăng nhập gần nhất","09:14 23/07/2026"]],`${drawerButton("Chỉnh sửa","","edit")}${button("Đặt lại mật khẩu","quiet","key")}${linkButton("permissions", "Đổi nhóm quyền", "primary", "manage_accounts")}${softDeleteButton()}`),
    variant: "access-workspace admin-entity-workspace"
  }));
}

function permissions(screen) {
  const groups = ["EMPLOYEE","MANAGER","DEV"];
  const actions = [
    ["RequestViewOwn", "Xem đơn của bản thân", [1,1,1]],
    ["RequestViewDepartment", "Xem đơn trong phòng ban", [0,1,1]],
    ["RequestViewAll", "Xem đơn toàn công ty", [0,1,1]],
    ["RequestCreate", "Tạo đơn", [1,1,1]],
    ["RequestUpdateOwn", "Sửa đơn hợp lệ của bản thân", [1,1,1]],
    ["RequestCancelOwn", "Hủy đơn hợp lệ của bản thân", [1,1,1]],
    ["RequestApprove", "Phê duyệt đơn bổ sung", [0,1,1]],
    ["RequestReject", "Từ chối đơn bổ sung", [0,1,1]],
    ["RequestCatalogView", "Xem danh mục được phép đặt", [1,1,1]],
    ["LibraryView", "Xem dữ liệu danh mục", [1,1,1]],
    ["LibraryManage", "Quản trị dữ liệu danh mục", [0,1,1]],
    ["PermissionView", "Xem quản trị truy cập", [0,0,1]],
    ["PermissionManage", "Quản lý tài khoản và nhóm quyền", [0,0,1]],
    ["ReportViewOwn", "Xem báo cáo của bản thân", [1,1,1]],
    ["ReportViewDepartment", "Xem báo cáo phòng ban", [0,1,1]],
    ["ReportViewAll", "Xem báo cáo toàn công ty", [0,1,1]],
    ["ReportExport", "Xuất báo cáo", [0,1,1]],
    ["PeriodSettle", "Rà soát và chốt kỳ", [0,1,1]]
  ];
  return shell(screen, `<div class="page-stack permissions-page"><section class="card permissions-card"><div class="card-header"><div><h3>Ma trận quyền thao tác</h3><p>Ba vai trò thống nhất: Nhân viên, Quản lý và Quản trị hệ thống</p></div><div class="page-actions">${linkButton("users", "Quản lý người dùng", "quiet", "group")}${button("Xem nhật ký","", "history")}${button("Lưu ma trận quyền","primary","save")}</div></div><div class="permission-matrix-scroll virtualized-grid" data-grid-mode="scroll-virtualized"><table class="permission-matrix"><thead><tr><th>Quyền thao tác</th>${groups.map(group=>`<th class="${group === "DEV" ? "developer-owner-column" : ""}">${group}<span class="subline">${roleLabels[group]}</span></th>`).join("")}</tr></thead><tbody>${actions.map(([code, name, grants])=>`<tr><td><strong>${name}</strong><span class="subline">${code}</span></td>${grants.map((value,index)=>`<td class="${groups[index] === "DEV" ? "developer-owner-column" : ""}">${value?`<span class="permission-check">${icon("check")}</span>`:`<span class="permission-none">—</span>`}</td>`).join("")}</tr>`).join("")}</tbody></table></div>${virtualizedFooter("Tổng cộng 18 quyền thao tác · 3 vai trò cố định")}</section><div class="two-column permission-notes"><section class="card"><div class="card-header"><div><h3>DEV · Quản trị hệ thống</h3><p>Có toàn bộ quyền giao diện để quản trị và kiểm thử hệ thống</p></div>${badge("Toàn quyền","blue")}</div><div class="card-body"><div class="account-alert">${icon("security")}<span>Quyền truy cập đầy đủ vẫn phải tuân theo các điều kiện nghiệp vụ, thời hạn, giới hạn và dữ liệu đã chốt.</span></div><p class="permission-note-copy">Mọi thay đổi nhóm quyền, phân công người dùng và quyền thao tác đều lưu người thực hiện cùng thời điểm cập nhật.</p></div></section><section class="card"><div class="card-header"><div><h3>Phạm vi ba vai trò</h3><p>Quyền được trình bày theo ngôn ngữ nghiệp vụ</p></div>${badge("Đang áp dụng","green")}</div><div class="card-body"><dl class="key-values"><dt>Nhân viên</dt><dd>Tạo và theo dõi đơn của bản thân</dd><dt>Quản lý</dt><dd>Duyệt đơn bổ sung, quản lý danh mục, báo cáo và chốt kỳ</dd><dt>Quản trị hệ thống</dt><dd>Toàn bộ quyền và quản lý người dùng</dd><dt>Thay đổi gần nhất</dt><dd>Nguyễn An Nam · TEST · 09:02 23/07/2026</dd></dl></div></section></div></div>`);
}

function reports(screen) {
  const rows = [["IT","Công nghệ thông tin","18","122","18.420.000 ₫","+8,2%"],["HR","Nhân sự","12","87","10.760.000 ₫","−2,1%"],["FA","Tài chính – Kế toán","15","96","14.250.000 ₫","+3,4%"],["OPS","Vận hành","24","188","26.870.000 ₫","+12,8%"],["MKT","Marketing","10","63","9.340.000 ₫","−1,4%"]];
  return shell(screen, `<div class="page-stack">${filterBar({search:"Tìm phòng ban",filters:["Kỳ 07/2026","Toàn công ty","Phiên bản hiện hành"],action:`${linkButton("period-demand", "Xem nguồn dữ liệu kỳ", "quiet", "table_view")}${button("Xóa bộ lọc","quiet","filter_alt_off")}${button("Xuất CSV","","description")}${button("Xuất Excel","primary","table_view")}`})}${kpis([{label:"Tổng chi phí",value:"79,6M",icon:"payments",meta:"+6,4% so với 06/2026"},{label:"Tổng số lượng",value:"640",icon:"inventory_2",meta:"84 mặt hàng"},{label:"Chi phí / nhân sự",value:"895K",icon:"person",meta:"89 nhân sự đang hoạt động"},{label:"Chênh lệch giá",value:"−2,8%",icon:"trending_down",meta:"So với giá tham chiếu"}])}<div class="split-layout">${chart("Chi phí theo 6 kỳ gần nhất")}${tableCard("Chi phí theo phòng ban","Số liệu được ghi nhận khi chốt kỳ",["Mã","Phòng ban",{label:"Mặt hàng",align:"number"},{label:"Số lượng",align:"number"},{label:"Chi phí",align:"number"},{label:"So kỳ trước",align:"number"}],rows,{summary:"Hiển thị 1–5 trên 9 phòng ban",pageCount:2})}</div></div>`);
}

function states(screen) {
  return shell(screen, `<div class="page-stack">${heading("Thông báo và trạng thái hệ thống","Mẫu hiển thị hộp thư, mất kết nối, thiếu quyền, lỗi và trạng thái đang tải",badge("M8 · Mẫu đại diện","blue"))}<div class="state-grid"><article class="card state-card"><div style="width:92%"><div class="card-header" style="padding:0 0 10px;border:0"><div><h3>Hộp thư thông báo</h3><p>Thông báo nghiệp vụ được lưu theo tài khoản và dẫn tới đúng màn hình liên quan.</p></div>${badge("3 chưa đọc","blue")}</div><div class="readiness-list" style="padding:0;text-align:left"><div class="readiness-row">${icon("check_circle")}<div><p>Đơn bổ sung đã được duyệt</p><small>Đơn BS-0726-014 · 5 phút trước</small></div>${badge("Mới","blue")}</div><div class="readiness-row">${icon("schedule")}<div><p>Kỳ 07/2026 sắp hết hạn nhận đơn</p><small>Còn 02 ngày · mở màn hình rà soát kỳ</small></div>${icon("chevron_right")}</div><div class="readiness-row">${icon("price_change")}<div><p>Bảng giá VPP-2026-07 đã được công bố</p><small>Nhà cung cấp An Phát · 08:45 hôm nay</small></div>${icon("chevron_right")}</div></div></div></article><article class="card state-card"><div><span class="state-icon">${icon("cloud_sync")}</span><h3>Đang kết nối lại</h3><p>Kết nối tạm thời bị gián đoạn. Hệ thống đang tự khôi phục phiên làm việc.</p>${badge("Tự động thử lại","blue")}</div></article><article class="card state-card"><div><span class="state-icon">${icon("lock")}</span><h3>Bạn không có quyền xem nội dung này</h3><p>Tài khoản hiện tại chưa được cấp quyền chức năng cần thiết.</p>${linkButton("my-orders", "Về trang phù hợp", "", "arrow_back")}</div></article><article class="card state-card"><div><span class="state-icon" style="color:var(--atlas-danger);background:rgba(198,63,69,.08)">${icon("error")}</span><h3>Chưa thể tải dữ liệu</h3><p>Hãy kiểm tra kết nối rồi thử lại. Mã lỗi kỹ thuật chỉ được lưu trong nhật ký hệ thống.</p>${button("Thử tải lại","primary","refresh")}</div></article><article class="card state-card"><div><span class="state-icon">${icon("inbox")}</span><h3>Chưa có dữ liệu</h3><p>Giải thích vì sao danh sách đang trống và chỉ hiện nút hành động khi người dùng có quyền thực hiện.</p>${linkButton("order-create", "Tạo bản ghi đầu tiên", "primary", "add")}</div></article><article class="card state-card"><div style="width:80%"><div class="skeleton-line" style="width:42%;height:16px"></div><div class="skeleton-line" style="width:70%"></div><div class="skeleton-line" style="width:84%"></div><div class="skeleton-line" style="width:62%"></div><h3>Đang tải theo đúng bố cục</h3><p>Khung chờ giữ nguyên hình dạng nội dung sắp xuất hiện, không dùng một thanh tải chung cho toàn trang.</p></div></article></div></div>`);
}

function systemBoard(screen) {
  return shell({...screen, nav:"library", tab:"pricing"}, `<div class="system-board"><div class="system-column"><section class="card"><div class="card-header"><div><h3>Màu sắc và lớp bề mặt</h3><p>Dùng đường viền để phân lớp, nền trung tính lạnh, xanh dương và xanh ngọc của GTAS VPP</p></div></div><div class="card-body"><div class="swatches"><div class="swatch"><i style="background:#0284c7"></i>Màu chính</div><div class="swatch"><i style="background:#0f9f93"></i>Xanh ngọc</div><div class="swatch"><i style="background:#172b4d"></i>Màu chữ</div><div class="swatch"><i style="background:#f5f6f8"></i>Nền trang</div><div class="swatch"><i style="background:#fff"></i>Bề mặt</div><div class="swatch"><i style="background:#dfe3e8"></i>Đường viền</div></div></div></section><section class="card"><div class="card-header"><div><h3>Hệ thống chữ</h3><p>Phông chữ hệ thống cho thao tác thường ngày · Poppins cho tiêu đề và số liệu cần nhấn mạnh</p></div></div><div class="card-body"><div class="type-sample-display">Kỳ đặt hàng 07/2026</div><div class="type-sample-body">Đơn hàng, bảng dữ liệu và biểu mẫu dùng cỡ chữ 14/20 để đọc nhanh, rõ và ổn định.</div><div class="subline" style="margin-top:10px">DEMO-PPJ-0726 · phông chữ đơn cách dành cho mã định danh kỹ thuật</div></div></section><section class="card"><div class="card-header"><div><h3>Điều khiển và trạng thái tương tác</h3><p>Vùng bấm 40px · bo góc 7px · nền phản hồi trung tính · vạch xanh cho mục đang chọn</p></div></div><div class="card-body"><div class="interaction-demo">${button("Hành động chính","primary","check")}${button("Hành động phụ","","edit")}${button("Xóa hoặc hủy","danger","delete")}${button("Chưa khả dụng","disabled","download")}</div><div class="interaction-demo" style="margin-top:10px"><div class="nav-row active">${icon("shopping_bag")}<span class="nav-label">Đang chọn</span></div><div class="nav-row">${icon("history")}<span class="nav-label">Bình thường</span></div><div class="search-box">${icon("search")}Tìm kiếm</div><div class="select-box"><span>Chọn giá trị</span>${icon("expand_more")}</div></div></div></section></div><div class="system-column"><section class="card"><div class="card-header"><div><h3>Menu tài khoản</h3><p>Phân cấp kiểu Apple, nội dung theo nghiệp vụ GTAS VPP</p></div></div><div class="card-body" style="text-align:center"><span class="avatar" style="width:64px;height:64px;font-size:20px;margin:auto">NN</span><h3 style="margin:12px 0 3px">Nguyễn An Nam</h3><p style="margin:0;color:var(--atlas-muted);font-size:11px">annam@gtas.local</p><p style="margin:8px 0 16px;font-size:11px">Công nghệ thông tin ${badge("IT","blue")}</p><div class="readiness-list" style="padding:0;text-align:left"><div class="readiness-row">${icon("language")}<div><p>Ngôn ngữ</p><small>Tiếng Việt</small></div>${icon("chevron_right")}</div><div class="readiness-row">${icon("contrast")}<div><p>Giao diện</p><small>Nền sáng</small></div>${icon("chevron_right")}</div><div class="readiness-row">${icon("notifications")}<div><p>Thông báo</p><small>3 thông báo chưa đọc</small></div>${badge("3","blue")}</div><div class="readiness-row" style="color:var(--atlas-danger)">${icon("logout")}<div><p>Đăng xuất</p><small>Kết thúc phiên hiện tại</small></div></div></div></div></section><section class="card"><div class="card-header"><div><h3>Mẫu trạng thái dùng chung</h3><p>Mẫu đề xuất ban đầu</p></div></div><div class="card-body"><div class="three-column"><div>${badge("Đang tải","blue")}<div class="skeleton-line" style="width:90%"></div><div class="skeleton-line" style="width:65%"></div></div><div>${badge("Chưa có dữ liệu")}<p style="font-size:10px;color:var(--atlas-muted)">Giải thích ngữ cảnh và hành động tiếp theo</p></div><div>${badge("Có lỗi","red")}<p style="font-size:10px;color:var(--atlas-muted)">Thông báo an toàn và nút thử lại</p></div></div></div></section></div></div>`);
}

function accountScreen(screen) {
  const configs = {
    "account-login": {title:"Đăng nhập",desc:"Nhập thông tin tài khoản GTAS để tiếp tục",fields:[["Tên đăng nhập","annam"],["Mật khẩu","••••••••••"]],action:"Đăng nhập",actionTarget:"my-orders",links:[["Quên mật khẩu?","forgot-password"],["Tạo tài khoản","register"]]},
    "account-forgot": {title:"Khôi phục mật khẩu",desc:"Nhập email đã đăng ký. Hệ thống sẽ gửi liên kết để bạn đặt lại mật khẩu.",fields:[["Email công ty","annam@gtas.local"]],action:"Gửi liên kết",actionTarget:"login",alert:"Vì lý do bảo mật, hệ thống luôn trả về cùng một thông báo dù email có tồn tại hay không.",links:[["Quay lại đăng nhập","login"]]},
    "account-reset": {title:"Đặt lại mật khẩu",desc:"Tạo mật khẩu mới cho tài khoản của bạn.",fields:[["Mật khẩu mới","••••••••••"],["Xác nhận mật khẩu mới","••••••••••"]],action:"Đặt lại mật khẩu",actionTarget:"login",alert:"Mật khẩu phải có ít nhất 10 ký tự, gồm chữ hoa, chữ thường và số.",links:[["Quay lại đăng nhập","login"]]},
    "account-change": {title:"Đổi mật khẩu",desc:"Xác nhận mật khẩu hiện tại trước khi tạo mật khẩu mới.",fields:[["Mật khẩu hiện tại","••••••••••"],["Mật khẩu mới","••••••••••"],["Xác nhận mật khẩu mới","••••••••••"]],action:"Đổi mật khẩu",actionTarget:"my-orders",links:[["Quay lại ứng dụng","my-orders"]]},
    "account-logout": {title:"Đang đăng xuất",desc:"Hệ thống đang kết thúc phiên làm việc và đưa bạn về trang đăng nhập.",fields:[],progress:true,links:[]},
    "account-register": {title:"Tạo tài khoản",desc:"Tạo tài khoản để sử dụng trong môi trường phát triển và kiểm thử.",fields:[["Tên đăng nhập","annam"],["Họ và tên","Nguyễn An Nam"],["Email công ty","annam@gtas.local"],["Mật khẩu","••••••••••"],["Xác nhận mật khẩu","••••••••••"]],action:"Tạo tài khoản",actionTarget:"login",alert:"Tính năng đăng ký không tạo tài khoản trong môi trường thực.",links:[["Quay lại đăng nhập","login"]]}
  };
  const config = configs[screen.template];
  const fieldHtml = config.fields.map(([label,value])=>`<div class="form-group"><label>${label}</label><div class="input">${value}</div></div>`).join("");
  return `<div class="account-screen"><section class="account-card"><div class="account-topbar"><div class="account-brand"><span class="app-mark">V</span><strong>GTAS VPP</strong></div><div class="account-language"><span>EN</span><strong>VI</strong></div></div><h1>${config.title}</h1><p>${config.desc}</p>${config.alert?`<div class="account-alert">${icon("info")}<span>${config.alert}</span></div>`:""}${config.progress?`<div class="account-progress"><span></span><p>Đang kết thúc phiên và quay lại trang đăng nhập...</p></div>`:""}${fieldHtml}${config.action?linkButton(config.actionTarget,config.action,"primary","login"):""}${config.links?.length?`<div class="account-links">${config.links.map(([label,target])=>`<a ${screenTarget(target)}>${label}</a>`).join("")}</div>`:""}</section></div>`;
}

function renderTemplate(screen) {
  if (screen.template.startsWith("account-")) return accountScreen(screen);
  if (screen.template.startsWith("library-")) return library(screen);
  switch (screen.template) {
    case "system": return systemBoard(screen);
    case "my-orders": return myOrders(screen);
    case "order-create": return orderCreate(screen);
    case "history": return history(screen);
    case "catalog": return catalog(screen);
    case "department-summary": return management(screen, false);
    case "period-demand": return periodDemand(screen);
    case "approval": return approval(screen);
    case "supply-allocation": return supplyAllocation(screen);
    case "period-review": return periodReview(screen);
    case "settlement": return settlement(screen);
    case "users": return users(screen);
    case "permissions": return permissions(screen);
    case "reports": return reports(screen);
    case "states": return states(screen);
    default: return states(screen);
  }
}

function screenMarkup(screen, state = {}) {
  const classes = ["screen", state.collapsed ? "is-collapsed" : "", state.compact ? "is-compact" : ""].filter(Boolean).join(" ");
  return `<div class="${classes}" data-screen="${screen.id}" data-board="${screen.board}" data-theme="${state.theme || "light"}">${renderTemplate(screen)}</div>`;
}

function setPreviewScale() {
  document.querySelectorAll(".atlas-preview").forEach(preview => {
    const width = preview.getBoundingClientRect().width;
    preview.style.setProperty("--preview-scale", String(width / 1920));
  });
}

async function init() {
  const manifest = await fetch("manifest.json").then(response => response.json());
  const params = new URLSearchParams(location.search);
  const mode = params.get("mode") || "atlas";
  const selectedScreen = params.get("screen");
  const selectedBoard = params.get("board");
  const selectedTheme = params.get("theme") || "light";
  const collapsed = params.get("sidebar") === "collapsed";
  const compact = params.get("density") === "compact";
  const atlas = $("#atlas");

  if (mode === "render" || mode === "capture") {
    const isCapture = mode === "capture";
    document.body.classList.add(isCapture ? "capture-mode" : "render-mode");
    const screen = manifest.find(item => item.id === selectedScreen) || manifest[0];
    atlas.innerHTML = screenMarkup(screen, {theme:selectedTheme, collapsed, compact});
    if (!isCapture) {
      atlas.addEventListener("click", event => {
        const noteTrigger = event.target.closest("[data-note-trigger]");
        if (noteTrigger) {
          const popover = document.getElementById(noteTrigger.getAttribute("aria-controls"));
          atlas.querySelectorAll("[data-note-popover]:not([hidden])").forEach(item => {
            item.hidden = true;
            const owner = atlas.querySelector(`[aria-controls="${item.id}"]`);
            owner?.setAttribute("aria-expanded", "false");
          });
          if (popover) {
            popover.hidden = false;
            noteTrigger.setAttribute("aria-expanded", "true");
            const triggerRect = noteTrigger.getBoundingClientRect();
            const screen = noteTrigger.closest(".screen");
            const screenRect = screen?.getBoundingClientRect() || {left: 0, top: 0, width: innerWidth};
            const scale = screen ? screenRect.width / screen.offsetWidth : 1;
            const width = popover.offsetWidth;
            const height = popover.offsetHeight;
            const triggerLeft = (triggerRect.left - screenRect.left) / scale;
            const triggerTop = (triggerRect.top - screenRect.top) / scale;
            const triggerBottom = (triggerRect.bottom - screenRect.top) / scale;
            const viewportWidth = (innerWidth - screenRect.left) / scale;
            const viewportHeight = (innerHeight - screenRect.top) / scale;
            const left = Math.max(12, Math.min(triggerLeft, viewportWidth - width - 12));
            const below = triggerBottom + 6;
            const top = below + height <= viewportHeight - 12 ? below : Math.max(12, triggerTop - height - 6);
            popover.style.left = `${left}px`;
            popover.style.top = `${top}px`;
          }
          return;
        }
        const noteClose = event.target.closest("[data-note-close], [data-note-save]");
        if (noteClose) {
          const popover = noteClose.closest("[data-note-popover]");
          const owner = popover ? atlas.querySelector(`[aria-controls="${popover.id}"]`) : null;
          if (popover) popover.hidden = true;
          owner?.setAttribute("aria-expanded", "false");
          return;
        }
        const supplierSelector = event.target.closest("[data-supplier-selector]");
        if (supplierSelector) {
          const suppliers = supplierSelector.dataset.suppliers.split("|");
          const priceLists = supplierSelector.dataset.priceLists.split("|");
          const coverages = supplierSelector.dataset.coverages.split("|");
          const nextIndex = (Number(supplierSelector.dataset.selectedIndex || 0) + 1) % suppliers.length;
          supplierSelector.dataset.selectedIndex = String(nextIndex);
          atlas.querySelector("[data-supplier-value]").textContent = suppliers[nextIndex];
          atlas.querySelector("[data-price-list-value]").textContent = priceLists[nextIndex];
          atlas.querySelector("[data-source-coverage]").textContent = coverages[nextIndex];
          return;
        }
        const presetControl = event.target.closest("[data-column-presets]");
        if (presetControl) {
          const labels = presetControl.dataset.columnPresets.split("|");
          const nextIndex = (Number(presetControl.dataset.columnPresetIndex || 0) + 1) % labels.length;
          const presetKeys = ["default", "business", "audit", "all"];
          presetControl.dataset.columnPresetIndex = String(nextIndex);
          presetControl.querySelector("span:not(.ms)").textContent = `Cột · ${labels[nextIndex]}`;
          const workspace = presetControl.closest(".workspace")?.querySelector(".admin-entity-workspace");
          if (workspace) workspace.dataset.columnPreset = presetKeys[nextIndex];
          return;
        }
        const inspectorTab = event.target.closest("[data-admin-inspector-tab]");
        if (inspectorTab) {
          const inspector = inspectorTab.closest(".admin-record-inspector");
          const index = inspectorTab.dataset.adminInspectorTab;
          inspector.querySelectorAll("[data-admin-inspector-tab]").forEach(tab => {
            const active = tab === inspectorTab;
            tab.classList.toggle("active", active);
            tab.setAttribute("aria-selected", String(active));
          });
          inspector.querySelectorAll("[data-admin-inspector-panel]").forEach(panel => panel.classList.toggle("active", panel.dataset.adminInspectorPanel === index));
          return;
        }
        const softDelete = event.target.closest("[data-admin-soft-delete]");
        if (softDelete) {
          const inspector = softDelete.closest(".admin-record-inspector");
          const deleted = inspector.classList.toggle("is-soft-deleted");
          softDelete.querySelector("span:not(.ms)").textContent = deleted ? "Khôi phục" : "Vô hiệu hóa";
          softDelete.querySelector(".ms").textContent = deleted ? "restore" : "block";
          const status = inspector.querySelector("[data-admin-record-status] .badge");
          if (status) status.textContent = deleted ? "Đã vô hiệu hóa" : "Đang hoạt động";
          return;
        }
        const rowOpen = event.target.closest("[data-admin-row-open]");
        if (rowOpen) {
          const row = rowOpen.closest("tr");
          row.closest("tbody").querySelectorAll("tr").forEach(item => item.classList.toggle("is-selected", item === row));
          const inspector = atlas.querySelector(".admin-record-inspector");
          const cells = row.querySelectorAll("td");
          if (inspector && cells.length > 1) {
            inspector.querySelector(".inspector-hero h3").textContent = cells[1].textContent.trim();
            inspector.querySelector(".inspector-hero .mono").textContent = cells[0].textContent.trim();
          }
          return;
        }
        const drawerTrigger = event.target.closest("[data-admin-drawer-trigger]");
        if (drawerTrigger) {
          const layer = atlas.querySelector(".admin-drawer-layer");
          if (layer) {
            const createMode = drawerTrigger.dataset.adminDrawerMode === "create";
            const title = layer.querySelector("[data-admin-drawer-title]");
            const kicker = layer.querySelector("[data-admin-drawer-kicker]");
            title.textContent = `${createMode ? "Thêm" : "Chỉnh sửa"} ${title.dataset.entityLabel}`;
            kicker.textContent = createMode ? "Tạo bản ghi mới" : "Biểu mẫu quản trị";
            layer.querySelectorAll("[data-admin-field-value]").forEach((field, index) => {
              field.textContent = createMode && index < 2 ? "Nhập giá trị" : field.dataset.original;
              field.classList.toggle("is-placeholder", createMode && index < 2);
            });
            layer.hidden = false;
          }
          return;
        }
        const drawerClose = event.target.closest("[data-admin-drawer-close]");
        if (drawerClose) {
          const layer = drawerClose.closest(".admin-drawer-layer");
          if (layer) layer.hidden = true;
          return;
        }
        const drawerSave = event.target.closest("[data-admin-drawer-save]");
        if (drawerSave) {
          const layer = drawerSave.closest(".admin-drawer-layer");
          layer.hidden = true;
          return;
        }
        const trigger = event.target.closest("[data-screen-target]");
        if (!trigger) return;
        const target = trigger.dataset.screenTarget;
        if (!manifest.some(item => item.id === target)) return;
        const next = new URL(location.href);
        next.searchParams.set("screen", target);
        next.searchParams.set("mode", "render");
        location.href = next;
      });
      document.addEventListener("keydown", event => {
        if (event.key !== "Escape") return;
        const layer = atlas.querySelector(".admin-drawer-layer:not([hidden])");
        if (layer) layer.hidden = true;
      });
    }
  } else {
    if (mode === "contact") document.body.classList.add("contact-mode");
    const boards = [...new Set(manifest.map(item => item.board))];
    const select = $("#boardFilter");
    boards.forEach(board => select.insertAdjacentHTML("beforeend", `<option value="${board}">${board} · ${boardLabel(board)}</option>`));
    if (selectedBoard) select.value = selectedBoard;
    const visible = selectedBoard ? manifest.filter(item => item.board === selectedBoard) : manifest;
    atlas.innerHTML = visible.map(screen => `<article class="atlas-card" data-board="${screen.board}"><div class="atlas-card-header"><div><span class="atlas-board-name">${screen.board} · ${boardLabel(screen.board)}</span><strong>${screen.title}</strong><small>${screen.subtitle}</small></div><span class="atlas-index">${screen.board}.${formatIndex(screen.index)}</span></div><div class="atlas-preview">${screenMarkup(screen, {theme:selectedTheme, collapsed, compact})}</div><div class="atlas-card-footer"><span title="${screen.role}">${roleLabel(screen.role)}</span><a href="?screen=${screen.id}&mode=render" target="_blank">Mở vừa cửa sổ ↗</a></div></article>`).join("");
    select.addEventListener("change", () => { location.href = select.value === "all" ? location.pathname : `?board=${select.value}`; });
  }

  $("#sidebarToggle")?.addEventListener("click", () => document.querySelectorAll(".screen").forEach(screen => screen.classList.toggle("is-collapsed")));
  $("#themeToggle")?.addEventListener("click", () => document.querySelectorAll(".screen").forEach(screen => screen.dataset.theme = screen.dataset.theme === "dark" ? "light" : "dark"));
  $("#densityToggle")?.addEventListener("click", () => document.querySelectorAll(".screen").forEach(screen => screen.classList.toggle("is-compact")));
  setPreviewScale();
  addEventListener("resize", setPreviewScale);
  document.documentElement.dataset.ready = "true";
}

init().catch(error => {
  document.body.innerHTML = `<pre style="padding:24px;color:#b4232d">${error.stack || error}</pre>`;
  document.documentElement.dataset.ready = "error";
});
