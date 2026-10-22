# MCP and RAG end-to-end tests

Run from the repository root with Docker Desktop/Engine running and PowerShell 7:

```powershell
pwsh tests\e2e\Invoke-E2E.ps1
```

The same command runs in the `ai-e2e` GitHub Actions job on every PR and push to
`master`, and through manual dispatch. Docker Compose 2.24.4+ (including Compose
v5) is required for the override/reset tags. No GPU, paid API, host MCP/RAG server,
browser or development keys are needed. The first RAG startup downloads its
public embedding model.

The runner builds actual backend/database/MCP/RAG images, creates an isolated
Compose project, generates temporary signing/API keys and signs in disposable
users through shared's real account APIs. The production Compose file is reused
with an override that removes fixed names, host ports and backend `.env` files.
Development containers and their volumes are not stopped or deleted.

Coverage includes each feature's assistant calling a real domain MCP tool,
each scope's `docs_search` retrieving the real corpus, existing direct-tool API
routes, JWT/key/scope boundaries, persisted user-specific notification writes,
shared Ask the docs, and RAG's own MCP transport. Dedicated outage checks stop
RAG and a feature backend and assert explicit errors. A final retrieval test runs
after starting RAG again with the same volumes to exercise automatic reindexing.

Only **external AI text generation** is replaced. The test-only HTTP provider
requests a specified tool and verifies Garry sends back its real result before
generating a predictable response. It also supports streamed docs answers grounded
in actual retrieved context. These tests verify application integration, not a
real language model's answer quality or ability to select tools.

Reports and container diagnostics are written to the temporary
`languagewise-e2e-results` directory, or choose a location:

```powershell
pwsh tests\e2e\Invoke-E2E.ps1 -ResultsDirectory "$env:TEMP\my-ai-e2e-results"
```

The script fails on build/startup/test failures and always attempts cleanup of
its own containers, volumes and temporary keys. GitHub Actions uploads its report
directory as `ai-e2e-results`. Existing isolated service tests remain separate
and continue using fakes with MCP/RAG disabled.
