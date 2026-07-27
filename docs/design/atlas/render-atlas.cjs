const http = require("http");
const fs = require("fs");
const path = require("path");

const atlasRoot = __dirname;
// Atlas nằm ở docs/design/atlas nên gốc repository là ba cấp phía trên.
// Trước đây hai đường dẫn dưới đây được ghi cứng thành D:/WORK/gtas_vpp,
// khiến script chỉ chạy được trên đúng một máy.
const repoRoot = path.resolve(atlasRoot, "..", "..", "..");
// Playwright của Node được cài tại dependency host dùng chung cho tooling browser.
// Phải báo lỗi rõ ràng thay vì ném ra một MODULE_NOT_FOUND khó hiểu.
const playwrightPath = path.join(repoRoot, "scripts", "browser", "node_modules", "playwright");
if (!fs.existsSync(playwrightPath)) {
  throw new Error(
    `Không tìm thấy Playwright tại ${playwrightPath}. ` +
    "Chạy `npm ci` trong scripts/browser trước khi render Atlas."
  );
}
const { chromium } = require(playwrightPath);
const outputRoot = path.join(atlasRoot, "output");
const manifest = JSON.parse(fs.readFileSync(path.join(atlasRoot, "manifest.json"), "utf8"));
const manifestIds = new Set(manifest.map(screen => screen.id));
const atlasCss = fs.readFileSync(path.join(atlasRoot, "atlas.css"), "utf8");
const darkRepresentativeIds = new Set(["shell-system", "history", "order-create", "permissions"]);
const nestedHeaderExpectations = {
  "department-summary": "Quản lý|Tổng hợp phòng ban",
  "period-demand": "Vận hành kỳ|Gom nhu cầu",
  "supplement-approval": "Vận hành kỳ|Duyệt đơn bổ sung",
  "period-review": "Vận hành kỳ|Rà soát kỳ",
  "supply-allocation": "Vận hành kỳ|Chọn nguồn cung",
  "settlement-flow": "Vận hành kỳ|Chốt kỳ",
  "price-lists": "Bảng giá|Danh sách bảng giá",
  prices: "Bảng giá|Giá mặt hàng"
};
const contextualNavigationExpectations = {
  "my-orders": ["order-create", "history"],
  "order-create": ["my-orders"],
  "period-review": ["period-demand"],
  "period-demand": ["period-review", "supply-allocation"],
  "supply-allocation": ["period-demand", "settlement-flow"],
  "settlement-flow": ["supply-allocation"],
  "price-lists": ["prices"],
  prices: ["price-lists"],
  users: ["permissions"],
  permissions: ["users"],
  reports: ["period-demand"]
};

if (/::-webkit-scrollbar|scrollbar-(?:color|width)/.test(atlasCss)) {
  throw new Error("Atlas must use the browser/OS-native scrollbar chrome.");
}

const mimeTypes = {
  ".html": "text/html; charset=utf-8",
  ".css": "text/css; charset=utf-8",
  ".js": "text/javascript; charset=utf-8",
  ".json": "application/json; charset=utf-8",
  ".woff2": "font/woff2",
  ".ttf": "font/ttf",
  ".png": "image/png"
};

function safeResolve(base, requestPath) {
  const resolved = path.resolve(base, requestPath.replace(/^[/\\]+/, ""));
  return resolved.startsWith(path.resolve(base)) ? resolved : null;
}

function serveFile(response, filePath) {
  if (!filePath || !fs.existsSync(filePath) || fs.statSync(filePath).isDirectory()) {
    response.writeHead(404);
    response.end("Not found");
    return;
  }
  response.writeHead(200, { "Content-Type": mimeTypes[path.extname(filePath).toLowerCase()] || "application/octet-stream", "Cache-Control": "no-store" });
  fs.createReadStream(filePath).pipe(response);
}

const server = http.createServer((request, response) => {
  const url = new URL(request.url, "http://127.0.0.1");
  const decodedPath = decodeURIComponent(url.pathname);
  if (decodedPath.startsWith("/repo/")) {
    serveFile(response, safeResolve(repoRoot, decodedPath.slice("/repo/".length)));
    return;
  }
  const relative = decodedPath === "/" ? "index.html" : decodedPath;
  serveFile(response, safeResolve(atlasRoot, relative));
});

async function waitForAtlas(page) {
  await page.waitForFunction(() => ["true", "error"].includes(document.documentElement.dataset.ready));
  const state = await page.evaluate(() => ({ ready: document.documentElement.dataset.ready, text: document.body.innerText.slice(0, 4000) }));
  if (state.ready !== "true") {
    throw new Error(`Atlas bootstrap failed:\n${state.text}`);
  }
  await page.evaluate(() => document.fonts.ready);
  await page.waitForTimeout(80);
}

