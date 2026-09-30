"""FastMCP RAG server exposing the `retrieve_context` tool over HTTP.

Non-containerised by design: it binds to the host so Docker containers can reach
it through host.docker.internal:RAG_PORT. Run from the rag-server directory:

    python server.py

The MCP endpoint is served at http://<host>:<port>/mcp. A plain REST endpoint at
POST /query is also exposed for callers without an MCP client (the agentic loop,
feature backends); it shares the same retrieval function as the MCP tool.
"""

from __future__ import annotations

from fastmcp import FastMCP
from starlette.requests import Request
from starlette.responses import JSONResponse

from rag.config import load_settings
from rag.store import query

settings = load_settings()

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
    """Retrieve the most relevant documentation passages for a question.

    Args:
        query_text: A natural-language question or topic to search the corpus for.
        n_results: Maximum number of passages to return (default 5).

    Returns:
        The matching passages formatted as text, each labelled with its source
        service and heading, ordered from most to least relevant.
    """
    chunks = query(settings, query_text, n_results=n_results)
    if not chunks:
        return (
            "No matching context found. The index may be empty — run "
            "`python ingest.py` to build it from the corpus."
        )

    blocks: list[str] = []
    for rank, chunk in enumerate(chunks, start=1):
        blocks.append(
            f"[{rank}] source: {chunk.source} | heading: {chunk.heading} "
            f"| relevance: {1 - chunk.distance:.3f}\n{chunk.text}"
        )
    return "\n\n---\n\n".join(blocks)


@mcp.custom_route("/health", methods=["GET"])
async def health(_request: Request) -> JSONResponse:
    return JSONResponse({"status": "ok"})


@mcp.custom_route("/query", methods=["POST"])
async def query_route(request: Request) -> JSONResponse:
    """Plain HTTP/JSON equivalent of the retrieve_context tool, for callers with no MCP client."""
    try:
        body = await request.json()
    except ValueError:
        return JSONResponse({"error": "Request body must be JSON."}, status_code=400)

    query_text = str(body.get("query") or "").strip()
    if not query_text:
        return JSONResponse({"error": "'query' is required."}, status_code=400)
    n_results = int(body.get("n_results") or 5)

    chunks = query(settings, query_text, n_results=n_results)
    results = [
        {
            "source": chunk.source,
            "heading": chunk.heading,
            "relevance": round(1 - chunk.distance, 3),
            "text": chunk.text,
        }
        for chunk in chunks
    ]
    return JSONResponse({"results": results, "resultCount": len(results)})


if __name__ == "__main__":
    mcp.run(transport="http", host=settings.host, port=settings.port)
