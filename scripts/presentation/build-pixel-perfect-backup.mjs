import fs from "node:fs/promises";
import path from "node:path";
import { FileBlob, Presentation, PresentationFile } from "@oai/artifact-tool";

async function readImageBytes(imagePath) {
  const bytes = await fs.readFile(imagePath);
  return bytes.buffer.slice(bytes.byteOffset, bytes.byteOffset + bytes.byteLength);
}

const inputPath = process.argv[2];
const imageDir = process.argv[3];
const outputPath = process.argv[4];
if (!inputPath || !imageDir || !outputPath) {
  throw new Error("Usage: node build-pixel-perfect-backup.mjs <input.pptx> <image-dir> <output.pptx>");
}

const source = await PresentationFile.importPptx(await FileBlob.load(inputPath));
const notesSnapshot = await source.inspect({ kind: "notes", maxChars: 200000 });
const notesBySlide = new Map();
for (const line of notesSnapshot.ndjson.split(/\r?\n/)) {
  if (!line.trim()) continue;
  const record = JSON.parse(line);
  if (record.kind === "notes" && Number.isInteger(record.slide)) {
    notesBySlide.set(record.slide, record.text ?? "");
  }
}

const presentation = Presentation.create({ slideSize: { width: 960, height: 540 } });
for (let index = 0; index < source.slides.items.length; index += 1) {
  const slide = presentation.slides.add();
  const imagePath = path.join(imageDir, `slide-${String(index + 1).padStart(2, "0")}.png`);
  const image = slide.images.add({
    blob: await readImageBytes(imagePath),
    contentType: "image/png",
    alt: `GTAS VPP - slide ${index + 1}`,
    fit: "cover",
    position: { left: 0, top: 0, width: 960, height: 540 },
  });
  image.lockAspectRatio = true;

  const notes = notesBySlide.get(index + 1);
  if (notes) {
    slide.speakerNotes.textFrame.setText(notes);
    slide.speakerNotes.setVisible(true);
  }
}

const pptx = await PresentationFile.exportPptx(presentation);
await pptx.save(outputPath);
console.log(JSON.stringify({ outputPath, slides: presentation.slides.items.length }));
