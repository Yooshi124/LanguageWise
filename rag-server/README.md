# RAG Server

A small, containerised retrieval-augmented-generation server for
LanguageWise. It indexes the service documentation corpus with
[ChromaDB](https://www.trychroma.com/) and exposes a single MCP tool,
`retrieve_context`, through [FastMCP](https://gofastmcp.com/) over HTTP.

It runs on the Compose network at `http://rag-server:8100`; host tools can reach
`http://localhost:8100` (loopback only). Shared Ask the docs and the central MCP
`docs_search` tool use its REST API. Chroma is embedded, so no separate vector
database server is required.

## Compose startup and indexing

From the repository root, use `docker compose up -d --build`. The RAG container
rebuilds and verifies its searchable index before starting HTTP on every start or
restart. Missing/empty corpus or failed ingestion prevents startup instead of
serving an empty index. `/health` becomes available only after this verification.

The first start downloads the CPU-local `all-MiniLM-L6-v2` embedding model and
therefore needs internet access, but no GPU or embedding API key. Named volumes
persist `/data` (Chroma and the generated technical key) and the embedding cache.
The image runs as a non-root user.

The corpus is bundled Markdown, not generated documentation. After editing it:

```powershell
docker compose up -d --build rag-server
```

Restarting an existing image reindexes its existing bundled corpus:

```powershell
docker compose restart rag-server
```

There is no need to delete volumes to reindex. The old host `.chroma` directory
is not imported; Compose builds a fresh index from the corpus.

## Layout

```
rag-server/
  server.py            # FastMCP HTTP server exposing retrieve_context
  ingest.py            # (re)builds the ChromaDB index from the corpus
  startup.py           # container startup: index, verify, then serve
  rag/
    config.py          # settings loaded from environment / .env
    store.py           # chunking, ingestion, and retrieval over ChromaDB
  corpus/              # README-esque markdown, one file per service
  requirements.txt
  .env.example
```

## Host-only development

Stop the Compose RAG service first to avoid a port conflict.

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
Compose network reach the containerised service at `http://rag-server:8100/mcp`.

## REST endpoints

| Endpoint | Returns | Used by | Auth |
| --- | --- | --- | --- |
| `POST /query` | General passages at or above the relevance floor | The shared backend (`POST /api/rag/answer`, Garry's "Ask the docs" on every feature) and the MCP `docs_search` tool | None |
| `POST /query/technical` | General and `TECHNICAL-` passages, unfiltered (each result has `"technical": true/false`) | The internal agentic loop | `X-LanguageWise-Rag-Key` header |

Both take `{ "query": "...", "n_results": 5 }` (1-20) and return
`{ "results": [{ source, heading, relevance, text }], "resultCount", "confidence" }`.

`confidence` comes from the best match's relevance (1 minus cosine distance):
`high` (0.45 or more), `medium` (0.35 or more), `low` (0.25 or more), otherwise
`insufficient`. `POST /query` drops passages below 0.25, so an `insufficient`
response has no results. The bands are in `rag/store.py` and were calibrated on
this corpus (off-topic questions scored up to about 0.20, on-topic ones from
about 0.30); re-check them after large corpus changes.

The technical key is read from `RAG_TECHNICAL_KEY_PATH` (default
`.rag-technical-key`, git-ignored) and generated on first start if missing. The
agentic loop reads the same file by default.

In Compose, the generated key is `/data/.rag-technical-key` in the persistent
`rag-data` volume, not the old host key file. Existing host clients must use that
key to call the technical endpoint; no agentic-loop configuration is changed.

## Marking internal content

Start a heading with `TECHNICAL-<topic>` (lowercase letters, digits, and dashes)
to mark that section, and every sub-heading under it, as internal:

```markdown
## TECHNICAL-data-model: Database tables
```

Technical sections are only returned by `POST /query/technical`. Never put
secrets such as passwords, keys, or tokens in the corpus, even in technical
sections. Rebuild/recreate the Compose RAG image after editing the corpus
(or run `python ingest.py` for host-only development).

## The `retrieve_context` tool

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `query_text` | string | — | Natural-language question or topic to search for. |
| `n_results` | int | `5` | Maximum number of passages to return. |

It returns a confidence category followed by the most relevant general
(non-`TECHNICAL-`) corpus passages as text, each labelled with its source
service, heading, and a relevance score, ordered most-relevant first. When no
passage clears the relevance floor it says the context is insufficient.

## Configuration

All settings come from environment variables (see `.env.example`):
`RAG_HOST`, `RAG_PORT`, `RAG_CHROMA_DIR`, `RAG_CORPUS_DIR`, `RAG_COLLECTION`,
`RAG_TECHNICAL_KEY_PATH`.

## Tests

From `rag-server`, run `python -m unittest discover -s tests -v`. The
`rag-server-build-and-test` GHA job runs these startup/boundary tests and builds
the image. The separate `ai-e2e` job exercises real embeddings, REST/MCP retrieval,
all five feature assistants and shared docs answers; see `tests\e2e\README.md`.
