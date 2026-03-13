import argparse
import json
import os
import re
from collections import Counter, defaultdict
from urllib.parse import urlparse
from urllib.request import urlopen

try:
    import xlrd
except ImportError:
    os.system("python -m pip install xlrd==1.2.0")
    import xlrd


ROOT_DIR = os.path.join(os.path.dirname(__file__), "..")
DATA_DIR = os.path.join(ROOT_DIR, "WebBasedData")
DEFAULT_WORKBOOK_PATH = os.path.join(DATA_DIR, "screenshot_database.xlsx")
DEFAULT_OUTPUT_PATH = os.path.join(DATA_DIR, "screenshot-database-v2.json")
DEFAULT_INVALID_URLS_PATH = os.path.join(DATA_DIR, "invalid_urls.txt")
DEFAULT_NEW_URLS_PATH = os.path.join(DATA_DIR, "new_urls.txt")
WORKBOOK_DOWNLOAD_URL = (
    "https://docs.google.com/spreadsheets/d/"
    "1Zxgzs1BiTZipC7EiwNEb9cIchistIdr5/export?format=xlsx"
)
DATA_SHEET_INDEX = 0
HEADER_ROW_INDEX = 0


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Generate the icon database JSON from screenshot_database.xlsx"
    )
    parser.add_argument(
        "--download-workbook",
        action="store_true",
        help="Refresh the local workbook from Google Sheets before exporting",
    )
    parser.add_argument(
        "--workbook-path",
        default=DEFAULT_WORKBOOK_PATH,
        help="Path to the workbook to read",
    )
    parser.add_argument(
        "--output-path",
        default=DEFAULT_OUTPUT_PATH,
        help="Path to the generated screenshot-database-v2.json file",
    )
    parser.add_argument(
        "--invalid-urls-path",
        default=DEFAULT_INVALID_URLS_PATH,
        help="Path to the invalid_urls.txt blocklist",
    )
    parser.add_argument(
        "--new-urls-path",
        default=DEFAULT_NEW_URLS_PATH,
        help="Path to the generated new_urls.txt file",
    )
    return parser.parse_args()


def ensure_parent_directory(path: str) -> None:
    directory = os.path.dirname(path)
    if directory:
        os.makedirs(directory, exist_ok=True)


def download_workbook(path: str) -> None:
    ensure_parent_directory(path)
    with open(path, "wb") as workbook_file:
        workbook_file.write(urlopen(WORKBOOK_DOWNLOAD_URL).read())


def normalize_cell(value) -> str:
    if value is None:
        return ""
    if isinstance(value, str):
        return value.strip()
    if isinstance(value, float) and value.is_integer():
        return str(int(value))
    return str(value).strip()


def is_http_url(value: str) -> bool:
    if value == "":
        return False
    parsed = urlparse(value)
    return parsed.scheme in ("http", "https") and parsed.netloc != ""


def load_forbidden_urls(path: str) -> set[str]:
    if not os.path.exists(path):
        return set()

    with open(path, "r", encoding="utf-8") as file:
        return {line.strip() for line in file if line.strip()}


def load_workbook(path: str):
    return xlrd.open_workbook(path)


def read_row(worksheet, row_index: int) -> dict[str, object] | None:
    raw_key = worksheet.cell_value(row_index, 0)
    raw_icon = worksheet.cell_value(row_index, 1)
    key = normalize_cell(raw_key)
    icon = normalize_cell(raw_icon)

    screenshots: list[str] = []
    for column_index in range(2, worksheet.ncols):
        screenshot = normalize_cell(worksheet.cell_value(row_index, column_index))
        if screenshot:
            screenshots.append(screenshot)

    if key == "" and icon == "" and not screenshots:
        return None

    return {
        "key": key,
        "icon": icon,
        "images": screenshots,
        "raw_key": raw_key,
        "raw_icon": raw_icon,
    }


def merge_images(existing: list[str], incoming: list[str]) -> list[str]:
    merged: list[str] = []
    for image in [*existing, *incoming]:
        if image and image not in merged:
            merged.append(image)
    return merged


def summarize_anomalies(anomalies: dict[str, list[dict[str, object]]]) -> str:
    return ", ".join(
        f"{name}={len(entries)}" for name, entries in anomalies.items() if entries
    ) or "none"


