import fs from "node:fs/promises";
import path from "node:path";
import { FileBlob, PresentationFile } from "@oai/artifact-tool";

async function saveBlob(outputPath, blob) {
  await fs.mkdir(path.dirname(outputPath), { recursive: true });
  await fs.writeFile(outputPath, new Uint8Array(await blob.arrayBuffer()));
}

const inputPath = process.argv[2];
const outputDir = process.argv[3];
if (!inputPath || !outputDir) {
  throw new Error("Usage: node inspect-deck.mjs <input.pptx> <output-dir>");
}

await fs.mkdir(outputDir, { recursive: true });
const presentation = await PresentationFile.importPptx(await FileBlob.load(inputPath));
const snapshot = await presentation.inspect({
  kind: "deck,slide,textbox,shape,image,table,chart,notes,layout",
  maxChars: 300000,
});
await fs.writeFile(path.join(outputDir, "inspect.ndjson"), snapshot.ndjson, "utf8");

for (const [index, slide] of presentation.slides.items.entries()) {
  const stem = `slide-${String(index + 1).padStart(2, "0")}`;
  await saveBlob(path.join(outputDir, `${stem}.png`), await presentation.export({ slide, format: "png", scale: 1 }));
  const layout = await slide.export({ format: "layout" });
  await fs.writeFile(path.join(outputDir, `${stem}.layout.json`), await layout.text(), "utf8");
}

await saveBlob(
  path.join(outputDir, "montage.webp"),
  await presentation.export({ format: "webp", montage: true, scale: 0.35 }),
);
console.log(JSON.stringify({ slides: presentation.slides.items.length, outputDir }));
