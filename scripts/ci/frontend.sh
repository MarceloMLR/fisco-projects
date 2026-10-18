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
