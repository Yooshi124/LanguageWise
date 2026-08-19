"""ADAPT stage: turn accepted findings into a detailed implementation plan."""

from __future__ import annotations

from dataclasses import dataclass

from collectors.file_reader import CodeBundle
from collectors.repo_observer import Observation
from config.settings import Settings
from core.gemini_client import GeminiClient, ModelResponse, dump_json
from core.models import Finding, ImplementationPlan
from core.prompt_registry import PromptRegistry


@dataclass
class PlanResult:
    plan: ImplementationPlan
    response: ModelResponse


def build_plan(
    *,
    user_prompt: str,
    accepted: list[Finding],
    bundle: CodeBundle,
    observation: Observation,
    settings: Settings,
    prompts: PromptRegistry,
    client: GeminiClient,
) -> PlanResult:
    if not accepted:
        raise ValueError("build_plan requires at least one accepted finding.")

    system = prompts.render("planning", "system")
    task = prompts.render(
        "planning",
        "task",
        user_prompt=user_prompt,
        scope=str(settings.scope),
        observations=observation.as_text(),
        source_code=bundle.as_prompt_text(),
        accepted_findings=dump_json([finding.model_dump(mode="json") for finding in accepted]),
    )

    plan, response = client.generate_structured(
        prompt=task,
        schema=ImplementationPlan,
        system_instruction=system,
        model=settings.model,
    )
    return PlanResult(plan=plan, response=response)
