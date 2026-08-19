"""AGENT stage 4a: the Implementation Agent.

Reads the selected code plus the deterministic observations and proposes
evidence-backed findings.
"""

from __future__ import annotations

from dataclasses import dataclass

from collectors.file_reader import CodeBundle
from collectors.repo_observer import Observation
from config.settings import Settings
from core.gemini_client import GeminiClient, ModelResponse
from core.models import FindingSet
from core.prompt_registry import PromptRegistry

MAX_FINDINGS = 10


@dataclass
class AnalysisResult:
    findings: FindingSet
    response: ModelResponse
    prompt_used: str


def analyse(
    *,
    user_prompt: str,
    bundle: CodeBundle,
    observation: Observation,
    settings: Settings,
    prompts: PromptRegistry,
    client: GeminiClient,
    max_findings: int = MAX_FINDINGS,
) -> AnalysisResult:
    context = prompts.render(
        "analysis",
        "context",
        repo_root=str(settings.repo_root),
        scope=str(settings.scope),
        scope_mode="TARGETED_DIRECTORY" if settings.scope_is_targeted else "whole repository",
        observations=observation.as_text(),
        file_list=bundle.file_list_text(),
    )
    system = prompts.render("analysis", "system")
    task = prompts.render(
        "analysis",
        "task",
        context=context,
        source_code=bundle.as_prompt_text(),
        user_prompt=user_prompt,
        max_findings=max_findings,
    )

    findings, response = client.generate_structured(
        prompt=task,
        schema=FindingSet,
        system_instruction=system,
        model=settings.model,
    )
    findings.findings = findings.findings[:max_findings]
    return AnalysisResult(findings=findings, response=response, prompt_used=task)
