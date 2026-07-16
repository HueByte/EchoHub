#!/usr/bin/env bash
#
# Lint all Markdown files in the repository.
# Config, globs, and ignores are defined in .markdownlint-cli2.jsonc.
# The linter version is pinned in package.json — run `npm install` once,
# or just use `npm run lint:md` / `npm run lint:md:fix` directly.
#
# Usage:
#   ./scripts/lint-markdown.sh          # check
#   ./scripts/lint-markdown.sh --fix    # auto-fix
#
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$REPO_ROOT"

# Prefer the locally installed (version-pinned) linter; fall back to a one-off
# npx install of the same version pinned in package.json.
if [ -x "node_modules/.bin/markdownlint-cli2" ]; then
    LINT_CMD="npx --no-install markdownlint-cli2"
else
    LINT_CMD="npx --yes markdownlint-cli2@0.23.0"
fi

echo "Linting Markdown files..."
$LINT_CMD "$@"
echo "Markdown lint passed."
