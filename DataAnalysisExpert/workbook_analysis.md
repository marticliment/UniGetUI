# screenshot_database.xlsx analysis

Workbook: d:\dev\unigetui-app-icons\WebBasedData\screenshot_database.xlsx
Primary feed sheet: Sheet1

## Workbook structure
- Sheet1: maxRow=13412, maxColumn=22, nonemptyRows=12462, usedRange=A1:V12463
- Còpia de Sheet1: maxRow=13412, maxColumn=22, nonemptyRows=12462, usedRange=A1:V12463

## Feed model
- Export script reads only the first worksheet.
- Row 1 is treated as headers; data starts at row 2.
- Column A = package key, B = icon URL, C:X = screenshot URL slots.
- Screenshot export stops at the first blank screenshot cell in a row.

## Counts
- Populated data rows: 12461
- Rows with icon URL: 8384
- Rows with at least one screenshot URL: 1374
- Populated screenshot URL cells: 4246
- Screenshot URL cells ignored by current gap-sensitive export: 13

## Quality issues
- Exact duplicate key groups: 43
- Case-only duplicate groups: 1
- IDs with leading/trailing whitespace: 15
- Empty/whitespace IDs: 0
- Invalid-looking IDs: 0
- Suspicious manager-prefixed IDs: 0
- Rows with screenshots but no icon: 141
- Rows with malformed icon URL: 7
- Rows with whitespace/newline contamination: 23
- Rows likely not to match current lookup logic: 159
- Rows with screenshot gaps: 8

## Lookup compatibility
- Current runtime order: exact `ManagerName.PackageId`, then normalized icon id from `Package.GenerateIconId()`.
- Exact-form candidates: 34
- Normalized-id candidates: 12270
- Definite mismatch reasons:
  - does-not-look-like-exact-or-normalized-id: 144
  - key-has-whitespace: 13
  - prefixed-id-has-whitespace: 2

## Example rows
- [lookup mismatches] row 2 | A2='__test_entry_DO_NOT_EDIT_PLEASE' | B2='https://this.is.a.test/url/used_for/automated_unit_testing.png' | screenshots=C2, D2, E2
- [lookup mismatches] row 125 | A125='activebackupforbusinessag…' | B125='' | screenshots=none
- [lookup mismatches] row 142 | A142='activepartitionm…' | B142='' | screenshots=none
- [lookup mismatches] row 143 | A143='activepartitionr…' | B143='' | screenshots=none
- [malformed icon URLs] row 3361 | A3361='fontviewok' | B3361='https://i.postimg.cc/5NSK9JZ4/Font-View-OK-ico.png\n' | screenshots=C3361
- [malformed icon URLs] row 3362 | A3362='foobar2000' | B3362='https://i.postimg.cc/3NV8dpy4/foobar2000-ico.png\n' | screenshots=C3362, D3362, E3362
- [malformed icon URLs] row 6509 | A6509='nosqlworkbench' | B6509='data:image/png;base64,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' | screenshots=C6509
- [malformed icon URLs] row 10839 | A10839='vncserver' | B10839='https://www.pacisoft.vn/wp-content/uploads/2018/10/vnc-viewer-logo.png ' | screenshots=none
- [icon missing but screenshots exist] row 275 | A275='ahmsystemmanager' | B275='' | screenshots=C275, D275, E275
- [icon missing but screenshots exist] row 279 | A279='aida64-business' | B279='' | screenshots=C279
- [icon missing but screenshots exist] row 280 | A280='aida64-engineer' | B280='' | screenshots=C280
- [icon missing but screenshots exist] row 281 | A281='aida64-extreme' | B281='' | screenshots=C281
- [case-only duplicates] row 4541 | A4541='intellijidea-ultimate' | B4541='https://resources.jetbrains.com/storage/products/company/brand/logos/IntelliJ_IDEA_icon.png' | screenshots=none
- [case-only duplicates] row 4543 | A4543='IntelliJIDEA-Ultimate' | B4543='https://resources.jetbrains.com/storage/products/company/brand/logos/IntelliJ_IDEA_icon.png' | screenshots=C4543
- [exact duplicate keys] row 370 | A370='amd-ryzen-chipset' | B370='https://community.chocolatey.org/content/packageimages/amd-ryzen-chipset.2022.11.21.png' | screenshots=none
- [exact duplicate keys] row 12455 | A12455='amd-ryzen-chipset' | B12455='https://community.chocolatey.org/content/packageimages/amd-ryzen-chipset.2026.2.16.png' | screenshots=none
- [exact duplicate keys] row 862 | A862='batchimageconverter' | B862='http://i2.wp.com/filecr.com/wp-content/uploads/2022/04/batch-image-converter-logo.png' | screenshots=none
- [exact duplicate keys] row 12178 | A12178='batchimageconverter' | B12178='https://vovsoft.com/icons128/batch-image-converter.png' | screenshots=C12178
- [whitespace contamination] row 15 | A15='1history' | B15='https://i.postimg.cc/k4w2mzVS/98217212.png' | screenshots=C15, D15
- [whitespace contamination] row 3361 | A3361='fontviewok' | B3361='https://i.postimg.cc/5NSK9JZ4/Font-View-OK-ico.png\n' | screenshots=C3361
