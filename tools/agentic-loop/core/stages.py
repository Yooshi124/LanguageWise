"""Canonical agentic loop stages.

This module is the single source of truth for the loop's structure. Both the
console renderer and the evidence-log writer import from here, so what you see
on screen and what lands in the session markdown can never drift apart.
"""

from __future__ import annotations

from dataclasses import dataclass


@dataclass(frozen=True)
class Stage:
    number: int
    name: str
    description: str

    @property
    def banner(self) -> str:
        return f"STAGE {self.number}/{TOTAL_STAGES} - {self.name}"

    @property
    def heading(self) -> str:
        """Markdown heading used inside a round of the evidence log."""
        return f"### {self.number}. {self.name}"


PLAN = Stage(1, "PLAN", "The human states a targeted review goal.")
ACT = Stage(2, "ACT", "Scan the scope and gather the relevant source files.")
OBSERVE = Stage(3, "OBSERVE", "Collect deterministic, verifiable evidence about the code.")
AGENT = Stage(4, "AGENT", "Implementation agent proposes findings; review agent critiques them.")
HUMAN_REVIEW = Stage(5, "HUMAN REVIEW", "The human accepts or rejects each suggestion.")
ADAPT = Stage(6, "ADAPT", "Record the decision and save an implementation plan.")

STAGES: tuple[Stage, ...] = (PLAN, ACT, OBSERVE, AGENT, HUMAN_REVIEW, ADAPT)
TOTAL_STAGES = len(STAGES)

STAGE_NAMES: tuple[str, ...] = tuple(stage.name for stage in STAGES)
