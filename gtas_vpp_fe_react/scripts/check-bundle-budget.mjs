import { readdir, readFile } from 'node:fs/promises'
import { gzipSync } from 'node:zlib'

const assetsDirectory = new URL('../dist/assets/', import.meta.url)
const limits = {
  javascriptChunk: 140 * 1024,
  stylesheet: 30 * 1024,
  total: 550 * 1024,
}

const files = await readdir(assetsDirectory)
const measured = []

for (const file of files) {
  if (!file.endsWith('.js') && !file.endsWith('.css')) continue
  const content = await readFile(new URL(file, assetsDirectory))
  measured.push({ file, gzipBytes: gzipSync(content).byteLength })
}

const violations = measured.filter(({ file, gzipBytes }) => {
  const limit = file.endsWith('.css')
    ? limits.stylesheet
    : limits.javascriptChunk
  return gzipBytes > limit
})
const total = measured.reduce((sum, asset) => sum + asset.gzipBytes, 0)

if (total > limits.total) {
  violations.push({ file: 'TOTAL_JS_CSS', gzipBytes: total })
}

const toKiB = (bytes) => `${(bytes / 1024).toFixed(1)} KiB gzip`
const largest = measured.toSorted(
  (left, right) => right.gzipBytes - left.gzipBytes,
)[0]

console.log(
  `Bundle budget: ${toKiB(total)} total; largest ${largest.file} (${toKiB(largest.gzipBytes)}).`,
)

if (violations.length > 0) {
  for (const violation of violations) {
    console.error(
      `Bundle budget exceeded: ${violation.file} = ${toKiB(violation.gzipBytes)}.`,
    )
  }
  process.exitCode = 1
}
