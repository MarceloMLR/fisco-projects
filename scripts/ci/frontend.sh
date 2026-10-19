#!/usr/bin/env bash
# Checks the frontend (web/): dependency licenses, vulnerable packages, formatting, lint, types, tests and build.
set -euo pipefail
cd "$(dirname "$0")/../.."

cd web
npm ci --no-audit --no-fund
npm run licenses
npm audit --omit=dev
npm run format:check
npm run lint
npm run typecheck
npm test
npm run build

# ADR-0064: the fonts come from the application, never from Google (the CSS of Syncfusion imports them).
if grep -rl 'fonts.googleapis.com' ../src/FiscoProjects.Api/wwwroot; then
  echo 'The build loads fonts from Google (ADR-0064).' >&2
  exit 1
fi