def export_json(
    workbook_path: str,
    output_path: str,
    invalid_urls_path: str,
    new_urls_path: str,
) -> None:
    workbook = load_workbook(workbook_path)
    worksheet = workbook.sheet_by_index(DATA_SHEET_INDEX)
    forbidden_urls = load_forbidden_urls(invalid_urls_path)

    json_content = {
        "package_count": {
            "total": 0,
            "done": 0,
            "packages_with_icon": 0,
            "packages_with_screenshot": 0,
            "total_screenshots": 0,
        },
        "icons_and_screenshots": {},
    }

    exact_duplicate_rows: dict[str, list[int]] = defaultdict(list)
    casefolded_rows: dict[str, list[tuple[int, str]]] = defaultdict(list)
    anomalies: dict[str, list[dict[str, object]]] = defaultdict(list)
    trimmed_keys = 0
    trimmed_urls = 0

    for row_index in range(HEADER_ROW_INDEX + 1, worksheet.nrows):
        row = read_row(worksheet, row_index)
        if row is None:
            continue

        key = row["key"]
        icon = row["icon"]
        images = row["images"]

        if row["raw_key"] != key:
            trimmed_keys += 1

        if row["raw_icon"] != icon:
            trimmed_urls += 1

        if key == "":
            anomalies["blank_keys"].append({"row": row_index + 1})
            continue

        normalized_images: list[str] = []
        for image in images:
            trimmed_image = normalize_cell(image)
            if trimmed_image != image:
                trimmed_urls += 1
            if trimmed_image:
                normalized_images.append(trimmed_image)

        images = normalized_images

        if icon in forbidden_urls:
            anomalies["blocked_icon_urls"].append({"row": row_index + 1, "key": key, "icon": icon})
            icon = ""
        elif icon and not is_http_url(icon):
            anomalies["invalid_icon_urls"].append({"row": row_index + 1, "key": key, "icon": icon})
            icon = ""

        if icon == "" and images:
            anomalies["screenshots_without_icon"].append({"row": row_index + 1, "key": key})

        exact_duplicate_rows[key].append(row_index + 1)
        casefolded_rows[key.casefold()].append((row_index + 1, key))

        existing = json_content["icons_and_screenshots"].get(key)
        if existing is None:
            json_content["icons_and_screenshots"][key] = {
                "icon": icon,
                "images": images,
            }
            continue

        if icon and existing["icon"] and existing["icon"] != icon:
            anomalies["duplicate_icon_conflicts"].append(
                {
                    "key": key,
                    "row": row_index + 1,
                    "existing_icon": existing["icon"],
                    "incoming_icon": icon,
                }
            )

        if images and existing["images"] and existing["images"] != images:
            anomalies["duplicate_screenshot_conflicts"].append(
                {
                    "key": key,
                    "row": row_index + 1,
                }
            )

        if not existing["icon"] and icon:
            existing["icon"] = icon

        existing["images"] = merge_images(existing["images"], images)

    duplicate_keys = {
        key: rows for key, rows in exact_duplicate_rows.items() if len(rows) > 1
    }
    casefold_duplicates = {
        group[0][1].casefold(): group
        for group in casefolded_rows.values()
        if len({key for _, key in group}) > 1
    }

    if duplicate_keys:
        anomalies["duplicate_keys"] = [
            {"key": key, "rows": rows} for key, rows in sorted(duplicate_keys.items())
        ]
    if casefold_duplicates:
        anomalies["casefold_duplicate_keys"] = [
            {
                "keys": [key for _, key in group],
                "rows": [row for row, _ in group],
            }
            for _, group in sorted(casefold_duplicates.items())
        ]

    entries = json_content["icons_and_screenshots"]
    json_content["package_count"]["total"] = len(entries)
    json_content["package_count"]["done"] = sum(
        1 for value in entries.values() if value["icon"]
    )
    json_content["package_count"]["packages_with_icon"] = json_content["package_count"][
        "done"
    ]
    json_content["package_count"]["packages_with_screenshot"] = sum(
        1 for value in entries.values() if value["images"]
    )
    json_content["package_count"]["total_screenshots"] = sum(
        len(value["images"]) for value in entries.values()
    )

    old_content = ""
    if os.path.exists(output_path):
        with open(output_path, "r", encoding="utf-8") as infile:
            old_content = infile.read()
        old_urls = re.findall(r'https?://[^\s",]+', old_content)
    else:
        old_urls = []

    ensure_parent_directory(output_path)
    new_content = json.dumps(json_content, indent=4)
    with open(output_path, "w", encoding="utf-8") as outfile:
        outfile.write(new_content)

    new_urls = re.findall(r'https?://[^\s",]+', new_content)
    diff_urls = [url for url in new_urls if url not in old_urls]
    ensure_parent_directory(new_urls_path)
    with open(new_urls_path, "w", encoding="utf-8") as file:
        for url in diff_urls:
            file.write(url + "\n")

    print(f"Exported {len(entries)} keys from {worksheet.name}")
    print(
        "Counts: "
        + json.dumps(json_content["package_count"], separators=(",", ":"))
    )
    print(
        "Normalization: "
        + f"trimmed_keys={trimmed_keys}, trimmed_urls={trimmed_urls}, new_urls={len(diff_urls)}"
    )
    print(f"Anomalies: {summarize_anomalies(anomalies)}")

    for name in (
        "duplicate_keys",
        "casefold_duplicate_keys",
        "invalid_icon_urls",
        "blocked_icon_urls",
        "screenshots_without_icon",
    ):
        if anomalies.get(name):
            sample = anomalies[name][:5]
            print(f"Sample {name}: {json.dumps(sample, ensure_ascii=True)}")


def main() -> int:
    args = parse_args()
    os.chdir(DATA_DIR)

    if args.download_workbook or not os.path.exists(args.workbook_path):
        download_workbook(args.workbook_path)

    export_json(
        workbook_path=args.workbook_path,
        output_path=args.output_path,
        invalid_urls_path=args.invalid_urls_path,
        new_urls_path=args.new_urls_path,
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
