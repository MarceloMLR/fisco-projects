#!/usr/bin/env bash
# Makes the package of a version published on GitHub (ADR-0127), fiscoprojects-vX.Y.Z.zip, and attaches it to the
# version: the application (artifacts/app.zip, from scripts/cd/publish.sh), the file VERSION, the Terraform without the
# parameters of the environments of this repository, the scripts of infra/scripts and the guides. Everything but the
# application comes from the commit in Git, so nothing local goes along (local.auto.tfvars, the state, .terraform). The
# installations outside this repository are created and updated from the package (docs/instalacao.md).
set -euo pipefail
cd "$(dirname "$0")/../.."

version=${GITHUB_REF_NAME:-}
if [ "${GITHUB_REF_TYPE:-}" != tag ] || ! [[ "$version" =~ ^v[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
  echo "Publique a versão numa tag como v1.2.3: o pacote é só de versões." >&2
  exit 1
fi

artifacts=${ARTIFACTS:-artifacts}
if [ ! -f "$artifacts/app.zip" ]; then
  echo "Falta $artifacts/app.zip: rode antes scripts/cd/publish.sh." >&2
  exit 1
fi

staging=$(mktemp -d)
trap 'rm -rf "$staging"' EXIT
git archive --format=tar HEAD -- \
  infra/terraform infra/scripts docs/instalacao.md docs/restauracao.md docs/custos-estimados.md \
  ':(exclude)infra/terraform/environments' | tar -x -C "$staging"
cp "$artifacts/app.zip" "$staging/app.zip"
echo "$version" > "$staging/VERSION"

package="$artifacts/fiscoprojects-$version.zip"
rm -f "$package"
(cd "$staging" && zip -q -r - .) > "$package"
echo "Package: $package ($(du -h "$package" | cut -f1))"

gh release upload "$version" "$package" --clobber --repo "$GITHUB_REPOSITORY"
echo "Pacote da versão anexado: \`fiscoprojects-$version.zip\`" >> "${GITHUB_STEP_SUMMARY:-/dev/null}"
