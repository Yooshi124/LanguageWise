"""RAG collector: fetch retrieved documentation context for a topic.

Used by the loop's RAG validation mode (`python main.py --rag-validate` or the
`/rag-validate` REPL command). It queries the local RAG server for passages
relevant to the round's topic and formats them as evidence the analyst can
compare against the source code in scope.
"""

from __future__ import annotations

from dataclasses import dataclass, field

from core.rag_client import RagClient, RagError, RagResult

DEFAULT_TOPIC = "how this service is expected to behave"


@dataclass
class RagReport:
    """Retrieved documentation passages distilled to the evidence the loop needs."""

    topic: str
    results: list[RagResult] = field(default_factory=list)

    def summary_line(self) -> str:
        return f"'{self.topic}' - {len(self.results)} passage(s) retrieved"

    def as_prompt_text(self) -> str:
        lines = [
            "RETRIEVED DOCUMENTATION CONTEXT (RAG server)",
            f"Topic: {self.topic}",
            "",
        ]
        if not self.results:
            lines.append(
                "(no matching passages were found - the RAG index may be empty "
                "or the topic may not be covered by the corpus)"
            )
            return "\n".join(lines)

        for rank, result in enumerate(self.results, start=1):
            lines.extend(
                [
                    f"[{rank}] source: {result.source} | heading: {result.heading} "
                    f"| relevance: {result.relevance:.3f}",
                    result.text,
                    "",
                ]
            )
        return "\n".join(lines)


def fetch_rag_report(client: RagClient, topic: str | None, n_results: int | None = None) -> RagReport:
    """Query the RAG server for `topic` and return the result as a `RagReport`.

    Raises `RagError` (propagated from `RagClient.query`) if the server cannot
    be reached or returns an error.
    """
    resolved_topic = topic or DEFAULT_TOPIC
    result = client.query(resolved_topic, n_results=n_results)
    return RagReport(topic=resolved_topic, results=result.results)


__all__ = ["RagReport", "fetch_rag_report", "RagError"]
