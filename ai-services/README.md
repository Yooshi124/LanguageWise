# AI services

Shared Agentic AI services for the LanguageWise application.

## Status: Release 0 — runtime only

The project specification (section 4.2) requires an **Ollama runtime** and one approved
open-source LLM for Release 0. The runtime is already wired into
[`docker-compose.yml`](../docker-compose.yml) as the `ollama` service, listening on
`http://localhost:11434` with its model cache on the `ollama-models` named volume.

No application code calls it yet. AI-Mode — the
`Frontend → Backend/API → Ollama → LLM` workflow — is the next piece of work, and its
service code belongs in this directory.

## Pulling a model

The Ollama container starts with no models. Pull one once the stack is running:

```bash
docker compose exec ollama ollama pull qwen2.5:3b
docker compose exec ollama ollama list
```

Approved models are **Llama**, **Qwen** and **DeepSeek** (specification section 4.1).
The model cache survives `docker compose down`; it is only removed by
`docker compose down --volumes`.

## Checking it works

```bash
curl http://localhost:11434/api/tags
```

## Coming in later releases

| Release | Service | Notes |
| --- | --- | --- |
| Release 0 | Ollama runtime, AI-Mode | Local only |
| Release 1 | MCP server, RAG server | Local only |
| Release 2 | Multi-agent system (planner, worker, reviewer) | Local; disabled in the cloud deployment |
