"""AGENT stage 4b: the Review Agent (critic).

Runs on a separate, local model (Ollama + Gemma) so the human only sees
suggestions that survive scrutiny by a genuinely independent second model. This
pass is mandatory and cannot be disabled: if the local model is unreachable or
misbehaves, `OllamaError` propagates so the caller aborts the round rather than
silently showing unreviewed findings.
"""

from __future__ import annotations

import logging
from dataclasses import dataclass

from collectors.file_reader import CodeBundle
from collectors.repo_observer import Observation
from config.settings import Settings
from core.models import CritiqueResult, FindingSet
from core.ollama_client import OllamaClient
from core.openrouter_client import ModelResponse, dump_json
from core.prompt_registry import PromptRegistry

logger = logging.getLogger(__name__)

# Appended to the task when the first pass drops every proposed finding. Small
# local models over-fire on scepticism, so a wholesale rejection gets one
# mandatory re-check with an explicit keep-first bias before it is believed.
RECHECK_INSTRUCTION = """
RECHECK REQUIRED
Your first pass dropped every proposed finding. That outcome is rarely correct,
so re-examine each one with a keep-first bias:
- KEEP is the default. Drop only when the supplied source code actively
  contradicts the finding - not merely when it fails to prove it.
- Claims about missing call sites, endpoints or persistence are verified by
  absence: if you cannot find the call in the supplied code, the finding stands.
- When in doubt between AMEND and DROP, amend.
Return the corrected final list, keeping every finding that survives this check.
"""


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

    if not result.findings and proposed.findings:
        logger.info(
            "Review agent dropped all %s finding(s); running the mandatory re-check.",
            len(proposed.findings),
        )
        recheck, recheck_response = client.generate_structured(
            prompt=task + "\n" + RECHECK_INSTRUCTION,
            schema=CritiqueResult,
            system_instruction=system,
            model=settings.ollama_review_model,
        )
        if recheck_response:
            recheck_response.attempts += response.attempts
        if recheck.findings:
            result = recheck
            response = recheck_response
        else:
            # The re-check confirmed the wholesale rejection; keep the first
            # result but note the confirmation in the notes for the record.
            logger.info("Re-check confirmed dropping all findings.")

    return CritiqueOutcome(result=result, response=response)
