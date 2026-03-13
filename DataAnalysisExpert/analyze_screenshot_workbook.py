from __future__ import annotations

import json
import re
import zipfile
from collections import Counter, defaultdict
from pathlib import Path
from typing import Any
from urllib.parse import urlparse
from xml.etree import ElementTree as ET


WORKBOOK_PATH = Path(r"d:\dev\unigetui-app-icons\WebBasedData\screenshot_database.xlsx")
REPORT_PATH = Path(r"d:\dev\unigetui-app-icons\DataAnalysisExpert\workbook_analysis.md")
JSON_PATH = Path(r"d:\dev\unigetui-app-icons\DataAnalysisExpert\workbook_analysis.json")

DATA_SHEET_INDEX = 0
HEADER_ROW = 1
DATA_START_ROW = 2
KEY_COLUMN = 1
ICON_COLUMN = 2
FIRST_SCREENSHOT_COLUMN = 3
LAST_EXPORTED_COLUMN = 24  # A:X; current generator stops after zero-based index 23

VALID_MANAGER_PREFIXES = [
    "Winget",
    "Scoop",
    "Chocolatey",
    "Pip",
    "Npm",
    "vcpkg",
    "Cargo",
    "PowerShell",
    "PowerShell7",
]
KNOWN_MANAGERLIKE_PREFIXES = VALID_MANAGER_PREFIXES + ["WinGet", "Vcpkg"]
NORMALIZED_ID_PATTERN = re.compile(r"^[@a-z0-9][@a-z0-9+:-]*(?:-[@a-z0-9+:-]+)*$")
NS = {
    "main": "http://schemas.openxmlformats.org/spreadsheetml/2006/main",
    "rel": "http://schemas.openxmlformats.org/officeDocument/2006/relationships",
    "pkgrel": "http://schemas.openxmlformats.org/package/2006/relationships",
}


class SimpleCell:
    def __init__(self, value: str):
        self.value = value


class SimpleSheet:
    def __init__(self, title: str, cells: dict[tuple[int, int], str]):
        self.title = title
        self._cells = cells
        self.max_row = max((row for row, _ in cells.keys()), default=0)
        self.max_column = max((col for _, col in cells.keys()), default=0)

    def cell(self, row: int, column: int) -> SimpleCell:
        return SimpleCell(self._cells.get((row, column), ""))

    def iter_rows(
        self,
        min_row: int = 1,
        max_row: int | None = None,
        max_col: int | None = None,
        values_only: bool = False,
    ):
        last_row = self.max_row if max_row is None else max_row
        last_col = self.max_column if max_col is None else max_col
        for row_index in range(min_row, last_row + 1):
            row_values = [self._cells.get((row_index, col_index), "") for col_index in range(1, last_col + 1)]
            if values_only:
                yield tuple(row_values)
            else:
                yield tuple(SimpleCell(value) for value in row_values)


class SimpleWorkbook:
    def __init__(self, worksheets: list[SimpleSheet]):
        self.worksheets = worksheets


def get_column_letter(column_index: int) -> str:
    letters: list[str] = []
    value = column_index
    while value > 0:
        value, remainder = divmod(value - 1, 26)
        letters.append(chr(ord("A") + remainder))
    return "".join(reversed(letters))


def column_index_from_reference(cell_reference: str) -> int:
    letters = ""
    for char in cell_reference:
        if char.isalpha():
            letters += char.upper()
        else:
            break
    value = 0
    for char in letters:
        value = value * 26 + (ord(char) - ord("A") + 1)
    return value


def read_shared_strings(archive: zipfile.ZipFile) -> list[str]:
    path = "xl/sharedStrings.xml"
    if path not in archive.namelist():
        return []

    root = ET.fromstring(archive.read(path))
    values: list[str] = []
    for item in root.findall("main:si", NS):
        values.append("".join(item.itertext()))
    return values


