#!/usr/bin/env bash
# Publishes artifacts/app.zip in the App Service of an environment (ADR-0115) and waits for /health, while the
# application starts and, in Development, applies its migrations. In an environment with the local sign-in, then loads
# the demonstration data that is missing (ADR-0089). ENVIRONMENT names the environment of infra/terraform/environments,
# and the Azure CLI must be signed in.
set -euo pipefail
cd "$(dirname "$0")/../.."

environment=${ENVIRONMENT:?Set ENVIRONMENT to an environment of infra/terraform/environments, as dev.}
tfvars="infra/terraform/environments/$environment.tfvars"
installation=$(sed -n 's/^installation *= *"\([^"]*\)".*/\1/p' "$tfvars")
# The names of infra/terraform/main.tf.
app="app-fiscoprojects-$installation-$environment"
group="rg-fiscoprojects-$installation-$environment"
address="https://$app.azurewebsites.net"

# The startup is followed here, by /health: the tracking of the Azure CLI can keep waiting after the site is up.
az webapp deploy --resource-group "$group" --name "$app" --src-path artifacts/app.zip --type zip --track-status false --output none
sleep 20

healthy=0
for _ in $(seq 1 60); do
  if [ "$(curl -s -o /dev/null -w '%{http_code}' --max-time 30 "$address/health")" = 200 ]; then
    healthy=1
    break
  fi
  sleep 10
done
if [ "$healthy" -eq 0 ]; then
  echo "$address/health did not answer 200 in 10 minutes." >&2
  exit 1
fi
echo "Published: $address"

if grep -q '^aspnetcore_environment *= *"Development"' "$tfvars"; then
  (cd web && npm run demo:data -- "$address")
fi
