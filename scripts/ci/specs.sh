#!/usr/bin/env bash
# Validates the OpenSpec specs and changes, and checks that archived changes have every task done.
set -euo pipefail
cd "$(dirname "$0")/../.."

npm ci --prefix scripts/ci --no-audit --no-fund --silent
export OPENSPEC_TELEMETRY=0
openspec=scripts/ci/node_modules/.bin/openspec
"$openspec" validate --all --strict --no-interactive
"$openspec" validate --archived --no-interactive
