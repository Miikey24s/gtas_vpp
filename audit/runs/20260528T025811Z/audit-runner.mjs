import fs from 'node:fs/promises';
import path from 'node:path';
import { chromium } from 'playwright';

const root = 'C:/ANNAM/TT/SRS/code/gtas_vpp';
const runId = '20260528T025811Z';
const artifactDir = path.join(root, 'audit/runs', runId);
const frontendUrl = process.env.FRONTEND_URL || 'http://localhost:5203';
const username = process.env.VPP_USERNAME || '';
const password = process.env.VPP_PASSWORD || '';
const timeoutMs = Number(process.env.TIMEOUT_PER_ROUTE_MS || 30000);
const storageStatePath = path.join(artifactDir, 'storage-state.json');

const viewports = [
  { name: 'mobile', width: 390, height: 844 },
  { name: 'tablet', width: 768, height: 1024 },
  { name: 'desktop', width: 1920, height: 1080 },
];

const routes = [
  { key: 'dashboard.my-orders', path: '/dashboard?tab=0', pageCode: 'DASHBOARD', title: 'MyOrders' },
  { key: 'dashboard.history', path: '/dashboard?tab=1', pageCode: 'DASHBOARD', title: 'History' },
  { key: 'dashboard.catalog', path: '/dashboard?tab=2', pageCode: 'DASHBOARD', title: 'Catalog' },
  { key: 'dashboard.management.department', path: '/dashboard?tab=3&managementTab=department', pageCode: 'DASHBOARD', title: 'DepartmentSummary' },
  { key: 'dashboard.management.all', path: '/dashboard?tab=3&managementTab=all', pageCode: 'DASHBOARD', title: 'AllOrdersSummary' },
  { key: 'dashboard.period-operations', path: '/dashboard?tab=5', pageCode: 'DASHBOARD', title: 'PeriodOperations' },
  { key: 'dashboard.order-create.new', path: '/dashboard/order-create', pageCode: 'DASHBOARD', title: 'OrderCreate' },
  {
    key: 'dashboard.order-create.edit',
    path: '/dashboard/order-create?orderId=5e83b7b1-befa-4f0f-965f-82139a36f020',
    pageCode: 'DASHBOARD',
    title: 'OrderEdit',
    dynamicSample: '5e83b7b1-befa-4f0f-965f-82139a36f020',
  },
  { key: 'library.classes', path: '/library?tab=0', pageCode: 'LIBRARY', title: 'ClassDefinitions' },
  { key: 'library.categories', path: '/library?tab=1', pageCode: 'LIBRARY', title: 'OperationCategories' },
  { key: 'library.items', path: '/library?tab=2', pageCode: 'LIBRARY', title: 'Operations' },
  { key: 'library.suppliers', path: '/library?tab=3', pageCode: 'LIBRARY', title: 'Suppliers' },
  { key: 'library.departments', path: '/library?tab=5', pageCode: 'LIBRARY', title: 'Departments' },
  { key: 'library.pricing.price-lists', path: '/library?tab=6&pricingTab=price-lists', pageCode: 'LIBRARY', title: 'PriceLists' },
  { key: 'library.pricing.prices', path: '/library?tab=6&pricingTab=prices', pageCode: 'LIBRARY', title: 'Prices' },
  { key: 'permission.user', path: '/permission?tab=0', pageCode: 'PERMISSION', title: 'PermissionUser' },
  { key: 'permission.component', path: '/permission?tab=1', pageCode: 'PERMISSION', title: 'PermissionComponent' },
  { key: 'report', path: '/report', pageCode: 'REPORT', title: 'Reports' },
];

function routeUrl(routePath) {
  return new URL(routePath, frontendUrl).toString();
}

async function ensureDirs() {
  await fs.mkdir(path.join(artifactDir, 'screenshots'), { recursive: true });
  await fs.mkdir(path.join(artifactDir, 'dom'), { recursive: true });
  await fs.mkdir(path.join(artifactDir, 'a11y'), { recursive: true });
}

