#!/usr/bin/env bash
# Checks that the production deployment runs on the tag of a version, vX.Y.Z (ADR-0125), before anything else: the
# workflow is started by hand on the tag, and the GitHub environment "prod" accepts only the tags v*.
set -euo pipefail
cd "$(dirname "$0")/../.."

if [ "${GITHUB_REF_TYPE:-}" != tag ] || ! [[ "${GITHUB_REF_NAME:-}" =~ ^v[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
  echo "Rode a publicação em produção na tag de uma versão, como v1.2.3 (em \"Use workflow from\", escolha a tag)." >&2
  exit 1
fi

echo "Versão: $GITHUB_REF_NAME"
echo "Versão publicada: \`$GITHUB_REF_NAME\`" >> "${GITHUB_STEP_SUMMARY:-/dev/null}"
