"""Session and round state: identity, timing and accumulated totals."""

from __future__ import annotations

import uuid
from dataclasses import dataclass, field
from datetime import datetime, timezone
from pathlib import Path


def utc_now() -> datetime:
    return datetime.now(timezone.utc)


def format_timestamp(moment: datetime | None = None) -> str:
    return (moment or utc_now()).strftime("%Y-%m-%d %H:%M:%S UTC")


def new_guid() -> str:
    return str(uuid.uuid4())


@dataclass
class RoundState:
    number: int
    prompt: str
    started_at: datetime = field(default_factory=utc_now)
    plan_path: Path | None = None
    findings_count: int = 0
    accepted_count: int = 0
    tokens: int = 0

    def duration_seconds(self) -> float:
        return (utc_now() - self.started_at).total_seconds()


@dataclass
class SessionState:
    guid: str = field(default_factory=new_guid)
    started_at: datetime = field(default_factory=utc_now)
    rounds: list[RoundState] = field(default_factory=list)

    def start_round(self, prompt: str) -> RoundState:
        state = RoundState(number=len(self.rounds) + 1, prompt=prompt)
        self.rounds.append(state)
        return state

    @property
    def round_count(self) -> int:
        return len(self.rounds)

    @property
    def total_findings(self) -> int:
        return sum(round_.findings_count for round_ in self.rounds)

    @property
    def total_accepted(self) -> int:
        return sum(round_.accepted_count for round_ in self.rounds)

    @property
    def total_tokens(self) -> int:
        return sum(round_.tokens for round_ in self.rounds)

    @property
    def plans_created(self) -> list[Path]:
        return [round_.plan_path for round_ in self.rounds if round_.plan_path]
