# Student service testing tools

These scripts cover the post-commit endpoint tests and the post-commit AI-assisted unit test generation. Pre-commit runs only the security scans in the root `.pre-commit-config.yaml`.

## Files

- `student_endpoints.json`: maps each student to their service and the two endpoints they own, with the source file and the existing NUnit test that covers each endpoint.
- `student_endpoints.py`: two subcommands.
  - `filter --service <name>` prints a `dotnet test --filter` expression for that service's endpoint tests.
  - `report --service <name> --results-dir <dir>` reads the TRX files and writes `reports/post-commit/endpoints/<student>.md`.
- `ai_unit_tests.py`: sends each endpoint's source to OpenRouter and asks for test cases. It writes `reports/post-commit/ai-unit-tests/<student>.json` and `summary.md`.

## Running locally

```bash
.venv/bin/python tools/testing/student_endpoints.py filter --service quizzes-courses-service
OPENROUTER_API_KEY=... .venv/bin/python tools/testing/ai_unit_tests.py
```

The post-commit reports are written under `reports/post-commit/`. The `reports/` folder is gitignored.

## CI

- Each service workflow (`mini-games-service.yml`, `chat-discussion-service.yml`, `quizzes-courses-service.yml`, `quests-achievements-notifications-service.yml`, `leaderboard-analytics-service.yml`) runs the filtered endpoint tests on push, then uploads `<service>-post-commit-reports`.
- `post-commit-ai-tests.yml` runs the AI generator on push to `master` and on manual dispatch. It needs the `OPENROUTER_API_KEY` repository secret.

## Caveats

- The AI step records the generated test cases for review. It does not execute them.
- The endpoint steps reuse existing NUnit tests through a filter. They do not send new HTTP requests.
- The OpenRouter model and base URL in `ai_unit_tests.py` are copied from `tools/agentic-loop/config/settings.py`. Update both if the model changes.
