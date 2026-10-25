#!/usr/bin/env bash
# Checks the Terraform code of infra/terraform (ADR-0053, ADR-0114): the format and the validation of every module,
# with the providers of the lock file and the pinned Terraform, without Azure credentials.
set -euo pipefail
cd "$(dirname "$0")/../.."

terraform=$(bash scripts/ci/terraform.sh)

"$terraform" -chdir=infra/terraform fmt -check -recursive -diff
"$terraform" -chdir=infra/terraform init -backend=false -input=false -lockfile=readonly
"$terraform" -chdir=infra/terraform validate
