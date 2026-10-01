"""Thin HTTP client for the local RAG server's technical REST endpoint.

The RAG server (`rag-server/`) runs locally and non-containerised. The loop uses
`POST /query/technical`, which also returns TECHNICAL- (service internals)
passages and requires the shared technical key.
"""

from __future__ import annotations

from dataclasses import dataclass, field

import requests

from config.settings import Settings


class RagError(RuntimeError):
    """Raised when the local RAG server is unreachable or returns an error."""


@dataclass(frozen=True)
class RagResult:
    source: str
    heading: str
    relevance: float
    text: str
    technical: bool = False


INSUFFICIENT_CONFIDENCE = "insufficient"


@dataclass(frozen=True)
class RagQueryResult:
    query: str
    results: list[RagResult] = field(default_factory=list)
    # high, medium, low, or insufficient, from the RAG server's best match.
    confidence: str = INSUFFICIENT_CONFIDENCE


class RagClient:
    """Calls the RAG server's `POST /query/technical` endpoint."""

    def __init__(self, settings: Settings, session: "requests.Session | None" = None) -> None:
        self._settings = settings
        self._session = session or requests.Session()

    def query(self, text: str, n_results: int | None = None) -> RagQueryResult:
        if not self._settings.rag_technical_key:
            raise RagError(
                "No technical RAG key found. Start the RAG server once to generate "
                "rag-server/.rag-technical-key, or set RAG_TECHNICAL_KEY."
            )
        url = f"{self._settings.rag_base_url.rstrip('/')}/query/technical"
        body = {"query": text, "n_results": n_results or self._settings.rag_default_n_results}

        try:
            resp = self._session.post(
                url,
                json=body,
                headers={"X-LanguageWise-Rag-Key": self._settings.rag_technical_key},
                timeout=self._settings.rag_request_timeout_seconds,
            )
        except requests.exceptions.ConnectionError as exc:
            raise RagError(
                f"Could not reach the RAG server at {self._settings.rag_base_url}. "
                "It runs locally and is not containerised - start it from the "
                "rag-server directory with `python server.py`."
            ) from exc
        except requests.exceptions.Timeout as exc:
            raise RagError(
                f"The RAG server at {self._settings.rag_base_url} did not respond "
                f"within {self._settings.rag_request_timeout_seconds}s."
            ) from exc
        except requests.exceptions.RequestException as exc:
            raise RagError(f"RAG server request failed: {exc}") from exc

        if resp.status_code == 401:
            raise RagError(
                "The RAG server rejected the technical key. Make sure RAG_TECHNICAL_KEY "
                "or RAG_TECHNICAL_KEY_PATH matches rag-server/.rag-technical-key."
            )
        if not resp.ok:
            detail = resp.text[:500]
            raise RagError(f"RAG server request failed with HTTP {resp.status_code}: {detail}")

        try:
            payload = resp.json()
        except ValueError as exc:
            raise RagError(f"RAG server returned a non-JSON response: {resp.text[:500]}") from exc

        results = [
            RagResult(
                source=str(item.get("source", "")),
                heading=str(item.get("heading", "")),
                relevance=float(item.get("relevance", 0.0)),
                text=str(item.get("text", "")),
                technical=item.get("technical") is True,
            )
            for item in payload.get("results", [])
        ]
        confidence = str(payload.get("confidence") or INSUFFICIENT_CONFIDENCE)
        return RagQueryResult(query=text, results=results, confidence=confidence)
