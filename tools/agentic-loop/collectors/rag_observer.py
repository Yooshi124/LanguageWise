"""RAG collector: fetch retrieved documentation context for a topic.

Used by the loop's RAG validation mode (`python main.py --rag-validate` or the
`/rag-validate` REPL command). It queries the local RAG server for passages
relevant to the round's topic and formats them as evidence the analyst can
compare against the source code in scope.
"""

from __future__ import annotations

from dataclasses import dataclass, field

from core.rag_client import INSUFFICIENT_CONFIDENCE, RagClient, RagError, RagResult

DEFAULT_TOPIC = "how this service is expected to behave"


@dataclass
class RagReport:
    """Retrieved documentation passages distilled to the evidence the loop needs."""

    topic: str
    results: list[RagResult] = field(default_factory=list)
    confidence: str = INSUFFICIENT_CONFIDENCE

    @property
    def insufficient(self) -> bool:
        return self.confidence == INSUFFICIENT_CONFIDENCE or not self.results

    def summary_line(self) -> str:
        return (
            f"'{self.topic}' - {len(self.results)} passage(s) retrieved, "
            f"confidence: {self.confidence}"
        )

    def as_prompt_text(self) -> str:
        lines = [
            "RETRIEVED DOCUMENTATION CONTEXT (RAG server)",
            f"Topic: {self.topic}",
            f"Confidence: {self.confidence}",
            "",
        ]
        if self.insufficient:
            lines.append(
                "(insufficient context - no passage matches this topic closely enough. "
                "The documentation does not cover it, so do not report drift from "
                "documented behaviour based on the passages below, if any.)"
            )
            if not self.results:
                return "\n".join(lines)
            lines.append("")

        for rank, result in enumerate(self.results, start=1):
            lines.extend(
                [
                    f"[{rank}] source: {result.source} | heading: {result.heading} "
                    f"| relevance: {result.relevance:.3f}"
                    + (" | internal (TECHNICAL)" if result.technical else ""),
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
    return RagReport(topic=resolved_topic, results=result.results, confidence=result.confidence)


__all__ = ["RagReport", "fetch_rag_report", "RagError"]
