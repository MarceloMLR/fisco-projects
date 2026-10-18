#!/usr/bin/env bash
# Fails when the code does not follow .editorconfig. Run `dotnet format FiscoProjects.slnx` to fix it.
set -euo pipefail
cd "$(dirname "$0")/../.."

dotnet format FiscoProjects.slnx --verify-no-changes
