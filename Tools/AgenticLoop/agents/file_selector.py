"""ACT stage, part two: decide which files the review actually needs.

A manifest of paths is cheap to send; whole repositories are not. The model picks
the relevant files, and every path it returns is checked against the manifest so a
hallucinated path can never reach the reader.
"""

from __future__ import annotations

import logging
import re
from dataclasses import dataclass, field

from collectors.repo_scanner import ManifestEntry, ScanResult
from config.settings import Settings
from core.gemini_client import GeminiClient, GeminiError, ModelResponse
from core.models import FileSelection
from core.prompt_registry import PromptRegistry

logger = logging.getLogger(__name__)

MANIFEST_LINE_LIMIT = 2000
STOPWORDS = frozenset(
    {
        "the", "and", "for", "with", "that", "this", "from", "into", "look", "review",
        "check", "please", "code", "all", "any", "are", "our", "have", "has", "how",
        "what", "why", "over", "make", "sure", "can", "you", "its", "is", "in", "on",
        "of", "to", "a", "at", "be", "it", "as", "or", "if", "do", "does", "we",
    }
)


@dataclass
class SelectionResult:
    entries: list[ManifestEntry]
    reasons: dict[str, str]
    rationale: str
    fallback_used: bool = False
    dropped_paths: list[str] = field(default_factory=list)
    response: ModelResponse | None = None

    def as_text(self) -> str:
        if not self.entries:
            return "(no files selected)"
        return "\n".join(
            f"- {entry.relative_path} — {self.reasons.get(entry.relative_path, 'no reason given')}"
            for entry in self.entries
        )


def _keywords(prompt: str) -> list[str]:
    tokens = re.findall(r"[a-z0-9_]+", prompt.lower())
    return [token for token in tokens if len(token) > 2 and token not in STOPWORDS]


def _normalise_path(raw: str) -> str:
    """Normalise a model-supplied path to the manifest's form.

    `lstrip('./')` must not be used here: it also eats the leading dot of paths
    such as `.github/workflows/ci.yml`, which would drop them from the review.
    """
    candidate = raw.strip().replace("\\", "/")
    while candidate.startswith("./"):
        candidate = candidate[2:]
    return candidate.lstrip("/")


def heuristic_selection(scan: ScanResult, prompt: str, limit: int) -> list[ManifestEntry]:
    """Keyword fallback used when the selection call fails."""
    keywords = _keywords(prompt)
    if not keywords:
        return sorted(scan.entries, key=lambda e: e.size_bytes, reverse=True)[:limit]

    scored: list[tuple[int, ManifestEntry]] = []
    for entry in scan.entries:
        haystack = entry.relative_path.lower()
        score = sum(3 for keyword in keywords if keyword in haystack)
        if score == 0:
            try:
                body = entry.path.read_text(encoding="utf-8", errors="ignore").lower()
            except OSError:
                body = ""
            score = sum(1 for keyword in keywords if keyword in body)
        if score:
            scored.append((score, entry))

    scored.sort(key=lambda pair: (-pair[0], pair[1].relative_path))
    if scored:
        return [entry for _, entry in scored[:limit]]
    return sorted(scan.entries, key=lambda e: e.size_bytes, reverse=True)[:limit]


def select_files(
    *,
    scan: ScanResult,
    user_prompt: str,
    settings: Settings,
    prompts: PromptRegistry,
    client: GeminiClient,
) -> SelectionResult:
    """Ask Gemini which files matter, then validate its answer against the manifest."""
    if not scan.entries:
        return SelectionResult(entries=[], reasons={}, rationale="No files in scope.")

    limit = settings.max_files_in_context
    if len(scan.entries) <= limit:
        reasons = {
            entry.relative_path: "Whole scope fits within MAX_FILES_IN_CONTEXT."
            for entry in scan.entries
        }
        return SelectionResult(
            entries=list(scan.entries),
            reasons=reasons,
            rationale=(
                f"Scope contains {len(scan.entries)} file(s), at or below the "
                f"{limit}-file budget, so every file was included without a selection call."
            ),
        )

    system = prompts.render("selection", "system", max_files=limit)
    task = prompts.render(
        "selection",
        "task",
        user_prompt=user_prompt,
        scope=str(scan.scope),
        manifest=scan.manifest_text(limit=MANIFEST_LINE_LIMIT),
        max_files=limit,
    )

    try:
        selection, response = client.generate_structured(
            prompt=task,
            schema=FileSelection,
            system_instruction=system,
            model=settings.selection_model,
        )
    except GeminiError as exc:
        logger.warning("File selection call failed, using keyword fallback: %s", exc)
        entries = heuristic_selection(scan, user_prompt, limit)
        return SelectionResult(
            entries=entries,
            reasons={e.relative_path: "Chosen by keyword fallback." for e in entries},
            rationale=f"Selection model unavailable ({exc}); used local keyword matching.",
            fallback_used=True,
        )

    lookup = scan.by_relative_path()
    # Windows paths are case-insensitive, so a difference in case should not
    # silently drop a file the model legitimately asked for.
    lookup_ci = {path.lower(): entry for path, entry in lookup.items()}
    entries: list[ManifestEntry] = []
    reasons: dict[str, str] = {}
    dropped: list[str] = []

    for item in selection.files:
        candidate = _normalise_path(item.path)
        entry = lookup.get(candidate) or lookup_ci.get(candidate.lower())
        if entry is None:
            dropped.append(item.path)
            continue
        if entry.relative_path in reasons:
            continue
        entries.append(entry)
        reasons[entry.relative_path] = item.reason.strip() or "No reason given."
        if len(entries) >= limit:
            break

    if not entries:
        entries = heuristic_selection(scan, user_prompt, limit)
        reasons = {e.relative_path: "Chosen by keyword fallback." for e in entries}
        return SelectionResult(
            entries=entries,
            reasons=reasons,
            rationale=(
                "The selection model returned no usable paths"
                + (f" (dropped: {', '.join(dropped)})" if dropped else "")
                + "; used local keyword matching instead."
            ),
            fallback_used=True,
            dropped_paths=dropped,
            response=response,
        )

    return SelectionResult(
        entries=entries,
        reasons=reasons,
        rationale=selection.rationale.strip(),
        dropped_paths=dropped,
        response=response,
    )
