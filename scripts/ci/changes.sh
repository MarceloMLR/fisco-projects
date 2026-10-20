#!/usr/bin/env bash
# Says whether the commit under verification changes code (code=true) or only documentation (code=false), compared
# with the last commit of the pull request that passed the verification (ADR-0090). Documentation is what is in docs/,
# openspec/ and .claude/, and the .md files outside web/; anything else is code.
#
# The base of the comparison, in this order:
#   CHANGES_BASE                     a commit, for the tests and to run it by hand;
#   PR_NUMBER, REPOSITORY, HEAD_SHA  on GitHub: the newest other commit of the pull request with the "verify" check
#                                    passed, read with `gh api` (GH_TOKEN with read access to checks and pull requests);
#   otherwise                        where the branch left main (git merge-base origin/main HEAD).
# HEAD is the commit under verification: on GitHub, the pull request merged with main as it is now, so code that main
# got after the last verified commit counts as changed too.
set -euo pipefail
cd "$(dirname "$0")/../.."

# The newest commit of the pull request, but HEAD_SHA, whose "verify" check passed; nothing when none did.
last_verified() {
  local commits sha passed
  commits=$(gh api "repos/${REPOSITORY}/pulls/${PR_NUMBER}/commits?per_page=100" --paginate --jq '.[].sha') || return 0
  for sha in $(printf '%s\n' "$commits" | tac); do
    if [ "$sha" = "${HEAD_SHA:-}" ]; then
      continue
    fi
    passed=$(gh api "repos/${REPOSITORY}/commits/${sha}/check-runs?check_name=verify" \
      --jq '[.check_runs[] | select(.conclusion == "success")] | length') || continue
    if [ "${passed:-0}" -gt 0 ]; then
      echo "$sha"
      return 0
    fi
  done
}

base="${CHANGES_BASE:-}"
if [ -z "$base" ] && [ -n "${PR_NUMBER:-}" ] && [ -n "${REPOSITORY:-}" ] && command -v gh > /dev/null; then
  base=$(last_verified)
  if [ -n "$base" ]; then
    echo "Base: $base, the last commit of the pull request that passed the verification." >&2
  fi
fi
if [ -z "$base" ]; then
  base=$(git merge-base origin/main HEAD 2> /dev/null || true)
  echo "Base: ${base:-none}, where the branch left main." >&2
fi

code=true
if [ -n "$base" ] && git cat-file -e "${base}^{commit}" 2> /dev/null; then
  files=$(git diff --name-only "$base" HEAD)
  printf '%s\n' "$files" | sed '/^$/d; s/^/  /' >&2
  code=false
  while IFS= read -r file; do
    case "$file" in
      '' | docs/* | openspec/* | .claude/*) ;;
      web/*) code=true ;;
      *.md) ;;
      *) code=true ;;
    esac
  done <<< "$files"
else
  echo "The base is not in the history: everything runs." >&2
fi

echo "code=$code"
if [ -n "${GITHUB_OUTPUT:-}" ]; then
  echo "code=$code" >> "$GITHUB_OUTPUT"
fi
