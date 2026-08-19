"""HUMAN REVIEW stage: turn a free-text reply into accepted finding indices.

Local parsing handles the common shapes ("1 and 2", "1,3", "1-3", "all", "none")
without an API call. Anything else falls back to a small structured Gemini call so
the human can answer however feels natural.
"""

from __future__ import annotations

import logging
import re
from dataclasses import dataclass

from core.gemini_client import GeminiClient, GeminiError, ModelResponse, dump_json
from core.models import Decision, Finding
from core.prompt_registry import PromptRegistry

logger = logging.getLogger(__name__)

ACCEPT_ALL_WORDS = frozenset(
    {"all", "everything", "every", "yes", "y", "accept all", "all of them", "both"}
)
REJECT_ALL_WORDS = frozenset(
    {"none", "no", "n", "nothing", "skip", "reject", "reject all", "neither", "cancel"}
)
EXCEPT_PATTERN = re.compile(r"\b(?:all|everything)\b.*\b(?:except|but|apart from)\b", re.IGNORECASE)
# Rejection phrasing next to numbers means the numbers are *not* acceptances.
NEGATION_PATTERN = re.compile(
    r"\b(?:reject|rejects|rejected|rejecting|do not|don t|dont|does not|doesn t|not|"
    r"skip|ignore|ignoring|drop|exclude|excluding|omit|without|never|decline|refuse|"
    r"leave out|nothing)\b",
    re.IGNORECASE,
)
RANGE_PATTERN = re.compile(r"\b(\d+)\s*(?:-|–|—|to|through|thru)\s*(\d+)\b")
NUMBER_PATTERN = re.compile(r"\d+")


@dataclass
class DecisionResult:
    accepted: list[int]
    interpretation: str
    source: str
    response: ModelResponse | None = None
    error: str | None = None

    def rejected(self, total: int) -> list[int]:
        return [index for index in range(1, total + 1) if index not in self.accepted]


def _normalise(indices: list[int], total: int) -> list[int]:
    return sorted({index for index in indices if 1 <= index <= total})


def parse_locally(reply: str, total: int) -> list[int] | None:
    """Return accepted indices, or None when the reply needs model interpretation."""
    text = reply.strip().lower()
    if not text:
        return []

    condensed = re.sub(r"[^a-z0-9\s,\-–—]", " ", text)
    condensed = re.sub(r"\s+", " ", condensed).strip()

    if condensed in REJECT_ALL_WORDS:
        return []
    if condensed in ACCEPT_ALL_WORDS:
        return list(range(1, total + 1))

    is_exception = bool(EXCEPT_PATTERN.search(condensed))
    is_negated = bool(NEGATION_PATTERN.search(condensed))

    numbers: list[int] = []
    out_of_range = False
    for match in RANGE_PATTERN.finditer(condensed):
        start, end = int(match.group(1)), int(match.group(2))
        if start > end or start < 1 or end > total:
            out_of_range = True
        if start <= end and start <= total:
            numbers.extend(range(max(start, 1), min(end, total) + 1))

    # Strip range tokens with the pattern itself. A plain str.replace would also
    # rewrite them where they appear inside other numbers ("1-2" inside "11-20"),
    # leaving digit fragments that look like extra selections.
    remainder = RANGE_PATTERN.sub(" ", condensed)
    for value in NUMBER_PATTERN.findall(remainder):
        number = int(value)
        if not 1 <= number <= total:
            out_of_range = True
        numbers.append(number)

    if not numbers:
        words = condensed.split()
        if any(word in words for word in REJECT_ALL_WORDS):
            return []
        if any(word in words for word in ACCEPT_ALL_WORDS) and not is_exception and not is_negated:
            return list(range(1, total + 1))
        return None

    # "reject 1 and 2" or "fix 1 but not 2" must never be read as plain acceptance:
    # hand anything with rejection phrasing to the model rather than guessing.
    if is_negated and not is_exception:
        return None

    selected = _normalise(numbers, total)
    if is_exception:
        # A bad index in an exclusion ("all except 99") would silently accept
        # everything, so treat it as ambiguous instead.
        if out_of_range:
            return None
        return [index for index in range(1, total + 1) if index not in selected]
    if not selected:
        # Numbers were present but all out of range — let the model try.
        return None
    return selected


def _describe(accepted: list[int], total: int) -> str:
    if not accepted:
        return "No suggestions accepted."
    if len(accepted) == total:
        return f"Accepted all {total} suggestion(s)."
    listed = ", ".join(str(index) for index in accepted)
    return f"Accepted suggestion(s) {listed} of {total}."


def parse_decision(
    *,
    reply: str,
    findings: list[Finding],
    prompts: PromptRegistry,
    client: GeminiClient | None,
) -> DecisionResult:
    total = len(findings)
    local = parse_locally(reply, total)
    if local is not None:
        return DecisionResult(
            accepted=local,
            interpretation=_describe(local, total),
            source="local parser",
        )

    if client is None:
        return DecisionResult(
            accepted=[],
            interpretation="Reply could not be interpreted and no model was available.",
            source="local parser",
            error="ambiguous reply",
        )

    numbered = "\n".join(
        f"{index}. {finding.title} — {finding.problem}"
        for index, finding in enumerate(findings, start=1)
    )
    try:
        decision, response = client.generate_structured(
            prompt=prompts.render(
                "decision", "task", findings=numbered or dump_json([]), user_reply=reply
            ),
            schema=Decision,
            system_instruction=prompts.render("decision", "system", max_index=total),
        )
    except GeminiError as exc:
        logger.warning("Decision interpretation failed: %s", exc)
        return DecisionResult(
            accepted=[],
            interpretation="Could not interpret the reply; nothing was accepted.",
            source="model (failed)",
            error=str(exc),
        )

    accepted = _normalise(decision.accepted, total)
    interpretation = decision.interpretation.strip()
    if sorted(set(decision.accepted)) != accepted or not interpretation:
        # The model named indices that do not exist; describe what will actually
        # happen rather than repeating a claim the plan will not honour.
        interpretation = _describe(accepted, total)
    return DecisionResult(
        accepted=accepted,
        interpretation=interpretation,
        source="model",
        response=response,
    )