def read_workbook(path: Path) -> SimpleWorkbook:
    with zipfile.ZipFile(path) as archive:
        shared_strings = read_shared_strings(archive)

        workbook_root = ET.fromstring(archive.read("xl/workbook.xml"))
        rels_root = ET.fromstring(archive.read("xl/_rels/workbook.xml.rels"))
        rel_targets = {
            rel.attrib["Id"]: rel.attrib["Target"]
            for rel in rels_root.findall("pkgrel:Relationship", NS)
        }

        worksheets: list[SimpleSheet] = []
        for sheet in workbook_root.findall("main:sheets/main:sheet", NS):
            sheet_name = sheet.attrib["name"]
            rel_id = sheet.attrib[f"{{{NS['rel']}}}id"]
            target = rel_targets[rel_id]
            if not target.startswith("/"):
                target = f"xl/{target}"
            else:
                target = target.lstrip("/")

            sheet_root = ET.fromstring(archive.read(target))
            cells: dict[tuple[int, int], str] = {}

            for row in sheet_root.findall("main:sheetData/main:row", NS):
                for cell in row.findall("main:c", NS):
                    reference = cell.attrib.get("r", "")
                    if not reference:
                        continue
                    match = re.match(r"([A-Z]+)(\d+)", reference, flags=re.IGNORECASE)
                    if not match:
                        continue

                    column_index = column_index_from_reference(reference)
                    row_index = int(match.group(2))
                    cell_type = cell.attrib.get("t")

                    if cell_type == "inlineStr":
                        inline = cell.find("main:is", NS)
                        value = "".join(inline.itertext()) if inline is not None else ""
                    else:
                        value_node = cell.find("main:v", NS)
                        raw_value = value_node.text if value_node is not None and value_node.text is not None else ""
                        if cell_type == "s":
                            value = shared_strings[int(raw_value)] if raw_value != "" else ""
                        else:
                            value = raw_value

                    cells[(row_index, column_index)] = value

            worksheets.append(SimpleSheet(sheet_name, cells))

    return SimpleWorkbook(worksheets)


def normalize_text(value: Any) -> str:
    if value is None:
        return ""
    if isinstance(value, str):
        return value
    if isinstance(value, float) and value.is_integer():
        return str(int(value))
    return str(value)


def is_nonempty(value: str) -> bool:
    return value != ""


def is_valid_http_url(value: str) -> bool:
    if value == "":
        return False
    if value != value.strip():
        return False
    if any(ch in value for ch in "\r\n\t"):
        return False
    parsed = urlparse(value)
    return parsed.scheme in {"http", "https"} and bool(parsed.netloc)


def has_whitespace_contamination(value: str) -> bool:
    return value != value.strip() or any(ch in value for ch in "\r\n\t")


def sheet_structure(sheet) -> dict[str, Any]:
    nonempty_rows = 0
    nonempty_cols = Counter()
    first_nonempty_row = None
    last_nonempty_row = None
    first_nonempty_col = None
    last_nonempty_col = None

    for row_index, row in enumerate(
        sheet.iter_rows(min_row=1, max_row=sheet.max_row, max_col=sheet.max_column, values_only=True),
        start=1,
    ):
        row_has_value = False
        for col_index, value in enumerate(row, start=1):
            text = normalize_text(value)
            if text != "":
                row_has_value = True
                nonempty_cols[col_index] += 1
                first_nonempty_col = col_index if first_nonempty_col is None else min(first_nonempty_col, col_index)
                last_nonempty_col = col_index if last_nonempty_col is None else max(last_nonempty_col, col_index)
        if row_has_value:
            nonempty_rows += 1
            first_nonempty_row = row_index if first_nonempty_row is None else first_nonempty_row
            last_nonempty_row = row_index

    header = [
        normalize_text(sheet.cell(HEADER_ROW, column).value)
        for column in range(1, min(sheet.max_column, LAST_EXPORTED_COLUMN) + 1)
    ]

    columns = []
    for col_index in range(1, sheet.max_column + 1):
        if nonempty_cols[col_index] == 0:
            continue
        columns.append(
            {
                "column": get_column_letter(col_index),
                "index": col_index,
                "nonemptyCells": nonempty_cols[col_index],
                "header": normalize_text(sheet.cell(HEADER_ROW, col_index).value),
            }
        )

    return {
        "title": sheet.title,
        "maxRow": sheet.max_row,
        "maxColumn": sheet.max_column,
        "nonemptyRowCount": nonempty_rows,
        "firstNonemptyRow": first_nonempty_row,
        "lastNonemptyRow": last_nonempty_row,
        "firstNonemptyColumn": get_column_letter(first_nonempty_col) if first_nonempty_col else None,
        "lastNonemptyColumn": get_column_letter(last_nonempty_col) if last_nonempty_col else None,
        "headerPreview": header,
        "columns": columns,
    }


