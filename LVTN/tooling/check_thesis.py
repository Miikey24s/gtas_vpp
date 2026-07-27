from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
AUDIT = ROOT / "LVTN" / "tooling" / "audit_thesis.py"


completed = subprocess.run(
    [sys.executable, str(AUDIT), *sys.argv[1:]],
    cwd=ROOT,
    check=True,
    capture_output=True,
    text=True,
    encoding="utf-8",
)
result = json.loads(completed.stdout)

failures: list[str] = []


def require(condition: bool, message: str) -> None:
    if not condition:
        failures.append(message)


require(result["zip_bad_member"] is None, "DOCX ZIP contains a corrupt member")
require(result["page_border_count"] == 1, "Expected exactly one cover page border")
require(result["page_border_displays"] == ["firstPage"], "Cover border must apply to the first page only")
require(
    result["cover_title_lines"] == [
        "XÂY DỰNG WEBSITE QUẢN LÝ",
        "VĂN PHÒNG PHẨM PHONG PHÚ",
    ],
    "Cover title line break differs from the owner-approved layout",
)
require(result["toc_entry_count"] == result["toc_link_count"], "TOC entries are not fully linked")
require(not result["toc_unresolved_anchors"], "TOC contains unresolved anchors")
require(result["figure_list_entry_count"] == result["figure_bookmark_count"], "Figure list/bookmark counts differ")
require(result["figure_list_entry_count"] == result["figure_list_link_count"], "Figure list links are incomplete")
require(not result["figure_list_caption_mismatches"], "Figure list captions differ from body captions")
require(not result["figure_list_invalid_page_results"], "Figure list contains invalid page results")
require(not result["figure_list_invalid_pageref_fields"], "Figure list contains invalid PAGEREF fields")
require(not result["body_caption_missing_or_extra_bookmarks"], "Body captions have missing or extra bookmarks")
require(not result["unresolved_internal_anchors"], "Internal hyperlinks contain unresolved anchors")
require(not result["broken_external_link_ids"], "External hyperlinks have missing relationships")
require(not result["orphan_hyperlink_relationship_ids"], "DOCX contains orphan hyperlink relationships")
require(not result["missing_internal_relationship_targets"], "DOCX contains missing internal relationship targets")
require(result["bookmark_names_unique"], "Bookmark names are not unique")
require(result["bookmark_ids_unique"], "Bookmark IDs are not unique")
require(result["bookmark_start_end_ids_match"], "Bookmark start/end IDs differ")
require(not result["orphan_media"], "DOCX contains orphan media")
require(not result["missing_media_targets"], "DOCX contains missing media targets")
require(not result["figure_caption_wrong_multiplicity"], "Figure captions do not occur exactly twice")
require(not result["unexpected_figure_caption_ids"], "Unexpected figure caption IDs found")
require(all(value == 0 for value in result["tracked_changes"].values()), "Tracked changes remain in the final DOCX")
require(not result["comment_parts"], "Comment parts remain in the final DOCX")
require(not any(result["error_text_present"].values()), "Word field error text remains in the final DOCX")
require(all(result["corrected_figure_names_present"].values()), "Corrected figure names are missing")

if failures:
    for failure in failures:
        print(f"ERROR: {failure}", file=sys.stderr)
    raise SystemExit(1)

print(
    "Canonical thesis verification passed: "
    f"{result['toc_link_count']} TOC links, "
    f"{result['figure_list_link_count']} figure links, "
    f"{result['internal_hyperlinks']} internal hyperlinks."
)