async function waitForBlazor(page, timeout = timeoutMs) {
  await page.waitForFunction(() => {
    const main =
      document.querySelector('main') ||
      document.querySelector('.rz-content') ||
      document.querySelector('.vpp-layout-body') ||
      document.querySelector('.rz-body');
    if (!main || main.children.length === 0) return false;

    const busy = Array.from(document.querySelectorAll('.rz-spinner,.loading,[aria-busy="true"]')).some((el) => {
      const style = getComputedStyle(el);
      return style.display !== 'none' && style.visibility !== 'hidden' && el.getClientRects().length > 0;
    });
    if (busy) return false;

    const blazorError = document.querySelector('#blazor-error-ui');
    if (blazorError && getComputedStyle(blazorError).display === 'block') return false;

    const reconnect = document.querySelector('#components-reconnect-modal');
    if (reconnect) {
      const style = getComputedStyle(reconnect);
      if (style.display !== 'none' && style.visibility !== 'hidden' && reconnect.getClientRects().length > 0) return false;
    }

    return true;
  }, { timeout });

  await page.waitForFunction(() => {
    const textLength = (document.body.innerText || '').trim().length;
    const routeContent = document.querySelector(
      '.vpp-content [role="tablist"], .vpp-content .rz-data-grid, .vpp-content table, .vpp-content .vpp-wizard-toolbar, .vpp-content .vpp-empty-state, .vpp-content .rz-alert'
    );
    return textLength > 180 || Boolean(routeContent);
  }, { timeout: Math.min(timeout, 8000) });

  await page.waitForTimeout(1000);
}

async function loginIfNeeded(page) {
  if (!page.url().includes('/Account/Login')) return false;
  if (!username || !password) {
    throw new Error('Login required but VPP_USERNAME/VPP_PASSWORD were not provided to the runner.');
  }

  await page.locator('#Username, input[name="Username"]').first().fill(username);
  await page.locator('#Password, input[name="Password"], input[name="PasswordText"]').first().fill(password);
  await page.getByRole('button', { name: /login/i }).click();
  await page.waitForURL((url) => !url.pathname.includes('/Account/Login'), { timeout: timeoutMs });
  await waitForBlazor(page);
  await page.context().storageState({ path: storageStatePath });
  return true;
}

async function auditDom(page) {
  return page.evaluate(() => {
    const visible = (el) => {
      const rect = el.getBoundingClientRect();
      const style = getComputedStyle(el);
      return rect.width > 0 && rect.height > 0 && style.display !== 'none' && style.visibility !== 'hidden';
    };

    const bodyWidth = Math.max(document.documentElement.scrollWidth, document.body.scrollWidth);
    const viewportWidth = window.innerWidth;
    const horizontalOverflow = bodyWidth - viewportWidth;

    const overflowOffenders = Array.from(document.body.querySelectorAll('*'))
      .filter(visible)
      .map((el) => {
        const rect = el.getBoundingClientRect();
        return {
          tag: el.tagName.toLowerCase(),
          cls: String(el.className || '').slice(0, 160),
          role: el.getAttribute('role') || '',
          text: (el.innerText || el.getAttribute('aria-label') || '').trim().replace(/\s+/g, ' ').slice(0, 160),
          left: Math.round(rect.left),
          right: Math.round(rect.right),
          width: Math.round(rect.width),
        };
      })
      .filter((x) => x.right > viewportWidth + 8 || x.left < -8)
      .filter((x) => !x.cls.includes('rz-dialog-mask'))
      .slice(0, 12);

    const clippedControls = Array.from(document.querySelectorAll('button,a,[role="button"],[role="tab"],input,textarea,.rz-button,.rz-tabview-title,.rz-badge'))
      .filter(visible)
      .filter((el) => !el.closest('.sidebar-collapsed'))
      .filter((el) => el.scrollWidth > el.clientWidth + 3 || el.scrollHeight > el.clientHeight + 3)
      .map((el) => ({
        tag: el.tagName.toLowerCase(),
        cls: String(el.className || '').slice(0, 120),
        role: el.getAttribute('role') || '',
        text: (el.innerText || el.getAttribute('aria-label') || el.getAttribute('title') || '').trim().replace(/\s+/g, ' ').slice(0, 160),
        clientWidth: el.clientWidth,
        scrollWidth: el.scrollWidth,
        clientHeight: el.clientHeight,
        scrollHeight: el.scrollHeight,
      }))
      .filter((x) => x.text.length > 0 && !/^(assignment|library_books|analytics|settings|logout|dark_mode|light_mode|notifications|menu)$/i.test(x.text))
      .slice(0, 12);

    const blazorError = document.querySelector('#blazor-error-ui');
    const reconnect = document.querySelector('#components-reconnect-modal');
    const visibleBlazorError = Boolean(blazorError && getComputedStyle(blazorError).display === 'block');
    const visibleReconnect = Boolean(reconnect && getComputedStyle(reconnect).display !== 'none' && reconnect.getClientRects().length > 0);
    const bodyText = (document.body.innerText || '').trim().replace(/\s+/g, ' ');

    const interactiveWithoutName = Array.from(document.querySelectorAll('button,a[href],input,textarea,select,[role="button"],[role="link"],[role="tab"]'))
      .filter(visible)
      .filter((el) => {
        if (el.disabled || el.getAttribute('aria-hidden') === 'true') return false;
        const name = [
          el.getAttribute('aria-label'),
          el.getAttribute('title'),
          el.getAttribute('placeholder'),
          el.innerText,
          el.getAttribute('value'),
        ].filter(Boolean).join(' ').trim();
        return name.length === 0;
      })
      .map((el) => ({
        tag: el.tagName.toLowerCase(),
        type: el.getAttribute('type') || '',
        cls: String(el.className || '').slice(0, 120),
        role: el.getAttribute('role') || '',
      }))
      .slice(0, 12);

    return {
      url: location.href,
      title: document.title,
      bodyTextLength: bodyText.length,
      visibleBlazorError,
      visibleReconnect,
      horizontalOverflow,
      overflowOffenders,
      clippedControls,
      interactiveWithoutName,
      tableCount: document.querySelectorAll('table').length,
      dialogCount: document.querySelectorAll('.rz-dialog,.rz-dialog-wrapper,[role="dialog"]').length,
      tabCount: document.querySelectorAll('[role="tab"]').length,
    };
  });
}

