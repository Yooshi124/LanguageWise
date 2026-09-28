# RAG Server

A small, **non-containerised** retrieval-augmented-generation server for
LanguageWise. It indexes the service documentation corpus with
[ChromaDB](https://www.trychroma.com/) and exposes a single MCP tool,
`retrieve_context`, through [FastMCP](https://gofastmcp.com/) over HTTP.

It runs on the host so Docker containers can reach it via
`host.docker.internal:8100`. It is intentionally simple — no separate
backend/frontend/database/test folders — and is meant to be integrated into the
Garry AI assistant later.

## Layout

```
rag-server/
  server.py            # FastMCP HTTP server exposing retrieve_context
  ingest.py            # (re)builds the ChromaDB index from the corpus
  rag/
    config.py          # settings loaded from environment / .env
    store.py           # chunking, ingestion, and retrieval over ChromaDB
  corpus/              # README-esque markdown, one file per service
  requirements.txt
  .env.example
```

## Setup

From this directory:

```powershell
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
Copy-Item .env.example .env   # optional: adjust host/port/paths
```

The first run downloads ChromaDB's default embedding model
(`all-MiniLM-L6-v2`), which is then cached locally — no embedding API key is
required.

## Build the index

```powershell
python ingest.py
```

Re-run this whenever you add or edit files in `corpus/`.

## Run the server

```powershell
python server.py
```

The MCP endpoint is served at `http://0.0.0.0:8100/mcp`. Containers on the
Compose network reach it at `http://host.docker.internal:8100/mcp`.

## The `retrieve_context` tool

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `query_text` | string | — | Natural-language question or topic to search for. |
| `n_results` | int | `5` | Maximum number of passages to return. |

It returns the most relevant corpus passages as text, each labelled with its
source service, heading, and a relevance score, ordered most-relevant first.

## Configuration

All settings come from environment variables (see `.env.example`):
`RAG_HOST`, `RAG_PORT`, `RAG_CHROMA_DIR`, `RAG_CORPUS_DIR`, `RAG_COLLECTION`.
