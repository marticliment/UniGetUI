---
name: translation-diff-translate
description: Translate a sparse JSON working copy produced by the export skill while preserving UniGetUI placeholders and existing terminology.
---

# translation diff translate

Use this skill after `translation-diff-export` produced a `.source.json`, `.translated.json`, and `.reference.json` set and you want to update the sparse translated working copy.

## Scope

- Translate only the keys present in the current source patch.
- Treat `.translated.json` as the only writable file during the translation pass.
- Keep the translated working copy sparse by omitting keys that are not translated yet.
- Reuse terminology and style from `.reference.json`.
- Preserve placeholders, named tokens, HTML-like fragments, escape sequences, and line breaks.
- Hand the completed working copy to `translation-diff-import` for merge-back.

## Execution Expectations

1. Edit `.translated.json` directly. Do not create helper scripts, temporary reports, or coverage probes.
2. Do not install packages or search for external translation services unless the user explicitly asks for automation.
3. If `.translated.json` is empty, that is expected. Translate from `.source.json` into `.translated.json`.
4. Use `.reference.json` only as terminology guidance, not as a reason to pause and analyze the rest of the repository.
5. Start translating immediately. Do not spend time estimating patch size or building import or export helpers.
6. If the patch is too large to finish reasonably in one pass, report that constraint to the user instead of creating automation on your own.

## Inputs

- `.source.json`: immutable English source patch for the current translation pass.
- `.translated.json`: sparse working copy that should contain only completed translations.
- `.reference.json`: already translated destination-language strings outside the current source patch.
- Full target `lang_{code}.json`: current destination-language file.
- Neutral `lang_en.json`: current source-language file.

## Script

- `scripts/write-translation-handoff.ps1`: Writes a small `.prompt.md` file that points an agent at this skill with the concrete patch paths created by the export skill.

## Usage

Generate a handoff prompt for a translated French working copy:

```powershell
pwsh ./.agents/skills/translation-diff-translate/scripts/write-translation-handoff.ps1 \
  -BaseName lang \
  -Language fr \
  -SourcePatch ./generated/translation-diff-export/lang.diff.fr.source.json \
  -TranslatedPatch ./generated/translation-diff-export/lang.diff.fr.translated.json \
  -ReferencePatch ./generated/translation-diff-export/lang.diff.fr.reference.json \
  -TargetJson ./src/UniGetUI.Core.LanguageEngine/Assets/Languages/lang_fr.json \
  -NeutralJson ./src/UniGetUI.Core.LanguageEngine/Assets/Languages/lang_en.json \
  -MergedTargetJson ./src/UniGetUI.Core.LanguageEngine/Assets/Languages/lang_fr.merged.json \
  -ValidationScript ./.agents/skills/translation-diff-import/scripts/validate-language-file.ps1 \
  -OutputPrompt ./generated/translation-diff-export/lang.diff.fr.prompt.md
```

## Translation Rules

1. Do not edit `.source.json`.
2. Write completed translations only into `.translated.json`.
3. If a key is not translated yet, omit it from `.translated.json` instead of copying the English source value.
4. Preserve key names exactly as-is.
5. Preserve placeholders, named tokens, HTML-like fragments, escape sequences, and line breaks.
6. Keep translated entries in the same key order as `.source.json` when adding new entries.
7. Use `.reference.json` to match existing terminology and tone in the destination language.
8. Do not introduce keys that are not present in the source patch.
9. Do not create side files, scripts, or analysis artifacts as part of the translation pass.

## After Translation

Merge the sparse working copy back into the full destination-language file:

```powershell
pwsh ./.agents/skills/translation-diff-import/scripts/import-translation-diff.ps1 \
  -TranslatedPatch ./generated/translation-diff-export/lang.diff.fr.translated.json \
  -SourcePatch ./generated/translation-diff-export/lang.diff.fr.source.json \
  -TargetJson ./src/UniGetUI.Core.LanguageEngine/Assets/Languages/lang_fr.json \
  -NeutralJson ./src/UniGetUI.Core.LanguageEngine/Assets/Languages/lang_en.json \
  -OutputJson ./src/UniGetUI.Core.LanguageEngine/Assets/Languages/lang_fr.merged.json

pwsh ./.agents/skills/translation-diff-import/scripts/validate-language-file.ps1 \
  -NeutralJson ./src/UniGetUI.Core.LanguageEngine/Assets/Languages/lang_en.json \
  -TargetJson ./src/UniGetUI.Core.LanguageEngine/Assets/Languages/lang_fr.merged.json \
  -PatchJson ./generated/translation-diff-export/lang.diff.fr.source.json
```