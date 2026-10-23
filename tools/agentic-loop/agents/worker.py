"""Multi-agent AGENT stage 4a: the Worker Agent.

Executes the planner's workflow plan using only the selected code and the
deterministic observations, and flags any evidence it could not find.
"""

from __future__ import annotations

from dataclasses import dataclass

from collectors.file_reader import CodeBundle
from collectors.repo_observer import Observation
from config.settings import Settings
from core.models import WorkerOutput, WorkflowPlan
from core.openrouter_client import ModelResponse, OpenRouterClient, dump_json
from core.prompt_registry import PromptRegistry


@dataclass
class WorkerResult:
    output: WorkerOutput
    response: ModelResponse


def execute_plan(
    *,
    user_request: str,
    plan: WorkflowPlan,
    bundle: CodeBundle,
    observation: Observation,
    settings: Settings,
    prompts: PromptRegistry,
    client: OpenRouterClient,
) -> WorkerResult:
    system = prompts.render("worker", "system")
    task = prompts.render(
        "worker",
        "task",
        user_request=user_request,
        plan=dump_json(plan),
        observations=observation.as_text(),
        source_code=bundle.as_prompt_text(),
    )

    output, response = client.generate_structured(
        prompt=task,
        schema=WorkerOutput,
        system_instruction=system,
        model=settings.worker_model,
    )
    return WorkerResult(output=output, response=response)
