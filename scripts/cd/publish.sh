#!/usr/bin/env bash
# Builds the package of the application (ADR-0055, ADR-0115): the screens, built into the wwwroot of the API, and the
# API published for the .NET of the App Service, in artifacts/app.zip.
set -euo pipefail
cd "$(dirname "$0")/../.."

rm -rf artifacts
mkdir -p artifacts
(cd web && npm ci --no-audit --no-fund && npm run build)
dotnet publish src/FiscoProjects.Api/FiscoProjects.Api.csproj --configuration Release --output artifacts/app
(cd artifacts/app && zip -q -r ../app.zip .)
echo "Package: artifacts/app.zip ($(du -h artifacts/app.zip | cut -f1))"
