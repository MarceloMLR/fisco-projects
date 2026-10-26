#!/usr/bin/env bash
# Compares the infrastructure of an environment with the Terraform code (ADR-0115): terraform plan with the state of the
# environment, without applying. A difference only warns, because the apply stays with whoever operates the
# installation (docs/instalacao.md). ENVIRONMENT names the environment; the credentials come from the ARM_* variables,
# and the Azure CLI must be signed in.
set -euo pipefail
cd "$(dirname "$0")/../.."

environment=${ENVIRONMENT:?Set ENVIRONMENT to an environment of infra/terraform/environments, as dev.}
summary=${GITHUB_STEP_SUMMARY:-/dev/null}
installation=$(sed -n 's/^installation *= *"\([^"]*\)".*/\1/p' "infra/terraform/environments/$environment.tfvars")
name="fiscoprojects-$installation-$environment"

# The e-mail of the alerts is personal, and the logs of the repository are public (ADR-0116): it is not a variable of
# the workflow, whose values GitHub shows in every step, but read here from the action group of the installation, which
# the apply keeps, and masked before anything else.
alert_email=$(az monitor action-group show --resource-group "rg-$name" --name "ag-$name" \
  --query "emailReceivers[0].emailAddress" --output tsv)
if [ -z "$alert_email" ]; then
  echo "The action group ag-$name has no e-mail: apply the infrastructure first (docs/instalacao.md)." >&2
  exit 1
fi
if [ -n "${GITHUB_ACTIONS:-}" ]; then
  echo "::add-mask::$alert_email"
fi
export TF_VAR_alert_email=$alert_email

# The address of the SharePoint site names the tenant (ADR-0120): read from the application as the last apply left it,
# and masked the same way. Without it, the files stay where the settings of the environment put them.
site_url=$(az webapp config appsettings list --resource-group "rg-$name" --name "app-$name" \
  --query "[?name=='FileStorage__SharePoint__SiteUrl'].value | [0]" --output tsv)
if [ -n "$site_url" ]; then
  if [ -n "${GITHUB_ACTIONS:-}" ]; then
    echo "::add-mask::$site_url"
  fi
  export TF_VAR_sharepoint_site_url=$site_url
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
