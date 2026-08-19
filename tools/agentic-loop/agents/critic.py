"""AGENT stage 4b: the Review Agent (critic).

Challenges the implementation agent's findings against the same evidence, so the
human only sees suggestions that survive scrutiny.
"""

from __future__ import annotations

from dataclasses import dataclass

from collectors.file_reader import CodeBundle
from collectors.repo_observer import Observation
from config.settings import Settings
from core.gemini_client import GeminiClient, ModelResponse, dump_json
from core.models import CritiqueResult, FindingSet
from core.prompt_registry import PromptRegistry


@dataclass
class CritiqueOutcome:
    result: CritiqueResult
    response: ModelResponse | None
    skipped_reason: str | None = None

    @property
    def was_run(self) -> bool:
        return self.skipped_reason is None


def critique(
    *,
    user_prompt: str,
    proposed: FindingSet,
    bundle: CodeBundle,
    observation: Observation,
    settings: Settings,
    prompts: PromptRegistry,
    client: GeminiClient,
) -> CritiqueOutcome:
    if not settings.enable_review_agent:
        return CritiqueOutcome(
            result=CritiqueResult(findings=proposed.findings, summary=proposed.summary),
            response=None,
            skipped_reason="Disabled via ENABLE_REVIEW_AGENT=false.",
        )

    if not proposed.findings:
        return CritiqueOutcome(
            result=CritiqueResult(findings=[], summary=proposed.summary),
            response=None,
            skipped_reason="The implementation agent raised no findings to critique.",
        )

    system = prompts.render("critique", "system")
    task = prompts.render(
        "critique",
        "task",
        user_prompt=user_prompt,
        observations=observation.as_text(),
        source_code=bundle.as_prompt_text(),
        findings=dump_json(proposed),
    )

    result, response = client.generate_structured(
        prompt=task,
        schema=CritiqueResult,
        system_instruction=system,
        model=settings.review_model,
    )
    return CritiqueOutcome(result=result, response=response)
