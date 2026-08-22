"""AGENT stage 4b: the Review Agent (critic).

Runs on a separate, local model (Ollama + Gemma) so the human only sees
suggestions that survive scrutiny by a genuinely independent second model. This
pass is mandatory and cannot be disabled: if the local model is unreachable or
misbehaves, `OllamaError` propagates so the caller aborts the round rather than
silently showing unreviewed findings.
"""

from __future__ import annotations

from dataclasses import dataclass

from collectors.file_reader import CodeBundle
from collectors.repo_observer import Observation
from config.settings import Settings
from core.gemini_client import ModelResponse, dump_json
from core.models import CritiqueResult, FindingSet
from core.ollama_client import OllamaClient
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
    client: OllamaClient,
) -> CritiqueOutcome:
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
        model=settings.ollama_review_model,
    )
    return CritiqueOutcome(result=result, response=response)
