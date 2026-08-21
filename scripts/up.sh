#!/usr/bin/env bash
# Builds and starts the whole integrated application with Docker Compose.
#
# Usage: ./scripts/up.sh [--no-build]
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "${REPO_ROOT}"

ARGS=(compose up --detach)
if [[ "${1:-}" != "--no-build" ]]; then
    ARGS+=(--build)
fi

echo "Starting LanguageWise..."
docker "${ARGS[@]}"

echo
echo "Waiting for services to report healthy..."
sleep 10
docker compose ps

cat <<'EOF'

Open the application:
  Home (shared)                       http://localhost:3000
  Student 1  Mini Games               http://localhost:3001
  Student 2  Discussion Forum         http://localhost:3002
  Student 3  Quizzes and Courses      http://localhost:3003
  Student 4  Quests and Achievements  http://localhost:3004
  Student 5  Leaderboard              http://localhost:3005
EOF
