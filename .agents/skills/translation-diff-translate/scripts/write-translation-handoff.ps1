param(
    [Parameter(Mandatory = $true)]
    [string]$BaseName,

    [Parameter(Mandatory = $true)]
    [string]$Language,

    [Parameter(Mandatory = $true)]
    [string]$SourcePatch,

    [Parameter(Mandatory = $true)]
    [string]$TranslatedPatch,

    [Parameter(Mandatory = $true)]
    [string]$ReferencePatch,

    [Parameter(Mandatory = $true)]
    [string]$TargetJson,

    [Parameter(Mandatory = $true)]
    [string]$NeutralJson,

    [Parameter(Mandatory = $true)]
    [string]$MergedTargetJson,

    [Parameter(Mandatory = $true)]
    [string]$ValidationScript,

    [Parameter(Mandatory = $true)]
    [string]$OutputPrompt,

    [string]$BaseSummary = 'No git baseline was provided. The patch contains untranslated UniGetUI entries only.'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function New-Utf8File {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string]$Content
    )

    $directory = Split-Path -Path $Path -Parent
    if (-not [string]::IsNullOrWhiteSpace($directory)) {
        New-Item -Path $directory -ItemType Directory -Force | Out-Null
    }

    $encoding = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText($Path, $Content, $encoding)
}

$promptPath = [System.IO.Path]::GetFullPath($OutputPrompt)
$promptDirectory = Split-Path -Path $promptPath -Parent
if (-not [string]::IsNullOrWhiteSpace($promptDirectory)) {
    New-Item -Path $promptDirectory -ItemType Directory -Force | Out-Null
}

$promptContent = @"
# Translate $BaseName to $Language

## Use Skill

Use the translation-diff-translate skill for the translation step.

Detailed workflow and validation rules live in:

- ./.agents/skills/translation-diff-translate/SKILL.md

## Context

- Source patch file: $SourcePatch
- Translated working copy: $TranslatedPatch
- Reference translations: $ReferencePatch
- Full target language file: $TargetJson
- Neutral language file: $NeutralJson
- $BaseSummary

## Translation Task

1. Update only the translated working copy.
2. If the translated working copy is empty, translate directly from the source patch into it.
3. Keep the translated working copy sparse by omitting keys that are not translated yet.
4. Use the reference translations file for terminology, style, and consistency.
5. Preserve placeholders, named tokens, HTML-like fragments, escape sequences, and line breaks.
6. Do not edit the source patch file.
7. Do not write helper scripts, temporary reports, coverage checks, or automation for this task.
8. Do not search for external translation tools or install packages unless the user explicitly asks for automation.
9. Start translating immediately instead of analyzing repo-wide translation coverage.

## Expected Action

- Read the source patch and reference file.
- Edit the translated patch file directly.
- Add translated JSON entries in source order.
- Leave untranslated keys out.
- When finished, use the import and validation commands below.

## Import After Translation

~~~powershell
pwsh ./.agents/skills/translation-diff-import/scripts/import-translation-diff.ps1 -TranslatedPatch "$TranslatedPatch" -SourcePatch "$SourcePatch" -TargetJson "$TargetJson" -NeutralJson "$NeutralJson" -OutputJson "$MergedTargetJson"
pwsh "$ValidationScript" -NeutralJson "$NeutralJson" -TargetJson "$MergedTargetJson" -PatchJson "$SourcePatch"
~~~

The import skill validates the translated working copy against the source patch, merges it into the full target file in English key order, and the validation script checks placeholders, HTML-like fragments, and line-break parity.
"@

New-Utf8File -Path $promptPath -Content $promptContent.TrimStart()