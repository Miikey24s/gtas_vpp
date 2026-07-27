---
name: gtas-vpp-thesis-docx
description: Edit and verify the canonical GTAS VPP graduation thesis safely. Use for LVTN Word or DOCX content, cover, fields, TOC, figure list, internal links, page layout, screenshots, diagrams, formatting review, or replacing LVTN/NguyenAnNam_DH52201078.docx after owner approval.
---

# GTAS VPP Thesis DOCX

## Load authority

1. Read root `AGENTS.md`, `LVTN/AGENTS.md`, and `LVTN/README.md`.
2. Use the `documents` skill for every `.docx` edit and its render-and-verify workflow.
3. Read `LVTN/diagrams/README.md` before changing a diagram.
4. Run `./scripts/gtas.cmd preflight -Scope thesis`.

## Protect the canonical source

- Treat `LVTN/NguyenAnNam_DH52201078.docx` as read-only until owner approval.
- Verify the current canonical hash against `LVTN/README.md` before starting.
- Copy it to `LVTN/checkpoints/local_review.docx` or another ignored temporary path.
- Edit only the review copy. Never maintain working/checkpoint variants in Git.
- Do not change application code, API, database or business rules to make the thesis appear correct; update the thesis to match verified source behavior.

## Edit with layout safety

- Preserve section breaks, first-page border, headers/footers, styles, captions, bookmarks, hyperlinks and field codes unless the task explicitly changes them.
- Update TOC, figure list, references and internal links through Word fields/tooling; do not replace them with static text.
- Keep both `.puml` and `.svg` for technical diagrams and preserve black-and-white print readability.
- Use anonymized, owner-approved screenshots; keep render/contact-sheet output in ignored paths.

## Verify the review copy

1. Update affected Word fields using scripts in `LVTN/tooling/` or Microsoft Word as required.
2. Render all affected pages, then inspect page breaks, margins, border, captions, tables, links and neighboring pages visually.
3. Run:

```powershell
python LVTN/tooling/check_thesis.py LVTN/checkpoints/local_review.docx
```

4. Re-open the review copy after field updates and re-render if pagination changed.
5. Report the review path and affected pages to the owner.

## Promote after approval

Only after explicit owner approval, replace the canonical DOCX with the approved review copy, rerun the full structural check and final render review, verify the new hash, and stage only the canonical source plus intentionally changed reusable assets/tooling. Never commit signed/private documents, checkpoints, render output or personal data.
