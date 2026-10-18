#!/usr/bin/env bash
# Spell check with the repository cspell.json (the same one the VS Code extension uses) plus the Portuguese dictionary.
set -euo pipefail
cd "$(dirname "$0")/../.."

npm ci --prefix scripts/ci --no-audit --no-fund --silent
scripts/ci/node_modules/.bin/cspell --config scripts/ci/spelling.json --gitignore --no-progress --no-summary "**"