async function render() {
  for (const directory of ["screens", "boards", "dark"]) {
    const directoryPath = path.join(outputRoot, directory);
    fs.mkdirSync(directoryPath, { recursive: true });
    for (const entry of fs.readdirSync(directoryPath)) {
      if (entry.toLowerCase().endsWith(".png")) fs.unlinkSync(path.join(directoryPath, entry));
    }
  }

  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1920, height: 1080 }, deviceScaleFactor: 1, colorScheme: "light" });
  const page = await context.newPage();
  const consoleErrors = [];
  page.on("console", message => { if (message.type() === "error") consoleErrors.push(message.text()); });
  page.on("pageerror", error => consoleErrors.push(error.message));

  const report = { generatedAt: new Date().toISOString(), screenCount: manifest.length, screens: [], boards: [], darkRepresentatives: [], consoleErrors };

  for (const screen of manifest) {
    await page.setViewportSize({ width: 1920, height: 1080 });
    await page.goto(`http://127.0.0.1:${server.address().port}/?mode=capture&screen=${screen.id}`, { waitUntil: "networkidle" });
    await waitForAtlas(page);
    const geometry = await page.locator(".screen").evaluate(element => {
      const rect = element.getBoundingClientRect();
      const body = document.body;
      const shell = element.querySelector(".app-shell");
      const sidebar = element.querySelector(".sidebar");
      const header = element.querySelector(".primary-header");
      const canvas = element.querySelector(".page-canvas");
      const accountCard = element.querySelector(".account-card");
      const sidebarRect = sidebar?.getBoundingClientRect();
      const headerRect = header?.getBoundingClientRect();
      const canvasRect = canvas?.getBoundingClientRect();
      const accountRect = accountCard?.getBoundingClientRect();
      return {
        width: rect.width,
        height: rect.height,
        left: rect.left,
        top: rect.top,
        horizontalOverflow: body.scrollWidth > 1920,
        verticalOverflow: body.scrollHeight > 1080,
        canvasOverflow: canvas ? canvas.scrollWidth > canvas.clientWidth + 1 || canvas.scrollHeight > canvas.clientHeight + 1 : false,
        shellValid: shell ? Math.abs(sidebarRect.width - 286) <= 1
          && Math.abs(headerRect.left - sidebarRect.right) <= 1
          && Math.abs(headerRect.height - 72) <= 1
          && Math.abs(canvasRect.top - headerRect.bottom) <= 1 : true,
        activeTabCount: shell ? element.querySelectorAll(".header-tab.active").length : 0,
        workflowHeaderCount: shell ? element.querySelectorAll(".order-workflow-header").length : 0,
        accountCentered: accountCard ? Math.abs((accountRect.left + accountRect.width / 2) - 960) <= 1
          && Math.abs((accountRect.top + accountRect.height / 2) - 540) <= 1 : true
      };
    });
    if (geometry.width !== 1920 || geometry.height !== 1080 || geometry.left !== 0 || geometry.top !== 0
      || geometry.horizontalOverflow || geometry.verticalOverflow || geometry.canvasOverflow
      || !geometry.shellValid || !geometry.accountCentered
      || (screen.template.startsWith("account-")
        ? geometry.activeTabCount !== 0
        : screen.template === "order-create"
          ? geometry.activeTabCount !== 0 || geometry.workflowHeaderCount !== 1
          : geometry.activeTabCount !== 1)) {
      throw new Error(`Invalid geometry for ${screen.id}: ${JSON.stringify(geometry)}`);
    }
    const filename = `${String(screen.index).padStart(2, "0")}-${screen.id}.png`;
    await page.screenshot({ path: path.join(outputRoot, "screens", filename), animations: "disabled" });
    report.screens.push({ id: screen.id, board: screen.board, file: `screens/${filename}`, status: "DRAFT", geometry });

    if (darkRepresentativeIds.has(screen.id)) {
      await page.goto(`http://127.0.0.1:${server.address().port}/?mode=capture&screen=${screen.id}&theme=dark`, { waitUntil: "networkidle" });
      await waitForAtlas(page);
      const darkFile = `${String(screen.index).padStart(2, "0")}-${screen.id}-dark.png`;
      await page.screenshot({ path: path.join(outputRoot, "dark", darkFile), animations: "disabled" });
      report.darkRepresentatives.push({ id: screen.id, file: `dark/${darkFile}`, status: "DRAFT" });
    }
  }

  const reviewViewports = [
    { name: "browser-1080p", width: 1904, height: 914 },
    { name: "desktop", width: 1536, height: 864 },
    { name: "compact-desktop", width: 1366, height: 768 }
  ];
  report.responsiveReview = [];
  for (const viewport of reviewViewports) {
    await page.setViewportSize({ width: viewport.width, height: viewport.height });
    for (const screen of manifest) {
      await page.goto(`http://127.0.0.1:${server.address().port}/?mode=render&screen=${screen.id}`, { waitUntil: "networkidle" });
      await waitForAtlas(page);
      const geometry = await page.locator(".screen").evaluate((element, size) => {
        const rect = element.getBoundingClientRect();
        const body = document.body;
        const canvas = element.querySelector(".page-canvas");
        return {
          width: rect.width,
          height: rect.height,
          bodyHorizontalOverflow: body.scrollWidth > size.width + 1,
          bodyVerticalOverflow: body.scrollHeight > size.height + 1,
          canvasHorizontalOverflow: canvas ? canvas.scrollWidth > canvas.clientWidth + 1 : false,
          collectionWorkspaceCount: element.querySelectorAll(".workspace-collection").length,
          masterDetailWorkspaceCount: element.querySelectorAll(".master-detail-workspace").length,
          historyWorkspaceCount: element.querySelectorAll(".history-page").length,
          pagerCount: element.querySelectorAll(".pager").length,
          pagingFooterSummaries: [...element.querySelectorAll(".pager > span:first-child")].map(summary => summary.textContent.trim()),
          virtualizedFooterSummaries: [...element.querySelectorAll(".virtualized-grid-footer > span:first-child")].map(summary => summary.textContent.trim()),
          dataFooterHeights: [...element.querySelectorAll(".pager, .virtualized-grid-footer")].map(footer => footer.getBoundingClientRect().height),
          hasLegacyScrollPrompt: element.textContent.includes("Cuộn để xem thêm"),
          pagedGridFillCount: element.querySelectorAll(".paged-grid-fill").length,
          virtualizedGridCount: element.querySelectorAll(".virtualized-grid[data-grid-mode='scroll-virtualized']").length,
          virtualizedGridFooterCount: element.querySelectorAll(".virtualized-grid-footer").length,
          scrollableVirtualizedGridCount: [...element.querySelectorAll(".virtualized-grid[data-grid-mode='scroll-virtualized']")]
            .filter(grid => grid.scrollHeight > grid.clientHeight + 1).length,
          virtualizedGridBoundaries: [...element.querySelectorAll(".virtualized-grid[data-grid-mode='scroll-virtualized']")].map(grid => {
            grid.scrollTop = grid.scrollHeight;
            const footer = grid.nextElementSibling?.matches(".virtualized-grid-footer") ? grid.nextElementSibling : null;
            const stickyHeader = grid.querySelector("thead");
            const lastCell = grid.querySelector("tbody tr:last-child td");
            const lastRow = lastCell?.closest("tr");
            const gridRect = grid.getBoundingClientRect();
            const stickyHeaderRect = stickyHeader?.getBoundingClientRect();
            const footerRect = footer?.getBoundingClientRect();
            const lastRowRect = lastRow?.getBoundingClientRect();
            const visibleTop = Math.max(gridRect.top, stickyHeaderRect?.bottom || gridRect.top);
            return {
              atBottom: Math.abs(grid.scrollTop - (grid.scrollHeight - grid.clientHeight)) <= 1,
              lastRowFullyVisible: !!lastRowRect && lastRowRect.top >= visibleTop - 1 && lastRowRect.bottom <= gridRect.bottom + 1,
              lastRowToFooterGap: footerRect && lastRowRect ? footerRect.top - lastRowRect.bottom : null,
              lastRowBorderBottom: lastCell ? parseFloat(window.getComputedStyle(lastCell).borderBottomWidth) || 0 : null,
              footerBorderTop: footer ? parseFloat(window.getComputedStyle(footer).borderTopWidth) || 0 : null
            };
          }),
          orderExportActionCount: element.querySelectorAll(".order-export-action").length,
          printableOrderSheetCount: element.querySelectorAll(".printable-order-sheet").length,
          orderSheetFieldLabels: [...element.querySelectorAll(".printable-order-sheet .order-sheet-field-grid dt")].map(label => label.textContent.trim()),
          orderSheetNoteLabels: [...element.querySelectorAll(".printable-order-sheet .order-sheet-note span:first-child")].map(label => label.textContent.trim()),
          approvalKpiCount: element.querySelectorAll(".kpi").length,
          approvalQueueHeaders: [...element.querySelectorAll(".approval-queue-list .data-table th")].map(header => header.textContent.trim()),
          scrollableApprovalQueueCount: [...element.querySelectorAll(".approval-queue-list .virtualized-grid")]
            .filter(grid => grid.scrollHeight > grid.clientHeight + 1).length,
          orderCreateHeaders: [...element.querySelectorAll(".interaction-grid-card .data-table th")].map(header => header.textContent.trim()),
          catalogHeaders: [...element.querySelectorAll(".catalog-readonly-list .data-table th")].map(header => header.textContent.trim()),
          orderNoteCount: element.querySelectorAll(".order-note-input").length,
          orderNoteTriggerCount: element.querySelectorAll(".order-note-trigger").length,
          orderNoteTriggerLabel: element.querySelector(".order-note-trigger span:not(.ms)")?.textContent.trim() || "",
          orderPrimaryActionLabel: element.querySelector(".order-draft-actions .btn.primary span:not(.ms)")?.textContent.trim() || "",
          orderDraftActionLabels: [...element.querySelectorAll(".order-draft-actions .btn")].map(action => action.querySelector("span:not(.ms)")?.textContent.trim() || ""),
          workflowBackActionCount: element.querySelectorAll(".order-workflow-header .workflow-back").length,
          itemNoteCount: element.querySelectorAll(".item-note-trigger").length,
          itemNotePopoverCount: element.querySelectorAll("[data-item-note-popover]").length,
          visibleItemNotePopoverCount: element.querySelectorAll("[data-item-note-popover]:not([hidden])").length,
          orderNoteUnderlineCount: [...element.querySelectorAll(".order-note-input")]
            .filter(note => parseFloat(getComputedStyle(note).borderBottomWidth) > 0).length,
          selectedRemoveActionCount: element.querySelectorAll(".selected-remove-action").length,
          catalogRemoveActionCount: element.querySelectorAll(".catalog-remove-action").length,
          selectedStateCount: element.querySelectorAll(".selected-state-indicator").length,
          orderWorkflowHeaderCount: element.querySelectorAll(".order-workflow-header").length,
          headerTabCount: element.querySelectorAll(".header-tabs").length,
          wizardToolbarCount: element.querySelectorAll(".wizard-toolbar").length,
          workflowStepLabels: [...element.querySelectorAll(".workflow-steps .step > span:last-child")].map(label => label.textContent.trim()),
          workflowHeaderCenterDeltas: (() => {
            const header = element.querySelector(".primary-header.is-workflow");
            if (!header) return [];
            const headerRect = header.getBoundingClientRect();
            const headerCenter = (headerRect.top + headerRect.bottom) / 2;
            return [...header.querySelectorAll(".workflow-back, .workflow-steps .step")].map(item => {
              const rect = item.getBoundingClientRect();
              return ((rect.top + rect.bottom) / 2) - headerCenter;
            });
          })(),
          workflowHeaderHorizontalDelta: (() => {
            const workflow = element.querySelector(".order-workflow-header");
            const steps = workflow?.querySelector(".workflow-steps");
            if (!workflow || !steps) return null;
            const workflowRect = workflow.getBoundingClientRect();
            const stepsRect = steps.getBoundingClientRect();
            return ((stepsRect.left + stepsRect.right) / 2) - ((workflowRect.left + workflowRect.right) / 2);
          })(),
          nestedHeaderCount: element.querySelectorAll(".header-tab.is-nested").length,
          navigationTargets: [...new Set([...element.querySelectorAll("[data-screen-target]")].map(item => item.dataset.screenTarget).filter(Boolean))],
          permissionGroupHeaders: [...element.querySelectorAll(".permission-matrix thead th")].slice(1).map(header => header.childNodes[0]?.textContent?.trim() || header.textContent.trim()),
          permissionActionRows: element.querySelectorAll(".permission-matrix tbody tr").length,
          developerOwnerGrantCount: element.querySelectorAll(".permission-matrix tbody td.developer-owner-column .permission-check").length,
          developerOwnerGuardCount: [...element.querySelectorAll(".permission-notes")].filter(note => note.textContent.includes("DEV") && note.textContent.includes("Quản trị hệ thống") && note.textContent.includes("Toàn quyền")).length,
          adminEntityWorkspaceCount: element.querySelectorAll(".admin-entity-workspace").length,
          adminColumnPresetValues: [...element.querySelectorAll(".column-preset-control")].map(control => control.dataset.columnPresets),
          adminInspectorTabs: [...element.querySelectorAll(".admin-record-inspector .record-inspector-tabs button")].map(tab => tab.textContent.trim()),
          adminDrawerTriggerCount: element.querySelectorAll("[data-admin-drawer-trigger]").length,
          adminActionHeaderCount: [...element.querySelectorAll(".admin-entity-list th:last-child")].filter(header => header.textContent.trim() === "Thao tác").length,
          adminTechnicalPreviewCount: element.querySelectorAll(".admin-record-inspector .admin-technical-preview").length,
          adminSoftDeleteActionCount: [...element.querySelectorAll(".admin-record-inspector .inspector-actions .btn")].filter(action => action.textContent.includes("Vô hiệu hóa")).length,
          adminPriceExpireActionCount: [...element.querySelectorAll(".admin-record-inspector .inspector-actions .btn")].filter(action => action.textContent.includes("Đánh dấu hết hiệu lực")).length,
          exposedSecretFieldCount: [...element.querySelectorAll(".admin-entity-workspace")].filter(workspace => /PasswordHash|SecurityStamp|RefreshToken|AuthenticatorKey/i.test(workspace.textContent)).length,
          adminDrawerCount: element.querySelectorAll(".admin-drawer-layer").length,
          visibleAdminDrawerCount: [...element.querySelectorAll(".admin-drawer-layer")].filter(layer => !layer.hidden).length,
          nestedHeaderLabels: [...element.querySelectorAll(".header-tab.is-nested :is(.nested-header-ancestor, .nested-header-leaf)")].map(label => label.textContent.trim()),
          nestedHeaderLeafMetrics: [...element.querySelectorAll(".nested-header-leaf")].map(leaf => {
            const rect = leaf.getBoundingClientRect();
            const underline = window.getComputedStyle(leaf, "::after");
            return {
              width: rect.width,
              underlineWidth: parseFloat(underline.width) || 0,
              underlineBottom: parseFloat(underline.bottom) || 0
            };
          }),
          selectedDraftScrollRegions: [...element.querySelectorAll(".selected-order-scroll-region[data-navigation='scroll']")].map(region => ({
            scrollable: region.scrollHeight > region.clientHeight + 1,
            rowCount: region.querySelectorAll(".selected-draft-item").length,
            fullyVisibleRowCount: [...region.querySelectorAll(".selected-draft-item")].filter(row => {
              const regionRect = region.getBoundingClientRect();
              const rowRect = row.getBoundingClientRect();
              return rowRect.top >= regionRect.top - 1 && rowRect.bottom <= regionRect.bottom + 1;
            }).length,
            headerCenters: [...region.querySelectorAll(".selected-order-grid-header > span")].map(cell => {
              const rect = cell.getBoundingClientRect();
              return (rect.left + rect.right) / 2;
            }),
            rowCenters: [...(region.querySelector(".selected-draft-row")?.children || [])].slice(0, 4).map(cell => {
              const rect = cell.getBoundingClientRect();
              return (rect.left + rect.right) / 2;
            })
          })),
          hasDuplicateRemoveCopy: element.textContent.includes("Bỏ chọn"),
          compactAddButtonHeights: [...element.querySelectorAll(".interaction-grid-card .btn.compact")]
            .map(button => button.getBoundingClientRect().height),
          compactAddButtonWidths: [...element.querySelectorAll(".interaction-grid-card .btn.compact")]
            .map(button => button.getBoundingClientRect().width),
          nonFlatButtonCount: [...element.querySelectorAll(".btn")].filter(button => {
            const styles = window.getComputedStyle(button);
            return styles.backgroundImage !== "none"
              || styles.boxShadow !== "none"
              || (parseFloat(styles.borderTopWidth) || 0) > 0;
          }).length,
          buttonContentCenterDeltas: [...element.querySelectorAll(".btn")].map(button => {
            const content = [...button.children].filter(child => child.getBoundingClientRect().width > 0);
            if (content.length === 0) return { x: 0, y: 0 };
            const buttonRect = button.getBoundingClientRect();
            const rects = content.map(child => child.getBoundingClientRect());
            const left = Math.min(...rects.map(rect => rect.left));
            const right = Math.max(...rects.map(rect => rect.right));
            const top = Math.min(...rects.map(rect => rect.top));
            const bottom = Math.max(...rects.map(rect => rect.bottom));
            return {
              x: ((left + right) / 2) - ((buttonRect.left + buttonRect.right) / 2),
              y: ((top + bottom) / 2) - ((buttonRect.top + buttonRect.bottom) / 2)
            };
          }),
          inspectorActionRightDeltas: [...element.querySelectorAll(".inspector-actions")].map(actions => {
            const lastButton = actions.querySelector(":scope > .btn:last-child") || actions.querySelector(":scope > .btn:last-of-type");
            if (!lastButton) return 0;
            const actionsRect = actions.getBoundingClientRect();
            const buttonRect = lastButton.getBoundingClientRect();
            const actionsStyles = window.getComputedStyle(actions);
            return (actionsRect.right - (parseFloat(actionsStyles.paddingRight) || 0)) - buttonRect.right;
          }),
          nestedFilterChrome: [...element.querySelectorAll(".interaction-grid-card > .filter-bar")].map(filter => {
            const styles = window.getComputedStyle(filter);
            return {
              top: parseFloat(styles.borderTopWidth) || 0,
              right: parseFloat(styles.borderRightWidth) || 0,
              bottom: parseFloat(styles.borderBottomWidth) || 0,
              left: parseFloat(styles.borderLeftWidth) || 0,
              radius: parseFloat(styles.borderTopLeftRadius) || 0
            };
          }),
          catalogHeaderFilterChrome: [...element.querySelectorAll(".catalog-readonly-list > .card-header > .filter-bar")].map(filter => {
            const styles = window.getComputedStyle(filter);
            return {
              top: parseFloat(styles.borderTopWidth) || 0,
              right: parseFloat(styles.borderRightWidth) || 0,
              bottom: parseFloat(styles.borderBottomWidth) || 0,
              left: parseFloat(styles.borderLeftWidth) || 0,
              radius: parseFloat(styles.borderTopLeftRadius) || 0
            };
          }),
          currencySymbolCount: (element.textContent.match(/₫/g) || []).length,
          managementOrderHeaders: [...element.querySelectorAll(".management-orders-table th")].map(header => header.textContent.trim()),
          managementOrderHeaderFontSizes: [...element.querySelectorAll(".management-orders-table .data-table th")]
            .map(cell => window.getComputedStyle(cell).fontSize),
          managementOrderCellFontSizes: [...element.querySelectorAll(".management-orders-table .data-table td")]
            .map(cell => window.getComputedStyle(cell).fontSize),
          managementFilterLabels: [...element.querySelectorAll(".history-filters .select-box")].map(filter => filter.textContent.trim()),
          periodScopeLabels: [...element.querySelectorAll(".history-scope button")]
            .map(button => button.textContent.replace("calendar_month", "").trim()),
          activePeriodScopeLabels: [...element.querySelectorAll(".history-scope button.active")]
            .map(button => button.textContent.replace("calendar_month", "").trim()),
          unitFilterCount: [...element.querySelectorAll(".select-box")]
            .filter(filter => filter.textContent.trim().startsWith("Tất cả đơn vị")).length,
          pagedGridViewportBottomDeltas: (() => {
            if (!canvas) return [];
            const canvasRect = canvas.getBoundingClientRect();
            const canvasStyles = window.getComputedStyle(canvas);
            const contentBottom = canvasRect.bottom - (parseFloat(canvasStyles.paddingBottom) || 0);
            return [...element.querySelectorAll(".paged-grid")].map(grid => contentBottom - grid.getBoundingClientRect().bottom);
          })(),
          historyDetailKpiTopDelta: (() => {
            const detail = element.querySelector(".history-detail");
            const kpis = element.querySelector(".history-kpis");
            return detail && kpis ? detail.getBoundingClientRect().top - kpis.getBoundingClientRect().top : null;
          })(),
          orderDetailRegionCount: element.querySelectorAll(".order-detail-toolbar").length,
          orderDetailHeaders: [...element.querySelectorAll(".order-detail-table th")].slice(0, 6).map(header => header.textContent.trim()),
          periodFlowStepLabels: [...element.querySelectorAll(".period-flow-step strong")].map(label => label.textContent.trim()),
          activePeriodFlowStepCount: element.querySelectorAll(".period-flow-step.active").length,
          procurementApprovalActionCount: [...element.querySelectorAll(".btn")].filter(action => ["Phê duyệt","Từ chối"].includes(action.textContent.replace(/check|close/g, "").trim())).length,
          supplierSelectorCount: element.querySelectorAll("[data-supplier-selector]").length,
          sourcePriceListValues: [...element.querySelectorAll("[data-price-list-value]")].map(value => value.textContent.trim()),
          sourceCoverageValues: [...element.querySelectorAll("[data-source-coverage]")].map(value => value.textContent.trim()),
          sourceExceptionCount: element.querySelectorAll(".exception-row").length,
          sourceExceptionAuditCount: [...element.querySelectorAll(".exception-row")].filter(row => row.textContent.includes("Lưu người chọn") && row.textContent.toLowerCase().includes("lý do")).length,
          immutableSnapshotCount: [...element.querySelectorAll(".settlement-confirmation")].filter(panel => panel.textContent.includes("Sau khi xác nhận") && panel.textContent.includes("không tự thay đổi")).length,
          periodDemandWorkspaceCount: element.querySelectorAll(".period-demand-layout").length
        };
      }, viewport);
      if (Math.abs(geometry.width - viewport.width) > 1 || Math.abs(geometry.height - viewport.height) > 1
        || geometry.bodyHorizontalOverflow || geometry.bodyVerticalOverflow || geometry.canvasHorizontalOverflow) {
        throw new Error(`Invalid responsive review geometry for ${screen.id} at ${viewport.width}x${viewport.height}: ${JSON.stringify(geometry)}`);
      }
      if (geometry.pagingFooterSummaries.some(summary => !summary.startsWith("Hiển thị "))
        || geometry.virtualizedFooterSummaries.some(summary => !summary.startsWith("Tổng cộng "))
        || geometry.dataFooterHeights.some(height => Math.abs(height - 40) > 0.5)
        || geometry.hasLegacyScrollPrompt) {
        throw new Error(`Invalid shared data-footer contract for ${screen.id} at ${viewport.width}x${viewport.height}: ${JSON.stringify({ paging: geometry.pagingFooterSummaries, virtualized: geometry.virtualizedFooterSummaries, heights: geometry.dataFooterHeights })}`);
      }
      if (["my-orders", "history", "department-summary", "supplement-approval"].includes(screen.id)
        && (geometry.orderDetailRegionCount !== 1
          || geometry.orderDetailHeaders.join("|") !== "#|Mặt hàng|Danh mục|Đơn vị|Số lượng|Ghi chú")) {
        throw new Error(`Invalid shared order-detail contract for ${screen.id} at ${viewport.width}x${viewport.height}: ${JSON.stringify(geometry)}`);
      }
      if (screen.id === "order-create" && (geometry.virtualizedGridCount !== 1 || geometry.pagerCount !== 0)) {
        throw new Error(`Invalid interactive order-grid contract for ${screen.id} at ${viewport.width}x${viewport.height}: ${JSON.stringify(geometry)}`);
      }
      if (screen.id === "order-create"
        && (geometry.orderCreateHeaders.join("|") !== "#|Mặt hàng|Danh mục|Đơn vị|Thao tác"
          || geometry.orderNoteCount !== 1
          || geometry.orderNoteTriggerCount !== 1
          || geometry.orderNoteTriggerLabel !== "Ghi chú"
          || geometry.orderPrimaryActionLabel !== "Tiếp tục"
          || geometry.orderDraftActionLabels.join("|") !== "Quay lại|Lưu nháp|Ghi chú|Tiếp tục"
          || geometry.workflowBackActionCount !== 0
          || geometry.itemNoteCount !== 6
          || geometry.itemNotePopoverCount !== 6
          || geometry.visibleItemNotePopoverCount !== 0
          || geometry.orderNoteUnderlineCount !== 1
          || geometry.selectedRemoveActionCount !== 6
          || geometry.catalogRemoveActionCount !== 6
          || geometry.selectedStateCount !== 0
          || geometry.orderWorkflowHeaderCount !== 1
          || geometry.headerTabCount !== 0
          || geometry.wizardToolbarCount !== 0
          || geometry.workflowStepLabels.join("|") !== "Chọn mặt hàng|Kiểm tra và gửi"
          || geometry.workflowHeaderCenterDeltas.some(delta => Math.abs(delta) > 0.75)
          || Math.abs(geometry.workflowHeaderHorizontalDelta ?? Number.MAX_VALUE) > 0.75
          || geometry.selectedDraftScrollRegions.length !== 1
          || geometry.selectedDraftScrollRegions[0].rowCount !== 6
          || (!geometry.selectedDraftScrollRegions[0].scrollable && geometry.selectedDraftScrollRegions[0].fullyVisibleRowCount !== 6)
          || geometry.selectedDraftScrollRegions[0].headerCenters.some((center, index) => Math.abs(center - geometry.selectedDraftScrollRegions[0].rowCenters[index]) > 0.75)
          || geometry.hasDuplicateRemoveCopy
          || geometry.compactAddButtonHeights.length < 3
          || geometry.compactAddButtonHeights.some(height => Math.abs(height - 30) > 0.5)
          || geometry.compactAddButtonWidths.some(width => Math.abs(width - 66) > 0.5)
          || geometry.currencySymbolCount !== 0)) {
        throw new Error(`Invalid employee order-create business contract at ${viewport.width}x${viewport.height}: ${JSON.stringify(geometry)}`);
      }
      const expectedNestedHeader = nestedHeaderExpectations[screen.id];
      if (expectedNestedHeader
        ? geometry.nestedHeaderCount !== 1
          || geometry.nestedHeaderLabels.join("|") !== expectedNestedHeader
          || geometry.nestedHeaderLeafMetrics.length !== 1
          || Math.abs(geometry.nestedHeaderLeafMetrics[0].width - geometry.nestedHeaderLeafMetrics[0].underlineWidth) > 0.75
          || Math.abs(geometry.nestedHeaderLeafMetrics[0].underlineBottom) > 0.5
        : geometry.nestedHeaderCount !== 0) {
        throw new Error(`Invalid nested header path for ${screen.id} at ${viewport.width}x${viewport.height}: ${JSON.stringify(geometry)}`);
      }
      const invalidNavigationTargets = geometry.navigationTargets.filter(target => !manifestIds.has(target));
      const missingContextualTargets = (contextualNavigationExpectations[screen.id] || []).filter(target => !geometry.navigationTargets.includes(target));
      if (invalidNavigationTargets.length || missingContextualTargets.length || (screen.id !== "logout" && geometry.navigationTargets.length === 0)) {
        throw new Error(`Invalid navigation contract for ${screen.id} at ${viewport.width}x${viewport.height}: ${JSON.stringify({ invalidNavigationTargets, missingContextualTargets, geometry })}`);
      }
      if (screen.id === "permissions"
        && (geometry.permissionGroupHeaders.join("|") !== "EMPLOYEE|MANAGER|DEV"
          || geometry.permissionActionRows !== 18
          || geometry.developerOwnerGrantCount !== 18
          || geometry.developerOwnerGuardCount !== 1)) {
        throw new Error(`Invalid three-persona RBAC atlas at ${viewport.width}x${viewport.height}: ${JSON.stringify(geometry)}`);
      }
      if (screen.id === "classes"
        && (geometry.pagerCount !== 2
          || geometry.adminEntityWorkspaceCount !== 0
          || geometry.adminDrawerCount !== 0
          || geometry.exposedSecretFieldCount !== 0)) {
        throw new Error(`Invalid lookup master/detail workspace for ${screen.id} at ${viewport.width}x${viewport.height}: ${JSON.stringify(geometry)}`);
      }
      const libraryAdminScreens = ["categories", "items", "suppliers", "price-lists", "prices", "departments"];
      if (libraryAdminScreens.includes(screen.id)
        && (geometry.adminEntityWorkspaceCount !== 1
          || geometry.adminColumnPresetValues.join("|") !== "Mặc định|Nghiệp vụ|Nhật ký|Tất cả"
          || geometry.adminInspectorTabs.join("|") !== "Thông tin chung|Quan hệ|Bản dịch|Nhật ký|Kỹ thuật"
          || geometry.adminDrawerTriggerCount !== 0
          || geometry.adminActionHeaderCount !== 1
          || geometry.adminTechnicalPreviewCount !== 1
          || (screen.id === "price-lists"
            ? geometry.adminSoftDeleteActionCount !== 0 || geometry.adminPriceExpireActionCount !== 1
            : geometry.adminSoftDeleteActionCount !== 1 || geometry.adminPriceExpireActionCount !== 0)
          || geometry.adminDrawerCount !== 0
          || geometry.visibleAdminDrawerCount !== 0
          || geometry.exposedSecretFieldCount !== 0)) {
        throw new Error(`Invalid shared admin-entity workspace for ${screen.id} at ${viewport.width}x${viewport.height}: ${JSON.stringify(geometry)}`);
      }
      if (screen.id === "users"
        && (geometry.adminEntityWorkspaceCount !== 1
          || geometry.adminColumnPresetValues.join("|") !== "Mặc định|Nghiệp vụ|Nhật ký|Tất cả"
          || geometry.adminInspectorTabs.join("|") !== "Thông tin chung|Quan hệ|Bản dịch|Nhật ký|Kỹ thuật"
          || geometry.adminDrawerTriggerCount < 1
          || geometry.adminActionHeaderCount !== 1
          || geometry.adminTechnicalPreviewCount !== 1
          || geometry.adminSoftDeleteActionCount !== 1
          || geometry.adminDrawerCount !== 1
          || geometry.visibleAdminDrawerCount !== 0
          || geometry.exposedSecretFieldCount !== 0)) {
        throw new Error(`Invalid user administration workspace at ${viewport.width}x${viewport.height}: ${JSON.stringify(geometry)}`);
      }
      if (geometry.nonFlatButtonCount !== 0) {
        throw new Error(`Non-flat button chrome detected for ${screen.id} at ${viewport.width}x${viewport.height}: ${JSON.stringify(geometry)}`);
      }
      if (geometry.buttonContentCenterDeltas.some(delta => Math.abs(delta.x) > 1 || Math.abs(delta.y) > 1)) {
        throw new Error(`Button content is not visually centered for ${screen.id} at ${viewport.width}x${viewport.height}: ${JSON.stringify(geometry)}`);
      }
      if (geometry.inspectorActionRightDeltas.some(delta => Math.abs(delta) > 1)) {
        throw new Error(`Form/inspector actions are not trailing-aligned for ${screen.id} at ${viewport.width}x${viewport.height}: ${JSON.stringify(geometry)}`);
      }
      if (geometry.nestedFilterChrome.some(chrome => chrome.top !== 0
        || chrome.right !== 0
        || chrome.bottom !== 1
        || chrome.left !== 0
        || chrome.radius !== 0)) {
        throw new Error(`Nested interaction filter still has duplicated rounded chrome for ${screen.id} at ${viewport.width}x${viewport.height}: ${JSON.stringify(geometry)}`);
      }
      if (screen.id === "catalog" && (geometry.pagerCount !== 1
        || geometry.catalogHeaders.join("|") !== "Mặt hàng|Danh mục|Đơn vị|Mô tả"
        || geometry.catalogHeaderFilterChrome.length !== 1
        || geometry.catalogHeaderFilterChrome.some(chrome => chrome.top !== 0
          || chrome.right !== 0
          || chrome.bottom !== 0
          || chrome.left !== 0
          || chrome.radius !== 0))) {
        throw new Error(`Invalid read-only paging contract for ${screen.id} at ${viewport.width}x${viewport.height}: ${JSON.stringify(geometry)}`);
      }
      if (screen.id === "history" && Math.abs(geometry.historyDetailKpiTopDelta ?? Number.MAX_VALUE) > 1) {
        throw new Error(`Invalid History detail/KPI top alignment at ${viewport.width}x${viewport.height}: ${JSON.stringify(geometry)}`);
      }
      // Xuất theo đơn có backend thật từ 2026-07-26 (D10):
      // GET /api/VPPRequest/orders/{id}/export.pdf|export.xlsx. Các màn có
      // order detail/order sheet phải hiển thị đúng hai nút PDF + Excel.
      if (["my-orders", "history", "department-summary", "supplement-approval"].includes(screen.id) && geometry.orderExportActionCount !== 2) {
        throw new Error(`Invalid order export action contract for ${screen.id} at ${viewport.width}x${viewport.height}: ${JSON.stringify(geometry)}`);
      }
      if (["history", "department-summary", "supplement-approval"].includes(screen.id)
        && (geometry.printableOrderSheetCount !== 1
          || geometry.orderSheetFieldLabels.join("|") !== "Kỳ|Loại đơn|Người đặt|Phòng ban")) {
        throw new Error(`Invalid printable order-sheet contract for ${screen.id} at ${viewport.width}x${viewport.height}: ${JSON.stringify(geometry)}`);
      }
      if (screen.id === "supplement-approval"
        && (geometry.approvalKpiCount !== 0
          || geometry.pagerCount !== 1
          || geometry.scrollableApprovalQueueCount !== 0
          || geometry.approvalQueueHeaders.join("|") !== "Mã đơn|Người đặt|Phòng|Mặt hàng|Số lượng|Ghi chú|Trạng thái"
          || geometry.orderSheetNoteLabels.join("|") !== "Ghi chú đơn")) {
        throw new Error(`Invalid action-first approval workspace for ${screen.id} at ${viewport.width}x${viewport.height}: ${JSON.stringify(geometry)}`);
      }
      const periodFlowScreens = ["period-review", "period-demand", "supply-allocation", "settlement-flow"];
      if (periodFlowScreens.includes(screen.id)
        && (geometry.periodFlowStepLabels.join("|") !== "Rà soát|Gom nhu cầu|Chọn nguồn cung|Chốt kỳ"
          || geometry.activePeriodFlowStepCount !== 1)) {
        throw new Error(`Invalid period-operation flow navigation for ${screen.id} at ${viewport.width}x${viewport.height}: ${JSON.stringify(geometry)}`);
      }
      if (screen.id === "period-review" && geometry.procurementApprovalActionCount !== 0) {
        throw new Error(`Procurement review must not expose supplement approval actions at ${viewport.width}x${viewport.height}: ${JSON.stringify(geometry)}`);
      }
      if (screen.id === "supplement-approval" && geometry.procurementApprovalActionCount !== 2) {
        throw new Error(`Manager is missing supplement decision actions at ${viewport.width}x${viewport.height}: ${JSON.stringify(geometry)}`);
      }
      if (screen.id === "period-demand" && geometry.periodDemandWorkspaceCount !== 1) {
        throw new Error(`Company demand was not consolidated into period operations at ${viewport.width}x${viewport.height}: ${JSON.stringify(geometry)}`);
      }
      if (screen.id === "supply-allocation"
        && (geometry.supplierSelectorCount !== 1
          || geometry.sourcePriceListValues.join("|") !== "PL-2026-H2"
          || geometry.sourceCoverageValues.join("|") !== "272/286"
          || geometry.sourceExceptionCount !== 2
          || geometry.sourceExceptionAuditCount !== 2)) {
        throw new Error(`Invalid supplier-first allocation and per-item exception contract at ${viewport.width}x${viewport.height}: ${JSON.stringify(geometry)}`);
      }
      if (screen.id === "settlement-flow" && geometry.immutableSnapshotCount !== 1) {
        throw new Error(`Settlement preview does not explain immutable saved-result behavior at ${viewport.width}x${viewport.height}: ${JSON.stringify(geometry)}`);
      }
      const pagingScreens = [
        "history", "catalog", "department-summary", "supplement-approval", "period-review", "period-demand", "settlement-flow",
        "classes", "categories", "items", "suppliers", "price-lists", "prices", "departments", "users", "reports"
      ];
      const virtualizedScreens = ["my-orders", "order-create", "history", "department-summary", "period-demand", "supply-allocation", "supplement-approval", "permissions"];
      const expectedPagerCount = screen.id === "classes" ? 2 : pagingScreens.includes(screen.id) ? 1 : 0;
      const expectedVirtualizedCount = virtualizedScreens.includes(screen.id) ? 1 : 0;
      if (geometry.pagerCount !== expectedPagerCount
        || geometry.virtualizedGridCount !== expectedVirtualizedCount
        || geometry.scrollableVirtualizedGridCount !== expectedVirtualizedCount
        || geometry.virtualizedGridFooterCount !== expectedVirtualizedCount) {
        throw new Error(`Invalid screen-wide data navigation contract for ${screen.id} at ${viewport.width}x${viewport.height}: ${JSON.stringify({ expectedPagerCount, expectedVirtualizedCount, geometry })}`);
      }
      if (geometry.virtualizedGridBoundaries.some(boundary => !boundary.atBottom
        || !boundary.lastRowFullyVisible
        || boundary.lastRowToFooterGap < -0.75
        || boundary.lastRowToFooterGap > 1.5
        || boundary.lastRowBorderBottom !== 0
        || boundary.footerBorderTop !== 1)) {
        throw new Error(`Virtualized grid/footer boundary is not a single clean separator for ${screen.id} at ${viewport.width}x${viewport.height}: ${JSON.stringify(geometry)}`);
      }
      if (expectedPagerCount === 1
        && geometry.pagedGridViewportBottomDeltas.some(delta => Math.abs(delta) > 1.5)) {
        throw new Error(`Paged workspace does not reach the viewport bottom for ${screen.id} at ${viewport.width}x${viewport.height}: ${JSON.stringify(geometry)}`);
      }
      const masterDetailScreens = ["supplement-approval", "categories", "items", "suppliers", "price-lists", "prices", "departments", "users"];
      const collectionScreens = ["catalog", ...masterDetailScreens];
      if (collectionScreens.includes(screen.id) && geometry.collectionWorkspaceCount !== 1) {
        throw new Error(`Invalid shared collection workspace contract for ${screen.id} at ${viewport.width}x${viewport.height}: ${JSON.stringify(geometry)}`);
      }
      if (masterDetailScreens.includes(screen.id) && geometry.masterDetailWorkspaceCount !== 1) {
        throw new Error(`Invalid shared master-detail workspace contract for ${screen.id} at ${viewport.width}x${viewport.height}: ${JSON.stringify(geometry)}`);
      }
      if (["history", "department-summary"].includes(screen.id) && geometry.historyWorkspaceCount !== 1) {
        throw new Error(`Invalid history-like order workspace contract for ${screen.id} at ${viewport.width}x${viewport.height}: ${JSON.stringify(geometry)}`);
      }
      if (screen.id === "department-summary"
        && geometry.managementFilterLabels.some(label => label.startsWith("Tất cả phòng ban"))) {
        throw new Error(`Invalid single-department management scope at ${viewport.width}x${viewport.height}: ${JSON.stringify(geometry)}`);
      }
      const expectedManagementHeaders = screen.id === "department-summary"
        ? "#|Kỳ|Mã đơn|Họ tên|Loại đơn|Trạng thái|Ngày gửi|Ghi chú"
        : "#|Kỳ|Mã đơn|Họ tên|Phòng ban|Loại đơn|Trạng thái|Ngày gửi";
      if (screen.id === "department-summary"
        && (geometry.managementOrderHeaders.join("|") !== expectedManagementHeaders
          || new Set(geometry.managementOrderHeaderFontSizes).size !== 1
          || geometry.managementOrderHeaderFontSizes[0] !== "11px"
          || new Set(geometry.managementOrderCellFontSizes).size !== 1
          || geometry.managementOrderCellFontSizes[0] !== "12px")) {
        throw new Error(`Incomplete management order columns at ${viewport.width}x${viewport.height}: ${JSON.stringify(geometry)}`);
      }
      if (["history", "department-summary"].includes(screen.id)
        && (geometry.periodScopeLabels.join("|") !== "Tất cả kỳ|Kỳ này|3 tháng|6 tháng|12 tháng|Tùy chọn"
          || geometry.activePeriodScopeLabels.join("|") !== (screen.id === "history" ? "Tất cả kỳ" : "Kỳ này"))) {
        throw new Error(`Inconsistent history-like period scope at ${viewport.width}x${viewport.height}: ${JSON.stringify(geometry)}`);
      }
      const unitFilterScreens = [
        "my-orders", "order-create", "history", "catalog", "department-summary",
        "supplement-approval", "items", "prices"
      ];
      const expectedUnitFilterCount = unitFilterScreens.includes(screen.id) ? 1 : 0;
      if (geometry.unitFilterCount !== expectedUnitFilterCount) {
        throw new Error(`Invalid unit-filter coverage for ${screen.id} at ${viewport.width}x${viewport.height}: ${JSON.stringify({ expectedUnitFilterCount, geometry })}`);
      }
      report.responsiveReview.push({ id: screen.id, viewport: viewport.name, geometry });
    }
  }

  report.navigationReview = [];
  for (const [source, targets] of Object.entries(contextualNavigationExpectations)) {
    for (const target of targets) {
      await page.goto(`http://127.0.0.1:${server.address().port}/?mode=render&screen=${source}`, { waitUntil: "networkidle" });
      await waitForAtlas(page);
      const trigger = page.locator(`[data-screen-target="${target}"]`).last();
      await Promise.all([
        page.waitForURL(url => url.searchParams.get("screen") === target && url.searchParams.get("mode") === "render"),
        trigger.click()
      ]);
      await waitForAtlas(page);
      report.navigationReview.push({ source, target, status: "PASS" });
    }
  }

  report.itemNotePopoverReview = [];
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(`http://127.0.0.1:${server.address().port}/?mode=render&screen=order-create`, { waitUntil: "networkidle" });
  await waitForAtlas(page);
  const firstNoteTrigger = page.locator(".item-note-trigger").first();
  await firstNoteTrigger.click();
  const openNoteState = await page.locator("[data-item-note-popover]:not([hidden])").evaluate(popover => {
    const rect = popover.getBoundingClientRect();
    return {
      title: popover.querySelector(".item-note-popover-header strong")?.textContent.trim(),
      value: popover.querySelector(".item-note-editor")?.textContent.trim(),
      actionLabels: [...popover.querySelectorAll(".item-note-popover-actions button")]
        .map(button => button.querySelector("span:not(.ms)")?.textContent.trim() || button.textContent.trim()),
      withinViewport: rect.left >= 0 && rect.top >= 0 && rect.right <= innerWidth + 1 && rect.bottom <= innerHeight + 1
    };
  });
  await page.screenshot({ path: path.join(outputRoot, "review-order-create-note-popover.png"), animations: "disabled" });
  await page.locator("[data-item-note-popover]:not([hidden]) [data-note-save]").click();
  const closedNoteState = {
    visiblePopoverCount: await page.locator("[data-item-note-popover]:not([hidden])").count(),
    expanded: await firstNoteTrigger.getAttribute("aria-expanded")
  };
  if (openNoteState.title !== "Giấy in A4 Double A 70gsm"
    || openNoteState.value !== "In hồ sơ dự án quý III"
    || openNoteState.actionLabels.join("|") !== "Hủy|Lưu ghi chú"
    || !openNoteState.withinViewport
    || closedNoteState.visiblePopoverCount !== 0
    || closedNoteState.expanded !== "false") {
    throw new Error(`Item note popover is not functional: ${JSON.stringify({ openNoteState, closedNoteState })}`);
  }
  await page.locator(".order-note-trigger").click();
  const openOrderNoteState = await page.locator("[data-order-note-popover]:not([hidden])").evaluate(popover => ({
    title: popover.querySelector(".item-note-popover-header strong")?.textContent.trim(),
    value: popover.querySelector(".order-note-input")?.textContent.trim(),
    withinViewport: (() => {
      const rect = popover.getBoundingClientRect();
      return rect.left >= 0 && rect.top >= 0 && rect.right <= innerWidth + 1 && rect.bottom <= innerHeight + 1;
    })()
  }));
  await page.locator("[data-order-note-popover]:not([hidden]) [data-note-save]").click();
  const closedOrderNoteState = {
    visiblePopoverCount: await page.locator("[data-order-note-popover]:not([hidden])").count(),
    expanded: await page.locator(".order-note-trigger").getAttribute("aria-expanded")
  };
  if (openOrderNoteState.title !== "Đơn kỳ 07/2026"
    || openOrderNoteState.value !== "Bổ sung vật tư phục vụ dự án và phòng họp trong tháng 7"
    || !openOrderNoteState.withinViewport
    || closedOrderNoteState.visiblePopoverCount !== 0
    || closedOrderNoteState.expanded !== "false") {
    throw new Error(`Order note popover is not functional: ${JSON.stringify({ openOrderNoteState, closedOrderNoteState })}`);
  }
  report.itemNotePopoverReview.push({ openNoteState, closedNoteState, openOrderNoteState, closedOrderNoteState, status: "PASS" });

  report.supplySelectionReview = [];
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(`http://127.0.0.1:${server.address().port}/?mode=render&screen=supply-allocation`, { waitUntil: "networkidle" });
  await waitForAtlas(page);
  const initialSourceState = await page.locator(".source-selection-card").evaluate(card => ({
    supplier: card.querySelector("[data-supplier-value]").textContent.trim(),
    priceList: card.querySelector("[data-price-list-value]").textContent.trim(),
    coverage: card.querySelector("[data-source-coverage]").textContent.trim(),
    supplierBeforePriceList: !!(card.querySelector("[data-supplier-selector]").compareDocumentPosition(card.querySelector("[data-price-list-value]")) & Node.DOCUMENT_POSITION_FOLLOWING)
  }));
  await page.locator("[data-supplier-selector]").click();
  const changedSourceState = await page.locator(".source-selection-card").evaluate(card => ({
    supplier: card.querySelector("[data-supplier-value]").textContent.trim(),
    priceList: card.querySelector("[data-price-list-value]").textContent.trim(),
    coverage: card.querySelector("[data-source-coverage]").textContent.trim()
  }));
  if (initialSourceState.supplier !== "An Phát Office" || initialSourceState.priceList !== "PL-2026-H2" || initialSourceState.coverage !== "272/286" || !initialSourceState.supplierBeforePriceList
    || changedSourceState.supplier !== "Minh Hải Stationery" || changedSourceState.priceList !== "PL-MH-2026-H2" || changedSourceState.coverage !== "279/286") {
    throw new Error(`Supplier-first price-list filtering is not functional: ${JSON.stringify({ initialSourceState, changedSourceState })}`);
  }
  report.supplySelectionReview.push({ initialSourceState, changedSourceState, status:"PASS" });

  report.adminDrawerReview = [];
  // Legacy drawer interaction audit retained for history only. Library W-E now uses
  // runtime-aligned inline editing + inspector actions, so the old drawer gate is skipped.
  if (false) {
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(`http://127.0.0.1:${server.address().port}/?mode=render&screen=items`, { waitUntil: "networkidle" });
  await waitForAtlas(page);
  await page.locator("[data-admin-drawer-trigger]").first().click();
  const drawerGeometry = await page.locator(".admin-drawer-layer").evaluate(layer => {
    const canvas = layer.closest(".page-canvas");
    const drawer = layer.querySelector(".admin-entity-drawer");
    const layerRect = layer.getBoundingClientRect();
    const canvasRect = canvas.getBoundingClientRect();
    const drawerRect = drawer.getBoundingClientRect();
    return {
      hidden: layer.hidden,
      topDelta: layerRect.top - canvasRect.top,
      bottomDelta: layerRect.bottom - canvasRect.bottom,
      rightDelta: drawerRect.right - layerRect.right,
      drawerWidth: drawerRect.width,
      title: drawer.querySelector("[data-admin-drawer-title]").textContent.trim(),
      sectionTitles: [...drawer.querySelectorAll(".admin-form-section h3")].map(title => title.textContent.trim()),
      fieldLabels: [...drawer.querySelectorAll(".admin-form-grid label")].map(label => label.childNodes[0].textContent.trim()),
      placeholderCount: drawer.querySelectorAll("[data-admin-field-value].is-placeholder").length,
      actionLabels: [...drawer.querySelectorAll(".admin-drawer-actions .btn")].map(action => action.querySelector("span:not(.ms)")?.textContent.trim() || ""),
      bodyScrollable: drawer.querySelector(".admin-drawer-body").scrollHeight >= drawer.querySelector(".admin-drawer-body").clientHeight
    };
  });
  if (drawerGeometry.hidden
    || Math.abs(drawerGeometry.topDelta) > 0.5
    || Math.abs(drawerGeometry.bottomDelta) > 0.5
    || Math.abs(drawerGeometry.rightDelta) > 0.5
    || drawerGeometry.drawerWidth < 400
    || drawerGeometry.drawerWidth > 481
    || drawerGeometry.title !== "Thêm mặt hàng"
    || drawerGeometry.placeholderCount !== 2
    || drawerGeometry.sectionTitles.join("|") !== "Thông tin chung|Quan hệ nghiệp vụ|Kỹ thuật chỉ đọc"
    || drawerGeometry.fieldLabels.join("|") !== "Mã mặt hàng|Tên mặt hàng|Mô tả|Trạng thái|Danh mục|Đơn vị"
    || drawerGeometry.actionLabels.join("|") !== "Hủy|Lưu bản ghi") {
    throw new Error(`Invalid admin drawer geometry: ${JSON.stringify(drawerGeometry)}`);
  }
  await page.screenshot({ path: path.join(outputRoot, "screens", "20-items-drawer.png"), fullPage: false, animations: "disabled" });
  await page.locator("[data-admin-drawer-close]").last().click();
  const drawerHiddenAfterClose = await page.locator(".admin-drawer-layer").evaluate(layer => layer.hidden);
  if (!drawerHiddenAfterClose) throw new Error("Admin drawer did not close after user action.");
  await page.locator('[data-admin-drawer-mode="edit"]').first().click();
  const editDrawerState = await page.locator(".admin-drawer-layer").evaluate(layer => ({
    title: layer.querySelector("[data-admin-drawer-title]").textContent.trim(),
    placeholderCount: layer.querySelectorAll("[data-admin-field-value].is-placeholder").length,
    firstValue: layer.querySelector("[data-admin-field-value]").textContent.trim()
  }));
  await page.keyboard.press("Escape");
  if (editDrawerState.title !== "Chỉnh sửa mặt hàng" || editDrawerState.placeholderCount !== 0 || editDrawerState.firstValue !== "VPP_7B9F34EE") {
    throw new Error(`Admin edit drawer did not restore entity data: ${JSON.stringify(editDrawerState)}`);
  }

  const inspectorTabReview = [];
  for (let index = 0; index < 5; index += 1) {
    await page.locator(`[data-admin-inspector-tab="${index}"]`).click();
    inspectorTabReview.push(await page.locator(".admin-record-inspector").evaluate((inspector, expectedIndex) => ({
      activeTab: inspector.querySelector("[data-admin-inspector-tab].active")?.dataset.adminInspectorTab,
      activePanel: inspector.querySelector("[data-admin-inspector-panel].active")?.dataset.adminInspectorPanel,
      visiblePanelCount: [...inspector.querySelectorAll("[data-admin-inspector-panel]")].filter(panel => window.getComputedStyle(panel).display !== "none").length
    }), String(index)));
  }
  if (inspectorTabReview.some((state,index) => state.activeTab !== String(index) || state.activePanel !== String(index) || state.visiblePanelCount !== 1)) {
    throw new Error(`Admin inspector tabs are not functional: ${JSON.stringify(inspectorTabReview)}`);
  }

  const presetReview = [];
  const presetControl = page.locator("[data-column-presets]");
  for (let index = 0; index < 4; index += 1) {
    if (index > 0) await presetControl.click();
    presetReview.push(await page.locator(".admin-entity-workspace").evaluate(workspace => ({
      preset: workspace.dataset.columnPreset || "default",
      label: workspace.closest(".workspace").querySelector("[data-column-presets] span:not(.ms)").textContent.trim(),
      visibleHeaders: [...workspace.querySelectorAll(".admin-entity-list th")].filter(header => window.getComputedStyle(header).display !== "none").map(header => header.textContent.trim())
    })));
  }
  if (presetReview.map(state => state.preset).join("|") !== "default|business|audit|all"
    || !presetReview[1].visibleHeaders.includes("Mô tả nghiệp vụ")
    || presetReview[2].visibleHeaders.includes("Trạng thái")
    || !presetReview[2].visibleHeaders.includes("Ngày tạo")
    || !presetReview[3].visibleHeaders.includes("RowVersion")) {
    throw new Error(`Admin column presets are not functional: ${JSON.stringify(presetReview)}`);
  }

  await page.locator("[data-admin-soft-delete]").click();
  const deletedState = await page.locator(".admin-record-inspector").evaluate(inspector => ({
    deleted: inspector.classList.contains("is-soft-deleted"),
    action: inspector.querySelector("[data-admin-soft-delete] span:not(.ms)").textContent.trim(),
    status: inspector.querySelector("[data-admin-record-status] .badge").textContent.trim()
  }));
  await page.locator("[data-admin-soft-delete]").click();
  const restoredState = await page.locator(".admin-record-inspector").evaluate(inspector => ({
    deleted: inspector.classList.contains("is-soft-deleted"),
    action: inspector.querySelector("[data-admin-soft-delete] span:not(.ms)").textContent.trim(),
    status: inspector.querySelector("[data-admin-record-status] .badge").textContent.trim()
  }));
  if (!deletedState.deleted || deletedState.action !== "Khôi phục" || deletedState.status !== "Đã vô hiệu hóa"
    || restoredState.deleted || restoredState.action !== "Vô hiệu hóa" || restoredState.status !== "Đang hoạt động") {
    throw new Error(`Admin soft-delete state is not reversible: ${JSON.stringify({ deletedState, restoredState })}`);
  }

  await page.locator("[data-admin-row-open]").nth(1).click();
  const rowSelectionState = await page.locator(".admin-entity-workspace").evaluate(workspace => ({
    selectedRows: workspace.querySelectorAll("tbody tr.is-selected").length,
    selectedCode: workspace.querySelector("tbody tr.is-selected td")?.textContent.trim(),
    inspectorCode: workspace.querySelector(".inspector-hero .mono")?.textContent.trim()
  }));
  if (rowSelectionState.selectedRows !== 1 || rowSelectionState.selectedCode !== rowSelectionState.inspectorCode) {
    throw new Error(`Admin row selection did not update inspector: ${JSON.stringify(rowSelectionState)}`);
  }
  report.adminDrawerReview.push({ screen: "items", geometry: drawerGeometry, editDrawerState, inspectorTabReview, presetReview, deletedState, restoredState, rowSelectionState, closeStatus: "PASS", screenshot: "screens/20-items-drawer.png" });

  const adminEntityExpectations = {
    classes: { title:"Thêm loại danh mục", firstFields:"Mã loại|Tên loại" },
    categories: { title:"Thêm danh mục", firstFields:"Mã danh mục|Tên danh mục" },
    items: { title:"Thêm mặt hàng", firstFields:"Mã mặt hàng|Tên mặt hàng" },
    suppliers: { title:"Thêm nhà cung cấp", firstFields:"Tên viết tắt|Tên nhà cung cấp" },
    "price-lists": { title:"Thêm bảng giá", firstFields:"Mã bảng giá|Tên bảng giá" },
    prices: { title:"Thêm giá mặt hàng", firstFields:"Đơn giá|Thuế VAT" },
    departments: { title:"Thêm phòng ban", firstFields:"Mã phòng ban|Tên phòng ban" },
    users: { title:"Thêm người dùng", firstFields:"Tên đăng nhập|Họ và tên" }
  };
  report.adminEntityScreenReview = [];
  for (const [screenId, expectation] of Object.entries(adminEntityExpectations)) {
    await page.goto(`http://127.0.0.1:${server.address().port}/?mode=render&screen=${screenId}`, { waitUntil: "networkidle" });
    await waitForAtlas(page);
    const control = page.locator("[data-column-presets]");
    await control.click();
    await control.click();
    await control.click();
    const allColumnsState = await page.locator(".admin-entity-workspace").evaluate(workspace => {
      const scroller = workspace.querySelector(".admin-grid-table");
      return {
        preset: workspace.dataset.columnPreset,
        hasBusiness: [...workspace.querySelectorAll("th")].some(header => header.textContent.trim() === "Mô tả nghiệp vụ" && window.getComputedStyle(header).display !== "none"),
        hasAudit: [...workspace.querySelectorAll("th")].some(header => header.textContent.trim() === "Ngày tạo" && window.getComputedStyle(header).display !== "none"),
        hasTechnical: [...workspace.querySelectorAll("th")].some(header => header.textContent.trim() === "RowVersion" && window.getComputedStyle(header).display !== "none"),
        horizontallyScrollable: scroller.scrollWidth > scroller.clientWidth + 1
      };
    });
    await page.locator('[data-admin-inspector-tab="4"]').click();
    const technicalPanelState = await page.locator(".admin-record-inspector").evaluate(inspector => ({
      activePanel: inspector.querySelector("[data-admin-inspector-panel].active")?.dataset.adminInspectorPanel,
      hasSecretValue: /PasswordHash|SecurityStamp|RefreshToken|AuthenticatorKey/i.test(inspector.textContent),
      hasNonRenderRule: inspector.textContent.includes("Không hiển thị")
    }));
    await page.locator('[data-admin-drawer-mode="create"]').click();
    const createState = await page.locator(".admin-drawer-layer").evaluate(layer => ({
      title: layer.querySelector("[data-admin-drawer-title]").textContent.trim(),
      firstFields: [...layer.querySelectorAll(".admin-form-grid label")].slice(0,2).map(label => label.childNodes[0].textContent.trim()).join("|"),
      placeholderCount: layer.querySelectorAll("[data-admin-field-value].is-placeholder").length,
      exposedSecret: /PasswordHash|SecurityStamp|RefreshToken|AuthenticatorKey/i.test(layer.textContent)
    }));
    await page.locator("[data-admin-drawer-close]").last().click();
    let lifecycleState;
    if (screenId === "price-lists") {
      lifecycleState = await page.locator(".admin-record-inspector").evaluate(inspector => ({
        restored: true,
        action: inspector.querySelector(".inspector-actions .btn.danger span:not(.ms)")?.textContent.trim(),
        status: inspector.querySelector(".inspector-hero .badge")?.textContent.trim()
      }));
    } else {
      await page.locator("[data-admin-soft-delete]").click();
      await page.locator("[data-admin-soft-delete]").click();
      lifecycleState = await page.locator(".admin-record-inspector").evaluate(inspector => ({
        restored: !inspector.classList.contains("is-soft-deleted")
      }));
    }
    const lifecycleValid = screenId === "price-lists"
      ? lifecycleState.action === "Đánh dấu hết hiệu lực" && lifecycleState.status === "Đang chọn"
      : lifecycleState.restored;
    if (allColumnsState.preset !== "all" || !allColumnsState.hasBusiness || !allColumnsState.hasAudit || !allColumnsState.hasTechnical || !allColumnsState.horizontallyScrollable
      || technicalPanelState.activePanel !== "4" || technicalPanelState.hasSecretValue || !technicalPanelState.hasNonRenderRule
      || createState.title !== expectation.title || createState.firstFields !== expectation.firstFields || createState.placeholderCount !== 2 || createState.exposedSecret
      || !lifecycleValid) {
      throw new Error(`Incomplete admin entity interaction for ${screenId}: ${JSON.stringify({ allColumnsState, technicalPanelState, createState, lifecycleState })}`);
    }
    report.adminEntityScreenReview.push({ screen:screenId, allColumnsState, technicalPanelState, createState, lifecycleState, lifecycleReview:"PASS" });
  }

  }

  report.adminEntityScreenReview = [];
  for (const screenId of ["categories", "items", "suppliers", "price-lists", "prices", "departments", "users"]) {
    await page.goto(`http://127.0.0.1:${server.address().port}/?mode=render&screen=${screenId}`, { waitUntil: "networkidle" });
    await waitForAtlas(page);
    const state = await page.locator(".admin-entity-workspace").evaluate(workspace => ({
      inspectorTabs: [...workspace.querySelectorAll("[data-admin-inspector-tab]")].map(tab => tab.textContent.trim()),
      drawerTriggers: workspace.closest(".page-canvas").querySelectorAll("[data-admin-drawer-trigger]").length,
      hasSoftDelete: workspace.querySelectorAll("[data-admin-soft-delete]").length,
      hasExpire: [...workspace.querySelectorAll(".inspector-actions .btn")].some(button => button.textContent.includes("Đánh dấu hết hiệu lực")),
      exposedSecret: /PasswordHash|SecurityStamp|RefreshToken|AuthenticatorKey/i.test(workspace.textContent)
    }));
    const expectsDrawer = screenId === "users";
    const lifecycleValid = screenId === "price-lists" ? state.hasExpire : state.hasSoftDelete === 1;
    if (state.inspectorTabs.join("|") !== "Thông tin chung|Quan hệ|Bản dịch|Nhật ký|Kỹ thuật"
      || state.drawerTriggers !== (expectsDrawer ? 2 : 0)
      || !lifecycleValid
      || state.exposedSecret) {
      throw new Error(`Incomplete runtime-aligned admin entity contract for ${screenId}: ${JSON.stringify(state)}`);
    }
    report.adminEntityScreenReview.push({ screen:screenId, ...state, lifecycleReview:"PASS" });
  }

  await page.goto(`http://127.0.0.1:${server.address().port}/?mode=render&screen=classes`, { waitUntil: "networkidle" });
  await waitForAtlas(page);
  const classWorkspaceState = await page.locator(".library-class-workspace").evaluate(workspace => ({
    tableTitles: [...workspace.querySelectorAll(".card-header h3")].map(title => title.textContent.trim()),
    pagerCount: workspace.querySelectorAll(".pager").length,
    drawerCount: workspace.closest(".page-canvas").querySelectorAll(".admin-drawer-layer").length
  }));
  if (classWorkspaceState.tableTitles.join("|") !== "Loại danh mục|Giá trị · Đơn vị tính"
    || classWorkspaceState.pagerCount !== 2
    || classWorkspaceState.drawerCount !== 0) {
    throw new Error(`Invalid class/value workspace: ${JSON.stringify(classWorkspaceState)}`);
  }
  report.adminEntityScreenReview.push({ screen:"classes", ...classWorkspaceState, lifecycleReview:"PASS" });

  report.overviewReview = [];
  for (const viewport of [
    { name: "desktop-100", width: 1920, height: 1080 },
    { name: "desktop-150-zoom-equivalent", width: 1280, height: 720 }
  ]) {
    await page.setViewportSize({ width: viewport.width, height: viewport.height });
    await page.goto(`http://127.0.0.1:${server.address().port}/`, { waitUntil: "networkidle" });
    await waitForAtlas(page);
    const geometry = await page.locator(".atlas-grid").evaluate((grid, size) => {
      const gridRect = grid.getBoundingClientRect();
      const gridStyles = window.getComputedStyle(grid);
      const cards = [...grid.querySelectorAll(":scope > .atlas-card")];
      const cardRects = cards.slice(0, 3).map(card => card.getBoundingClientRect());
      const cardStyles = cards.slice(0, 3).map(card => window.getComputedStyle(card));
      return {
        cardCount: cards.length,
        columnCount: gridStyles.gridTemplateColumns.split(" ").filter(Boolean).length,
        columnGap: parseFloat(gridStyles.columnGap) || 0,
        width: gridRect.width,
        left: gridRect.left,
        firstRowTopDelta: Math.max(...cardRects.map(rect => rect.top)) - Math.min(...cardRects.map(rect => rect.top)),
        firstToSecondGap: cardRects[1].left - cardRects[0].right,
        secondToThirdGap: cardRects[2].left - cardRects[1].right,
        roundedCardCount: cardStyles.filter(styles => parseFloat(styles.borderTopLeftRadius) > 0).length,
        shadowedCardCount: cardStyles.filter(styles => styles.boxShadow !== "none").length,
        bodyHorizontalOverflow: document.body.scrollWidth > size.width + 1
      };
    }, viewport);
    if (geometry.cardCount !== manifest.length
      || geometry.columnCount !== 3
      || Math.abs(geometry.columnGap) > 0.1
      || Math.abs(geometry.width - viewport.width) > 1
      || Math.abs(geometry.left) > 1
      || Math.abs(geometry.firstRowTopDelta) > 0.5
      || Math.abs(geometry.firstToSecondGap) > 0.5
      || Math.abs(geometry.secondToThirdGap) > 0.5
      || geometry.roundedCardCount !== 0
      || geometry.shadowedCardCount !== 0
      || geometry.bodyHorizontalOverflow) {
      throw new Error(`Invalid checkerboard overview at ${viewport.width}x${viewport.height}: ${JSON.stringify(geometry)}`);
    }
    report.overviewReview.push({ viewport: viewport.name, geometry });
  }

  await page.setViewportSize({ width: 1920, height: 1080 });
  await page.goto(`http://127.0.0.1:${server.address().port}/`, { waitUntil: "networkidle" });
  await waitForAtlas(page);
  await page.screenshot({ path: path.join(outputRoot, "atlas-overview.png"), fullPage: true, animations: "disabled" });

  const boards = [...new Set(manifest.map(item => item.board))];
  for (const board of boards) {
    await page.goto(`http://127.0.0.1:${server.address().port}/?mode=contact&board=${board}`, { waitUntil: "networkidle" });
    await waitForAtlas(page);
    const file = `${board.toLowerCase()}-contact-sheet.png`;
    await page.screenshot({ path: path.join(outputRoot, "boards", file), fullPage: true, animations: "disabled" });
    report.boards.push({ board, file: `boards/${file}`, status: "DRAFT" });
  }

  fs.writeFileSync(path.join(outputRoot, "review-manifest.json"), JSON.stringify(report, null, 2));
  await browser.close();

  if (consoleErrors.length) {
    throw new Error(`Atlas rendered with console errors:\n${consoleErrors.join("\n")}`);
  }
  return report;
}

server.listen(0, "127.0.0.1", async () => {
  try {
    const report = await render();
    process.stdout.write(`Rendered ${report.screenCount} screens, ${report.boards.length} board sheets, ${report.darkRepresentatives.length} dark representatives.\n`);
    server.close();
  } catch (error) {
    process.stderr.write(`${error.stack || error}\n`);
    server.close(() => process.exit(1));
  }
});
