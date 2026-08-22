#!/usr/bin/env bash
# Stops the application. Pass --clean to also wipe every SQLite database.
#
# Usage: ./scripts/down.sh [--clean]
set -euo pipefail

cd "$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

if [[ "${1:-}" == "--clean" ]]; then
    echo "Stopping and wiping all databases..."
    docker compose down --volumes
else
    echo "Stopping (databases are kept)..."
    docker compose down
fi
