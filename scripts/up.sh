#!/usr/bin/env bash
# Builds and starts the whole application with Docker Compose.
#
# Usage: ./scripts/up.sh [--detach]
set -euo pipefail

cd "$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

# Ollama also runs natively on some machines and would collide on 11434.
if [[ -z "${OLLAMA_PORT:-}" ]] && (command -v ss >/dev/null && ss -ltn 2>/dev/null | grep -q ':11434 '); then
    export OLLAMA_PORT=11435
    echo "Port 11434 is already in use, publishing Ollama on ${OLLAMA_PORT} instead."
fi

if [[ "${1:-}" == "--detach" || "${1:-}" == "-d" ]]; then
    docker compose up --build --detach
    cat <<'EOF'

LanguageWise is running:
  Home                                  http://localhost:3000
  Mini Games                (Kyan)      http://localhost:3001
  Discussion Forum          (Lachlan)   http://localhost:3002
  Quizzes and Courses       (Justin)    http://localhost:3003
  Quests and Achievements   (Amber)     http://localhost:3004
  Leaderboard and Analytics (Roan)      http://localhost:3005

  Backends 5000-5005 and database services 6000-6005 expose /health.
EOF
else
    docker compose up --build
fi
