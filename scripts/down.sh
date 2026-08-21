#!/usr/bin/env bash
# Stops the integrated application.
#
# By default the SQLite named volumes are kept, so your data survives a restart.
# Pass --clean to delete them and force a fresh re-seed on the next start.
#
# Usage: ./scripts/down.sh [--clean]
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "${REPO_ROOT}"

ARGS=(compose down)
if [[ "${1:-}" == "--clean" ]]; then
    echo "WARNING: removing named volumes - every database will be re-seeded on the next start."
    ARGS+=(--volumes)
fi

docker "${ARGS[@]}"

echo "LanguageWise stopped."
