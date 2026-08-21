#!/usr/bin/env bash
# Builds every .NET microservice in the LanguageWise solution.
#
# Usage: ./scripts/build.sh [Debug|Release]
set -euo pipefail

CONFIGURATION="${1:-Release}"
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

echo "Building LanguageWise.sln (${CONFIGURATION})..."
dotnet build "${REPO_ROOT}/LanguageWise.sln" --configuration "${CONFIGURATION}" --nologo

echo "Build succeeded."
