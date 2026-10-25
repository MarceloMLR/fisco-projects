#!/usr/bin/env bash
# Decides the verification of the pull request from the result of each part (ADR-0090): changes and specs always pass;
# backend, format, frontend and infra pass when the commit changes code (CODE=true), and may be skipped when it changes only
# documentation. Each result is success, failure, cancelled or skipped, as GitHub Actions names them.
set -euo pipefail
cd "$(dirname "$0")/../.."

failed=0

check() {
  local part=$1 result=$2 allowed=$3
  if [[ " $allowed " == *" $result "* ]]; then
    echo "$part: $result"
  else
    echo "$part: $result, but it must be ${allowed// / or }" >&2
    failed=1
  fi
}

check changes "${CHANGES:-}" success
check specs "${SPECS:-}" success
if [ "${CODE:-}" = false ]; then
  code_parts="success skipped"
else
  code_parts=success
fi
check backend "${BACKEND:-}" "$code_parts"
check format "${FORMAT:-}" "$code_parts"
check frontend "${FRONTEND:-}" "$code_parts"
check infra "${INFRA:-}" "$code_parts"

exit "$failed"
