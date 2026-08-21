#!/usr/bin/env bash
# Runs the NUnit test suite.
#
# Usage: ./scripts/test.sh [all|shared|student-1..student-5] [Debug|Release]
set -euo pipefail

SERVICE="${1:-all}"
CONFIGURATION="${2:-Release}"
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

if [[ "${SERVICE}" == "all" ]]; then
    TARGET="${REPO_ROOT}/LanguageWise.sln"
elif [[ "${SERVICE}" == "shared" ]]; then
    TARGET="${REPO_ROOT}/shared/tests/LanguageWise.Shared.Api.Tests/LanguageWise.Shared.Api.Tests.csproj"
elif [[ "${SERVICE}" =~ ^student-([1-5])$ ]]; then
    PASCAL="Student${BASH_REMATCH[1]}"
    TARGET="${REPO_ROOT}/${SERVICE}/tests/LanguageWise.${PASCAL}.Api.Tests/LanguageWise.${PASCAL}.Api.Tests.csproj"
else
    echo "Unknown service '${SERVICE}'. Use all, shared, or student-1..student-5." >&2
    exit 1
fi

echo "Testing ${SERVICE} (${CONFIGURATION})..."
dotnet test "${TARGET}" --configuration "${CONFIGURATION}" --nologo

echo "All tests passed."
