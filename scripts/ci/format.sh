#!/usr/bin/env bash
# Fails when the code does not follow .editorconfig. Run `dotnet format GerenWare.slnx` to fix it.
set -euo pipefail
cd "$(dirname "$0")/../.."

dotnet format GerenWare.slnx --verify-no-changes
