#!/usr/bin/env bash
# Compares the infrastructure of an environment with the Terraform code (ADR-0115): terraform plan with the state of the
# environment, without applying. A difference only warns, because the apply stays with whoever operates the
# installation (docs/instalacao.md). ENVIRONMENT names the environment; the credentials come from the ARM_* variables.
set -euo pipefail
cd "$(dirname "$0")/../.."

environment=${ENVIRONMENT:?Set ENVIRONMENT to an environment of infra/terraform/environments, as dev.}
summary=${GITHUB_STEP_SUMMARY:-/dev/null}
if [ -n "${TF_VAR_alert_email:-}" ] && [ -n "${GITHUB_ACTIONS:-}" ]; then
  echo "::add-mask::$TF_VAR_alert_email"
fi

terraform=$(bash scripts/ci/terraform.sh)
plan=$(mktemp)
trap 'rm -f "$plan"' EXIT

"$terraform" -chdir=infra/terraform init -input=false -backend-config="environments/$environment.backend.hcl" > /dev/null
set +e
"$terraform" -chdir=infra/terraform plan -input=false -no-color -lock-timeout=5m -detailed-exitcode \
  -var-file="environments/$environment.tfvars" > "$plan" 2>&1
status=$?
set -e

case "$status" in
  0)
    echo "Infrastructure of $environment matches the code." | tee -a "$summary"
    ;;
  2)
    changes=$(grep -E '^  # |^Plan:' "$plan")
    echo "$changes"
    {
      echo "### Infraestrutura de $environment diferente do código"
      echo
      echo "Aplique o plano, como em docs/instalacao.md."
      echo
      echo '```'
      echo "$changes"
      echo '```'
    } >> "$summary"
    if [ -n "${GITHUB_ACTIONS:-}" ]; then
      echo "::warning::A infraestrutura de $environment está diferente do código: aplique o plano (docs/instalacao.md)."
    fi
    ;;
  *)
    cat "$plan" >&2
    exit "$status"
    ;;
esac
