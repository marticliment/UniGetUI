---
name: translation-status-summary
description: Summarize UniGetUI translation status across all languages using the repository PowerShell workflow.
---

# translation status summary

Use this skill when you need a current summary of translation coverage across all UniGetUI language files.

## Scope

- Read the checked-in UniGetUI language files under `src/UniGetUI.Core.LanguageEngine/Assets/Languages`.
- Summarize translation status for every language in `LanguagesReference.json`.
- Report translated, missing, empty, and source-equal entry counts.
- Show computed completion percentages alongside stored metadata from `TranslatedPercentages.json`.
- Support table, JSON, and markdown output.

## Prerequisites

- PowerShell 7 (`pwsh`).

## Scripts

- `scripts/get-translation-status-summary.ps1`: Thin wrapper around the repository script `scripts/get_translation_status.ps1`.
- `../../../../scripts/get_translation_status.ps1`: Canonical implementation for computing translation status.

## Usage

Show the default table summary:

```powershell
pwsh ./.agents/skills/translation-status-summary/scripts/get-translation-status-summary.ps1
```

Show only incomplete languages as markdown:

```powershell
pwsh ./.agents/skills/translation-status-summary/scripts/get-translation-status-summary.ps1 \
  -OutputFormat Markdown \
  -OnlyIncomplete
```

Write JSON output to a file:

```powershell
pwsh ./.agents/skills/translation-status-summary/scripts/get-translation-status-summary.ps1 \
  -OutputFormat Json \
  -OutputPath ./generated/translation-status.json
```

## Output

Each row includes:

- language code and display name
- computed completion percentage
- stored percentage from metadata, when available
- delta between computed and stored percentages
- translated entry count
- missing entry count
- empty entry count
- source-equal entry count
- extra-key count

The default table and markdown modes also include an overview block with total languages, incomplete languages, fully translated languages, and the outstanding untranslated-entry breakdown.

## Notes

- The script treats source-equal values as untranslated for non-English languages.
- Missing stored percentages are left blank rather than coerced to `0%`.
- Use `-IncludeEnglish` if you want the `en` row included in the report.
- Use `-OnlyIncomplete` to focus on languages that still need work.