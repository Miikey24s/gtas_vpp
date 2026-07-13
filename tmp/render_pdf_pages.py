import argparse
from pathlib import Path

import pypdfium2 as pdfium


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("pdf")
    ap.add_argument("outdir")
    ap.add_argument("--first", type=int, default=1)
    ap.add_argument("--last", type=int, default=10)
    ap.add_argument("--scale", type=float, default=2.0)
    args = ap.parse_args()

    outdir = Path(args.outdir)
    outdir.mkdir(parents=True, exist_ok=True)
    pdf = pdfium.PdfDocument(args.pdf)
    last = min(args.last, len(pdf))
    for page_number in range(args.first, last + 1):
        page = pdf[page_number - 1]
        bitmap = page.render(scale=args.scale)
        image = bitmap.to_pil()
        image.save(outdir / f"page-{page_number:02d}.png")
        page.close()
    print(f"pages={len(pdf)} rendered={args.first}-{last}")


if __name__ == "__main__":
    main()