function classify(route, viewport, dom, consoleErrors, failedResponses, elapsedMs, timedOut, redirectedToLogin) {
  const bugs = [];
  const screenshot = `screenshots/${route.key}-${viewport.name}.png`;

  if (timedOut) {
    bugs.push({
      severity: 'Critical',
      routeKey: route.key,
      viewport: viewport.name,
      type: 'timeout',
      repro: `${route.path} did not finish Blazor-ready wait in ${timeoutMs}ms.`,
      screenshot,
      suspectFile: '',
      fixed: false,
    });
  }

  if (redirectedToLogin) {
    bugs.push({
      severity: 'Critical',
      routeKey: route.key,
      viewport: viewport.name,
      type: 'auth-redirect',
      repro: `${route.path} redirected to login despite saved authenticated state.`,
      screenshot,
      suspectFile: '',
      fixed: false,
    });
  }

  if (dom?.visibleBlazorError || dom?.visibleReconnect) {
    bugs.push({
      severity: 'Critical',
      routeKey: route.key,
      viewport: viewport.name,
      type: 'blazor-error',
      repro: `${route.path} showed Blazor error or reconnect modal.`,
      screenshot,
      suspectFile: '',
      fixed: false,
    });
  }

  const serverFailures = failedResponses.filter((r) => r.status >= 500);
  if (serverFailures.length > 0) {
    bugs.push({
      severity: 'Critical',
      routeKey: route.key,
      viewport: viewport.name,
      type: 'server-error',
      repro: `Browser response status >=500 while loading ${route.path}: ${serverFailures.map((r) => `${r.status} ${r.url}`).join('; ')}`,
      screenshot,
      suspectFile: '',
      fixed: false,
    });
  }

  if (dom && dom.bodyTextLength < 20 && !redirectedToLogin) {
    bugs.push({
      severity: 'High',
      routeKey: route.key,
      viewport: viewport.name,
      type: 'blank-route',
      repro: `${route.path} rendered almost no visible text (${dom.bodyTextLength} chars).`,
      screenshot,
      suspectFile: '',
      fixed: false,
    });
  }

  if (dom && dom.horizontalOverflow > 12) {
    bugs.push({
      severity: viewport.name === 'desktop' ? 'Medium' : 'High',
      routeKey: route.key,
      viewport: viewport.name,
      type: 'body-horizontal-overflow',
      repro: `${route.path} page body overflows viewport by ${Math.round(dom.horizontalOverflow)}px; first offenders: ${dom.overflowOffenders.map((x) => `${x.tag}.${x.cls || x.role || 'no-class'} "${x.text}"`).join(' | ')}`,
      screenshot,
      suspectFile: '',
      fixed: false,
    });
  }

  if (dom && dom.clippedControls.length > 0) {
    bugs.push({
      severity: 'Medium',
      routeKey: route.key,
      viewport: viewport.name,
      type: 'clipped-control-text',
      repro: `${route.path} has clipped interactive/control text: ${dom.clippedControls.map((x) => `${x.tag} "${x.text}" ${x.clientWidth}x${x.clientHeight}->${x.scrollWidth}x${x.scrollHeight}`).join(' | ')}`,
      screenshot,
      suspectFile: '',
      fixed: false,
    });
  }

  if (dom && dom.interactiveWithoutName.length > 0) {
    bugs.push({
      severity: 'Medium',
      routeKey: route.key,
      viewport: viewport.name,
      type: 'unnamed-interactive',
      repro: `${route.path} has interactive elements without accessible names: ${dom.interactiveWithoutName.map((x) => `${x.tag}.${x.cls || x.role || x.type}`).join(' | ')}`,
      screenshot,
      suspectFile: '',
      fixed: false,
    });
  }

  const consoleFailures = consoleErrors.filter((entry) => !/favicon|ResizeObserver/i.test(entry.text));
  if (consoleFailures.length > 0) {
    bugs.push({
      severity: consoleFailures.some((entry) => /exception|unhandled|circuit|Error:/i.test(entry.text)) ? 'High' : 'Medium',
      routeKey: route.key,
      viewport: viewport.name,
      type: 'console-error',
      repro: `${route.path} emitted browser console errors: ${consoleFailures.slice(0, 3).map((entry) => entry.text.slice(0, 240)).join(' | ')}`,
      screenshot,
      suspectFile: '',
      fixed: false,
    });
  }

  return {
    key: route.key,
    path: route.path,
    pageCode: route.pageCode,
    viewport: viewport.name,
    status: bugs.some((b) => b.severity === 'Critical' || b.severity === 'High') ? 'BUG' : 'OK',
    elapsedMs,
    finalUrl: dom?.url || '',
    title: dom?.title || '',
    dom,
    failedResponses,
    consoleErrors,
    bugs,
  };
}