def classify_lookup_shape(key: str) -> dict[str, Any]:
    manager_prefix = None
    suffix = ""
    if "." in key:
        manager_prefix, suffix = key.split(".", 1)

    is_exact_candidate = bool(manager_prefix in VALID_MANAGER_PREFIXES and suffix)
    is_normalized_candidate = bool(
        key
        and key == key.strip()
        and not any(ch in key for ch in " _./,\r\n\t")
        and key == key.lower()
        and NORMALIZED_ID_PATTERN.fullmatch(key)
    )

    definite_mismatch_reason = None
    if key.strip() == "":
        definite_mismatch_reason = "blank-or-whitespace"
    elif manager_prefix in {"WinGet", "Vcpkg"}:
        definite_mismatch_reason = f"wrong-manager-prefix-case:{manager_prefix}"
    elif manager_prefix in VALID_MANAGER_PREFIXES and has_whitespace_contamination(suffix):
        definite_mismatch_reason = "prefixed-id-has-whitespace"
    elif has_whitespace_contamination(key):
        definite_mismatch_reason = "key-has-whitespace"
    elif manager_prefix in KNOWN_MANAGERLIKE_PREFIXES and manager_prefix not in VALID_MANAGER_PREFIXES:
        definite_mismatch_reason = f"unknown-manager-prefix:{manager_prefix}"
    elif not is_exact_candidate and not is_normalized_candidate:
        definite_mismatch_reason = "does-not-look-like-exact-or-normalized-id"

    return {
        "managerPrefix": manager_prefix,
        "suffix": suffix,
        "isExactCandidate": is_exact_candidate,
        "isNormalizedCandidate": is_normalized_candidate,
        "definiteMismatchReason": definite_mismatch_reason,
    }


def row_entry(sheet, row_index: int) -> dict[str, Any]:
    key = normalize_text(sheet.cell(row_index, KEY_COLUMN).value)
    icon = normalize_text(sheet.cell(row_index, ICON_COLUMN).value)

    screenshot_values = []
    screenshot_cells = []
    for column in range(FIRST_SCREENSHOT_COLUMN, LAST_EXPORTED_COLUMN + 1):
        value = normalize_text(sheet.cell(row_index, column).value)
        screenshot_cells.append(
            {
                "column": get_column_letter(column),
                "value": value,
            }
        )
        if value != "":
            screenshot_values.append({"column": get_column_letter(column), "value": value})

    contiguous_screenshots = []
    gap_found = False
    ignored_after_gap = []
    for cell in screenshot_cells:
        if cell["value"] == "":
            gap_found = True
            continue
        if gap_found:
            ignored_after_gap.append(cell)
        else:
            contiguous_screenshots.append(cell)

    populated = any(
        value != ""
        for value in [key, icon, *[cell["value"] for cell in screenshot_cells]]
    )

    return {
        "row": row_index,
        "key": key,
        "icon": icon,
        "screenshots": contiguous_screenshots,
        "allScreenshotCells": screenshot_cells,
        "ignoredScreenshotsAfterGap": ignored_after_gap,
        "isPopulated": populated,
    }


def collect_examples(items: list[dict[str, Any]], limit: int = 5) -> list[dict[str, Any]]:
    return items[:limit]


