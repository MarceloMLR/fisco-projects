#!/usr/bin/env bash
# Fails when a NuGet package of the backend, direct or transitive, has a license outside the allowed list (ADR-0061):
# the permissive licenses, as in the frontend. Runs after the build, with the packages already restored.
set -euo pipefail
cd "$(dirname "$0")/../.."

allowed=" MIT MIT-0 Apache-2.0 BSD-2-Clause BSD-3-Clause ISC 0BSD MS-PL Unlicense "
# Native parts of Microsoft.Data.SqlClient and of MSAL, which ship their Microsoft terms in a file and are
# redistributed with the application.
accepted=" Microsoft.Data.SqlClient.SNI.runtime Microsoft.Identity.Client.NativeInterop "
packages="${NUGET_PACKAGES:-$HOME/.nuget/packages}"

# Whether some part of an expression joined by OR, or every part of one joined by AND, is allowed.
allowed_expression() {
  local expression=${1//[()]/} part
  if [[ "$expression" == *" OR "* ]]; then
    for part in ${expression// OR / }; do
      [[ "$allowed" == *" $part "* ]] && return 0
    done
    return 1
  fi
  for part in ${expression// AND / }; do
    [[ "$allowed" == *" $part "* ]] || return 1
  done
}

failed=0
while read -r id version; do
  nuspec="$packages/${id,,}/${version,,}/${id,,}.nuspec"
  if [ ! -f "$nuspec" ]; then
    echo "$id $version: package not restored, run the build first" >&2
    failed=1
    continue
  fi
  license=$(sed -n 's:.*<license type="expression">\([^<]*\)</license>.*:\1:p' "$nuspec")
  if [ -z "$license" ] && [[ "$accepted" == *" $id "* ]]; then
    continue
  fi
  if [ -z "$license" ] || ! allowed_expression "$license"; then
    echo "$id $version: license ${license:-without an SPDX expression} is not allowed" >&2
    failed=1
  fi
done < <(dotnet list FiscoProjects.slnx package --include-transitive --format json \
  | jq -r '[.projects[].frameworks[]? | (.topLevelPackages[]?, .transitivePackages[]?) | "\(.id) \(.resolvedVersion)"] | unique[]')

if [ "$failed" -eq 0 ]; then
  echo "NuGet licenses: all allowed."
fi
exit "$failed"
