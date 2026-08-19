"""
render_deck.py - Export PPTX slides to high-res PNGs, generate a contact sheet, and export speaker notes.

Usage:
    python render_deck.py <input.pptx> [--output-dir <dir>] [--contact-sheet] [--columns <int>]
"""

import argparse
import math
import os
import subprocess
import sys
from PIL import Image
from pptx import Presentation

# Ensure UTF-8 output on Windows
if sys.stdout and hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")
if sys.stderr and hasattr(sys.stderr, "reconfigure"):
    sys.stderr.reconfigure(encoding="utf-8")


def render_pptx_via_powerpoint(input_pptx, output_dir):
    script_dir = os.path.dirname(os.path.abspath(__file__))
    ps1_script = os.path.join(script_dir, "render-powerpoint.ps1")
    os.makedirs(output_dir, exist_ok=True)

    cmd = [
        "powershell",
        "-NoLogo",
        "-NoProfile",
        "-ExecutionPolicy",
        "Bypass",
        "-File",
        ps1_script,
        "-InputPptx",
        os.path.abspath(input_pptx),
        "-OutputDirectory",
        os.path.abspath(output_dir),
    ]

    print(f"[Render] Exporting slides using PowerPoint COM...")
    proc = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8")
    if proc.returncode != 0:
        print(f"[Error] PowerPoint export failed:\n{proc.stderr}")
        raise RuntimeError(f"PowerPoint COM export failed with exit code {proc.returncode}")
    print(f"[Render] {proc.stdout.strip()}")


def create_contact_sheet(slide_images, output_path, cols=4, thumb_width=640):
    if not slide_images:
        return

    first_img = Image.open(slide_images[0])
    aspect = first_img.height / first_img.width
    thumb_height = int(thumb_width * aspect)
    first_img.close()

    total = len(slide_images)
    cols = min(cols, total)
    rows = math.ceil(total / cols)

    padding = 16
    grid_w = cols * thumb_width + (cols + 1) * padding
    grid_h = rows * thumb_height + (rows + 1) * padding

    contact_img = Image.new("RGB", (grid_w, grid_h), color=(30, 30, 30))

    for idx, img_path in enumerate(slide_images):
        col = idx % cols
        row = idx // cols
        x = padding + col * (thumb_width + padding)
        y = padding + row * (thumb_height + padding)

        with Image.open(img_path) as im:
            resized = im.resize((thumb_width, thumb_height), Image.Resampling.LANCZOS)
            contact_img.paste(resized, (x, y))

    contact_img.save(output_path, quality=90)
    print(f"[Contact Sheet] Created: {output_path} ({grid_w}x{grid_h})")


def export_notes(input_pptx, output_file):
    prs = Presentation(input_pptx)
    lines = [f"# Speaker Notes: {os.path.basename(input_pptx)}\n"]
    for idx, slide in enumerate(prs.slides, start=1):
        title = ""
        if slide.shapes.title and slide.shapes.title.text.strip():
            title = f" - {slide.shapes.title.text.strip()}"
        lines.append(f"## Slide {idx:02d}{title}\n")
        if slide.has_notes_slide and slide.notes_slide.notes_text_frame:
            notes = slide.notes_slide.notes_text_frame.text.strip()
            if notes:
                lines.append(f"{notes}\n")
            else:
                lines.append("*(No notes)*\n")
        else:
            lines.append("*(No notes)*\n")
        lines.append("---\n")

    with open(output_file, "w", encoding="utf-8") as f:
        f.write("\n".join(lines))
    print(f"[Notes] Exported notes to: {output_file}")


def main():
    parser = argparse.ArgumentParser(description="Render PPTX slides to PNG and contact sheet")
    parser.add_argument("pptx", help="Path to PPTX file")
    parser.add_argument("--output-dir", "-o", help="Output directory (defaults to .artifacts/<deck_name>_render)")
    parser.add_argument("--columns", "-c", type=int, default=4, help="Number of columns in contact sheet")
    parser.add_argument("--no-contact-sheet", action="store_true", help="Skip creating contact sheet")

    args = parser.parse_args()

    input_pptx = os.path.abspath(args.pptx)
    if not os.path.exists(input_pptx):
        print(f"File not found: {input_pptx}", file=sys.stderr)
        sys.exit(1)

    deck_name = os.path.splitext(os.path.basename(input_pptx))[0]
    if args.output_dir:
        out_dir = os.path.abspath(args.output_dir)
    else:
        out_dir = os.path.abspath(os.path.join(".artifacts", f"{deck_name}_render"))

    os.makedirs(out_dir, exist_ok=True)

    # 1. Render slides
    render_pptx_via_powerpoint(input_pptx, out_dir)

    # 2. Find rendered slide images
    slide_images = sorted([
        os.path.join(out_dir, f)
        for f in os.listdir(out_dir)
        if f.startswith("slide-") and f.endswith(".png")
    ])

    # 3. Contact sheet
    if not args.no_contact_sheet and slide_images:
        contact_sheet_path = os.path.join(out_dir, f"{deck_name}_contact_sheet.png")
        create_contact_sheet(slide_images, contact_sheet_path, cols=args.columns)

    # 4. Speaker notes
    notes_path = os.path.join(out_dir, f"{deck_name}_notes.md")
    export_notes(input_pptx, notes_path)

    print(f"[Done] All outputs saved in: {out_dir}")


if __name__ == "__main__":
    main()
