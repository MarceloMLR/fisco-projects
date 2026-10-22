#!/usr/bin/env bash
# Prepares a codespace for the demonstration (ADR-0113), once, when it is created: the password of the local database,
# the connection of the API, the packages, the build of the screens and of the API, and the image of the database.
set -euo pipefail
cd "$(dirname "$0")/.."

if [ ! -f .env ]; then
  # A password of this codespace only, with the upper case, lower case, digit and symbol SQL Server asks for.
  echo "MSSQL_SA_PASSWORD=Demo_$(tr -d '-' < /proc/sys/kernel/random/uuid)_Fp1" > .env
fi
set -a
. ./.env
set +a
dotnet user-secrets set "ConnectionStrings:FiscoProjects" \
  "Server=localhost,1433;Database=FiscoProjects;User Id=sa;Password=${MSSQL_SA_PASSWORD};TrustServerCertificate=True" \
  --project src/FiscoProjects.Api > /dev/null

(cd web && npm ci && npm run build)
dotnet build src/FiscoProjects.Api

for _ in $(seq 1 60); do
  docker info > /dev/null 2>&1 && break
  sleep 2
done
docker compose pull
