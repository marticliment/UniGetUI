$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

git -C $repoRoot config core.hooksPath .githooks

Write-Host 'Configured git hooks path to .githooks' -ForegroundColor Green
Write-Host 'The pre-commit hook will run dotnet format whitespace on staged files under src.' -ForegroundColor Green