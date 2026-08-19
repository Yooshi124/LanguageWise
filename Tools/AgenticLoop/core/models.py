"""Pydantic schemas.

These double as the JSON schemas handed to Gemini for structured output, so the
model's replies are validated rather than parsed out of free text.
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
    items: list[PlanItem] = Field(default_factory=list)
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
