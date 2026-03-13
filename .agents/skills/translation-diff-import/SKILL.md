---
name: translation-diff-import
description: Import a translated JSON patch back into the full UniGetUI target-language JSON file.
---

# translation diff import

Use this skill after the export skill produced a `.source.json`, `.translated.json`, and `.reference.json` set and you want to merge the translated working copy back into the full target-language JSON file.

## Scope

- Accept the sparse `.translated.json` working copy from the export skill.
- Validate the translated working copy against the matching `.source.json` file before merge.
- Merge translated keys back into the full target-language JSON file with `cirup file-merge`.
- Preserve the existing target-file key order while updating translated values.
- Provide a PowerShell validation step for the merged output.

## Prerequisites

- PowerShell 7 (`pwsh`).
- `cirup` available on `PATH`.

## Scripts

- `scripts/import-translation-diff.ps1`: Validates and merges a translated patch into the full language file.
- `scripts/validate-language-file.ps1`: Validates placeholder, token, HTML-fragment, and newline parity for either a full language file or just the active patch keys against `lang_en.json`.

## Usage

Import the translated French working copy from the export skill into the full French JSON file:

```powershell
pwsh ./.agents/skills/translation-diff-import/scripts/import-translation-diff.ps1 \
  -TranslatedPatch ./generated/translation-diff-export/lang.diff.fr.translated.json \
  -SourcePatch ./generated/translation-diff-export/lang.diff.fr.source.json \
  -TargetJson ./src/UniGetUI.Core.LanguageEngine/Assets/Languages/lang_fr.json \
  -NeutralJson ./src/UniGetUI.Core.LanguageEngine/Assets/Languages/lang_en.json \
  -OutputJson ./src/UniGetUI.Core.LanguageEngine/Assets/Languages/lang_fr.merged.json
```

Validate the merged output:

```powershell
pwsh ./.agents/skills/translation-diff-import/scripts/validate-language-file.ps1 \
  -NeutralJson ./src/UniGetUI.Core.LanguageEngine/Assets/Languages/lang_en.json \
  -TargetJson ./src/UniGetUI.Core.LanguageEngine/Assets/Languages/lang_fr.merged.json \
  -PatchJson ./generated/translation-diff-export/lang.diff.fr.source.json
```

Optional parameters:

- `-OutputDir` (default: `generated/translation-diff-import`)
- `-AllowUnchangedValues`
- `-KeepIntermediate`

Recommended input mapping from the export skill:

- `-TranslatedPatch`: `generated/translation-diff-export/lang.diff.fr.translated.json`
- `-SourcePatch`: `generated/translation-diff-export/lang.diff.fr.source.json`
- `.reference.json`: not imported directly; it is only used during translation for terminology and style guidance

## Output

- A merged `.json` file that contains both the previously translated entries and the imported patch values.
- If `-KeepIntermediate` is used, patch snapshots are preserved under `generated/translation-diff-import/tmp/`.

If the translated working copy is sparse, only the completed translations are merged. Existing translated entries that are not part of the patch stay in the target JSON file.

## Notes

- If the target JSON file does not exist yet, the script can create an output from the translated patch alone.
- When `-SourcePatch` is provided, the script validates translated keys, placeholder tokens, HTML-like fragments, newline counts, and likely untranslated values before delegating the merge to `cirup` while allowing missing keys for partial progress.
- The import skill expects `.translated.json` to remain sparse. Untranslated keys should be omitted instead of copied in English unless you intentionally bypass that check with `-AllowUnchangedValues`.
- The validation script is the PowerShell replacement for the repository's older Python translation checks.