async function auditRoute(page, route, viewport) {
  await page.setViewportSize({ width: viewport.width, height: viewport.height });

  const consoleErrors = [];
  const failedResponses = [];
  const onConsole = (msg) => {
    if (msg.type() === 'error') {
      consoleErrors.push({ type: msg.type(), text: msg.text(), location: msg.location() });
    }
  };
  const onResponse = (response) => {
    const status = response.status();
    if (status >= 400) {
      failedResponses.push({ status, url: response.url() });
    }
  };

  page.on('console', onConsole);
  page.on('response', onResponse);

  const started = Date.now();
  let dom = null;
  let timedOut = false;
  let redirectedToLogin = false;
  const screenshotPath = path.join(artifactDir, 'screenshots', `${route.key}-${viewport.name}.png`);

  try {
    await page.goto(routeUrl(route.path), { waitUntil: 'domcontentloaded', timeout: timeoutMs });
    redirectedToLogin = page.url().includes('/Account/Login');
    if (redirectedToLogin) {
      await loginIfNeeded(page);
      await page.goto(routeUrl(route.path), { waitUntil: 'domcontentloaded', timeout: timeoutMs });
    }
    await waitForBlazor(page);
    await page.waitForTimeout(200);
    dom = await auditDom(page);
  } catch (error) {
    timedOut = /Timeout/i.test(String(error));
    dom = {
      url: page.url(),
      title: await page.title().catch(() => ''),
      bodyTextLength: 0,
      visibleBlazorError: false,
      visibleReconnect: false,
      horizontalOverflow: 0,
      overflowOffenders: [],
      clippedControls: [],
      interactiveWithoutName: [],
      error: String(error),
    };
  }

  await page.screenshot({ path: screenshotPath, fullPage: true }).catch(() => {});
  const elapsedMs = Date.now() - started;
  page.off('console', onConsole);
  page.off('response', onResponse);

  const result = classify(route, viewport, dom, consoleErrors, failedResponses, elapsedMs, timedOut, redirectedToLogin);
  if (result.bugs.length > 0) {
    await fs.writeFile(path.join(artifactDir, 'dom', `${route.key}-${viewport.name}.html`), await page.content(), 'utf8');
  }
  await fs.writeFile(path.join(artifactDir, 'a11y', `${route.key}-${viewport.name}.json`), JSON.stringify({
    scanner: 'mcp-accessibility-scanner unavailable; Playwright DOM heuristic used',
    routeKey: route.key,
    viewport: viewport.name,
    dom: result.dom,
    bugs: result.bugs.filter((bug) => ['clipped-control-text', 'unnamed-interactive'].includes(bug.type)),
  }, null, 2), 'utf8');

  return result;
}

async function main() {
  await ensureDirs();
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ storageState: storageStatePath, viewport: { width: 1280, height: 720 } });
  const page = await context.newPage();
  const results = [];

  for (const route of routes) {
    for (const viewport of viewports) {
      const result = await auditRoute(page, route, viewport);
      results.push(result);
      const line = `${new Date().toISOString()} ${route.key} ${viewport.name} ${result.status} ${result.elapsedMs}ms`;
      await fs.appendFile(path.join(artifactDir, 'progress.md'), `${line}\n`, 'utf8');
      console.log(line);
    }
  }

  await fs.writeFile(path.join(artifactDir, 'audit-results.json'), JSON.stringify({
    runId,
    frontendUrl,
    routes,
    viewports,
    results,
    bugs: results.flatMap((result) => result.bugs),
  }, null, 2), 'utf8');

  await context.storageState({ path: storageStatePath });
  await browser.close();
}

main().catch(async (error) => {
  await fs.writeFile(path.join(artifactDir, 'audit-runner-error.txt'), String(error.stack || error), 'utf8').catch(() => {});
  console.error(error);
  process.exitCode = 1;
});
