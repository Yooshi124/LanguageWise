"""FastMCP RAG server exposing the `retrieve_context` tool over HTTP.

Non-containerised by design: it binds to the host so Docker containers can reach
it through host.docker.internal:RAG_PORT. Run from the rag-server directory:

    python server.py

The MCP endpoint is served at http://<host>:<port>/mcp. Plain REST endpoints are
also exposed for callers without an MCP client:

- POST /query            General documentation only, without passages below the
                         relevance floor. Used by the shared backend and the MCP
                         docs_search tool. No key required.
- POST /query/technical  General plus TECHNICAL- sections (service internals), all
                         passages. For the internal agentic loop; requires the
                         X-LanguageWise-Rag-Key header to match the key in
                         RAG_TECHNICAL_KEY_PATH, which is generated on first start.

Both return a confidence category (high, medium, low, insufficient) from the best match.

The MCP retrieve_context tool is general-only.
"""

from __future__ import annotations

import hmac
import os
import secrets
from pathlib import Path

from fastmcp import FastMCP
from starlette.requests import Request
from starlette.responses import JSONResponse

from rag.config import load_settings
from rag.store import INSUFFICIENT, RetrievedChunk, confidence_for, query, relevance, supported

TECHNICAL_KEY_HEADER = "X-LanguageWise-Rag-Key"
MAX_RESULTS = 20

settings = load_settings()


def _load_or_create_technical_key(path: Path) -> str:
    if path.is_file():
        key = path.read_text(encoding="utf-8").strip()
        if key:
            return key
    key = secrets.token_hex(32)
    path.write_text(key, encoding="utf-8")
    try:
        os.chmod(path, 0o600)
    except OSError:
        pass  # Not supported on every platform; the file is git-ignored either way.
    print(f"Generated the technical RAG key at {path}")
    return key


technical_key = _load_or_create_technical_key(settings.technical_key_path)

mcp = FastMCP(
    name="languagewise-rag",
    instructions=(
        "Retrieval-augmented context over LanguageWise service documentation. "
        "Call retrieve_context with a natural-language question to fetch the most "
        "relevant passages describing how users and agents use each service."
    ),
)


@mcp.tool
def retrieve_context(query_text: str, n_results: int = 5) -> str:
    """Retrieve the most relevant general documentation passages for a question.

    Args:
        query_text: A natural-language question or topic to search the corpus for.
        n_results: Maximum number of passages to return (default 5).

    Returns:
        A confidence category, then the matching passages formatted as text, each
        labelled with its source service and heading, ordered from most to least
        relevant. Says the context is insufficient when nothing matches closely.
    """
    chunks = query(settings, query_text, n_results=_clamp(n_results))
    confidence = confidence_for(chunks)
    chunks = supported(chunks)
    if confidence == INSUFFICIENT or not chunks:
        return (
            "Confidence: insufficient. No LanguageWise documentation matches this question "
            "closely enough to answer it."
        )

    blocks: list[str] = [f"Confidence: {confidence}"]
    for rank, chunk in enumerate(chunks, start=1):
        blocks.append(
            f"[{rank}] source: {chunk.source} | heading: {chunk.heading} "
            f"| relevance: {relevance(chunk):.3f}\n{chunk.text}"
        )
    return "\n\n---\n\n".join(blocks)


@mcp.custom_route("/health", methods=["GET"])
async def health(_request: Request) -> JSONResponse:
    return JSONResponse({"status": "ok"})


@mcp.custom_route("/query", methods=["POST"])
async def query_route(request: Request) -> JSONResponse:
    """General documentation search, safe to show to learners."""
    return await _query_response(request, include_technical=False)


@mcp.custom_route("/query/technical", methods=["POST"])
async def technical_query_route(request: Request) -> JSONResponse:
    """General and TECHNICAL- documentation search for the internal agentic loop."""
    supplied = request.headers.get(TECHNICAL_KEY_HEADER, "")
    if not hmac.compare_digest(supplied.encode(), technical_key.encode()):
        return JSONResponse({"error": "A valid technical RAG key is required."}, status_code=401)
    return await _query_response(request, include_technical=True)


async def _query_response(request: Request, include_technical: bool) -> JSONResponse:
    try:
        body = await request.json()
    except ValueError:
        return JSONResponse({"error": "Request body must be JSON."}, status_code=400)
    if not isinstance(body, dict):
        return JSONResponse({"error": "Request body must be a JSON object."}, status_code=400)

    query_text = str(body.get("query") or "").strip()
    if not query_text:
        return JSONResponse({"error": "'query' is required."}, status_code=400)
    try:
        n_results = _clamp(int(body.get("n_results") or 5))
    except (TypeError, ValueError):
        return JSONResponse({"error": "'n_results' must be a whole number."}, status_code=400)

    chunks = query(settings, query_text, n_results=n_results, include_technical=include_technical)
    confidence = confidence_for(chunks)
    if not include_technical:
        chunks = supported(chunks)
    results = [_result(chunk, include_technical) for chunk in chunks]
    return JSONResponse({"results": results, "resultCount": len(results), "confidence": confidence})


def _result(chunk: RetrievedChunk, include_technical: bool) -> dict[str, object]:
    result: dict[str, object] = {
        "source": chunk.source,
        "heading": chunk.heading,
        "relevance": round(relevance(chunk), 3),
        "text": chunk.text,
    }
    if include_technical:
        result["technical"] = chunk.technical
    return result


def _clamp(n_results: int) -> int:
    return max(1, min(n_results, MAX_RESULTS))


if __name__ == "__main__":
    mcp.run(transport="http", host=settings.host, port=settings.port)
