"""Multi-agent PLAN stage: the Planner Agent.

Turns the human's request into a short workflow plan (objective, steps,
evidence required, human approval) before any code is read, so the plan can
steer file selection in the ACT stage.
"""

from __future__ import annotations

from dataclasses import dataclass

from config.settings import Settings
from core.models import WorkflowPlan
from core.openrouter_client import ModelResponse, OpenRouterClient
from core.prompt_registry import PromptRegistry


@dataclass
class PlannerResult:
    plan: WorkflowPlan
    response: ModelResponse


def plan_workflow(
    *,
    user_request: str,
    settings: Settings,
    prompts: PromptRegistry,
    client: OpenRouterClient,
) -> PlannerResult:
    system = prompts.render("workflow_planner", "system")
    task = prompts.render(
        "workflow_planner",
        "task",
        user_request=user_request,
        scope=str(settings.scope),
    )

    plan, response = client.generate_structured(
        prompt=task,
        schema=WorkflowPlan,
        system_instruction=system,
        model=settings.model,
    )
    return PlannerResult(plan=plan, response=response)