def render_row_example(item: dict[str, Any]) -> str:
    screenshots = ", ".join(f"{cell['column']}{item['row']}" for cell in item.get("screenshots", [])[:3])
    if item.get("ignoredScreenshotsAfterGap"):
        screenshots = screenshots + (", " if screenshots else "") + "ignored: " + ", ".join(
            f"{cell['column']}{item['row']}" for cell in item["ignoredScreenshotsAfterGap"][:3]
        )
    return (
        f"row {item['row']} | A{item['row']}={item['key']!r} | "
        f"B{item['row']}={item['icon']!r} | screenshots={screenshots or 'none'}"
    )


def main() -> None:
    workbook = read_workbook(WORKBOOK_PATH)
    sheet_summaries = [sheet_structure(sheet) for sheet in workbook.worksheets]

    data_sheet = workbook.worksheets[DATA_SHEET_INDEX]
    rows = [row_entry(data_sheet, row_index) for row_index in range(DATA_START_ROW, data_sheet.max_row + 1)]
    populated_rows = [row for row in rows if row["isPopulated"]]

    keys = [row["key"] for row in populated_rows if row["key"] != ""]
    icon_rows = [row for row in populated_rows if row["icon"] != ""]
    screenshot_rows = [row for row in populated_rows if row["screenshots"]]
    screenshot_cell_count = sum(len(row["screenshots"]) for row in populated_rows)
    ignored_screenshot_cell_count = sum(len(row["ignoredScreenshotsAfterGap"]) for row in populated_rows)

    exact_duplicates = defaultdict(list)
    case_insensitive_duplicates = defaultdict(list)
    for row in populated_rows:
        key = row["key"]
        if key == "":
            continue
        exact_duplicates[key].append(row)
        case_insensitive_duplicates[key.lower()].append(row)

    exact_duplicate_groups = [group for group in exact_duplicates.values() if len(group) > 1]
    case_only_duplicate_groups = []
    for group in case_insensitive_duplicates.values():
        if len(group) > 1 and len({item["key"] for item in group}) > 1:
            case_only_duplicate_groups.append(group)

    leading_trailing_id_rows = [row for row in populated_rows if row["key"] != "" and row["key"] != row["key"].strip()]
    empty_id_rows = [row for row in populated_rows if row["key"].strip() == ""]
    invalid_looking_id_rows = []
    suspicious_prefixed_rows = []
    icon_missing_screenshots_rows = []
    malformed_icon_rows = []
    contaminated_rows = []
    lookup_mismatch_rows = []
    screenshot_gap_rows = [row for row in populated_rows if row["ignoredScreenshotsAfterGap"]]

    for row in populated_rows:
        lookup_shape = classify_lookup_shape(row["key"])
        row["lookupShape"] = lookup_shape
        key = row["key"]
        icon = row["icon"]
        screenshots = row["screenshots"]

        if key.strip() == "" or key.startswith(("http://", "https://")) or re.fullmatch(r"[-._ ]+", key or ""):
            invalid_looking_id_rows.append(row)

        if lookup_shape["managerPrefix"] in KNOWN_MANAGERLIKE_PREFIXES and not lookup_shape["isExactCandidate"]:
            suspicious_prefixed_rows.append(row)

        if icon == "" and screenshots:
            icon_missing_screenshots_rows.append(row)

        if icon != "" and not is_valid_http_url(icon):
            malformed_icon_rows.append(row)

        screenshot_values = [cell["value"] for cell in row["allScreenshotCells"] if cell["value"] != ""]
        if has_whitespace_contamination(key) or has_whitespace_contamination(icon) or any(
            has_whitespace_contamination(value) for value in screenshot_values
        ):
            contaminated_rows.append(row)

        if lookup_shape["definiteMismatchReason"]:
            lookup_mismatch_rows.append(row)

    examples = []
    for title, source, limit in [
        ("lookup mismatches", lookup_mismatch_rows, 4),
        ("malformed icon URLs", malformed_icon_rows, 4),
        ("icon missing but screenshots exist", icon_missing_screenshots_rows, 4),
        ("case-only duplicates", [item for group in case_only_duplicate_groups for item in group], 2),
        ("exact duplicate keys", [item for group in exact_duplicate_groups for item in group], 4),
        ("whitespace contamination", contaminated_rows, 4),
        ("screenshot gaps ignored by exporter", screenshot_gap_rows, 4),
    ]:
        for item in source[:limit]:
            if len(examples) >= 20:
                break
            examples.append({"category": title, **item})
        if len(examples) >= 20:
            break

    result = {
        "workbookPath": str(WORKBOOK_PATH),
        "sheetSummaries": sheet_summaries,
        "feedModel": {
            "dataSheet": data_sheet.title,
            "headerRow": HEADER_ROW,
            "dataStartsAtRow": DATA_START_ROW,
            "exportedColumns": "A:X",
            "columnMeaning": {
                "A": "package key",
                "B": "icon URL",
                "C:X": "screenshot URL slots; exporter stops at the first blank screenshot cell in a row",
            },
        },
        "counts": {
            "populatedDataRows": len(populated_rows),
            "rowsWithIconUrl": len(icon_rows),
            "rowsWithAtLeastOneScreenshotUrl": len(screenshot_rows),
            "populatedScreenshotUrlCells": screenshot_cell_count,
            "screenshotUrlCellsIgnoredAfterGap": ignored_screenshot_cell_count,
        },
        "quality": {
            "exactDuplicateKeyGroups": len(exact_duplicate_groups),
            "exactDuplicateRows": sum(len(group) for group in exact_duplicate_groups),
            "caseOnlyDuplicateGroups": len(case_only_duplicate_groups),
            "caseOnlyDuplicateRows": sum(len(group) for group in case_only_duplicate_groups),
            "leadingOrTrailingWhitespaceIds": len(leading_trailing_id_rows),
            "emptyOrWhitespaceIds": len(empty_id_rows),
            "invalidLookingIds": len(invalid_looking_id_rows),
            "suspiciousManagerPrefixedIds": len(suspicious_prefixed_rows),
            "iconMissingButScreenshotsExist": len(icon_missing_screenshots_rows),
            "iconPresentButMalformedUrl": len(malformed_icon_rows),
            "rowsWithWhitespaceContamination": len(contaminated_rows),
            "rowsLikelyNotMatchingLookupLogic": len(lookup_mismatch_rows),
            "screenshotGapRows": len(screenshot_gap_rows),
        },
        "lookupCompatibility": {
            "exactManagerPrefixes": VALID_MANAGER_PREFIXES,
            "rowsThatLookLikeExactManagerPackageIds": sum(
                1 for row in populated_rows if row["lookupShape"]["isExactCandidate"]
            ),
            "rowsThatLookLikeNormalizedIds": sum(
                1 for row in populated_rows if row["lookupShape"]["isNormalizedCandidate"]
            ),
            "definiteMismatchReasons": Counter(
                row["lookupShape"]["definiteMismatchReason"] for row in lookup_mismatch_rows
            ),
        },
        "samples": {
            "headerRow": {
                f"{get_column_letter(column)}1": normalize_text(data_sheet.cell(1, column).value)
                for column in range(1, min(data_sheet.max_column, LAST_EXPORTED_COLUMN) + 1)
            },
            "firstDataRows": [
                {
                    "row": item["row"],
                    "A": item["key"],
                    "B": item["icon"],
                    "screenshots": [cell["value"] for cell in item["screenshots"][:3]],
                }
                for item in populated_rows[:5]
            ],
        },
        "topExamples": examples,
        "exampleBuckets": {
            "lookupMismatch": collect_examples(lookup_mismatch_rows, 8),
            "malformedIconUrl": collect_examples(malformed_icon_rows, 8),
            "iconMissingWithScreenshots": collect_examples(icon_missing_screenshots_rows, 8),
            "leadingTrailingWhitespaceIds": collect_examples(leading_trailing_id_rows, 8),
            "whitespaceContamination": collect_examples(contaminated_rows, 8),
            "caseOnlyDuplicateGroups": [collect_examples(group, 4) for group in case_only_duplicate_groups[:5]],
            "exactDuplicateGroups": [collect_examples(group, 4) for group in exact_duplicate_groups[:5]],
            "screenshotGapRows": collect_examples(screenshot_gap_rows, 8),
        },
    }

    JSON_PATH.write_text(json.dumps(result, indent=2, ensure_ascii=True), encoding="utf-8")

    lines = []
    lines.append("# screenshot_database.xlsx analysis")
    lines.append("")
    lines.append(f"Workbook: {WORKBOOK_PATH}")
    lines.append(f"Primary feed sheet: {data_sheet.title}")
    lines.append("")
    lines.append("## Workbook structure")
    for summary in sheet_summaries:
        lines.append(
            f"- {summary['title']}: maxRow={summary['maxRow']}, maxColumn={summary['maxColumn']}, "
            f"nonemptyRows={summary['nonemptyRowCount']}, usedRange={summary['firstNonemptyColumn']}{summary['firstNonemptyRow']}:{summary['lastNonemptyColumn']}{summary['lastNonemptyRow']}"
        )
    lines.append("")
    lines.append("## Feed model")
    lines.append("- Export script reads only the first worksheet.")
    lines.append("- Row 1 is treated as headers; data starts at row 2.")
    lines.append("- Column A = package key, B = icon URL, C:X = screenshot URL slots.")
    lines.append("- Screenshot export stops at the first blank screenshot cell in a row.")
    lines.append("")
    lines.append("## Counts")
    lines.append(f"- Populated data rows: {result['counts']['populatedDataRows']}")
    lines.append(f"- Rows with icon URL: {result['counts']['rowsWithIconUrl']}")
    lines.append(f"- Rows with at least one screenshot URL: {result['counts']['rowsWithAtLeastOneScreenshotUrl']}")
    lines.append(f"- Populated screenshot URL cells: {result['counts']['populatedScreenshotUrlCells']}")
    if ignored_screenshot_cell_count:
        lines.append(f"- Screenshot URL cells ignored by current gap-sensitive export: {ignored_screenshot_cell_count}")
    lines.append("")
    lines.append("## Quality issues")
    for label, value in [
        ("Exact duplicate key groups", result['quality']['exactDuplicateKeyGroups']),
        ("Case-only duplicate groups", result['quality']['caseOnlyDuplicateGroups']),
        ("IDs with leading/trailing whitespace", result['quality']['leadingOrTrailingWhitespaceIds']),
        ("Empty/whitespace IDs", result['quality']['emptyOrWhitespaceIds']),
        ("Invalid-looking IDs", result['quality']['invalidLookingIds']),
        ("Suspicious manager-prefixed IDs", result['quality']['suspiciousManagerPrefixedIds']),
        ("Rows with screenshots but no icon", result['quality']['iconMissingButScreenshotsExist']),
        ("Rows with malformed icon URL", result['quality']['iconPresentButMalformedUrl']),
        ("Rows with whitespace/newline contamination", result['quality']['rowsWithWhitespaceContamination']),
        ("Rows likely not to match current lookup logic", result['quality']['rowsLikelyNotMatchingLookupLogic']),
        ("Rows with screenshot gaps", result['quality']['screenshotGapRows']),
    ]:
        lines.append(f"- {label}: {value}")
    lines.append("")
    lines.append("## Lookup compatibility")
    lines.append(
        "- Current runtime order: exact `ManagerName.PackageId`, then normalized icon id from `Package.GenerateIconId()`."
    )
    lines.append(
        f"- Exact-form candidates: {result['lookupCompatibility']['rowsThatLookLikeExactManagerPackageIds']}"
    )
    lines.append(
        f"- Normalized-id candidates: {result['lookupCompatibility']['rowsThatLookLikeNormalizedIds']}"
    )
    lines.append("- Definite mismatch reasons:")
    for reason, count in result["lookupCompatibility"]["definiteMismatchReasons"].items():
        lines.append(f"  - {reason}: {count}")
    lines.append("")
    lines.append("## Example rows")
    for item in examples:
        lines.append(f"- [{item['category']}] {render_row_example(item)}")

    REPORT_PATH.write_text("\n".join(lines) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()