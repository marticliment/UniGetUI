---
name: translation-diff-export
description: Export a JSON translation patch containing only untranslated or source-changed UniGetUI strings for a target language.
---

# translation diff export

Use this skill when you need a small UniGetUI translation patch instead of sending a full language JSON file for review or translation.

## Scope

- Export only strings that are still untranslated in the target language file.
- Treat missing, empty, or English-equal target values as untranslated.
- Optionally include strings whose English source value changed since a git baseline.
- Produce an immutable English source patch, a sparse translated working copy, and a translated reference corpus.
- Generate a companion markdown handoff file that points the translator to `translation-diff-translate` and the merge step in `translation-diff-import`.

## Prerequisites

- PowerShell 7 (`pwsh`).
- `git` available on `PATH`.
- `cirup` available on `PATH`.

## Scripts

- `scripts/export-translation-diff.ps1`: Exports JSON patch artifacts and generates a translation handoff prompt.
- `scripts/test-translation-diff.ps1`: Runs a local end-to-end smoke test against the checked-in UniGetUI language files.

## Usage

Export untranslated French strings only:

```powershell
pwsh ./.agents/skills/translation-diff-export/scripts/export-translation-diff.ps1 \
  -NeutralJson ./src/UniGetUI.Core.LanguageEngine/Assets/Languages/lang_en.json \
  -TargetJson ./src/UniGetUI.Core.LanguageEngine/Assets/Languages/lang_fr.json \
  -Language fr
```

Export untranslated French strings plus English source values changed since `origin/main`:

```powershell
pwsh ./.agents/skills/translation-diff-export/scripts/export-translation-diff.ps1 \
  -NeutralJson ./src/UniGetUI.Core.LanguageEngine/Assets/Languages/lang_en.json \
  -TargetJson ./src/UniGetUI.Core.LanguageEngine/Assets/Languages/lang_fr.json \
  -Language fr \
  -BaseRef origin/main
```

Optional parameters:

- `-OutputDir` (default: `generated/translation-diff-export`)
- `-KeepIntermediate`

Run the built-in smoke test:

```powershell
pwsh ./.agents/skills/translation-diff-export/scripts/test-translation-diff.ps1
```

## How It Works

1. The script loads `lang_en.json` and the selected `lang_{code}.json` file.
2. It uses `cirup file-diff` to find missing keys and `cirup file-intersect` to find keys whose target value still matches English.
3. It adds any keys whose target value is present but empty, because those are not surfaced by `cirup` set operations.
4. If `-BaseRef` is provided, the script loads the old `lang_en.json` from git history and uses `cirup diff-with-base` to include keys that are new or whose English value changed.
5. It writes the English source patch as JSON.
6. It writes a reference JSON file containing already translated target-language entries that are outside the current patch.
7. It creates or refreshes a sparse translated working copy, preserving only still-valid translated entries from a previous patch file.
8. It invokes `translation-diff-translate` to write a companion `.prompt.md` handoff file for the translation and merge step.

## Output

For `lang_en.json` and language `fr`, the script generates:

- `generated/translation-diff-export/lang.diff.fr.source.json`
- `generated/translation-diff-export/lang.diff.fr.translated.json`
- `generated/translation-diff-export/lang.diff.fr.reference.json`
- `generated/translation-diff-export/lang.diff.fr.prompt.md`

If `-KeepIntermediate` is used, git-baseline snapshots are kept under `generated/translation-diff-export/tmp/`.

The smoke test writes its temporary artifacts under `generated/translation-diff-export-demo/`.

## Hand Off To Translate

After exporting the patch, use the generated `.prompt.md` file with `translation-diff-translate` to update the sparse translated working copy.

The generated prompt includes the concrete file paths for:

- `.source.json`
- `.translated.json`
- `.reference.json`
- the full target `lang_{code}.json`
- the follow-up import and validation commands

## Hand Off To Import

After translating the patch, merge it back into the full language file:

```powershell
pwsh ./.agents/skills/translation-diff-import/scripts/import-translation-diff.ps1 \
  -TranslatedPatch ./generated/translation-diff-export/lang.diff.fr.translated.json \
  -SourcePatch ./generated/translation-diff-export/lang.diff.fr.source.json \
  -TargetJson ./src/UniGetUI.Core.LanguageEngine/Assets/Languages/lang_fr.json \
  -NeutralJson ./src/UniGetUI.Core.LanguageEngine/Assets/Languages/lang_en.json \
  -OutputJson ./src/UniGetUI.Core.LanguageEngine/Assets/Languages/lang_fr.merged.json
```

The `.source.json` file stays as the English reference. The `.translated.json` file should contain only completed translations during a partial translation pass.

The `.reference.json` file contains already translated destination-language strings that are not in the current source patch, so the translator can reuse existing terminology and style.

Keep the `.translated.json` file sparse. If a key is not translated yet, leave it out instead of copying the English source value into the working copy.