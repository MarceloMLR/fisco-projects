#!/usr/bin/env bash
# Builds the backend with warnings as errors. NuGet audit warnings (vulnerable packages) fail it too.
set -euo pipefail
cd "$(dirname "$0")/../.."

dotnet build FiscoProjects.slnx -warnaserror
