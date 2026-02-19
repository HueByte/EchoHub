#!/usr/bin/env bash
#
# Lint all Markdown files in the repository.
# Config, globs, and ignores are defined in .markdownlint-cli2.jsonc.
#
# Usage:
#   ./scripts/lint-markdown.sh          # check
#   ./scripts/lint-markdown.sh --fix    # auto-fix
#
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$REPO_ROOT"

# Resolve markdownlint-cli2 binary
if command -v markdownlint-cli2 &>/dev/null; then
    LINT_CMD="markdownlint-cli2"
else
    LINT_CMD="npx --yes markdownlint-cli2"
fi

echo "Linting Markdown files..."
$LINT_CMD "$@"
echo "Markdown lint passed."
