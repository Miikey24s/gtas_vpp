const http = require("http");
const fs = require("fs");
const path = require("path");

const atlasRoot = __dirname;
// Atlas nằm ở docs/design/atlas nên gốc repository là ba cấp phía trên.
// Trước đây đường dẫn này bị ghi cứng nên chỉ chạy được trên đúng một máy.
const repoRoot = path.resolve(atlasRoot, "..", "..", "..");
const port = Number(process.env.VPP_ATLAS_PORT || 4178);
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

http.createServer((request, response) => {
  const url = new URL(request.url, `http://127.0.0.1:${port}`);
  const decodedPath = decodeURIComponent(url.pathname);
  if (decodedPath.startsWith("/repo/")) {
    serveFile(response, safeResolve(repoRoot, decodedPath.slice("/repo/".length)));
    return;
  }
  serveFile(response, safeResolve(atlasRoot, decodedPath === "/" ? "index.html" : decodedPath));
}).listen(port, "127.0.0.1", () => {
  process.stdout.write(`GTAS VPP Design Atlas: http://127.0.0.1:${port}\nPress Ctrl+C to stop.\n`);
});
