const fs = require("fs");
const http = require("http");
const path = require("path");

const repositoryRoot = path.resolve(__dirname, "../..");
const { chromium } = require(path.join(repositoryRoot, "gtas_vpp_fe_react/node_modules/playwright"));
// Atlas đã được đưa vào repository tại docs/design/atlas nên script tự tìm được
// nguồn. Vẫn cho phép trỏ sang bản khác qua tham số hoặc GTAS_ATLAS_ROOT khi cần
// đối chiếu với một bản Atlas cũ nằm ngoài repository.
const defaultAtlasRoot = path.join(repositoryRoot, "docs/design/atlas");
const atlasArgument = process.argv[2] || process.env.GTAS_ATLAS_ROOT || defaultAtlasRoot;
const atlasRoot = path.resolve(atlasArgument);
if (!fs.existsSync(path.join(atlasRoot, "manifest.json"))) {
  throw new Error(`Không tìm thấy manifest.json trong thư mục Atlas: ${atlasRoot}`);
}
const outputRoot = path.join(repositoryRoot, "LVTN/screenshots/ch03/atlas");
// Khung desktop chuẩn của Atlas. Mục 3.3 được đặt trên trang ngang A4 để
// giữ cỡ chữ giao diện đủ đọc khi in, thay vì thu nhỏ ảnh 1920 px vào trang dọc.
const viewport = { width: 1536, height: 864 };
const manifest = JSON.parse(fs.readFileSync(path.join(atlasRoot, "manifest.json"), "utf8"));
const manifestById = new Map(manifest.map((screen) => [screen.id, screen]));

const selectedScreens = [
  "login",
  "my-orders",
  "order-create",
  "history",
  "catalog",
  "department-summary",
  "supplement-approval",
  "period-review",
  "supply-allocation",
  "settlement-flow",
  "items",
  "price-lists",
  "users",
  "permissions",
  "reports",
  "system-states"
];

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
  response.writeHead(200, {
    "Content-Type": mimeTypes[path.extname(filePath).toLowerCase()] || "application/octet-stream",
    "Cache-Control": "no-store"
  });
  fs.createReadStream(filePath).pipe(response);
}

async function waitForAtlas(page) {
  await page.waitForFunction(() => ["true", "error"].includes(document.documentElement.dataset.ready));
  const ready = await page.evaluate(() => document.documentElement.dataset.ready);
  if (ready !== "true") throw new Error(`Atlas bootstrap failed: ${ready}`);
  await page.evaluate(() => document.fonts.ready);
  await page.waitForTimeout(100);
}

async function main() {
  for (const screenId of selectedScreens) {
    if (!manifestById.has(screenId)) throw new Error(`Unknown Atlas screen: ${screenId}`);
  }
  fs.mkdirSync(outputRoot, { recursive: true });

  const server = http.createServer((request, response) => {
    const url = new URL(request.url, "http://127.0.0.1");
    const decodedPath = decodeURIComponent(url.pathname);
    if (decodedPath.startsWith("/repo/")) {
      serveFile(response, safeResolve(repositoryRoot, decodedPath.slice("/repo/".length)));
      return;
    }
    serveFile(response, safeResolve(atlasRoot, decodedPath === "/" ? "index.html" : decodedPath));
  });
  await new Promise((resolve) => server.listen(0, "127.0.0.1", resolve));

  const browser = await chromium.launch({ headless: true });
  try {
    const context = await browser.newContext({
      viewport,
      deviceScaleFactor: 2,
      colorScheme: "light"
    });
    const page = await context.newPage();
    const browserErrors = [];
    page.on("console", (message) => {
      if (message.type() === "error") browserErrors.push(message.text());
    });
    page.on("pageerror", (error) => browserErrors.push(error.message));

    for (const [index, screenId] of selectedScreens.entries()) {
      await page.goto(`http://127.0.0.1:${server.address().port}/?mode=render&screen=${screenId}`, {
        waitUntil: "networkidle"
      });
      await waitForAtlas(page);

      const geometry = await page.locator(".screen").evaluate((element) => {
        const accountCard = element.querySelector(".account-card");
        const header = element.querySelector(".primary-header");
        const canvas = element.querySelector(".page-canvas");
        const body = document.body;
        const viewportWidth = document.documentElement.clientWidth;
        const viewportHeight = document.documentElement.clientHeight;
        return {
          account: accountCard ? accountCard.getBoundingClientRect().toJSON() : null,
          header: header ? header.getBoundingClientRect().toJSON() : null,
          canvas: canvas ? canvas.getBoundingClientRect().toJSON() : null,
          bodyHorizontalOverflow: body.scrollWidth > viewportWidth + 1,
          bodyVerticalOverflow: body.scrollHeight > viewportHeight + 1,
          canvasHorizontalOverflow: canvas ? canvas.scrollWidth > canvas.clientWidth + 1 : false
        };
      });
      if (geometry.bodyHorizontalOverflow || geometry.bodyVerticalOverflow || geometry.canvasHorizontalOverflow) {
        throw new Error(`Atlas thesis capture overflow for ${screenId}: ${JSON.stringify(geometry)}`);
      }

      let clip;
      if (geometry.account) {
        const margin = 28;
        clip = {
          x: Math.max(0, geometry.account.x - margin),
          y: Math.max(0, geometry.account.y - margin),
          width: Math.min(viewport.width, geometry.account.width + margin * 2),
          height: Math.min(viewport.height, geometry.account.height + margin * 2)
        };
      } else {
        clip = {
          x: geometry.header.x,
          y: 0,
          width: geometry.header.width,
          height: viewport.height
        };
      }

      const fileName = `${String(index + 1).padStart(2, "0")}-${screenId}.png`;
      await page.screenshot({
        path: path.join(outputRoot, fileName),
        clip,
        animations: "disabled"
      });
      process.stdout.write(`CAPTURED=${fileName}\n`);
    }
    if (browserErrors.length) {
      throw new Error(`Atlas browser errors:\n${browserErrors.join("\n")}`);
    }
  } finally {
    await browser.close();
    await new Promise((resolve) => server.close(resolve));
  }
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
