#!/usr/bin/env bash
# Prints the path of the pinned Terraform (ADR-0053, ADR-0114): the one on the path when it is that version, or a copy
# downloaded from HashiCorp with its checksum, kept in the temporary folder for the next calls.
set -euo pipefail
cd "$(dirname "$0")/../.."

version=1.16.5

if command -v terraform > /dev/null \
  && [ "$(terraform version -json | sed -n 's/.*"terraform_version": *"\([^"]*\)".*/\1/p')" = "$version" ]; then
  command -v terraform
  exit 0
fi

case "$(uname -s)-$(uname -m)" in
  Linux-x86_64) platform=linux_amd64 ;;
  Darwin-arm64) platform=darwin_arm64 ;;
  *) echo "Install Terraform $version: this script downloads it only on Linux x64 and macOS arm64." >&2; exit 1 ;;
esac

folder="${RUNNER_TEMP:-${TMPDIR:-/tmp}}/terraform-$version"
if [ ! -x "$folder/terraform" ]; then
  mkdir -p "$folder"
  file="terraform_${version}_${platform}.zip"
  curl -fsSL -o "$folder/$file" "https://releases.hashicorp.com/terraform/$version/$file"
  curl -fsSL -o "$folder/sums" "https://releases.hashicorp.com/terraform/$version/terraform_${version}_SHA256SUMS"
  (cd "$folder" && grep " $file\$" sums | sha256sum -c - >&2)
  unzip -o -q "$folder/$file" terraform -d "$folder"
  rm -f "$folder/$file" "$folder/sums"
fi
echo "$folder/terraform"
