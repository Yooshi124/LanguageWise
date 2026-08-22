#!/usr/bin/env bash
# Runs the NUnit tests for every microservice, or just one of them.
#
# Usage: ./scripts/test.sh [all|<service>] [Debug|Release]
set -euo pipefail

SERVICE="${1:-all}"
CONFIGURATION="${2:-Debug}"
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

mapfile -t SOLUTIONS < <(find "${REPO_ROOT}" -name '*.slnx' -type f | sort)

failed=()
for solution in "${SOLUTIONS[@]}"; do
    name="$(basename "$(dirname "${solution}")")"
    [[ "${SERVICE}" != "all" && "${SERVICE}" != "${name}" ]] && continue

    echo ""
    echo "Testing ${name} (${CONFIGURATION})..."
    dotnet test "${solution}" --configuration "${CONFIGURATION}" --nologo || failed+=("${name}")
    found=1
done

if [[ -z "${found:-}" ]]; then
    echo "No solution found for '${SERVICE}'." >&2
    exit 1
fi

if [[ ${#failed[@]} -gt 0 ]]; then
    echo ""
    echo "Tests FAILED: ${failed[*]}" >&2
    exit 1
fi

echo ""
echo "All tests passed."
