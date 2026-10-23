"""Multi-agent AGENT stage 4b: the Reviewer Agent.

Checks the worker's output against the plan and the evidence using the
separately configured review model, and recommends a decision to the human.
Provider errors propagate so unreviewed output is never presented for approval.
"""

from __future__ import annotations

from dataclasses import dataclass

from collectors.file_reader import CodeBundle
from collectors.repo_observer import Observation
from config.settings import Settings
from core.models import ReviewerAssessment, WorkerOutput, WorkflowPlan
from core.openrouter_client import ModelResponse, OpenRouterClient, dump_json
from core.prompt_registry import PromptRegistry


@dataclass
class ReviewerResult:
    assessment: ReviewerAssessment
    response: ModelResponse


def review_output(
    *,
    user_request: str,
    plan: WorkflowPlan,
    worker_output: WorkerOutput,
    bundle: CodeBundle,
    observation: Observation,
    settings: Settings,
    prompts: PromptRegistry,
    client: OpenRouterClient,
) -> ReviewerResult:
    system = prompts.render("reviewer", "system")
    task = prompts.render(
        "reviewer",
        "task",
        user_request=user_request,
        plan=dump_json(plan),
        worker_output=dump_json(worker_output),
        observations=observation.as_text(),
        source_code=bundle.as_prompt_text(),
    )

    assessment, response = client.generate_structured(
        prompt=task,
        schema=ReviewerAssessment,
        system_instruction=system,
        model=settings.review_model,
    )
    return ReviewerResult(assessment=assessment, response=response)
