#!/usr/bin/env bash
# Runs every backend test. Database and API tests start SQL Server in a container, so Docker must be running.
set -euo pipefail
cd "$(dirname "$0")/../.."

dotnet test FiscoProjects.slnx
