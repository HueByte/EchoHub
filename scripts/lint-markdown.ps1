# EchoHub - Markdown Lint
#
# Runs markdownlint-cli2 over the repo. Rules, globs, and ignores all live in
# .markdownlint-cli2.jsonc; the linter version is pinned in package.json.
#
# Usage:
#   .\scripts\lint-markdown.ps1            # lint-only, exits non-zero on violations
#   .\scripts\lint-markdown.ps1 -Fix       # auto-fix what markdownlint can
#
# Requires: Node + npx on PATH. Prefers the locally installed linter
# (`npm install` once); otherwise npx fetches the same pinned version.

param(
    [switch]$Fix
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command npx -ErrorAction SilentlyContinue)) {
    Write-Host "  ERROR: npx not found on PATH. Install Node.js (https://nodejs.org) and retry." -ForegroundColor Red
    exit 1
}

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir   = Split-Path -Parent $ScriptDir

Push-Location $RootDir
try {
    # Local install is version-pinned via package.json/package-lock.json; the
    # npx fallback pins the same version so CI and local can never drift.
    if (Test-Path "node_modules/.bin/markdownlint-cli2") {
        $npxArgs = @('--no-install', 'markdownlint-cli2')
    } else {
        $npxArgs = @('--yes', 'markdownlint-cli2@0.23.0')
    }

    if ($Fix) {
        Write-Host "  Auto-fix mode (--fix): markdownlint will rewrite files in place." -ForegroundColor Yellow
        $npxArgs += '--fix'
    }

    Write-Host "  > npx $($npxArgs -join ' ')" -ForegroundColor Gray
    & npx @npxArgs
    $exit = $LASTEXITCODE

    if ($exit -eq 0) {
        Write-Host "  Markdown lint clean." -ForegroundColor Green
    } elseif ($Fix) {
        Write-Host ""
        Write-Host "  Some issues could not be auto-fixed. Review the output above and fix manually." -ForegroundColor Yellow
    } else {
        Write-Host ""
        Write-Host "  Markdown lint failed. Re-run with -Fix to auto-correct the fixable rules:" -ForegroundColor Red
        Write-Host "    .\scripts\lint-markdown.ps1 -Fix" -ForegroundColor Yellow
    }

    exit $exit
}
finally {
    Pop-Location
}
