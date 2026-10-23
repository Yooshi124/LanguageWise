"""Pydantic schemas.

These double as the JSON contracts the models are held to for structured output,
so replies are validated rather than parsed out of free text.
"""

from __future__ import annotations

from enum import Enum

from pydantic import BaseModel, Field

# Free-text fields are capped in the *schema sent to the model* (not validated
# locally) because an unbounded string invites the model to ramble there until it
# exhausts its output budget and never emits the findings array at all.
SUMMARY_MAX_CHARS = 400
RATIONALE_MAX_CHARS = 300


class Severity(str, Enum):
    HIGH = "high"
    MEDIUM = "medium"
    LOW = "low"


class SelectedFile(BaseModel):
    path: str = Field(description="Repository-relative path, copied exactly from the manifest.")
    reason: str = Field(description="One sentence on why this file matters for the request.")


class FileSelection(BaseModel):
    files: list[SelectedFile] = Field(
        default_factory=list,
        description="Files worth reading in full, most relevant first.",
    )
    rationale: str = Field(
        default="",
        description="Short explanation of the overall selection strategy, one sentence.",
        json_schema_extra={"maxLength": RATIONALE_MAX_CHARS},
    )


class Finding(BaseModel):
    title: str = Field(description="Short label for the issue.")
    problem: str = Field(description="What is wrong, stated concretely.")
    suggested_fix: str = Field(description="The specific change that would resolve it.")
    severity: Severity = Field(default=Severity.MEDIUM)
    files: list[str] = Field(
        default_factory=list,
        description="Repository-relative paths this finding applies to.",
    )
    evidence: str = Field(
        default="",
        description="The code or observation that proves the problem is real.",
    )


class FindingSet(BaseModel):
    findings: list[Finding] = Field(default_factory=list)
    summary: str = Field(
        default="",
        description=(
            "At most two sentences on the state of the reviewed code. "
            "Never put reasoning, working notes or commentary here."
        ),
        json_schema_extra={"maxLength": SUMMARY_MAX_CHARS},
    )


class CritiqueNote(BaseModel):
    finding_title: str
    verdict: str = Field(description="kept, amended, or dropped.")
    reason: str = Field(default="")


class CritiqueResult(BaseModel):
    findings: list[Finding] = Field(
        default_factory=list,
        description="The final, evidence-backed findings after critique.",
    )
    notes: list[CritiqueNote] = Field(
        default_factory=list,
        description="What the critic did to each original finding.",
    )
    summary: str = Field(
        default="",
        description=(
            "At most two sentences on the final state of the findings. "
            "Never put reasoning, working notes or commentary here."
        ),
        json_schema_extra={"maxLength": SUMMARY_MAX_CHARS},
    )


class PlanStep(BaseModel):
    order: int = Field(description="1-based step number.")
    action: str = Field(description="What to do, imperative and specific.")
    details: str = Field(default="", description="How to do it, including edge cases.")
    files: list[str] = Field(default_factory=list)


class PlanItem(BaseModel):
    finding_title: str
    problem: str
    goal: str = Field(description="The outcome once this item is implemented.")
    steps: list[PlanStep] = Field(default_factory=list)
    files_to_change: list[str] = Field(default_factory=list)
    tests: list[str] = Field(
        default_factory=list,
        description="Tests to add or update, named concretely.",
    )
    risks: list[str] = Field(default_factory=list)
    acceptance_criteria: list[str] = Field(default_factory=list)


class ImplementationPlan(BaseModel):
    title: str = Field(default="Implementation Plan")
    # Required: a plan is only built for >=1 accepted finding, so empty means wrong keys.
    items: list[PlanItem] = Field(min_length=1)
    summary: str = Field(
        default="",
        description=(
            "At most three sentences describing the plan as a whole. "
            "Never put reasoning or working notes here."
        ),
        json_schema_extra={"maxLength": SUMMARY_MAX_CHARS + 200},
    )
    suggested_order: list[str] = Field(
        default_factory=list,
        description="Finding titles in the order they should be tackled.",
    )
    open_questions: list[str] = Field(default_factory=list)


class Decision(BaseModel):
    """The parsed outcome of the human review stage."""

    accepted: list[int] = Field(
        default_factory=list,
        description="1-based indices of accepted findings.",
    )
    interpretation: str = Field(
        default="",
        description="One sentence restating what the human chose.",
        json_schema_extra={"maxLength": RATIONALE_MAX_CHARS},
    )


class Recommendation(str, Enum):
    ACCEPT = "accept"
    PARTIALLY_ACCEPT = "partially_accept"
    REJECT = "reject"


class WorkflowDecision(str, Enum):
    """The human's final call on a multi-agent workflow."""

    ACCEPT = "Accept"
    PARTIALLY_ACCEPT = "Partially Accept"
    REJECT = "Reject"
    PENDING = "Pending"

    @classmethod
    def parse(cls, reply: str) -> tuple["WorkflowDecision | None", str]:
        """Split a reply such as 'partial - needs a retest' into (decision, note)."""
        word, _, note = reply.strip().partition(" ")
        aliases = {
            "a": cls.ACCEPT,
            "accept": cls.ACCEPT,
            "p": cls.PARTIALLY_ACCEPT,
            "partial": cls.PARTIALLY_ACCEPT,
            "partially": cls.PARTIALLY_ACCEPT,
            "r": cls.REJECT,
            "reject": cls.REJECT,
        }
        decision = aliases.get(word.lower().rstrip(":,.-"))
        if decision is cls.PARTIALLY_ACCEPT and note.lower().startswith("accept"):
            note = note[len("accept"):]
        return decision, note.strip(" :,.-")


class WorkflowPlan(BaseModel):
    objective: str = Field(
        description="One sentence restating what the workflow must deliver.",
        json_schema_extra={"maxLength": RATIONALE_MAX_CHARS},
    )
    steps: list[str] = Field(
        min_length=1,
        description="Three to five ordered, concrete steps for the worker agent.",
    )
    evidence_required: list[str] = Field(
        default_factory=list,
        description="Files, components or facts the worker must ground its output in.",
    )
    human_approval: str = Field(
        default="",
        description="What the human must confirm before the output can be trusted.",
        json_schema_extra={"maxLength": RATIONALE_MAX_CHARS},
    )


class WorkerOutput(BaseModel):
    output: str = Field(description="The deliverable, produced only from supplied evidence.")
    evidence_used: list[str] = Field(
        default_factory=list,
        description="Repository-relative paths or observation lines the output relies on.",
    )
    limitations: list[str] = Field(
        default_factory=list,
        description="Missing evidence, assumptions avoided, or plan steps not completed.",
    )


class ReviewerAssessment(BaseModel):
    risk: Severity = Field(description="Overall risk of trusting the worker output as-is.")
    risk_summary: str = Field(
        description="One or two sentences naming the main risk.",
        json_schema_extra={"maxLength": SUMMARY_MAX_CHARS},
    )
    corrections: list[str] = Field(
        default_factory=list,
        description="Specific corrections the output needs, each citing the evidence.",
    )
    retest: list[str] = Field(
        default_factory=list,
        description="Checks a human should run to confirm the output.",
    )
    recommendation: Recommendation = Field(
        description="Advice to the human reviewer: accept, partially_accept or reject.",
    )
