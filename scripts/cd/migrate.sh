#!/usr/bin/env bash
# Applies the migrations of every module in the database of an environment where the application does not administer
# it, as production (ADR-0125): the migrate command of the package (artifacts/app), connected by Entra ID with the
# account of the Azure CLI, which administers the database, and the managed identity of the application, read from the
# App Service, which gets the reading and the writing of the data. ENVIRONMENT names the environment of
# infra/terraform/environments, and the Azure CLI must be signed in.
set -euo pipefail
cd "$(dirname "$0")/../.."

environment=${ENVIRONMENT:?Set ENVIRONMENT to an environment of infra/terraform/environments, as prod.}
tfvars="infra/terraform/environments/$environment.tfvars"
installation=$(sed -n 's/^installation *= *"\([^"]*\)".*/\1/p' "$tfvars")
aspnetcore=$(sed -n 's/^aspnetcore_environment *= *"\([^"]*\)".*/\1/p' "$tfvars")
# The names of infra/terraform/main.tf.
name="gerenware-$installation-$environment"
app="app-$name"
server="sql-$name.database.windows.net"
database="sqldb-gerenware"

principal=$(az webapp identity show --resource-group "rg-$name" --name "$app" --query principalId --output tsv)
if [ -z "$principal" ]; then
  echo "The App Service $app has no managed identity: apply the infrastructure first (docs/instalacao.md)." >&2
  exit 1
fi

ASPNETCORE_ENVIRONMENT="${aspnetcore:-Production}" \
ConnectionStrings__GerenWare="Server=tcp:$server,1433;Database=$database;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False" \
Database__AppIdentity__Name="$app" \
Database__AppIdentity__ObjectId="$principal" \
  dotnet artifacts/app/GerenWare.Api.dll migrate
echo "Migrations applied in $server."
