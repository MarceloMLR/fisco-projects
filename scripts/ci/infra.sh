#!/usr/bin/env bash
# Checks the Terraform code of infra/terraform (ADR-0053, ADR-0114): the format and the validation of every module,
# with the providers of the lock file, without Azure credentials. Uses the pinned Terraform, downloaded from
# HashiCorp with its checksum when the one on the path is another version.
set -euo pipefail
cd "$(dirname "$0")/../.."

version=1.16.5
terraform=terraform
if ! command -v terraform > /dev/null || [ "$(terraform version -json | sed -n 's/.*"terraform_version": *"\([^"]*\)".*/\1/p')" != "$version" ]; then
  case "$(uname -s)-$(uname -m)" in
    Linux-x86_64) platform=linux_amd64 ;;
    Darwin-arm64) platform=darwin_arm64 ;;
    *) echo "Install Terraform $version: this script downloads it only on Linux x64 and macOS arm64." >&2; exit 1 ;;
  esac
  folder=$(mktemp -d)
  trap 'rm -rf "$folder"' EXIT
  file="terraform_${version}_${platform}.zip"
  curl -fsSL -o "$folder/$file" "https://releases.hashicorp.com/terraform/$version/$file"
  curl -fsSL -o "$folder/sums" "https://releases.hashicorp.com/terraform/$version/terraform_${version}_SHA256SUMS"
  (cd "$folder" && grep " $file\$" sums | sha256sum -c -)
  unzip -q "$folder/$file" terraform -d "$folder"
  terraform="$folder/terraform"
fi

"$terraform" -chdir=infra/terraform fmt -check -recursive -diff
"$terraform" -chdir=infra/terraform init -backend=false -input=false -lockfile=readonly
"$terraform" -chdir=infra/terraform validate
